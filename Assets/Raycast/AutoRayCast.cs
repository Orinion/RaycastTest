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
    Camera cameraT;
    Transform cameraTransform;

    [SerializeField]
    Material NoOcclusion;
    Material Occlusion => previewPrefab.GetComponent<MeshRenderer>().material;

    [SerializeField]
    GameObject previewPrefab;
    List<GameObject> hits = new();
    GameObject preview = null;

    bool useOcclusoinMaterial = true;
    float last = 0f;
    private Vector3 cameraP => cameraTransform.position;
    private Vector3 goalCenter => cameraP + cameraTransform.forward * 100f;
    private Vector3 goal => cameraP + cameraTransform.forward * 0.5f;
    private Vector3 hitPosition(float returnedDepth, Vector3 requestedPosition) => (requestedPosition - cameraP).normalized * returnedDepth + cameraP;

    private void Start()
    {
        cameraTransform = cameraT.transform;
    }

    private Vector2 WorldToVP(Vector3 pos) => cameraT.WorldToViewportPoint(pos, Camera.MonoOrStereoscopicEye.Left);

    private void switchMaterial()
    {
        foreach (var g in hits)
            g.GetComponent<MeshRenderer>().material = useOcclusoinMaterial ? Occlusion : NoOcclusion;
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
        Vector3 up = cameraTransform.up;
        Vector3 right = cameraTransform.right;

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
                // increase distance for accrucy
                coords.Add(wp);
            }

        // Perform ray casts
        depthAccess.RaycastViewSpaceBlocking(coords.Select(c => WorldToVP(c)).ToList(), out List<float> results);
        // Create hit results
        foreach (var it in results.Select((x,i)=> new {depth=x,index=i}))
        {
            var p = hitPosition(it.depth, coords[it.index]);
            var g = Instantiate(previewPrefab, p, transform.rotation);
            if (!useOcclusoinMaterial) g.GetComponent<MeshRenderer>().material = NoOcclusion;
            g.transform.localScale = Vector3.one * 0.01f;
            hits.Add(g);
        }

        last = results.First();
        Destroy(preview);
        preview = null;
    }

    private void CreateSingleScan()
    {
        // Raycasting at the controller anchor's position
        var worldCenter = goalCenter;
        // to viewspace vector
        var viewSpaceCoordinate = WorldToVP(worldCenter);
        // Perform ray cast
        var depth = depthAccess.RaycastViewSpaceBlocking(viewSpaceCoordinate);
        var hit = hitPosition(depth, worldCenter);
        last = depth;
        var g = Instantiate(previewPrefab, hit, transform.rotation);
        g.transform.localScale = Vector3.one * 0.05f;
        if (!useOcclusoinMaterial) 
            g.GetComponent<MeshRenderer>().material = NoOcclusion;
        hits.Add(g);
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