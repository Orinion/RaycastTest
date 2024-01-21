using System.Collections.Generic;
using System.Linq;
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
        high = 100
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
    TMP_Text textf;
    [SerializeField]
    EnvironmentDepthAccess depthAccess;
    [SerializeField]
    Transform hand;
    [SerializeField]
    Camera cameraT;

    [SerializeField]
    Material NoOcclusion;

    [SerializeField]
    GameObject previewPrefab;
    List<GameObject> hits = new();
    GameObject preview = null;

    bool useOcclusoinMaterial = true;
    float last = 0f;
    private Vector3 goal => cameraT.transform.position + cameraT.transform.forward * 0.3f;
    private List<Vector2> WorldToVP(List<Vector3> points)  {
        Matrix4x4 V = Camera.main.worldToCameraMatrix;
        Matrix4x4 P = Camera.main.projectionMatrix;
        Matrix4x4 MVP = P * V; // Skipping M, point in world coordinates
        return points.Select(p => MVP.MultiplyPoint(p))
            .Select(p => new Vector2(p.x + 1f, p.y + 1f) * 0.5f)
            .ToList();
    }
    private Vector2 WorldToVP(Vector3 pos) => cameraT.WorldToViewportPoint(pos, Camera.MonoOrStereoscopicEye.Left);

    private void switchMaterial()
    {
        var occlusion = previewPrefab.GetComponent<MeshRenderer>().material;
        foreach (var g in hits)
            g.GetComponent<MeshRenderer>().material = useOcclusoinMaterial ? occlusion : NoOcclusion;
    }
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One))
            resolution = resolution.Next();

        if (OVRInput.GetDown(OVRInput.Button.Two))
            distance = distance.Next();

        if (OVRInput.GetDown(OVRInput.Button.Three)) { 
            foreach (var hit in hits)
                Destroy(hit);
            hits.Clear();
        }
        if (OVRInput.GetDown(OVRInput.Button.Four))
        {
            useOcclusoinMaterial = !useOcclusoinMaterial;
            switchMaterial();
        }

            if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger) 
            && preview == null) {
                preview = Instantiate(previewPrefab);
        }
        if (preview)
            preview.transform.position = goal;

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
        int pixel = (int)resolution;
        float diameter = ((int)distance) / 100f; // in cm
        Vector3 up = cameraT.transform.up;
        Vector3 right = cameraT.transform.right;

        float radius = diameter / 2f;
        float rPerPixel = diameter / pixel;

        List<Vector3> coords = new();
        // create list of viewspace vectors
        for (int y = 0; y < pixel; y++)
            for (int x = 0; x < pixel; x++)
            {
                var xdiff = up * (x * rPerPixel - radius);
                var ydiff = right * (y * rPerPixel - radius);
                var wp = scanCenter + xdiff + ydiff;
                //var vp = WorldToVP(wp);
                coords.Add(wp);
            }

        // Perform ray casts
        depthAccess.RaycastViewSpaceBlocking(WorldToVP(coords), out List<Vector3> results);
        // Create hit results
        foreach (var r in results.Skip(3).SkipLast(3))
        {
            //var p = new Vector3(r.x, 1-r.y, r.z);
            var g = Instantiate(previewPrefab, r, transform.rotation);
            if (!useOcclusoinMaterial) g.GetComponent<MeshRenderer>().material = NoOcclusion;
            g.transform.localScale = Vector3.one * 0.01f;
            hits.Add(g);
        }

        last = Vector3.Distance(results.First(), cameraT.transform.position);
        Destroy(preview);
        preview = null;
    }

    private void CreateSingleScan()
    {
        // Raycasting at the controller anchor's position
        var worldCenter = goal;
        // to viewspace vector
        var viewSpaceCoordinate = WorldToVP(worldCenter);
        // Perform ray cast
        var r = depthAccess.RaycastViewSpaceBlocking(viewSpaceCoordinate);

        last = Vector3.Distance(r, cameraT.transform.position);
        hits.Add(Instantiate(previewPrefab, r, transform.rotation));
        Destroy(preview);
        preview = null;
    }

    public string GetInfoText()
    {
        return "Res: " + resolution.ToString() 
            +( resolution == Resolution.single ? "": " Size: " + ((int)distance) + "cm" )
            + "\n" + last.ToString();
    }
}

public static class Extensions
{
    public static T Next<T>(this T src) where T : struct
    {
        T[] Arr = (T[])Enum.GetValues(src.GetType());
        int j = Array.IndexOf<T>(Arr, src) + 1;
        return (Arr.Length == j) ? Arr[0] : Arr[j];
    }
}