using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Misaki.ScreenSpaceWetness
{
    public class MaskPass : DrawRenderersCustomPass
    {
        public RTHandle _maskBuffer;

        public Material maskMaterial;
        public Material wetnessMaterial;

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            base.Setup(renderContext, cmd);

            _maskBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GraphicsFormat.R8_UNorm, useDynamicScale: true, name: "Wetness Mask Buffer");
        }

        protected override void Execute(CustomPassContext ctx)
        {
            if (wetnessMaterial == null || _maskBuffer == null)
            {
                return;
            }

            CoreUtils.SetRenderTarget(ctx.cmd, _maskBuffer, ClearFlag.Color);
            CustomPassUtils.DrawRenderers(ctx, layerMask, overrideMaterial: maskMaterial);

            wetnessMaterial.SetTexture("_maskBuffer", _maskBuffer.rt);
        }

        protected override void Cleanup()
        {
            base.Cleanup();

            CoreUtils.Destroy(maskMaterial);
            _maskBuffer.Release();
        }
    }
}
