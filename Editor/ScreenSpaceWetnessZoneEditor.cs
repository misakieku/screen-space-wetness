using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Misaki.ScreenSpaceWetness.Editor
{
    [CustomEditor(typeof(ScreenSpaceWetnessZone))]
    public class ScreenSpaceWetnessZoneEditor : UnityEditor.Editor
    {
        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;

        private ScreenSpaceWetnessZone dataSource;

        private void OnEnable()
        {
            dataSource = target as ScreenSpaceWetnessZone;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var visualAssetContainer = m_VisualTreeAsset.Instantiate();
            visualAssetContainer.dataSource = dataSource;

            RegisterChangeEvent<ChangeEvent<Vector3>>(visualAssetContainer.Q<Vector3Field>("center-vector3"), UpdateCameraProperty);
            RegisterChangeEvent<ChangeEvent<float>>(visualAssetContainer.Q<FloatField>("half-size-float"), UpdateCameraProperty);
            RegisterChangeEvent<ChangeEvent<float>>(visualAssetContainer.Q<FloatField>("half-length-float"), UpdateCameraProperty);
            visualAssetContainer.Q<EnumField>("shadow-quality-enum").RegisterValueChangedCallback(callback =>
            {
                dataSource.CreateRenderTarget((ShadowQuality)callback.newValue);
            });
            visualAssetContainer.Q<EnumField>("render-mode-enum").RegisterValueChangedCallback(callback =>
            {
                var mode = (RenderMode)callback.newValue;

                dataSource.UpdateCameraProperty(mode);
                dataSource.UpdateRenderProperty(mode);
            });

            root.Add(visualAssetContainer);

            return root;
        }

        private void RegisterChangeEvent<T>(VisualElement element, Action callback) where T : EventBase<T>, new()
        {
            element.RegisterCallback<T>(evt => callback());
        }

        private void UpdateCameraProperty()
        {
            dataSource.UpdateCameraProperty(dataSource.renderMode);
        }
    }
}