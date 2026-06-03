using UnityEngine;

namespace RoomVR.MiniGame
{
    public class DroppingGamePlatform : MonoBehaviour
    {
        [SerializeField] private float m_MoveSpeed = 2f;

        // this platform will move back and forth on the x axis, we want to clamp it to a certain range
        [SerializeField] private float m_MoveRange = 4f;
        private float m_Direction = 1f;
        private void Awake()
        {
            // randomly set the starting direction to left or right
            m_Direction = Random.value < 0.5f ? -1f : 1f;
        }

        private void Start()
        {
            DroppingGameController.Instance.RegisterPlatform(this);
        }

        private void Update()
        {
            var pos = transform.localPosition;
            pos.x += m_Direction * m_MoveSpeed * Time.deltaTime;
            if (Mathf.Abs(pos.x) >= m_MoveRange)
            {
                pos.x = Mathf.Clamp(pos.x, -m_MoveRange, m_MoveRange);
                m_Direction *= -1f;
            }
            transform.localPosition = pos;
        }

        public void IncreaseSpeed(float amount, bool isMultiplier = false)
        {
            if (!isMultiplier)
            {
                m_MoveSpeed += amount;
            }
            else
            {
                m_MoveSpeed *= amount;
            }
        }
    }
}