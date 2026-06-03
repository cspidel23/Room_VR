using System.Collections;
using UnityEngine;

namespace RoomVR.MiniGame
{
    public class DroppingGameGoal : MonoBehaviour
    {
        [SerializeField] private bool permanent = false;

        private void Start()
        {
            DroppingGameController.Instance.RegisterGoal(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("DroppingObject"))
            {
                // if in a scene with no OutsideConnectionController, will simply not fire
                if(OutsideConnectionController.Instance != null)
                {
                    OutsideConnectionController.Instance.TriggerExplosionEffect();
                }
                
                AudioController.Instance.PlayDropAudio();
                DroppingGameController.Instance.OnTargetHit(2);
                Destroy(other.gameObject);
                if (!permanent)
                {
                    StartCoroutine(ScaleDownAndDestroy());
                }
            }
        }

        // coroutine to cal when this is triggered, it will rapidly scale the object to 0,0,0 and then destroy it
        private IEnumerator ScaleDownAndDestroy()
        {
            float elapsedTime = 0f;
            Vector3 startScale = transform.localScale;
            Vector3 targetScale = Vector3.zero;

            while (elapsedTime < 0.5f)
            {
                // also move down on y axis as it scales down
                transform.position += Vector3.down * Time.deltaTime * 0.5f;
                transform.localScale = Vector3.Lerp(startScale, targetScale, elapsedTime / 0.5f);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            transform.localScale = targetScale;
            Destroy(gameObject);
        }
    }
}