using Meta.XR.Depth;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

// Based on comment by TudorJude: https://github.com/oculus-samples/Unity-DepthAPI/issues/16#issuecomment-1863006589
public class EnvironmentDepthAccess : MonoBehaviour
{
    private static readonly int raycastResultsId = Shader.PropertyToID("RaycastResults");
    private static readonly int raycastRequestsId = Shader.PropertyToID("RaycastRequests");
     
    [SerializeField] private ComputeShader _computeShader;

    private ComputeBuffer _requestsCB;
    private ComputeBuffer _resultsCB;


    /**
     * Perform a raycast at multiple view space coordinates and fill the result list.
     * Blocking means that this function will immediately return the result but is performance heavy.
     * List is expected to be the size of the requested coordinates.
     */
    public void RaycastViewSpaceBlocking(List<Vector2> viewSpaceCoords, out List<float> result)
    {
        result = DispatchCompute(viewSpaceCoords);
    }

    /**
     * Perform a raycast at a view space coordinate and return the result.
     * Blocking means that this function will immediately return the result but is performance heavy.
     */
    public float RaycastViewSpaceBlocking(Vector2 viewSpaceCoord)
    {
        var depthRaycastResult = DispatchCompute(new List<Vector2>() { viewSpaceCoord });
        return depthRaycastResult[0];
    }


    private List<float> DispatchCompute(List<Vector2> requestedPositions)
    {
        UpdateCurrentRenderingState();

        int count = requestedPositions.Count;

        var (requestsCB, resultsCB) = GetComputeBuffers(count);
        requestsCB.SetData(requestedPositions);

        _computeShader.SetBuffer(0, raycastRequestsId, requestsCB);
        _computeShader.SetBuffer(0, raycastResultsId, resultsCB);

        _computeShader.Dispatch(0, count, 1, 1);

        var raycastResults = new float[count];
        resultsCB.GetData(raycastResults);

        return raycastResults.ToList();
    }

    (ComputeBuffer, ComputeBuffer) GetComputeBuffers(int size)
    {
        if (_requestsCB != null && _resultsCB != null && _requestsCB.count != size)
        {
            _requestsCB.Release();
            _requestsCB = null;
            _resultsCB.Release();
            _resultsCB = null;
        }

        if (_requestsCB == null || _resultsCB == null)
        {
            _requestsCB = new ComputeBuffer(size, Marshal.SizeOf<Vector2>(), ComputeBufferType.Structured);
            _resultsCB = new ComputeBuffer(size, Marshal.SizeOf<float>(), ComputeBufferType.Structured);
        }

        return (_requestsCB, _resultsCB);
    }

    private void UpdateCurrentRenderingState()
    {
        _computeShader.SetTextureFromGlobal(0, EnvironmentDepthTextureProvider.DepthTextureID,
            EnvironmentDepthTextureProvider.DepthTextureID);
        _computeShader.SetMatrixArray(EnvironmentDepthTextureProvider.ReprojectionMatricesID,
            Shader.GetGlobalMatrixArray(EnvironmentDepthTextureProvider.Reprojection3DOFMatricesID));
        _computeShader.SetVector(EnvironmentDepthTextureProvider.ZBufferParamsID,
            Shader.GetGlobalVector(EnvironmentDepthTextureProvider.ZBufferParamsID));
    }

    private void OnDestroy()
    {
        if(_resultsCB != null)
            _resultsCB.Release();
    }
}