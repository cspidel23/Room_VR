using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace RoomVR.MiniGame
{
    public class DroppingGameController : MiniGameBase
    {
        public static DroppingGameController Instance { get; private set; }
        [SerializeField] private bool m_InvertControls = false;
        [SerializeField] private Transform m_Player;
        [SerializeField] private float m_PlayfieldHalfSize = 4.25f;

        [SerializeField] private GameObject m_dropPrefab;

        private List<DroppingGamePlatform> m_Platforms = new List<DroppingGamePlatform>();
        private List<DroppingGameGoal> m_Goals = new List<DroppingGameGoal>();

        private float m_PlayerSpeed = 3f;
        private bool m_IsActive;
        private int m_Score;
        private bool m_QueneInput = false;
        public override bool IsActive => m_IsActive;

        private float timer = 0f;
        private float dropCooldown = 0.5f;

        private float animTimer = 0f;
        [SerializeField] private float animCooldown = 0.25f; // add up to an additional .25 range by random

        private int score;
        [SerializeField] private TextMeshProUGUI m_ScoreText;

        [SerializeField] private GameObject[] m_PlayerSpritesLeft;
        [SerializeField] private GameObject[] m_PlayerSpritesRight;
        private bool isMovingLeft = false;
        private int m_CurrentSpriteIndex = 0;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }
        private void Start()
        {
            UpdateScoreText();
        }
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
            // if isMovingLeft is true, use the left sprites
            // go through timer, at end randomize the sprite index to 0 or 1, then reset timer
            animTimer += Time.deltaTime;
            if (animTimer >= animCooldown)
            {
                m_CurrentSpriteIndex = Random.Range(0, 2);
                animTimer = 0f;
            }
            // turn all sprites off
            foreach (var sprite in m_PlayerSpritesLeft)
            {
                sprite.SetActive(false);
            }
            foreach (var sprite in m_PlayerSpritesRight)
            {
                sprite.SetActive(false);
            }
            if (isMovingLeft)
            {
                m_PlayerSpritesLeft[m_CurrentSpriteIndex].SetActive(true);
            }
            else
            {
                m_PlayerSpritesRight[m_CurrentSpriteIndex].SetActive(true);
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
            if (m_InvertControls)
            {
                delta = -delta;
            }
            var pos = m_Player.localPosition + delta;
            pos.x = Mathf.Clamp(pos.x, -m_PlayfieldHalfSize, m_PlayfieldHalfSize);
            pos.z = Mathf.Clamp(pos.z, -m_PlayfieldHalfSize, m_PlayfieldHalfSize);
            m_Player.localPosition = pos;
            // if moving left should use the left sprites, when moving right use the right sprites

            if (input.x < 0)
            {
                isMovingLeft = true;
            }
            else if (input.x > 0)
            {
                isMovingLeft = false;
            }


        }

        public override void OnFireInput()
        {
            if (!m_IsActive)
                return;
            // instantiate the drop prefab at the player's position
            m_QueneInput = true;
            // GameObject instance = Instantiate(m_dropPrefab, m_Player.position, Quaternion.identity);
        }

        public void OnTargetHit(int scoreToAdd = 0)
        {
            m_Score += scoreToAdd;
            if (m_Score < 0)
            {
                m_Score = 0;
            }
            UpdateScoreText();

            if (scoreToAdd > 0)
            {
                LevelDifficultyChange();
            }

            if (m_Score >= m_Goals.Count)
            {
                Debug.Log("Game Cleared");
                RaiseGameCleared();
            }
        }

        private void UpdateScoreText()
        {
            if (m_ScoreText != null)
            {
                // set to at least 3 digits with leading zeros
                m_ScoreText.text = m_Score.ToString("D3");
            }
        }

        private void LevelDifficultyChange()
        {
            foreach (var platform in m_Platforms)
            {
                platform.IncreaseSpeed(1.2f, true);
            }
        }

        public void RegisterPlatform(DroppingGamePlatform platform)
        {
            if (!m_Platforms.Contains(platform))
            {
                m_Platforms.Add(platform);
            }
        }

        public void RegisterGoal(DroppingGameGoal goal)
        {
            if (!m_Goals.Contains(goal))
            {
                m_Goals.Add(goal);
            }
        }


    }
}
