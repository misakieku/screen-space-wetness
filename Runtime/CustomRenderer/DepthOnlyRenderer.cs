using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.RendererUtils;

namespace Misaki.ScreenSpaceWetness
{
    public enum DepthOnlyRendererUpdateMode
    {
        OnEnable,
        OnMove,
        EveryFrame,
        ViaScript
    }

    [ExecuteInEditMode]
    [RequireComponent(typeof(HDAdditionalCameraData))]
    public class DepthOnlyRenderer : MonoBehaviour, IDisposable
    {
        public bool enable;

        private static readonly RenderQueueRange _renderQueue_AllOpaque = new() { lowerBound = (int)RenderQueue.Background, upperBound = (int)RenderQueue.GeometryLast };
        private const SortingCriteria _opaqueSortingCriteria = SortingCriteria.CommonOpaque & (~SortingCriteria.QuantizedFrontToBack);

        public Camera depthOnlyCamera;
        public RenderTexture renderTarget;

        public DepthOnlyRendererUpdateMode updateMode = DepthOnlyRendererUpdateMode.OnMove;

        private HDAdditionalCameraData hdCameraData;

        private void OnEnable()
        {
            depthOnlyCamera = GetComponent<Camera>();
            depthOnlyCamera.targetTexture = renderTarget;
            depthOnlyCamera.enabled = false;

            hdCameraData = GetComponent<HDAdditionalCameraData>();
            hdCameraData.customRender += CustomRender;

            if (updateMode == DepthOnlyRendererUpdateMode.OnEnable || updateMode == DepthOnlyRendererUpdateMode.OnMove)
            {
                RequestRender();
            }
        }

        private void Update()
        {
            switch (updateMode)
            {
                case DepthOnlyRendererUpdateMode.OnMove:
                    if (transform.hasChanged)
                    {
                        RequestRender();
                        transform.hasChanged = false;
                    }
                    break;
                case DepthOnlyRendererUpdateMode.EveryFrame:
                    RequestRender();
                    break;
                default:
                    break;
            }
        }

        private void OnDisable()
        {
            hdCameraData.customRender -= CustomRender;
        }

        private void CustomRender(ScriptableRenderContext context, HDCamera hdCamera)
        {
            if (renderTarget == null)
            {
                return;
            }

            var cullingResults = new CullingResults();

            if (hdCamera.camera.TryGetCullingParameters(out var cullingParameters))
            {
                cullingResults = context.Cull(ref cullingParameters);
            }

            var renderDesc = new RendererListDesc(HDShaderPassNames.s_DepthOnlyName, cullingResults, hdCamera.camera)
            {
                rendererConfiguration = PerObjectData.None,
                renderQueueRange = _renderQueue_AllOpaque,
                sortingCriteria = _opaqueSortingCriteria,
                stateBlock = default(RenderStateBlock),
                batchLayerMask = uint.MaxValue,
                excludeObjectMotionVectors = false
            };

            var renderList = context.CreateRendererList(renderDesc);

            var cmd = new CommandBuffer();

            cmd.SetRenderTarget(hdCamera.camera.targetTexture);
            cmd.ClearRenderTarget(true, true, Color.clear);
            cmd.DrawRendererList(renderList);

            context.ExecuteCommandBuffer(cmd);
            context.Submit();

            cmd.Release();
        }

        public void InitializeProperties(RenderMode renderMode, Vector3 center, Vector2 size)
        {
            if (renderMode == RenderMode.None)
            {
                enable = false;
            }
            else
            {
                enable = true;
            }
            depthOnlyCamera.transform.localPosition = new Vector3(center.x, center.y + size.y, center.z);
            depthOnlyCamera.transform.rotation = Quaternion.LookRotation(Vector3.down);

            depthOnlyCamera.orthographic = true;
            depthOnlyCamera.orthographicSize = size.x;
            depthOnlyCamera.farClipPlane = size.y * 2.0f;
        }

        public void CreateRenderTarget(int size)
        {
            depthOnlyCamera.targetTexture = null;
            if (renderTarget != null)
            {
                renderTarget.Release();
            }

            renderTarget = new(size, size, 0)
            {
                graphicsFormat = GraphicsFormat.None,
                depthStencilFormat = GraphicsFormat.D32_SFloat_S8_UInt,
            };

            depthOnlyCamera.targetTexture = renderTarget;

            RequestRender();
        }

        public void RequestRender()
        {
            if (!enable)
                return;

            depthOnlyCamera.Render();
        }

        public void Dispose()
        {
            depthOnlyCamera.targetTexture = null;

            renderTarget.Release();
        }
    }
}
