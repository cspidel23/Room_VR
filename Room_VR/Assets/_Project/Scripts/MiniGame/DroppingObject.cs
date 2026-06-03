using UnityEngine;

namespace RoomVR.MiniGame
{
    public class DroppingObject : MonoBehaviour
    {
        [SerializeField] private GameObject m_BlastPrefab;

        private Rigidbody m_Rigidbody;

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Rigidbody.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
        }

        public void Blast()
        {
            Instantiate(m_BlastPrefab, transform.position, Quaternion.identity);
        }

    }
}