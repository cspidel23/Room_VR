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
    // - Both hands grab   → grab pose on both hands + game starts + hands locked to prop
    // - Any hand releases → force-release both hands + clear all poses + hands restored
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

        [Header("Two-Hand Visual Alignment")]
        [SerializeField]
        [Tooltip("Root transform of the left hand model. Locked to LeftHandAttach during two-hand grab.")]
        Transform m_LeftHandModel;

        [SerializeField]
        [Tooltip("Root transform of the right hand model. Locked to RightHandAttach during two-hand grab.")]
        Transform m_RightHandModel;

        [Header("Game")]
        [SerializeField]
        [Tooltip("Any MiniGameBase implementation. Swap to change the active game.")]
        MiniGameBase m_MiniGame;

        TwoHandGrabInteractable m_TwoHandGrab;
        XRGrabInteractable m_Grab;
        readonly List<IXRSelectInteractor> m_CurrentInteractors = new();
        bool m_FireWasPressed;

        HandLock m_Left;
        HandLock m_Right;
        bool m_HandsLocked;

        bool BothHandsGrabbing => m_CurrentInteractors.Count >= 2;

        // True while at least one hand is holding the prop. Used by ControllerModelAnimator
        // to only animate buttons / stick while the controller is actually held.
        public bool IsGrabbed => m_CurrentInteractors.Count > 0;

        void Awake()
        {
            m_TwoHandGrab = GetComponent<TwoHandGrabInteractable>();
            m_Grab = GetComponent<XRGrabInteractable>();
            m_Grab.selectEntered.AddListener(OnSelectEntered);
            m_Grab.selectExited.AddListener(OnSelectExited);

            // Capture each hand model's authored parent + local pose so it can be
            // restored exactly when the prop is released.
            m_Left  = new HandLock(m_LeftHandPose,  m_LeftHandModel,  m_TwoHandGrab.LeftHandAttach);
            m_Right = new HandLock(m_RightHandPose, m_RightHandModel, m_TwoHandGrab.RightHandAttach);
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

        void LateUpdate()
        {
            if (!m_HandsLocked) return;

            // While locked, the hand models are detached to the scene root (unit scale),
            // so writing their world pose here sticks without being overwritten by the
            // controllers' onBeforeRender tracking update. They follow the prop's attach
            // frame so the held pose stays correct even when the real controllers diverge.
            m_Left.FollowAttach();
            m_Right.FollowAttach();
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (m_CurrentInteractors.Contains(args.interactorObject))
                return;

            m_CurrentInteractors.Add(args.interactorObject);

            var hand = GetHandLock(args.interactorObject);
            if (hand != null)
            {
                // 掴んだ手にグラブポーズを適用（片手でも両手でも）
                hand.Pose?.SetGripOverride(1f);
                // The offset between the hand model and the interactor's attach transform
                // is constant (both are rigid children of the controller), so capturing it
                // here lets two-hand grab reproduce the exact single-hand visual.
                hand.CaptureOffset(args.interactorObject.GetAttachTransform(args.interactableObject));
            }

            if (BothHandsGrabbing && !m_HandsLocked)
            {
                m_Left.Lock();
                m_Right.Lock();
                m_HandsLocked = true;
            }

            if (BothHandsGrabbing && m_MiniGame != null && !m_MiniGame.IsActive)
                m_MiniGame.StartGame();
        }

        void OnSelectExited(SelectExitEventArgs args)
        {
            m_CurrentInteractors.Remove(args.interactorObject);

            // 離した手のポーズをクリア
            GetHandLock(args.interactorObject)?.Pose?.ClearGripOverride();

            if (m_HandsLocked)
            {
                m_Left.Unlock();
                m_Right.Unlock();
                m_HandsLocked = false;
            }

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

        // インタラクターの階層名から左右どちらの HandLock を返す
        HandLock GetHandLock(IXRSelectInteractor interactor)
        {
            var t = interactor.transform;
            while (t != null)
            {
                if (t.name.Contains("Left"))  return m_Left;
                if (t.name.Contains("Right")) return m_Right;
                t = t.parent;
            }
            return null;
        }

        // Holds one hand model's authored state and the captured grab offset, and
        // drives it onto the prop's attach frame while both hands are grabbing.
        sealed class HandLock
        {
            public HandPoseAnimator Pose { get; }

            readonly Transform m_Model;
            readonly Transform m_Attach;

            readonly Transform m_OriginalParent;
            readonly Vector3 m_OriginalLocalPos;
            readonly Quaternion m_OriginalLocalRot;
            readonly Vector3 m_OriginalLocalScale;

            // Rigid offset of the hand model relative to the attach frame (scale ignored).
            Vector3 m_OffsetPos;
            Quaternion m_OffsetRot = Quaternion.identity;
            bool m_HasOffset;

            public HandLock(HandPoseAnimator pose, Transform model, Transform attach)
            {
                Pose = pose;
                m_Model = model;
                m_Attach = attach;

                if (m_Model != null)
                {
                    m_OriginalParent = m_Model.parent;
                    m_OriginalLocalPos = m_Model.localPosition;
                    m_OriginalLocalRot = m_Model.localRotation;
                    m_OriginalLocalScale = m_Model.localScale;
                }
            }

            // Records the hand model pose relative to the interactor's attach transform.
            // Treated as a rigid (position + rotation only) offset so the prop's
            // non-uniform scale never distorts the hand.
            public void CaptureOffset(Transform interactorAttach)
            {
                if (m_Model == null || interactorAttach == null) return;

                var invRot = Quaternion.Inverse(interactorAttach.rotation);
                m_OffsetPos = invRot * (m_Model.position - interactorAttach.position);
                m_OffsetRot = invRot * m_Model.rotation;
                m_HasOffset = true;
            }

            // Detach to the scene root (unit scale) so the controller's tracking update
            // can no longer drag the hand model around. World pose is preserved to avoid
            // a one-frame pop; LateUpdate then drives it onto the attach frame.
            public void Lock()
            {
                if (m_Model == null) return;
                m_Model.SetParent(null, worldPositionStays: true);
            }

            // Restore the authored parent and local pose so the next single-hand grab
            // (and the resting hand) keep their original orientation.
            public void Unlock()
            {
                if (m_Model == null) return;
                m_Model.SetParent(m_OriginalParent, worldPositionStays: false);
                m_Model.localPosition = m_OriginalLocalPos;
                m_Model.localRotation = m_OriginalLocalRot;
                m_Model.localScale = m_OriginalLocalScale;
            }

            public void FollowAttach()
            {
                if (m_Model == null || m_Attach == null || !m_HasOffset) return;

                // Use the attach point's position + rotation as a rigid frame (its
                // lossyScale is non-uniform from the prop and must be ignored).
                var rot = m_Attach.rotation;
                m_Model.SetPositionAndRotation(
                    m_Attach.position + rot * m_OffsetPos,
                    rot * m_OffsetRot);
            }
        }
    }
}
