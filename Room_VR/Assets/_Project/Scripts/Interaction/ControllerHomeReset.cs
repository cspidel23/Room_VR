using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace RoomVR.Interaction
{
    // Keeps the controller prop reachable: it never gets thrown, and whenever the
    // player lets go it returns to its authored desk pose. Without this, throwing or
    // dropping the controller sends it out of reach and the experience cannot continue.
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(Rigidbody))]
    public class ControllerHomeReset : MonoBehaviour
    {
        XRGrabInteractable m_Grab;
        Rigidbody m_Rigidbody;
        ControllerGrabHandler m_GrabHandler;

        Vector3 m_HomePosition;
        Quaternion m_HomeRotation;
        void Awake()
        {
            m_Grab = GetComponent<XRGrabInteractable>();
            m_Rigidbody = GetComponent<Rigidbody>();
            m_GrabHandler = GetComponent<ControllerGrabHandler>();
            m_Grab.throwOnDetach = false;
        }

        void Start()
        {
            // The authored scene placement is "home".
            m_HomePosition = transform.position;
            m_HomeRotation = transform.rotation;

            // Pin it in place while resting so gravity never drags it off the desk.
            FreezeAtRest();
        }

        // Releases any current grab and snaps the prop back to its home pose immediately.
        // Wire this into a phase transition (e.g. GamePhaseDirector War On Enter) so the
        // controller is let go and reset while hidden. Without the release, a held grab
        // would keep pulling the prop back to the hand and the snap would be undone.
        //
        // Releases via ControllerGrabHandler (sequential SelectExit) rather than the
        // manager's CancelInteractableSelection, which crashes on the two-hand prop
        // because ForceReleaseAll re-enters XRI's selection loop.
        public void ReturnHomeNow()
        {
            if (m_GrabHandler != null)
                m_GrabHandler.ForceRelease();
            else if (m_Grab != null && m_Grab.isSelected && m_Grab.interactionManager != null)
                m_Grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)m_Grab);

            transform.SetPositionAndRotation(m_HomePosition, m_HomeRotation);
            FreezeAtRest();
        }

        void FreezeAtRest()
        {
            m_Rigidbody.linearVelocity = Vector3.zero;
            m_Rigidbody.angularVelocity = Vector3.zero;
        }
    }
}
