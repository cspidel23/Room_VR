using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoomVR.Experience
{
    // Full-screen fade implemented as a post-process: a high-priority global Volume whose
    // ColorAdjustments "Color Filter" is black. Driving the Volume weight 0..1 multiplies
    // the whole framebuffer toward black AFTER everything is drawn, so it covers the hands
    // and world geometry without needing a hide list.
    //
    // Requires post-processing enabled on the camera (URP camera "Post Processing" = on).
    // Note: world-space UI (e.g. the ending message) is part of the scene and will also be
    // darkened by this fade.
    [RequireComponent(typeof(Volume))]
    public class PostProcessFader : ScreenFaderBase
    {
        [SerializeField]
        [Tooltip("Volume priority. Keep high so the black color filter wins over other volumes.")]
        int m_Priority = 100;

        Volume m_Volume;

        void Awake()
        {
            m_Volume = GetComponent<Volume>();
            m_Volume.isGlobal = true;
            m_Volume.priority = m_Priority;

            // Build a runtime profile with a black Color Filter so weight 1 = black.
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var colorAdjustments = profile.Add<ColorAdjustments>(overrides: true);
            colorAdjustments.colorFilter.overrideState = true;
            colorAdjustments.colorFilter.value = Color.black;
            m_Volume.sharedProfile = profile;

            SetAlphaImmediate(0f); // start clear
        }

        protected override void ApplyFade(float alpha)
        {
            if (m_Volume != null) m_Volume.weight = alpha;
        }
    }
}
