using UnityEngine;

public class AudioController : MonoBehaviour
{
    public static AudioController Instance { get; private set; }
    [Header("Game Audio")]
    [SerializeField] private GameObject m_BlastAudioClip;
    [SerializeField] private GameObject m_DropAudioClip;

    [Header("Outside Audio")]
    [SerializeField] private GameObject m_OutsideAudioClip;

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

    public void PlayOutsideAudio()
    {
        GameObject clip = Instantiate(m_OutsideAudioClip, transform.position, Quaternion.identity);
        Destroy(clip, 5f);
    }

    // specify position in case of 3D audio
    public void PlayOutsideAudio(Vector3 position)
    {
        GameObject clip = Instantiate(m_OutsideAudioClip, position, Quaternion.identity);
        Destroy(clip, 5f);
    }
}
