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
        [SerializeField]
        [Tooltip("Seconds to ease back to the home pose after release. 0 = snap instantly.")]
        float m_ReturnDuration = 0.35f;

        XRGrabInteractable m_Grab;
        Rigidbody m_Rigidbody;

        Vector3 m_HomePosition;
        Quaternion m_HomeRotation;
        Coroutine m_Returning;

        void Awake()
        {
            m_Grab = GetComponent<XRGrabInteractable>();
            m_Rigidbody = GetComponent<Rigidbody>();

            // Never fling the prop on release; it must stay on the desk.
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
            StopReturning();
        }

        void OnSelectExited(SelectExitEventArgs args)
        {
            // Only return once every hand has let go.
            if (m_Grab.interactorsSelecting.Count > 0) return;

            StopReturning();
            m_Returning = StartCoroutine(ReturnHome());
        }

        IEnumerator ReturnHome()
        {
            var startPos = transform.position;
            var startRot = transform.rotation;

            if (m_ReturnDuration > 0f)
            {
                var t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime / m_ReturnDuration;
                    var k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                    transform.SetPositionAndRotation(
                        Vector3.Lerp(startPos, m_HomePosition, k),
                        Quaternion.Slerp(startRot, m_HomeRotation, k));
                    yield return null;
                }
            }

            transform.SetPositionAndRotation(m_HomePosition, m_HomeRotation);
            FreezeAtRest();
            m_Returning = null;
        }

        // Snaps the prop back to its home pose immediately. Useful to wire into a phase
        // transition (e.g. GamePhaseDirector.OnFadedToBlack) so it resets while hidden.
        public void ReturnHomeNow()
        {
            StopReturning();
            transform.SetPositionAndRotation(m_HomePosition, m_HomeRotation);
            FreezeAtRest();
        }

        void StopReturning()
        {
            if (m_Returning == null) return;
            StopCoroutine(m_Returning);
            m_Returning = null;
        }

        void FreezeAtRest()
        {
            m_Rigidbody.linearVelocity = Vector3.zero;
            m_Rigidbody.angularVelocity = Vector3.zero;
            m_Rigidbody.isKinematic = true;
        }
    }
}
