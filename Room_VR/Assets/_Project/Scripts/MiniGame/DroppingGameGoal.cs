using System.Collections;
using UnityEngine;

namespace RoomVR.MiniGame
{
    public class DroppingGameGoal : MonoBehaviour
    {
        [SerializeField] private bool permanent = false;

        // Initial state so the target can be restored on ResetGame (it is hidden, not
        // destroyed, when hit — destroyed targets could not come back).
        private Vector3 m_InitialLocalScale;
        private Vector3 m_InitialLocalPosition;
        private Coroutine m_HideRoutine;

        private void Awake()
        {
            m_InitialLocalScale = transform.localScale;
            m_InitialLocalPosition = transform.localPosition;
        }

        private void Start()
        {
            DroppingGameController.Instance.RegisterGoal(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("DroppingObject"))
            {
                // if in a scene with no OutsideConnectionController, will simply not fire
                if (OutsideConnectionController.Instance != null)
                {
                    OutsideConnectionController.Instance.TriggerExplosionEffect();
                }

                AudioController.Instance.PlayDropAudio();
                DroppingGameController.Instance.OnTargetHit(1);
                Destroy(other.gameObject);
                if (!permanent)
                {
                    if (m_HideRoutine != null) StopCoroutine(m_HideRoutine);
                    m_HideRoutine = StartCoroutine(ScaleDownAndHide());
                }
            }
        }

        // Rapidly scales the target to zero (and sinks it) then hides it. Kept (not
        // destroyed) so ResetGoal can bring it back for the next round.
        private IEnumerator ScaleDownAndHide()
        {
            float elapsedTime = 0f;
            Vector3 startScale = transform.localScale;
            Vector3 targetScale = Vector3.zero;

            while (elapsedTime < 0.5f)
            {
                transform.position += Vector3.down * Time.deltaTime * 0.5f;
                transform.localScale = Vector3.Lerp(startScale, targetScale, elapsedTime / 0.5f);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            transform.localScale = targetScale;
            m_HideRoutine = null;
            gameObject.SetActive(false);
        }

        // Restores the target to its initial state for a new round.
        public void ResetGoal()
        {
            if (m_HideRoutine != null)
            {
                StopCoroutine(m_HideRoutine);
                m_HideRoutine = null;
            }
            transform.localPosition = m_InitialLocalPosition;
            transform.localScale = m_InitialLocalScale;
            gameObject.SetActive(true);
        }
    }
}
