using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using RoomVR.MiniGame;

namespace RoomVR.Interaction
{
    // Starts the mini-game when BOTH hands grab the controller prop.
    // Attach transform logic is handled by TwoHandGrabInteractable.
    // If either hand releases, the remaining hand is force-released too.
    [RequireComponent(typeof(TwoHandGrabInteractable))]
    public class ControllerGrabHandler : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Left stick action (Vector2). Assign XRI Left/Thumbstick.")]
        InputActionReference m_StickAction;

        [SerializeField]
        [Tooltip("Fire action (Button). Assign XRI Left/Activate.")]
        InputActionReference m_TriggerAction;

        [SerializeField]
        MiniGameController m_MiniGame;

        [SerializeField]
        [Tooltip("Total targets to spawn when the game starts.")]
        int m_TotalTargets = 8;

        XRGrabInteractable m_Grab;
        readonly List<IXRSelectInteractor> m_CurrentInteractors = new();
        bool m_TriggerWasPressed;

        bool BothHandsGrabbing => m_CurrentInteractors.Count >= 2;

        void Awake()
        {
            m_Grab = GetComponent<XRGrabInteractable>();
            m_Grab.selectEntered.AddListener(OnSelectEntered);
            m_Grab.selectExited.AddListener(OnSelectExited);
        }

        void OnEnable()
        {
            m_StickAction?.action.Enable();
            m_TriggerAction?.action.Enable();
        }

        void OnDisable()
        {
            m_StickAction?.action.Disable();
            m_TriggerAction?.action.Disable();
        }

        void Update()
        {
            if (!BothHandsGrabbing || m_MiniGame == null)
                return;

            if (m_StickAction != null)
                m_MiniGame.MoveCrosshair(m_StickAction.action.ReadValue<Vector2>());

            if (m_TriggerAction != null)
            {
                var pressed = m_TriggerAction.action.IsPressed();
                if (pressed && !m_TriggerWasPressed)
                    m_MiniGame.DropBomb();
                m_TriggerWasPressed = pressed;
            }
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (!m_CurrentInteractors.Contains(args.interactorObject))
                m_CurrentInteractors.Add(args.interactorObject);

            if (BothHandsGrabbing && m_MiniGame != null && !m_MiniGame.IsActive)
                m_MiniGame.StartGame(m_TotalTargets);
        }

        void OnSelectExited(SelectExitEventArgs args)
        {
            m_CurrentInteractors.Remove(args.interactorObject);

            if (m_CurrentInteractors.Count > 0)
                ForceReleaseAll();
        }

        void ForceReleaseAll()
        {
            var toRelease = new List<IXRSelectInteractor>(m_CurrentInteractors);
            m_CurrentInteractors.Clear();
            foreach (var interactor in toRelease)
                m_Grab.interactionManager.SelectExit(interactor, m_Grab);
        }
    }
}
