using UnityEngine;

namespace RoomVR.MiniGame
{
    [RequireComponent(typeof(Collider))]
    public class FallingTarget : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Movement speed in world units per second.")]
        float m_Speed = 1.2f;

        [SerializeField]
        [Tooltip("Half-size boundary. Target bounces when reaching this limit.")]
        float m_BoundaryHalfSize = 3.8f;

        MiniGameController m_Controller;
        Vector3 m_MoveDir;

        public void Initialize(MiniGameController controller)
        {
            m_Controller = controller;
            var angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            m_MoveDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)).normalized;
        }

        void Update()
        {
            var pos = transform.localPosition + m_MoveDir * (m_Speed * Time.deltaTime);

            if (Mathf.Abs(pos.x) >= m_BoundaryHalfSize)
            {
                m_MoveDir.x = -m_MoveDir.x;
                pos.x = Mathf.Clamp(pos.x, -m_BoundaryHalfSize, m_BoundaryHalfSize);
            }

            if (Mathf.Abs(pos.z) >= m_BoundaryHalfSize)
            {
                m_MoveDir.z = -m_MoveDir.z;
                pos.z = Mathf.Clamp(pos.z, -m_BoundaryHalfSize, m_BoundaryHalfSize);
            }

            transform.localPosition = pos;
        }

        public void TakeHit()
        {
            m_Controller?.RegisterTargetDestroyed();
            Destroy(gameObject);
        }
    }
}
