using UnityEngine;

namespace RoomVR.MiniGame
{
    public class DroppingGameController : MiniGameBase
    {
        [SerializeField] private Transform m_Player;
        [SerializeField] private float m_PlayfieldHalfSize = 4.25f;

        [SerializeField] private GameObject m_dropPrefab;
        private float m_PlayerSpeed = 3f;
        private bool m_IsActive;
        private int m_Score;
        private bool m_QueneInput = false;
        public override bool IsActive => m_IsActive;

        private float timer = 0f;
        private float dropCooldown = 0.5f;


        private void Update()
        {
            // we want a timer cooldown to prevent the player from spamming drop
            timer += Time.deltaTime;
            if (m_QueneInput)
            {
                if (timer >= dropCooldown)
                {
                    m_QueneInput = false;
                    GameObject instance = Instantiate(m_dropPrefab, m_Player.position, Quaternion.identity);
                    timer = 0f;
                }
            }


        }
        public override void StartGame()
        {
            m_Score = 0;
            m_IsActive = true;
        }

        public override void StopGame()
        {
            m_IsActive = false;
        }

        // takes input x, y and converts to coordinates x, z
        public override void OnStickInput(Vector2 input)
        {
            if (!m_IsActive)
                return;

            var delta = new Vector3(input.x, 0f, 0f) * (m_PlayerSpeed * Time.deltaTime);
            var pos = m_Player.localPosition + delta;
            pos.x = Mathf.Clamp(pos.x, -m_PlayfieldHalfSize, m_PlayfieldHalfSize);
            pos.z = Mathf.Clamp(pos.z, -m_PlayfieldHalfSize, m_PlayfieldHalfSize);
            m_Player.localPosition = pos;
        }

        public override void OnFireInput()
        {
            if (!m_IsActive)
                return;
            // instantiate the drop prefab at the player's position
            m_QueneInput = true;
            // GameObject instance = Instantiate(m_dropPrefab, m_Player.position, Quaternion.identity);
        }


    }
}
