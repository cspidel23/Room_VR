using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace RoomVR.Interaction
{
    // Makes a single-hand grabbable object give the grabbing hand a custom grip shape.
    // Put this on an object that has an XRGrabInteractable: when grabbed, the hand forms
    // this object's HandGripPose; when released, the hand reverts. The pose is tunable in
    // the Inspector per object, just like the controller's grab pose.
    [RequireComponent(typeof(XRGrabInteractable))]
    public class GrabbableHandPose : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Hand shape applied to whichever hand grabs this object.")]
        HandGripPose m_Pose = new HandGripPose();

        [SerializeField]
        [Tooltip("Don't fling the object on release (place / drop in front instead).")]
        bool m_DisableThrow = true;

        XRGrabInteractable m_Grab;
        HandPoseAnimator m_ActiveHand;

        void Awake()
        {
            m_Grab = GetComponent<XRGrabInteractable>();
            if (m_DisableThrow) m_Grab.throwOnDetach = false;
        }

        void OnEnable()
        {
            m_Grab.selectEntered.AddListener(OnSelectEntered);
            m_Grab.selectExited.AddListener(OnSelectExited);
        }

        void OnDisable()
        {
            m_Grab.selectEntered.RemoveListener(OnSelectEntered);
            m_Grab.selectExited.RemoveListener(OnSelectExited);
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            var hand = HandPoseAnimator.ResolveFromInteractor(args.interactorObject.transform);
            if (hand == null) return;

            m_ActiveHand = hand;
            hand.SetGripPose(m_Pose);
            hand.SetGripOverride(1f);
        }

        void OnSelectExited(SelectExitEventArgs args)
        {
            if (m_ActiveHand == null) return;

            m_ActiveHand.ClearGripOverride();
            m_ActiveHand.ClearGripPose();
            m_ActiveHand = null;
        }
    }
}
