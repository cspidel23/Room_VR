using System.Collections;
using UnityEngine;

namespace RoomVR.Experience
{
    // Base for full-screen fades. Subclasses only implement ApplyFade(alpha),
    // where alpha 0 = fully clear and 1 = fully black. Lets GamePhaseDirector treat
    // a quad fader and a post-process fader the same way.
    public abstract class ScreenFaderBase : MonoBehaviour
    {
        float m_Alpha;
        Coroutine m_Fade;

        public bool IsBlack => m_Alpha >= 0.999f;
        public bool IsClear => m_Alpha <= 0.001f;

        // 0 = clear, 1 = black.
        protected abstract void ApplyFade(float alpha);

        protected void SetAlphaImmediate(float alpha)
        {
            m_Alpha = Mathf.Clamp01(alpha);
            ApplyFade(m_Alpha);
        }

        public Coroutine FadeOut(float duration) => StartFade(1f, duration); // to black
        public Coroutine FadeIn(float duration) => StartFade(0f, duration);  // to clear

        public void SetBlack(bool black)
        {
            if (m_Fade != null) { StopCoroutine(m_Fade); m_Fade = null; }
            SetAlphaImmediate(black ? 1f : 0f);
        }

        Coroutine StartFade(float target, float duration)
        {
            if (m_Fade != null) StopCoroutine(m_Fade);
            m_Fade = StartCoroutine(FadeTo(target, duration));
            return m_Fade;
        }

        IEnumerator FadeTo(float target, float duration)
        {
            var start = m_Alpha;

            if (duration <= 0f)
            {
                SetAlphaImmediate(target);
                m_Fade = null;
                yield break;
            }

            var t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                SetAlphaImmediate(Mathf.Lerp(start, target, Mathf.Clamp01(t)));
                yield return null;
            }

            SetAlphaImmediate(target);
            m_Fade = null;
        }
    }
}
