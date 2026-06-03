using UnityEngine;
using UnityEngine.InputSystem;

namespace RoomVR.Interaction
{
    // Animates the held controller prop's model parts (circle button, cross button,
    // thumbstick) from VR input, and drives the matching thumb on the hand models so
    // the fingers move together with the parts.
    //
    // Mapping (decided with the team):
    //   Circle (○) ← right controller A / PrimaryButton  → right hand thumb
    //   Cross  (×) ← right controller B / SecondaryButton → right hand thumb
    //   Stick      ← left controller thumbstick           → left hand thumb
    //
    // Input bindings are baked into the InputAction fields below (same approach as
    // ControllerGrabHandler), so no binding setup is needed in the Inspector.
    //
    // Sub-mesh nodes of the imported PS5 model have generic names (polySurface*, pCube*),
    // so the button / stick transforms must be assigned by hand in the Inspector.
    //
    // Reference: XRI Starter Assets "ControllerAnimator".
    public class ControllerModelAnimator : MonoBehaviour
    {
        [Header("Grab Gate")]
        [SerializeField]
        [Tooltip("Optional. When set, parts and fingers only animate while the prop is grabbed.")]
        ControllerGrabHandler m_GrabHandler;

        // ── Input ─────────────────────────────────────────────────────────────────

        [Header("Input")]
        [SerializeField]
        [Tooltip("Circle (○) button. Default: right controller A / PrimaryButton.")]
        InputAction m_CircleInput = new InputAction(
            name: "Circle",
            type: InputActionType.Value,
            binding: "<XRController>{RightHand}/{PrimaryButton}",
            expectedControlType: "Button");

        [SerializeField]
        [Tooltip("Cross (×) button. Default: right controller B / SecondaryButton.")]
        InputAction m_CrossInput = new InputAction(
            name: "Cross",
            type: InputActionType.Value,
            binding: "<XRController>{RightHand}/secondaryButton",
            expectedControlType: "Button");

        [SerializeField]
        [Tooltip("Thumbstick. Default: left controller Primary2DAxis.")]
        InputAction m_StickInput = new InputAction(
            name: "ModelStick",
            type: InputActionType.Value,
            binding: "<XRController>{LeftHand}/{Primary2DAxis}",
            expectedControlType: "Vector2");

        // ── Buttons ─────────────────────────────────────────────────────────────────

        [Header("Circle Button (○)")]
        [SerializeField]
        [Tooltip("Model sub-mesh that visually represents the circle button.")]
        Transform m_CircleButton;

        [Header("Cross Button (×)")]
        [SerializeField]
        [Tooltip("Model sub-mesh that visually represents the cross button.")]
        Transform m_CrossButton;

        [SerializeField]
        [Tooltip("Local-space offset applied to a button when fully pressed.")]
        Vector3 m_ButtonPressedOffset = new Vector3(0f, -0.02f, 0f);

        // ── Stick ─────────────────────────────────────────────────────────────────

        [Header("Thumbstick")]
        [SerializeField]
        [Tooltip("Model sub-mesh that visually represents the thumbstick.")]
        Transform m_Stick;

        [SerializeField]
        [Tooltip("Max tilt of the stick in degrees (x = pitch from stick.y, y = roll from stick.x).")]
        Vector2 m_StickRotationRange = new Vector2(20f, 20f);

        [SerializeField]
        [Tooltip("Rotate the stick around its mesh center instead of its (often off-center) pivot.")]
        bool m_StickRotateAroundCenter = true;

        [SerializeField]
        [Tooltip("Extra pivot nudge in the stick's parent-local space (used to fine-tune the center).")]
        Vector3 m_StickPivotOffset = Vector3.zero;

        // ── Finger Sync ─────────────────────────────────────────────────────────────

        [Header("Finger Sync")]
        [SerializeField]
        [Tooltip("Hand that presses the buttons (right). Its thumb curls when ○ / × are pressed.")]
        HandPoseAnimator m_ButtonHand;

        [SerializeField]
        [Tooltip("Hand that uses the stick (left). Its thumb tilts with the stick.")]
        HandPoseAnimator m_StickHand;

        [SerializeField]
        [Tooltip("Extra thumb curl angle (degrees) when a button is fully pressed.")]
        float m_ThumbButtonCurlAngle = 18f;

        [SerializeField]
        [Tooltip("Local axis the thumb curls around for button presses.")]
        Vector3 m_ThumbButtonCurlAxis = Vector3.right;

        [SerializeField]
        [Tooltip("Max thumb tilt (degrees) following the stick (x from stick.y, y from stick.x).")]
        Vector2 m_ThumbStickTiltAngle = new Vector2(12f, 12f);

        // ── State ─────────────────────────────────────────────────────────────────

        Vector3 m_CircleRestPos;
        Vector3 m_CrossRestPos;
        Vector3 m_StickRestPos;
        Quaternion m_StickRestRot;
        Vector3 m_StickPivotLocal;

        bool ShouldAnimate => m_GrabHandler == null || m_GrabHandler.IsGrabbed;

        void Awake()
        {
            if (m_CircleButton != null) m_CircleRestPos = m_CircleButton.localPosition;
            if (m_CrossButton != null) m_CrossRestPos = m_CrossButton.localPosition;
            if (m_Stick != null)
            {
                m_StickRestPos = m_Stick.localPosition;
                m_StickRestRot = m_Stick.localRotation;
                m_StickPivotLocal = ComputeStickPivotLocal();
            }
        }

        // Pivot point (in the stick's parent-local space) to rotate the stick around.
        // Defaults to the combined renderer bounds center so an off-center mesh pivot
        // does not make the stick swing oddly.
        Vector3 ComputeStickPivotLocal()
        {
            var pivot = m_StickRestPos;

            if (m_StickRotateAroundCenter)
            {
                var renderers = m_Stick.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    var bounds = renderers[0].bounds;
                    for (var i = 1; i < renderers.Length; i++)
                        bounds.Encapsulate(renderers[i].bounds);

                    pivot = m_Stick.parent != null
                        ? m_Stick.parent.InverseTransformPoint(bounds.center)
                        : bounds.center;
                }
            }

            return pivot + m_StickPivotOffset;
        }

        void OnEnable()
        {
            m_CircleInput.Enable();
            m_CrossInput.Enable();
            m_StickInput.Enable();
        }

        void OnDisable()
        {
            m_CircleInput.Disable();
            m_CrossInput.Disable();
            m_StickInput.Disable();
            ResetToRest();
        }

        void Update()
        {
            if (!ShouldAnimate)
            {
                ResetToRest();
                return;
            }

            var circle = m_CircleInput.ReadValue<float>();
            var cross = m_CrossInput.ReadValue<float>();
            var stick = m_StickInput.ReadValue<Vector2>();

            ApplyButton(m_CircleButton, m_CircleRestPos, circle);
            ApplyButton(m_CrossButton, m_CrossRestPos, cross);
            ApplyStick(stick);

            // Thumb follows whichever input that hand owns.
            var buttonPress = Mathf.Max(circle, cross);
            m_ButtonHand?.SetThumbOverlay(
                Quaternion.AngleAxis(buttonPress * m_ThumbButtonCurlAngle, m_ThumbButtonCurlAxis));
            m_StickHand?.SetThumbOverlay(
                Quaternion.Euler(-stick.y * m_ThumbStickTiltAngle.x, 0f, stick.x * m_ThumbStickTiltAngle.y));
        }

        void ApplyButton(Transform button, Vector3 restPos, float pressed)
        {
            if (button == null) return;
            button.localPosition = restPos + m_ButtonPressedOffset * Mathf.Clamp01(pressed);
        }

        void ApplyStick(Vector2 stick)
        {
            if (m_Stick == null) return;

            // Tilt around the stick's own axes (preserves the intuitive direction)...
            var newRot = m_StickRestRot *
                Quaternion.Euler(-stick.y * m_StickRotationRange.x, 0f, stick.x * m_StickRotationRange.y);

            // ...but pivot the whole part about the mesh center so it does not swing
            // off its (off-center) transform origin.
            var deltaParent = newRot * Quaternion.Inverse(m_StickRestRot);
            m_Stick.localRotation = newRot;
            m_Stick.localPosition = m_StickPivotLocal + deltaParent * (m_StickRestPos - m_StickPivotLocal);
        }

        void ResetToRest()
        {
            if (m_CircleButton != null) m_CircleButton.localPosition = m_CircleRestPos;
            if (m_CrossButton != null) m_CrossButton.localPosition = m_CrossRestPos;
            if (m_Stick != null)
            {
                m_Stick.localPosition = m_StickRestPos;
                m_Stick.localRotation = m_StickRestRot;
            }
            m_ButtonHand?.SetThumbOverlay(Quaternion.identity);
            m_StickHand?.SetThumbOverlay(Quaternion.identity);
        }
    }
}
