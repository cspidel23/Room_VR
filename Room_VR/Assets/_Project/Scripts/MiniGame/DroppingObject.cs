using UnityEngine;

namespace RoomVR.MiniGame
{
    public class DroppingObject : MonoBehaviour
    {
        private Rigidbody m_Rigidbody;
        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Rigidbody.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
        }

        private void Update()
        {
            // prevent the object from changing its position on the y axis

        }
    }
}