using UnityEngine;

namespace RoomVR.MiniGame
{
    public class DroppingGameObstacle : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("DroppingObject"))
            {
                AudioController.Instance.PlayBlastAudio();
                DroppingGameController.Instance.OnTargetHit();
                other.GetComponent<DroppingObject>().Blast();
                Destroy(other.gameObject);
            }
        }
    }
}