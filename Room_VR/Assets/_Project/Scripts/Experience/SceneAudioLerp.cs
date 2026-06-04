using System.Collections;
using System.Threading;
using UnityEngine;

public class SceneAudioLerp : MonoBehaviour
{
    [SerializeField] private bool m_startOn = false;
    [SerializeField] private float m_lerpTime = 1f; // lerps the volume in and out at this amount
    [SerializeField] private float m_volumeMultiplier = 1f;

    private AudioSource m_Source = null;
    private float targetVolume = 1f; // this grabs the starting volume when the scene loads

    private void Awake()
    {
        m_Source = GetComponent<AudioSource>();
        targetVolume = m_Source.volume * m_volumeMultiplier;
        m_Source.volume = 0;
    }

    private void Start()
    {
        if (m_startOn)
        {
            StartAudio();
        }
    }

    // method that is called when the audio should start
    public void StartAudio()
    {
        StartCoroutine(AudioLerp(targetVolume));
    }

    // method that is called when the audio should end
    public void EndAudio()
    {
        StartCoroutine(AudioLerp(0));
    }

    private IEnumerator AudioLerp(float targetVolume)
    {
        float currentVolume = m_Source.volume;
        float time = 0;

        while (time < m_lerpTime)
        {
            time += Time.deltaTime;

            m_Source.volume = Mathf.Lerp(
                currentVolume,
                targetVolume,
                time / m_lerpTime
            );

            yield return null;
        }

        m_Source.volume = targetVolume;
    }

}