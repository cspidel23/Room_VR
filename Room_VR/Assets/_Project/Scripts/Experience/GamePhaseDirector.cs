using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using RoomVR.MiniGame;

namespace RoomVR.Experience
{
    // Orchestrates the two-phase experience:
    //   Peaceful room  --(mini-game cleared)-->  War room  --(mini-game cleared)-->  Ending
    // Each clear fades to black, swaps the room state, then fades back in. The room
    // visuals are left as Inspector slots (GameObject groups + UnityEvent) so art can
    // be wired in later without touching this code.
    public class GamePhaseDirector : MonoBehaviour
    {
        public enum Phase { Peaceful, War, Ending }

        [Serializable]
        public class PhaseSetup
        {
            [Tooltip("GameObjects to activate when this phase begins.")]
            public GameObject[] enable;

            [Tooltip("GameObjects to deactivate when this phase begins.")]
            public GameObject[] disable;

            [Tooltip("Extra hook invoked when this phase begins.")]
            public UnityEvent onEnter;
        }

        [Header("References")]
        [SerializeField]
        [Tooltip("The active mini-game. Its OnGameCleared drives the phase transitions.")]
        MiniGameBase m_MiniGame;

        [SerializeField]
        [Tooltip("Screen fader for the black transitions. Use ScreenFader (quad) or PostProcessFader (covers everything).")]
        ScreenFaderBase m_Fader;

        [Header("Phase Visuals (wire art here)")]
        [SerializeField] PhaseSetup m_Peaceful;
        [SerializeField] PhaseSetup m_War;

        [Header("Ending")]
        [SerializeField]
        [Tooltip("Message object shown after the war phase is cleared. Hidden on start.")]
        GameObject m_EndingMessage;

        [SerializeField] UnityEvent m_OnEnding;

        [SerializeField]
        [Tooltip("Seconds the ending message stays fully shown before it fades out.")]
        float m_EndingHoldSeconds = 15f;

        [SerializeField]
        [Tooltip("Seconds to fade out the ending text, then the audio, before quitting.")]
        float m_EndingFadeDuration = 2f;

        [SerializeField]
        [Tooltip("CanvasGroup used to fade the ending message. Auto-added to the message if empty.")]
        CanvasGroup m_EndingCanvasGroup;

        [Header("Transition")]
        [SerializeField]
        [Tooltip("Disabled during transitions so the controller can't be grabbed/started mid-fade " +
                 "(e.g. the prop's XRGrabInteractable). Re-enabled when the next phase is playable.")]
        Behaviour[] m_DisableDuringTransition;

        [SerializeField] float m_FadeOutDuration = 1.2f;
        [SerializeField] float m_FadeInDuration = 1.2f;
        [SerializeField] float m_HoldBlack = 0.6f;

        [SerializeField]
        [Tooltip("Begin fully black and fade in on load.")]
        bool m_FadeInOnStart = true;

        [SerializeField]
        [Tooltip("Deactivated while the screen is black (e.g. the hand visuals) so nothing shows " +
                 "through the fade. Re-activated when fading back in.")]
        GameObject[] m_HideWhileBlack;

        [SerializeField]
        [Tooltip("Call MiniGame.ResetGame() when a new phase begins (while the screen is black).")]
        bool m_ResetMiniGameOnEnter = true;

        [Header("Transition Hooks (fully black)")]
        [SerializeField]
        [Tooltip("Invoked once the screen is fully black, before the room swaps. Good place to " +
                 "reset the controller / mini-game / layout while everything is hidden.")]
        UnityEvent m_OnFadedToBlack;

        [SerializeField]
        [Tooltip("Invoked after the new phase has fully faded back in.")]
        UnityEvent m_OnFadedIn;

        [Header("Cleared -> Transition")]
        [SerializeField]
        [Tooltip("After the game is cleared, transition this many seconds later at the latest.")]
        float m_ClearedHoldSeconds = 10f;

        [SerializeField]
        [Tooltip("Controller grab. Releasing it (placing it down) after a clear triggers the " +
                 "transition early. Leave empty to only use the timer.")]
        XRGrabInteractable m_ControllerGrab;

        // Global access so any script can query the current phase (matches the
        // Instance pattern used by AudioController / OutsideConnectionController etc.).
        public static GamePhaseDirector Instance { get; private set; }

        public Phase CurrentPhase { get; private set; } = Phase.Peaceful;
        public event Action<Phase> PhaseChanged;

        // Convenience checks. Static ones are null-safe so callers can write
        // GamePhaseDirector.InWar from anywhere.
        public bool IsWar => CurrentPhase == Phase.War;
        public bool IsPeaceful => CurrentPhase == Phase.Peaceful;
        public static bool InWar => Instance != null && Instance.CurrentPhase == Phase.War;
        public static bool InPeaceful => Instance != null && Instance.CurrentPhase == Phase.Peaceful;

        bool m_Busy;

        // After a clear we wait (timer or controller release) before actually transitioning.
        bool m_PendingTransition;
        float m_ClearedTime;

        void Awake()
        {
            if (Instance == null) Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            if (m_MiniGame != null) m_MiniGame.OnGameCleared += HandleCleared;
        }

        void OnDisable()
        {
            if (m_MiniGame != null) m_MiniGame.OnGameCleared -= HandleCleared;
        }

        void Start()
        {
            AudioListener.volume = 1f; // ensure a clean start (the ending fades this to 0)
            if (m_EndingMessage != null) m_EndingMessage.SetActive(false);

            ApplyPhase(m_Peaceful);
            CurrentPhase = Phase.Peaceful;
            PhaseChanged?.Invoke(CurrentPhase);

            if (m_FadeInOnStart && m_Fader != null)
                StartCoroutine(InitialReveal());
        }

        IEnumerator InitialReveal()
        {
            m_Fader.SetBlack(true);
            SetHidden(true);
            yield return null; // let the black hold for one frame
            yield return Fade(m_FadeInDuration, toBlack: false);
            SetHidden(false);
        }

        // The game raises this on clear. We don't transition immediately: instead we wait
        // until the player puts the controller down OR the hold timer elapses (Update).
        void HandleCleared()
        {
            if (m_Busy || m_PendingTransition) return;
            if (CurrentPhase == Phase.Ending) return;

            m_PendingTransition = true;
            m_ClearedTime = Time.time;
        }

        void Update()
        {
            if (!m_PendingTransition || m_Busy) return;

            var timeUp = Time.time - m_ClearedTime >= m_ClearedHoldSeconds;
            var released = m_ControllerGrab != null && !m_ControllerGrab.isSelected;
            if (!timeUp && !released) return;

            m_PendingTransition = false;
            if (CurrentPhase == Phase.Peaceful) StartCoroutine(TransitionToWar());
            else if (CurrentPhase == Phase.War) StartCoroutine(ShowEnding());
        }

        IEnumerator TransitionToWar()
        {
            m_Busy = true;
            SetTransitionLock(true);              // disable grab -> controller returns home
            if (m_MiniGame != null) m_MiniGame.StopGame();

            yield return Fade(m_FadeOutDuration, toBlack: true);

            // --- fully black: do everything hidden ---
            SetHidden(true);                      // hide the hand visuals etc.
            m_OnFadedToBlack?.Invoke();           // reset controller / layout hook
            ApplyPhase(m_War);                    // swap rooms + per-phase onEnter
            if (m_ResetMiniGameOnEnter && m_MiniGame != null) m_MiniGame.ResetGame();
            CurrentPhase = Phase.War;
            PhaseChanged?.Invoke(CurrentPhase);

            if (m_HoldBlack > 0f) yield return new WaitForSeconds(m_HoldBlack);

            SetHidden(false);                     // show the hands again
            yield return Fade(m_FadeInDuration, toBlack: false);

            SetTransitionLock(false);
            m_OnFadedIn?.Invoke();
            m_Busy = false;
        }

        IEnumerator ShowEnding()
        {
            m_Busy = true;
            SetTransitionLock(true);
            if (m_MiniGame != null) m_MiniGame.StopGame();

            yield return Fade(m_FadeOutDuration, toBlack: true);

            // Stay on black and reveal the message on top of it (per the concept:
            // fade to black, show the message, end). Keep the hands hidden.
            SetHidden(true);
            m_OnFadedToBlack?.Invoke();
            
            m_OnEnding?.Invoke();
            CurrentPhase = Phase.Ending;
            PhaseChanged?.Invoke(CurrentPhase);
            if (m_EndingMessage != null)
            {
                yield return new WaitForSeconds(10.0f);
                m_EndingMessage.SetActive(true);
                EnsureEndingCanvasGroup();
                if (m_EndingCanvasGroup != null) m_EndingCanvasGroup.alpha = 1f;
            }

            // Hold the message, then fade out the text, then the audio, then quit.
            yield return new WaitForSeconds(m_EndingHoldSeconds);
            yield return FadeOutEnding();
            QuitApplication();

            m_Busy = false;
        }

        void EnsureEndingCanvasGroup()
        {
            if (m_EndingCanvasGroup == null && m_EndingMessage != null)
            {
                m_EndingCanvasGroup = m_EndingMessage.GetComponent<CanvasGroup>();
                if (m_EndingCanvasGroup == null)
                    m_EndingCanvasGroup = m_EndingMessage.AddComponent<CanvasGroup>();
            }
        }

        // Fades the ending text out, then the remaining audio out.
        IEnumerator FadeOutEnding()
        {
            var dur = Mathf.Max(0.01f, m_EndingFadeDuration);

            if (m_EndingCanvasGroup != null)
            {
                var t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime / dur;
                    m_EndingCanvasGroup.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(t));
                    yield return null;
                }
                m_EndingCanvasGroup.alpha = 0f;
            }

            var startVol = AudioListener.volume;
            var a = 0f;
            while (a < 1f)
            {
                a += Time.deltaTime / dur;
                AudioListener.volume = Mathf.Lerp(startVol, 0f, Mathf.Clamp01(a));
                yield return null;
            }
            AudioListener.volume = 0f;
        }

        void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        Coroutine Fade(float duration, bool toBlack)
        {
            if (m_Fader == null) return null;
            return toBlack ? m_Fader.FadeOut(duration) : m_Fader.FadeIn(duration);
        }

        void ApplyPhase(PhaseSetup phase)
        {
            if (phase == null) return;

            if (phase.disable != null)
                foreach (var go in phase.disable)
                    if (go != null) go.SetActive(false);

            if (phase.enable != null)
                foreach (var go in phase.enable)
                    if (go != null) go.SetActive(true);

            phase.onEnter?.Invoke();
        }

        void SetTransitionLock(bool locked)
        {
            if (m_DisableDuringTransition == null) return;
            foreach (var b in m_DisableDuringTransition)
                if (b != null) b.enabled = !locked;
        }

        // Hide/show the objects that must not appear through the black fade (hands etc.).
        void SetHidden(bool hidden)
        {
            if (m_HideWhileBlack == null) return;
            foreach (var go in m_HideWhileBlack)
                if (go != null) go.SetActive(!hidden);
        }

        [ContextMenu("DEBUG: Clear Current Phase")]
        void DebugClearCurrentPhase() => HandleCleared();
    }
}
