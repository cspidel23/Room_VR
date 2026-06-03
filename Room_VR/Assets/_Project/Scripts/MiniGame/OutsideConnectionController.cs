using UnityEngine;
using System.Collections;
using RoomVR.Experience;

// this handles all outside connections from the minigame to the real world, primarily the outside explosion blasts consisting of audio, screenshake, and lighting flashes. 
public class OutsideConnectionController : MonoBehaviour
{
    public static OutsideConnectionController Instance { get; private set;}

    [Header("Random Explosions")]
    [SerializeField] private bool isRandBlasts = false;
    [SerializeField] private float minRandBlast = 5f;
    [SerializeField] private float maxRandBlast = 10f;

    [Header("Audio Effects")]
    [SerializeField] private GameObject m_AudioClipObject; // a gameobject that has a audioclip attached that auto plays
    [SerializeField] private Transform[] m_AudioClipLocations; // randomly take from one of the transforms and spawn the audio there


    [Header("Shake Effects")] // alternatively shake all the environmental objects instead
    [SerializeField] private Transform m_CameraOffsetObject; // this recieves camera shake, will require testing to see if works with the current camera setup. 
    [SerializeField] private float m_defaultShakeDuration = 0.5f;
    [SerializeField] private float m_defaultShakeMagnitude = 1f;
    [SerializeField] private AnimationCurve m_shakeCurve = AnimationCurve.EaseInOut(1f, 1f, 1f, 1f); // change to correct curve in editor
    private Vector3 originalCameraPos;

    [Header("Lighting Effects")]
    [SerializeField] private Light m_LightToFlash;
    [SerializeField] private float m_defaultLightFlashDuration = 0.5f;
    [SerializeField] private float m_defaultLightFlashIntensity = 10f;
    [SerializeField] private AnimationCurve m_lightCurve = AnimationCurve.EaseInOut(1f, 1f, 1f, 1f); // change to correct curve in editor
    private float originalLightIntensity;


    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        if (isRandBlasts)
        {
            StartCoroutine(TestExplosion());
        }
    }

    private IEnumerator TestExplosion()
    {
        yield return new WaitForSeconds(Random.Range(minRandBlast, maxRandBlast));
        TriggerExplosionEffect();
        StartCoroutine(TestExplosion());
    }

    private void TriggerOutsideAudio()
    {
        if (m_AudioClipLocations.Length == 0)
        {
            GameObject clip = Instantiate(m_AudioClipObject, transform.position, Quaternion.identity);
            Destroy(clip, 5f);
        }
        else
        {
            // pick a random transform from the array and use it as the transform.position location
            GameObject clip = Instantiate(m_AudioClipObject, m_AudioClipLocations[Random.Range(0, m_AudioClipLocations.Length)].transform.position, Quaternion.identity);
            Destroy(clip, 5f);
        }

    }

    // single method to trigger the explosion effects
    public void TriggerExplosionEffect()
    {
        var director = GamePhaseDirector.Instance;
        if (director != null && !director.IsWar) return;

        TriggerOutsideAudio();
        TriggerLightFlash();
        TriggerScreenShake();
    }

    private void TriggerLightFlash()
    {
        originalLightIntensity = m_LightToFlash.intensity;
        StartCoroutine(LightFlashCoroutine(m_defaultLightFlashDuration, m_defaultLightFlashIntensity));
    }

    private void TriggerLightFlash(float lightFlashDuration, float lightFlashIntensity)
    {
        originalLightIntensity = m_LightToFlash.intensity;
        StartCoroutine(LightFlashCoroutine(lightFlashDuration, lightFlashIntensity));
    }

    // coroutine for the light flash, instantly turning it on and then turning it off after the specified duration. 
    private IEnumerator LightFlashCoroutine(float lightFlashDuration, float lightFlashIntensity)
    {
        float elapsed = 0f;

        while (elapsed < lightFlashDuration)
        {
            m_LightToFlash.intensity =  lightFlashIntensity * m_lightCurve.Evaluate(elapsed / lightFlashDuration);

            elapsed += Time.deltaTime;
            yield return null;
        }

        m_LightToFlash.intensity = originalLightIntensity;

    }

    // triggers an screen shake effect on the transform
    private void TriggerScreenShake()
    {
        originalCameraPos = m_CameraOffsetObject.transform.localPosition;
        StartCoroutine(ScreenShakeCoroutine(m_defaultShakeDuration, m_defaultShakeMagnitude));
    }

    // triggers an screen shake effect on the transform
    private void TriggerScreenShake(float shakeDuration, float shakeMagnitude)
    {
        originalCameraPos = m_CameraOffsetObject.transform.localPosition;
        StartCoroutine(ScreenShakeCoroutine(shakeDuration, shakeMagnitude));
    }

    // coroutine for screen shake, randomly moves the transform within a sphere defined by the shake magnitude for the duration of the shake.
    private IEnumerator ScreenShakeCoroutine(float shakeDuration, float shakeMagnitude)
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float shake = m_shakeCurve.Evaluate(elapsed / shakeDuration) * shakeMagnitude;

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