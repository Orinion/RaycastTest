using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System;
using TMPro;

public class AutoRayCast : MonoBehaviour
{
    public enum Resolution : int
    {
        single = 1,
        low = 10,
        mid = 50,
        high = 200
    }
    public enum ScanSize : int
    {
        small = 5,
        mid = 10,
        high = 45
    }
    private Resolution resolution = Resolution.single;
    private ScanSize distance = ScanSize.small;

    [SerializeField]
    private EnvironmentDepthAccess depthAccess;
    [SerializeField]
    Transform hand;
    [SerializeField]
    Camera cameraT;
    [SerializeField]
    GameObject previewPrefab;
    GameObject preview = null;
    [SerializeField]
    TMP_Text textf;

    float last = 0f;
    private Vector3 goal => hand.position + hand.forward * 0.1f;

    private Vector3 WorldTo2d(Vector3 pos) => cameraT.WorldToViewportPoint(pos, Camera.MonoOrStereoscopicEye.Left);

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One))
            resolution = resolution.Next();

        if (OVRInput.GetDown(OVRInput.Button.Two))
            distance = distance.Next();

        if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger) 
            && preview == null) {
                preview = Instantiate(previewPrefab, transform);
        }
        if (preview)
            preview.transform.localPosition = goal;
        if(OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger))
        {
            if (resolution == Resolution.single)
                CreateSingleScan();
            else
                CreateScan();
        }

        textf.text = GetInfoText();
    }

    private void CreateScan()
    {
        // Raycasting at the controller anchor's position
        var scanCenter = goal;
        List<Vector2> coords = new List<Vector2>();
        int pixel = (int)resolution;
        float diameter = ((int)distance) / 100f; // in cm
        Vector3 up = cameraT.transform.up;
        Vector3 right = cameraT.transform.right;

        float radius = diameter / 2f;
        float rPerPixel = diameter / pixel;

        // create list of viewspace vectors
        for (int y = 0; y < pixel; y++)
            for (int x = 0; x < pixel; x++)
            {
                var xdiff = up * (x * rPerPixel - radius);
                var ydiff = right * (y * rPerPixel - radius);
                var wp = scanCenter + xdiff + ydiff;
                coords.Add(WorldTo2d(wp));
            }

        // Perform ray casts
        depthAccess.RaycastViewSpaceBlocking(coords, out List<Vector3> results);

        var vs = results.Select(p => {
            Debug.Log(p);
            if (float.IsNaN(p.x) || float.IsInfinity(p.x)) return null;
            return Instantiate(preview, p, transform.rotation);
        });

        Destroy(preview);
        preview = null;
        Debug.Log(GetInfoText());
    }

    private void CreateSingleScan()
    {
        // Raycasting at the controller anchor's position
        var worldCenter = goal;
        // to viewspace vector
        var viewSpaceCoordinate = WorldTo2d(worldCenter);
        // Perform ray cast
        var r = depthAccess.RaycastViewSpaceBlocking(viewSpaceCoordinate);
        // relative

        last = r.x;

        Destroy(preview);
        preview = null;
        Debug.Log(GetInfoText());
    }

    public string GetInfoText()
    {
        return "Scan " + " Res: " + resolution.ToString() + " Size: " + ((int)distance) + "cm" + "\n" + last.ToString();
    }

}

public static class Extensions
{

    public static T Next<T>(this T src) where T : struct
    {
        if (!typeof(T).IsEnum) throw new ArgumentException(String.Format("Argument {0} is not an Enum", typeof(T).FullName));

        T[] Arr = (T[])Enum.GetValues(src.GetType());
        int j = Array.IndexOf<T>(Arr, src) + 1;
        return (Arr.Length == j) ? Arr[0] : Arr[j];
    }
}