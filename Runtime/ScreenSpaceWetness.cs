using UnityEngine;
using UnityEngine.Rendering;

namespace Misaki.ScreenSpaceWetness
{
    [VolumeComponentMenu("Lighting/Screen Space Wetness")]
    public class ScreenSpaceWetness : VolumeComponent
    {
        public BoolParameter enable = new BoolParameter(false, BoolParameter.DisplayType.EnumPopup);

        public ClampedFloatParameter globalIntensity = new ClampedFloatParameter(1f, 0f, 1f);

        public BoolParameter overrideGlobalQuality = new BoolParameter(false);
        public EnumParameter<ShadowQuality> quality = new EnumParameter<ShadowQuality>(ShadowQuality.High);
    }
}