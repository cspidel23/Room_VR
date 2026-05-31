using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace RoomVR.Interaction
{
    // XRGrabInteractable that returns different attach transforms per hand.
    // GetAttachTransform is called every frame by XRI, so there are no timing issues.
    public class TwoHandGrabInteractable : XRGrabInteractable
    {
        [SerializeField]
        [Tooltip("Attach point used when the left hand grabs.")]
        Transform m_LeftHandAttach;

        [SerializeField]
        [Tooltip("Attach point used when the right hand grabs.")]
        Transform m_RightHandAttach;

        public override Transform GetAttachTransform(IXRInteractor interactor)
        {
            if (IsLeftHand(interactor) && m_LeftHandAttach != null)
                return m_LeftHandAttach;

            if (!IsLeftHand(interactor) && m_RightHandAttach != null)
                return m_RightHandAttach;

            return base.GetAttachTransform(interactor);
        }

        static bool IsLeftHand(IXRInteractor interactor)
        {
            var t = interactor.transform;
            while (t != null)
            {
                if (t.name.Contains("Left")) return true;
                if (t.name.Contains("Right")) return false;
                t = t.parent;
            }
            return false;
        }
    }
}
