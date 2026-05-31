using UnityEngine;

namespace RoomVR.MiniGame
{
    public class MiniGameController : MiniGameBase
    {
        [Header("Crosshair")]
        [SerializeField]
        Transform m_Crosshair;

        [SerializeField]
        float m_CrosshairSpeed = 3f;

        [SerializeField]
        float m_PlayfieldHalfSize = 4f;

        [Header("Hit Detection")]
        [SerializeField]
        [Tooltip("Overlap radius for hit detection. Should roughly match crosshair + target visual size.")]
        float m_BombRadius = 0.35f;

        [Header("Game Config")]
        [SerializeField]
        int m_TotalTargets = 8;

        [SerializeField]
        FallingObjectSpawner m_Spawner;

        bool m_IsActive;
        int m_Score;
        int m_TargetsRemaining;

        public override bool IsActive => m_IsActive;

        public override void StartGame()
        {
            m_Score = 0;
            m_TargetsRemaining = m_TotalTargets;
            m_IsActive = true;

            if (m_Crosshair != null)
            {
                var pos = m_Crosshair.localPosition;
                m_Crosshair.localPosition = new Vector3(0f, pos.y, 0f);
            }

            m_Spawner?.StartSpawning(m_TotalTargets);
        }

        public override void StopGame()
        {
            m_IsActive = false;
            m_Spawner?.StopSpawning();
        }

        public override void OnStickInput(Vector2 input)
        {
            if (!m_IsActive || m_Crosshair == null)
                return;

            var delta = new Vector3(input.x, 0f, input.y) * (m_CrosshairSpeed * Time.deltaTime);
            var pos = m_Crosshair.localPosition + delta;
            pos.x = Mathf.Clamp(pos.x, -m_PlayfieldHalfSize, m_PlayfieldHalfSize);
            pos.z = Mathf.Clamp(pos.z, -m_PlayfieldHalfSize, m_PlayfieldHalfSize);
            m_Crosshair.localPosition = pos;
        }

        public override void OnFireInput()
        {
            if (!m_IsActive || m_Crosshair == null)
                return;

            // Use the crosshair's actual world position so targets at Y=50 are detected.
            var hits = Physics.OverlapSphere(m_Crosshair.position, m_BombRadius, LayerMask.GetMask("MiniGame"));
            foreach (var hit in hits)
                hit.GetComponentInParent<FallingTarget>()?.TakeHit();
        }

        public void RegisterTargetDestroyed()
        {
            m_Score++;
            m_TargetsRemaining--;
            RaiseScoreChanged(m_Score);

            if (m_TargetsRemaining <= 0)
            {
                m_IsActive = false;
                RaiseGameCleared();
            }
        }
    }
}
