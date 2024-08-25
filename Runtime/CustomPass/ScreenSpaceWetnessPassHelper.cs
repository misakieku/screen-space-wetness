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
        private CustomPassVolume _wetnessPassVolume;
        [SerializeField]
        private CustomPassVolume _debugPassVolume;

        private WetnessPass _wetnessPass;
        private FullScreenCustomPass _debugPass;

        private void OnEnable()
        {
            CreateWetnessVolume();
            CreateDebugVolume();

            name = "Screen Space Wetness Pass Volume";
        }

        private void CreateWetnessVolume()
        {
            if (_wetnessPassVolume == null)
            {
                _wetnessPassVolume = gameObject.AddComponent<CustomPassVolume>();
                _wetnessPassVolume.injectionPoint = CustomPassInjectionPoint.AfterOpaqueDepthAndNormal;

                _wetnessPass = _wetnessPassVolume.AddPassOfType<WetnessPass>();
                _wetnessPass.name = "Wetness";
            }

            _wetnessPass ??= _wetnessPassVolume.customPasses.First(p => p is WetnessPass) as WetnessPass;
        }

        private void CreateDebugVolume()
        {
            if (_debugPassVolume == null)
            {
                _debugPassVolume = gameObject.AddComponent<CustomPassVolume>();
                _debugPassVolume.injectionPoint = CustomPassInjectionPoint.BeforePostProcess;

                _debugPass = _debugPassVolume.AddPassOfType<FullScreenCustomPass>();
                _debugPass.name = "Debug";
            }

            _debugPass ??= _debugPassVolume.customPasses.First(p => p is FullScreenCustomPass) as FullScreenCustomPass;
        }

        public void AssignMaterial(Material material)
        {
            if (_wetnessPass == null)
            {
                CreateWetnessVolume();
            }

            _wetnessPass.wetnessMaterial = material;
            _wetnessPass.materialsPassId = 0;

            if (_debugPass == null)
            {
                CreateDebugVolume();
            }

            _debugPass.fullscreenPassMaterial = material;
            _debugPass.materialPassName = "Debug";
        }

        public void ApplyRenderMode(RenderMode renderMode)
        {
            switch (renderMode)
            {
                case RenderMode.None:
                    _wetnessPassVolume.enabled = false;
                    _debugPassVolume.enabled = false;
                    break;
                case RenderMode.Wetness:
                    _wetnessPassVolume.enabled = true;
                    _debugPassVolume.enabled = false;
                    break;
                case RenderMode.Debug:
                    _wetnessPassVolume.enabled = false;
                    _debugPassVolume.enabled = true;
                    break;
            }
        }
    }
}