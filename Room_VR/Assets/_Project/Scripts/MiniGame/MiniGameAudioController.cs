using UnityEditor.Timeline.Actions;
using UnityEngine;

public class MiniGameAudioController : MonoBehaviour
{
    public static MiniGameAudioController Instance { get; private set; }
    [SerializeField] private GameObject m_BlastAudioClip;
    [SerializeField] private GameObject m_DropAudioClip;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

    }

    public void PlayBlastAudio()
    {
        GameObject clip = Instantiate(m_BlastAudioClip, transform.position, Quaternion.identity);
        Destroy(clip, 5f);
    }

    public void PlayDropAudio()
    {
        GameObject clip = Instantiate(m_DropAudioClip, transform.position, Quaternion.identity);
        Destroy(clip, 5f);
    }
}
