using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using RoomVR.MiniGame;

namespace RoomVR.Interaction
{
    // Manages the physical grab of the controller prop and routes fixed
    // VR inputs (stick / fire button) to whatever MiniGameBase is assigned.
    // Bindings are configured directly on this component — no external InputActionAsset needed.
    [RequireComponent(typeof(TwoHandGrabInteractable))]
    public class ControllerGrabHandler : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField]
        [Tooltip("Left stick for crosshair / aiming. Default: left thumbstick.")]
        InputAction m_StickAction = new InputAction(
            name: "Stick",
            type: InputActionType.Value,
            binding: "<XRController>{LeftHand}/{Primary2DAxis}");

        [SerializeField]
        [Tooltip("Fire buttons. Configure bindings in the Inspector.")]
        InputAction m_FireAction = new InputAction(name: "Fire", type: InputActionType.Button);

        [Header("Game")]
        [SerializeField]
        [Tooltip("Any MiniGameBase implementation. Swap to change the active game.")]
        MiniGameBase m_MiniGame;

        XRGrabInteractable m_Grab;
        readonly List<IXRSelectInteractor> m_CurrentInteractors = new();
        bool m_FireWasPressed;

        bool BothHandsGrabbing => m_CurrentInteractors.Count >= 2;

        void Awake()
        {
            m_Grab = GetComponent<XRGrabInteractable>();
            m_Grab.selectEntered.AddListener(OnSelectEntered);
            m_Grab.selectExited.AddListener(OnSelectExited);


        }

        void OnEnable()
        {
            m_StickAction.Enable();
            m_FireAction.Enable();
        }

        void OnDisable()
        {
            m_StickAction.Disable();
            m_FireAction.Disable();
        }

        void Update()
        {
            if (!BothHandsGrabbing || m_MiniGame == null)
                return;

            m_MiniGame.OnStickInput(m_StickAction.ReadValue<Vector2>());

            var pressed = m_FireAction.IsPressed();
            if (pressed && !m_FireWasPressed)
                m_MiniGame.OnFireInput();
            m_FireWasPressed = pressed;
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (!m_CurrentInteractors.Contains(args.interactorObject))
                m_CurrentInteractors.Add(args.interactorObject);

            if (BothHandsGrabbing && m_MiniGame != null && !m_MiniGame.IsActive)
                m_MiniGame.StartGame();
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
