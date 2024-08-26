using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Misaki.ScreenSpaceWetness
{
    public class WetnessColorPass : CustomPass
    {
        public Material wetnessMaterial;

        private RTHandle _tempColorBuffer;

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            _tempColorBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, useDynamicScale: true, name: "TMP Color Buffer");
        }

        protected override void Execute(CustomPassContext ctx)
        {
            if (injectionPoint != CustomPassInjectionPoint.BeforeTransparent)
            {
                Debug.LogError("Custom Pass ScreenSpaceWetness needs to be used at the injection point BeforeTransparent.");
                return;
            }

            if (wetnessMaterial == null)
            {
                return;
            }

            CoreUtils.DrawFullScreen(ctx.cmd, wetnessMaterial, shaderPassId: 1);
        }

        protected override void Cleanup()
        {
            _tempColorBuffer.Release();
        }
    }
}
