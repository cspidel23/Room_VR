using UnityEngine;

namespace RoomVR.MiniGame
{
    // Assigns a runtime RenderTexture to the mini-game camera and to a monitor renderer,
    // following the same pattern as VideoPlayerRenderTexture.
    [RequireComponent(typeof(Camera))]
    public class MiniGameRenderTextureSetup : MonoBehaviour
    {
        const string k_ShaderName = "Unlit/Texture";

        [SerializeField]
        [Tooltip("The monitor renderer that will display the mini-game.")]
        Renderer m_MonitorRenderer;

        [SerializeField]
        int m_Width = 1024;

        [SerializeField]
        int m_Height = 1024;

        void Start()
        {
            var rt = new RenderTexture(m_Width, m_Height, 16);
            rt.Create();

            GetComponent<Camera>().targetTexture = rt;

            if (m_MonitorRenderer != null)
            {
                var mat = new Material(Shader.Find(k_ShaderName));
                mat.mainTexture = rt;
                m_MonitorRenderer.material = mat;
            }
        }
    }
}
