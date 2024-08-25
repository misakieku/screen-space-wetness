using UnityEngine;
using UnityEngine.Rendering;

namespace Misaki.ScreenSpaceWetness
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class ScreenSpaceWetnessZone : MonoBehaviour
    {
        public RenderMode renderMode = RenderMode.Wetness;
        public int layerMask = 1;
        public float intensity = 1.0f;

        public Vector3 center;
        public Vector2 size = new Vector2(15, 15);

        public DepthOnlyRenderer depthRenderer;
        public ScreenSpaceWetnessPassHelper wetnessPassHelper;

        private Material _wetnessMaterial;

        public Texture2D firstNormalTexture;
        public Texture2D secondNormalTexture;

        public Vector4 normalParams = Vector4.one;
        public float normalStrength = 0.1f;
        public Vector2 flowDirection = Vector2.left;

        public float waterSmoothness = 0.95f;
        public Vector4 noiseScaleOffset = new(1.0f, 1.0f, 0.0f, 0.0f);
        public Vector2 noiseMinMax = new(0.0f, 1.0f);

        public ShadowQuality quality = ShadowQuality.Medium;
        public Vector2 bias = new(0.01f, 0.1f);
        public Texture3D rotationNoiseTexture;

        private void OnEnable()
        {
            depthRenderer = GetComponentInChildren<DepthOnlyRenderer>();
            wetnessPassHelper = GetComponentInChildren<ScreenSpaceWetnessPassHelper>();

            if (depthRenderer == null)
            {
                var rendererGO = new GameObject("DepthOnlyRenderer");
                rendererGO.transform.SetParent(transform);
                depthRenderer = rendererGO.AddComponent<DepthOnlyRenderer>();
            }

            if (wetnessPassHelper == null)
            {
                var customPassGO = new GameObject("ScreenSpaceWetnessPassHelper");
                customPassGO.transform.SetParent(transform);
                wetnessPassHelper = customPassGO.AddComponent<ScreenSpaceWetnessPassHelper>();
            }

            if (_wetnessMaterial == null)
            {
                _wetnessMaterial = CoreUtils.CreateEngineMaterial("FullScreen/ScreenSpaceWetness");
            }

            CreateRenderTarget(quality);
            UpdateCameraProperty(renderMode);

            wetnessPassHelper.AssignMaterial(_wetnessMaterial);
            wetnessPassHelper.ApplyRenderMode(renderMode);
        }

        private void OnDestroy()
        {
            depthRenderer.Dispose();
        }

        private void Update()
        {
            UpdateMaterial();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(center, new Vector3(size.x * 2.0f, size.y * 2.0f, size.x * 2.0f));
        }
        public void UpdateMaterial()
        {
            if (depthRenderer.depthOnlyCamera == null || _wetnessMaterial == null)
            {
                return;
            }

            var V = depthRenderer.depthOnlyCamera.worldToCameraMatrix;
            var P = GL.GetGPUProjectionMatrix(depthRenderer.depthOnlyCamera.projectionMatrix, false);

            var VP = P * V; // Order is important, V * P won't work

            var texelSize = new Vector4(depthRenderer.renderTarget.width, depthRenderer.renderTarget.height, 1.0f / depthRenderer.renderTarget.width, 1.0f / depthRenderer.renderTarget.height);

            _wetnessMaterial.SetFloat("_intensity", intensity);

            _wetnessMaterial.SetMatrix("_rainMatrix", VP);
            _wetnessMaterial.SetVector("_rainDirection", -depthRenderer.depthOnlyCamera.transform.forward);

            _wetnessMaterial.SetTexture("_waterNormal1", firstNormalTexture);
            _wetnessMaterial.SetTexture("_waterNormal2", secondNormalTexture);

            _wetnessMaterial.SetVector("_normalParams", normalParams);
            _wetnessMaterial.SetFloat("_normalStrength", normalStrength);
            _wetnessMaterial.SetVector("_flowDirection", flowDirection);

            _wetnessMaterial.SetFloat("_waterSmoothness", waterSmoothness);
            _wetnessMaterial.SetVector("_noiseScaleOffset", noiseScaleOffset);
            _wetnessMaterial.SetVector("_noiseMinMax", noiseMinMax);


            _wetnessMaterial.SetTexture("_shadowMap", depthRenderer.renderTarget);
            _wetnessMaterial.SetTexture("_samplingNoiseTexture", rotationNoiseTexture);

            _wetnessMaterial.SetVector("_shadowMap_TexelSize", texelSize);
            _wetnessMaterial.SetVector("_bias", bias);
        }

        public void UpdateCameraProperty(RenderMode renderMode)
        {
            depthRenderer.InitializeProperties(renderMode, center, size);
        }

        public void UpdateRenderProperty(RenderMode renderMode)
        {
            wetnessPassHelper.ApplyRenderMode(renderMode);
        }

        public void CreateRenderTarget(ShadowQuality quality)
        {
            depthRenderer.CreateRenderTarget((int)quality);
        }
    }
}