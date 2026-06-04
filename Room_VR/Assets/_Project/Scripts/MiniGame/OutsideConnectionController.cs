using UnityEngine;
using System.Collections;
using RoomVR.Experience;

// this handles all outside connections from the minigame to the real world, primarily the outside explosion blasts consisting of audio, screenshake, and lighting flashes. 
public class OutsideConnectionController : MonoBehaviour
{
    public static OutsideConnectionController Instance { get; private set; }

    [Header("Random Explosions")]
    [SerializeField] private bool m_isRandBlasts = false;
    [SerializeField] private float m_randBlastMultiplier = .33f;
    [SerializeField] private float m_minRandBlast = 5f;
    [SerializeField] private float m_maxRandBlast = 10f;

    [Header("Intensity Multipliers")]
    [SerializeField] private float m_minIntensityMultiplier = .5f;
    [SerializeField] private float m_maxIntensityMultiplier = 1.5f;

    [Header("Audio Effects")]
    [SerializeField] private GameObject m_AudioClipObject; // a gameobject that has a audioclip attached that auto plays
    [SerializeField] private float m_defaultSpeedMultiplier = 1f;
    [SerializeField] private float m_defaultAudioMagnitude = 1f;
    [SerializeField] private Transform[] m_AudioClipLocations; // randomly take from one of the transforms and spawn the audio there


    [Header("Shake Effects")] // alternatively shake all the environmental objects instead
    [SerializeField] private Transform m_CameraOffsetObject; // this recieves camera shake, will require testing to see if works with the current camera setup. 
    [SerializeField] private float m_defaultShakeDuration = 0.5f;
    [SerializeField] private float m_defaultShakeMagnitude = 1f;
    [SerializeField] private AnimationCurve m_ShakeCurve = AnimationCurve.EaseInOut(1f, 1f, 1f, 1f); // change to correct curve in editor
    private Vector3 originalCameraPos;

    [Header("Lighting Effects")]
    [SerializeField] private Light m_LightToFlash;
    [SerializeField] private float m_defaultLightFlashDuration = 0.5f;
    [SerializeField] private float m_defaultLightFlashIntensity = 10f;
    [SerializeField] private AnimationCurve m_LightCurve = AnimationCurve.EaseInOut(1f, 1f, 1f, 1f); // change to correct curve in editor
    private float originalLightIntensity;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        if (m_isRandBlasts)
        {
            StartCoroutine(TestExplosion());
        }
    }

    private IEnumerator TestExplosion()
    {
        yield return new WaitForSeconds(Random.Range(m_minRandBlast, m_maxRandBlast));
        TriggerExplosionEffect(m_randBlastMultiplier);
        StartCoroutine(TestExplosion());
    }

    // single method to trigger the explosion effects
    public void TriggerExplosionEffect(float blastMultiplier = 1f)
    {
        var director = GamePhaseDirector.Instance;
        if (director != null && !director.IsWar) return;

        // a random multiplier on intensity of the various actions
        float multiplier = Random.Range(m_minIntensityMultiplier, m_maxIntensityMultiplier) * blastMultiplier;

        TriggerOutsideAudio(multiplier); // ignore this one for now
        TriggerLightFlash(multiplier);
        TriggerScreenShake(multiplier);
    }

    private void TriggerOutsideAudio(float multiplier = 1f)
    {
        if (m_AudioClipLocations.Length == 0)
        {
            AudioInstantiate(m_AudioClipObject, transform, multiplier);
        }
        else
        {
            // pick a random transform from the array and use it as the transform.position location
            AudioInstantiate(m_AudioClipObject, m_AudioClipLocations[Random.Range(0, m_AudioClipLocations.Length)].transform, multiplier);
        }

    }

    private void AudioInstantiate(GameObject audioClip, Transform position, float multiplier)
    {
        GameObject clip = Instantiate(audioClip, position.position, Quaternion.identity);
        clip.GetComponent<AudioSource>().volume *= multiplier;
        Destroy(clip, 5f);
    }

    private void TriggerLightFlash(float multiplier = 1f)
    {
        originalLightIntensity = 0;
        StartCoroutine(LightFlashCoroutine(m_defaultLightFlashDuration * multiplier, m_defaultLightFlashIntensity * multiplier));
    }

    // triggers an screen shake effect on the transform
    private void TriggerScreenShake(float multiplier = 1f)
    {
        originalCameraPos = m_CameraOffsetObject.transform.localPosition;
        StartCoroutine(ScreenShakeCoroutine(m_defaultShakeDuration * multiplier, m_defaultShakeMagnitude * multiplier));
    }

    // coroutine for the light flash, instantly turning it on and then turning it off after the specified duration. 
    private IEnumerator LightFlashCoroutine(float lightFlashDuration, float lightFlashIntensity)
    {
        float elapsed = 0f;

        while (elapsed < lightFlashDuration)
        {
            m_LightToFlash.intensity = lightFlashIntensity * m_LightCurve.Evaluate(elapsed / lightFlashDuration);

            elapsed += Time.deltaTime;
            yield return null;
        }

        m_LightToFlash.intensity = originalLightIntensity;

    }

    // coroutine for screen shake, randomly moves the transform within a sphere defined by the shake magnitude for the duration of the shake.
    private IEnumerator ScreenShakeCoroutine(float shakeDuration, float shakeMagnitude)
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float shake = m_ShakeCurve.Evaluate(elapsed / shakeDuration) * shakeMagnitude;

            float x = Random.Range(-1f, 1f) * shake;
            float y = Random.Range(-1f, 1f) * shake;
            float z = Random.Range(-1f, 1f) * shake;

            m_CameraOffsetObject.transform.localPosition = originalCameraPos + new Vector3(x, y, z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        m_CameraOffsetObject.transform.localPosition = originalCameraPos;
    }

}