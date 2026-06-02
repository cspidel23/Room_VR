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
    // - Single hand grab  → grab pose on that hand only
    // - Both hands grab   → grab pose on both hands + game starts
    // - Any hand releases → force-release both hands + clear all poses
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

        [Header("Hand Pose")]
        [SerializeField]
        [Tooltip("HandPoseAnimator on the left hand model.")]
        HandPoseAnimator m_LeftHandPose;

        [SerializeField]
        [Tooltip("HandPoseAnimator on the right hand model.")]
        HandPoseAnimator m_RightHandPose;

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
            if (m_CurrentInteractors.Contains(args.interactorObject))
                return;

            m_CurrentInteractors.Add(args.interactorObject);

            // 掴んだ手にグラブポーズを適用（片手でも両手でも）
            GetHandPose(args.interactorObject)?.SetGripOverride(1f);

            if (BothHandsGrabbing && m_MiniGame != null && !m_MiniGame.IsActive)
                m_MiniGame.StartGame();
        }

        void OnSelectExited(SelectExitEventArgs args)
        {
            m_CurrentInteractors.Remove(args.interactorObject);

            // 離した手のポーズをクリア
            GetHandPose(args.interactorObject)?.ClearGripOverride();

            // 片手が離れたらもう片方も強制解放
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

        // インタラクターの階層名から左右どちらの HandPoseAnimator を返す
        HandPoseAnimator GetHandPose(IXRSelectInteractor interactor)
        {
            var t = interactor.transform;
            while (t != null)
            {
                if (t.name.Contains("Left"))  return m_LeftHandPose;
                if (t.name.Contains("Right")) return m_RightHandPose;
                t = t.parent;
            }
            return null;
        }
    }
}
