using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Misaki.ScreenSpaceWetness
{
    public class WetnessPass : CustomPass
    {
        public Material wetnessMaterial;
        public int materialsPassId;

        private RTHandle _tmpNormalBuffer;

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            _tmpNormalBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, useDynamicScale: true, name: "TMP Normal Buffer");
        }

        protected override void Execute(CustomPassContext ctx)
        {
            if (injectionPoint != CustomPassInjectionPoint.AfterOpaqueDepthAndNormal)
            {
                Debug.LogError("Custom Pass ScreenSpaceWetness needs to be used at the injection point AfterOpaqueDepthAndNormal.");
                return;
            }

            if (wetnessMaterial == null)
                return;

            CoreUtils.SetRenderTarget(ctx.cmd, _tmpNormalBuffer, ctx.cameraDepthBuffer);
            CoreUtils.DrawFullScreen(ctx.cmd, wetnessMaterial);
            CustomPassUtils.Copy(ctx, _tmpNormalBuffer, ctx.cameraNormalBuffer);
        }

        protected override void Cleanup()
        {
            _tmpNormalBuffer.Release();
        }
    }
}