using System;
using UnityEngine;

namespace RoomVR.MiniGame
{
    public class MiniGameController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The crosshair object that moves with stick input.")]
        Transform m_Crosshair;

        [SerializeField]
        [Tooltip("Movement speed of the crosshair in world units per second.")]
        float m_CrosshairSpeed = 3f;

        [SerializeField]
        [Tooltip("Half-size of the playfield. Crosshair is clamped within this boundary.")]
        float m_PlayfieldHalfSize = 4f;

        [SerializeField]
        [Tooltip("Radius within which targets are destroyed when a bomb drops.")]
        float m_BombRadius = 0.6f;

        [SerializeField]
        [Tooltip("The spawner that creates targets.")]
        FallingObjectSpawner m_Spawner;

        public event Action<int> OnScoreChanged;
        public event Action OnAllTargetsCleared;

        int m_Score;
        int m_TargetsRemaining;
        bool m_IsActive;

        public int Score => m_Score;
        public bool IsActive => m_IsActive;
        public Transform Crosshair => m_Crosshair;

        public void StartGame(int totalTargets)
        {
            m_Score = 0;
            m_TargetsRemaining = totalTargets;
            m_IsActive = true;

            if (m_Crosshair != null)
            {
                var pos = m_Crosshair.localPosition;
                m_Crosshair.localPosition = new Vector3(0f, pos.y, 0f);
            }

            m_Spawner?.StartSpawning(totalTargets);
        }

        public void StopGame()
        {
            m_IsActive = false;
            m_Spawner?.StopSpawning();
        }

        public void MoveCrosshair(Vector2 input)
        {
            if (!m_IsActive || m_Crosshair == null)
                return;

            var delta = new Vector3(input.x, 0f, input.y) * (m_CrosshairSpeed * Time.deltaTime);
            var pos = m_Crosshair.localPosition + delta;
            pos.x = Mathf.Clamp(pos.x, -m_PlayfieldHalfSize, m_PlayfieldHalfSize);
            pos.z = Mathf.Clamp(pos.z, -m_PlayfieldHalfSize, m_PlayfieldHalfSize);
            m_Crosshair.localPosition = pos;
        }

        public void DropBomb()
        {
            if (!m_IsActive || m_Crosshair == null)
                return;

            var bombPos = new Vector3(m_Crosshair.position.x, 0f, m_Crosshair.position.z);
            var hits = Physics.OverlapSphere(bombPos, m_BombRadius, LayerMask.GetMask("MiniGame"));

            foreach (var hit in hits)
            {
                var target = hit.GetComponentInParent<FallingTarget>();
                if (target != null)
                    target.TakeHit();
            }
        }

        public void RegisterTargetDestroyed()
        {
            m_Score++;
            m_TargetsRemaining--;
            OnScoreChanged?.Invoke(m_Score);

            if (m_TargetsRemaining <= 0)
            {
                m_IsActive = false;
                OnAllTargetsCleared?.Invoke();
            }
        }
    }
}
