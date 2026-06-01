using UnityEngine;

namespace RoomVR.MiniGame
{
    public class DroppingGameGoal : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("DroppingObject"))
            {
                DroppingGameController.Instance.OnTargetHit();
                Destroy(other.gameObject);
            }
        }
    }
}