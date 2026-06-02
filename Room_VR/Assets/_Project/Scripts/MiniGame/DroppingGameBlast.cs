using UnityEngine;
using System.Collections;

namespace RoomVR.MiniGame
{
    public class DroppingGameBlast : MonoBehaviour
    {
        [SerializeField] private bool m_IsAnimate = true;
        private Vector3 m_TargetScale;

        // on Awake, start at scale 0,0,0
        // start a coroutine that scales the blast up to 1, 1, 1 over .2 seconds
        // then start a coroutine that scales the blast down to 0, 0, 0 over .5 seconds
        // then destroy this game object
        private void Awake()
        {
            if (m_IsAnimate)
            {
                m_TargetScale = transform.localScale;
                transform.localScale = Vector3.zero;
                StartCoroutine(ScaleUp());
            }
            else
            {
                Destroy(gameObject, 5f);
            }
        }

        private IEnumerator ScaleUp()
        {
            float elapsedTime = 0f;
            Vector3 startScale = Vector3.zero;
            Vector3 targetScale = m_TargetScale;

            while (elapsedTime < 0.2f)
            {
                transform.localScale = Vector3.Lerp(startScale, targetScale, elapsedTime / 0.2f);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            transform.localScale = targetScale;
            StartCoroutine(ScaleDown());
        }

        private IEnumerator ScaleDown()
        {
            float elapsedTime = 0f;
            Vector3 startScale = m_TargetScale;
            Vector3 targetScale = Vector3.zero;

            while (elapsedTime < 0.5f)
            {
                transform.localScale = Vector3.Lerp(startScale, targetScale, elapsedTime / 0.5f);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            transform.localScale = targetScale;
            Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("DroppingObject"))
            {
                MiniGameAudioController.Instance.PlayBlastAudio();
                DroppingGameController.Instance.OnTargetHit();
                other.GetComponent<DroppingObject>().Blast();
                Destroy(other.gameObject);
            }
        }
    }
}