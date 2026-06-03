using UnityEngine;
using UnityEngine.Rendering;

namespace RoomVR.Experience
{
    // Fades the view to/from a solid colour using a quad placed in front of the camera.
    // Put this on a Quad parented just in front of the Main Camera, covering the view.
    // Note: a quad does NOT cover objects rendered after it (e.g. some hand visuals);
    // for a fade that covers everything, use PostProcessFader instead.
    public class ScreenFader : ScreenFaderBase
    {
        [SerializeField]
        [Tooltip("Fade colour (usually black).")]
        Color m_Color = Color.black;

        [SerializeField]
        [Tooltip("Start fully opaque (black) so the scene can fade in on load.")]
        bool m_StartBlack;

        Renderer m_Renderer;
        Material m_Material;

        void Awake()
        {
            m_Renderer = GetComponent<Renderer>();

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            m_Material = new Material(shader);
            MakeTransparent(m_Material);
            if (m_Renderer != null) m_Renderer.material = m_Material;

            SetAlphaImmediate(m_StartBlack ? 1f : 0f);
        }

        protected override void ApplyFade(float alpha)
        {
            var c = m_Color;
            c.a = alpha;
            if (m_Material != null)
            {
                if (m_Material.HasProperty("_BaseColor")) m_Material.SetColor("_BaseColor", c);
                if (m_Material.HasProperty("_Color")) m_Material.SetColor("_Color", c);
            }

            if (m_Renderer != null) m_Renderer.enabled = alpha > 0.001f;
        }

        static void MakeTransparent(Material mat)
        {
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
            if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", (int)CullMode.Off);
            mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }
    }
}
