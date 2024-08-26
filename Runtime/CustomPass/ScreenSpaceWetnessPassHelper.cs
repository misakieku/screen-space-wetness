using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Misaki.ScreenSpaceWetness
{
    public enum RenderMode
    {
        None,
        Wetness,
        Debug,
    }

    [ExecuteInEditMode]
    public class ScreenSpaceWetnessPassHelper : MonoBehaviour
    {
        [SerializeField]
        private CustomPassVolume _wetnessNormalPassVolume;
        [SerializeField]
        private CustomPassVolume _wetnessColorPassVolume;
        [SerializeField]
        private CustomPassVolume _debugPassVolume;

        private MaskPass _maskPass;
        private WetnessNormalPass _wetnessNormalPass;
        private FullScreenCustomPass _wetnessColorPass;
        private FullScreenCustomPass _debugPass;

        private void OnEnable()
        {
            CreateWetnessNormalVolume();
            CreateWetnessColorVolume();
            CreateDebugVolume();

            name = "Screen Space Wetness Pass Volume";
        }

        private void CreateWetnessNormalVolume()
        {
            if (_wetnessNormalPassVolume == null)
            {
                _wetnessNormalPassVolume = gameObject.AddComponent<CustomPassVolume>();
                _wetnessNormalPassVolume.injectionPoint = CustomPassInjectionPoint.AfterOpaqueDepthAndNormal;

                CreateMaskPass();
                CreateNormalPass();
            }

            _wetnessNormalPass ??= _wetnessNormalPassVolume.customPasses.FirstOrDefault(p => p is WetnessNormalPass) as WetnessNormalPass;
            _maskPass ??= _wetnessNormalPassVolume.customPasses.FirstOrDefault(p => p is MaskPass) as MaskPass;

            if (_maskPass == null)
            {
                CreateMaskPass();
            }

            if (_wetnessNormalPass == null)
            {
                CreateNormalPass();
            }

            void CreateMaskPass()
            {
                _maskPass = _wetnessNormalPassVolume.AddPassOfType<MaskPass>();
                _maskPass.name = "Wetness Mask";
            }

            void CreateNormalPass()
            {
                _wetnessNormalPass = _wetnessNormalPassVolume.AddPassOfType<WetnessNormalPass>();
                _wetnessNormalPass.name = "Wetness Normal";
            }
        }

        private void CreateWetnessColorVolume()
        {
            if (_wetnessColorPassVolume == null)
            {
                _wetnessColorPassVolume = gameObject.AddComponent<CustomPassVolume>();
                _wetnessColorPassVolume.injectionPoint = CustomPassInjectionPoint.BeforeTransparent;

                CreateColorPass();
            }

            _wetnessColorPass ??= _wetnessColorPassVolume.customPasses.FirstOrDefault(p => p is FullScreenCustomPass) as FullScreenCustomPass;

            if (_wetnessColorPass == null)
            {
                CreateColorPass();
            }

            void CreateColorPass()
            {
                _wetnessColorPass = _wetnessColorPassVolume.AddPassOfType<FullScreenCustomPass>();
                _wetnessColorPass.name = "Wetness Color";
            }
        }

        private void CreateDebugVolume()
        {
            if (_debugPassVolume == null)
            {
                _debugPassVolume = gameObject.AddComponent<CustomPassVolume>();
                _debugPassVolume.injectionPoint = CustomPassInjectionPoint.BeforePostProcess;

                CreateDebugPass();
            }

            _debugPass ??= _debugPassVolume.customPasses.FirstOrDefault(p => p is FullScreenCustomPass) as FullScreenCustomPass;

            if (_debugPass == null)
            {
                CreateDebugPass();
            }

            void CreateDebugPass()
            {
                _debugPass = _debugPassVolume.AddPassOfType<FullScreenCustomPass>();
                _debugPass.name = "Wetness Debug";
            }
        }

        public void AssignMaterial(Material wetnessMaterial, Material maskMaterial)
        {
            if (_wetnessNormalPass == null || _maskPass == null)
            {
                CreateWetnessNormalVolume();
            }

            _wetnessNormalPass.wetnessMaterial = wetnessMaterial;

            _maskPass.wetnessMaterial = wetnessMaterial;
            _maskPass.overrideMaterial = maskMaterial;

            if (_wetnessColorPass == null)
            {
                CreateWetnessColorVolume();
            }

            _wetnessColorPass.fullscreenPassMaterial = wetnessMaterial;
            _wetnessColorPass.materialPassName = "Color";

            if (_debugPass == null)
            {
                CreateDebugVolume();
            }

            _debugPass.fullscreenPassMaterial = wetnessMaterial;
            _debugPass.materialPassName = "Debug";
        }

        public void ApplyRenderMode(RenderMode renderMode)
        {
            switch (renderMode)
            {
                case RenderMode.None:
                    _wetnessNormalPassVolume.enabled = false;
                    _wetnessColorPassVolume.enabled = false;
                    _debugPassVolume.enabled = false;
                    break;
                case RenderMode.Wetness:
                    _wetnessNormalPassVolume.enabled = true;
                    _wetnessColorPassVolume.enabled = true;
                    _debugPassVolume.enabled = false;
                    break;
                case RenderMode.Debug:
                    _wetnessNormalPassVolume.enabled = true;
                    _wetnessColorPassVolume.enabled = false;
                    _debugPassVolume.enabled = true;
                    break;
            }
        }
    }
}