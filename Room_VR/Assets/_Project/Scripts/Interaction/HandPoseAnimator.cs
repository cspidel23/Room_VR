using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

namespace RoomVR.Interaction
{
    // Drives hand bone rotations based on controller Grip / Trigger input.
    //
    // Two separate poses are supported:
    //   Air grip  : fingers curl proportionally to Grip/Trigger input (maxCurlAngle)
    //   Grab pose : fixed finger angles + wrist orientation for holding the controller prop
    //
    // Workflow for grab pose adjustment:
    //   1. Enter Play Mode and grab the controller prop
    //   2. Edit "Grab Wrist Pose > Rotation Euler" and each finger's "Grab Curl Angle" live
    //   3. Right-click this component → "Copy Component"
    //   4. Stop Play Mode → Right-click → "Paste Component Values"  ← saves all values
    //   OR: Right-click → "Log Current Grab Pose Values" to see values in Console
    public class HandPoseAnimator : MonoBehaviour
    {
        // ── Finger Bones ─────────────────────────────────────────────────────────

        [Serializable]
        public struct JointGrabPose
        {
            [Tooltip("Curl axis (orientation) for this joint during grab. Zero = fall back to the finger's curlAxis / grabCurlAngle.")]
            public Vector3 curlAxis;

            [Tooltip("Curl angle for this joint during grab (degrees). Acts as the per-joint weight/amount.")]
            [Range(-180f, 180f)]
            public float curlAngle;
        }

        [Serializable]
        public struct FingerBones
        {
            [Tooltip("Joints from proximal to distal.")]
            public Transform[] joints;

            [Tooltip("Axis around which joints curl. Usually Vector3.right for fingers.")]
            public Vector3 curlAxis;

            [Tooltip("Max curl angle for air grip (proportional to button pressure).")]
            [Range(0f, 180f)]
            public float maxCurlAngle;

            [Tooltip("Fixed curl angle when holding the controller prop. 0 = use maxCurlAngle.")]
            [Range(0f, 180f)]
            public float grabCurlAngle;

            [Tooltip("Optional per-joint grab pose. Each entry matches 'joints' by index; when its " +
                     "curlAxis is non-zero it overrides curlAxis/grabCurlAngle for that joint only. " +
                     "Use this to fine-tune each joint's bend and orientation (e.g. the thumb resting on the stick).")]
            public JointGrabPose[] grabJoints;
        }

        [Header("Finger Bones")]
        [SerializeField] FingerBones m_Thumb  = new FingerBones { curlAxis = Vector3.forward, maxCurlAngle = 60f, grabCurlAngle = 45f };
        [SerializeField] FingerBones m_Index  = new FingerBones { curlAxis = Vector3.right,   maxCurlAngle = 80f, grabCurlAngle = 50f };
        [SerializeField] FingerBones m_Middle = new FingerBones { curlAxis = Vector3.right,   maxCurlAngle = 80f, grabCurlAngle = 70f };
        [SerializeField] FingerBones m_Ring   = new FingerBones { curlAxis = Vector3.right,   maxCurlAngle = 80f, grabCurlAngle = 70f };
        [SerializeField] FingerBones m_Pinky  = new FingerBones { curlAxis = Vector3.right,   maxCurlAngle = 80f, grabCurlAngle = 70f };

        // ── Wrist Grab Pose ───────────────────────────────────────────────────────

        [Serializable]
        public struct WristGrabPose
        {
            [Tooltip("Euler angle offset applied to the wrist's resting orientation during grab.")]
            public Vector3 rotationEuler;

            [Tooltip("Local position offset from the wrist's resting position during grab.")]
            public Vector3 positionOffset;
        }

        [Header("Wrist Grab Pose")]
        [SerializeField]
        [Tooltip("The wrist root bone (e.g. L_Wrist). Leave empty to skip wrist override.")]
        Transform m_Wrist;

        [SerializeField]
        [Tooltip("Wrist orientation and position used when holding the controller prop.")]
        WristGrabPose m_GrabWristPose;

        // ── Input ─────────────────────────────────────────────────────────────────

        [Header("Input")]
        [SerializeField]
        XRInputValueReader<float> m_GripInput = new XRInputValueReader<float>("Grip");

        [SerializeField]
        XRInputValueReader<float> m_TriggerInput = new XRInputValueReader<float>("Trigger");

        // ── Runtime state ─────────────────────────────────────────────────────────

        float m_GripOverride = -1f;

        // Extra local rotation applied to the thumb's proximal joint on top of the
        // grab pose, driven externally (e.g. by ControllerModelAnimator) so the thumb
        // reacts to button presses / stick movement. Identity = no extra motion.
        Quaternion m_ThumbOverlay = Quaternion.identity;

        readonly Dictionary<Transform, Quaternion> m_OriginalRotations = new();
        readonly Dictionary<Transform, Vector3>    m_OriginalPositions = new();

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        void Awake()
        {
            CacheOriginalRotations(m_Thumb);
            CacheOriginalRotations(m_Index);
            CacheOriginalRotations(m_Middle);
            CacheOriginalRotations(m_Ring);
            CacheOriginalRotations(m_Pinky);

            if (m_Wrist != null)
            {
                m_OriginalRotations[m_Wrist] = m_Wrist.localRotation;
                m_OriginalPositions[m_Wrist] = m_Wrist.localPosition;
            }
        }

        void OnEnable()
        {
            m_GripInput?.EnableDirectActionIfModeUsed();
            m_TriggerInput?.EnableDirectActionIfModeUsed();
        }

        void OnDisable()
        {
            m_GripInput?.DisableDirectActionIfModeUsed();
            m_TriggerInput?.DisableDirectActionIfModeUsed();
        }

        void Update()
        {
            var isGrabbing = m_GripOverride >= 0f;
            var grip    = isGrabbing ? m_GripOverride : m_GripInput?.ReadValue() ?? 0f;
            var trigger = isGrabbing ? 0f            : m_TriggerInput?.ReadValue() ?? 0f;

            ApplyFinger(m_Thumb,  grip,                    isGrabbing);
            ApplyFinger(m_Index,  Mathf.Max(grip, trigger), isGrabbing);
            ApplyFinger(m_Middle, grip,                    isGrabbing);
            ApplyFinger(m_Ring,   grip,                    isGrabbing);
            ApplyFinger(m_Pinky,  grip,                    isGrabbing);

            ApplyThumbOverlay(isGrabbing);
            ApplyWrist(isGrabbing);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        public void SetGripOverride(float t) => m_GripOverride = Mathf.Clamp01(t);

        // Sets the extra thumb rotation for this frame. Pass Quaternion.identity to clear.
        // Only takes effect while the hand is in grab pose.
        public void SetThumbOverlay(Quaternion overlay) => m_ThumbOverlay = overlay;

        public void ClearGripOverride() => m_GripOverride = -1f;

        // ── Private helpers ───────────────────────────────────────────────────────

        void ApplyFinger(FingerBones finger, float t, bool isGrabbing)
        {
            if (finger.joints == null) return;

            var airAngle = t * finger.maxCurlAngle;

            for (var i = 0; i < finger.joints.Length; i++)
            {
                var joint = finger.joints[i];
                if (joint == null) continue;
                if (!m_OriginalRotations.TryGetValue(joint, out var original)) continue;

                Vector3 axis;
                float angle;

                if (isGrabbing && TryGetJointGrab(finger, i, out var jointAxis, out var jointAngle))
                {
                    // Per-joint grab override: independent angle (weight) and axis (orientation).
                    axis = jointAxis;
                    angle = jointAngle;
                }
                else if (isGrabbing && finger.grabCurlAngle > 0f)
                {
                    axis = finger.curlAxis;
                    angle = finger.grabCurlAngle;
                }
                else
                {
                    axis = finger.curlAxis;
                    angle = airAngle;
                }

                joint.localRotation = original * Quaternion.AngleAxis(angle, axis);
            }
        }

        // Returns the per-joint grab override for joints[index] if one is configured
        // (i.e. its curlAxis is non-zero). Otherwise the finger-level pose is used.
        static bool TryGetJointGrab(FingerBones finger, int index, out Vector3 axis, out float angle)
        {
            axis = default;
            angle = 0f;

            if (finger.grabJoints == null || index >= finger.grabJoints.Length) return false;

            var jp = finger.grabJoints[index];
            if (jp.curlAxis.sqrMagnitude < 1e-6f) return false;

            axis = jp.curlAxis;
            angle = jp.curlAngle;
            return true;
        }

        // Adds m_ThumbOverlay on top of the thumb's proximal joint so button / stick
        // input rotates the whole thumb from its base. No-op when not grabbing.
        void ApplyThumbOverlay(bool isGrabbing)
        {
            if (!isGrabbing) return;
            if (m_Thumb.joints == null || m_Thumb.joints.Length == 0) return;

            var root = m_Thumb.joints[0];
            if (root == null || !m_OriginalRotations.ContainsKey(root)) return;

            root.localRotation *= m_ThumbOverlay;
        }

        void ApplyWrist(bool isGrabbing)
        {
            if (m_Wrist == null) return;
            if (!m_OriginalRotations.TryGetValue(m_Wrist, out var origRot)) return;
            if (!m_OriginalPositions.TryGetValue(m_Wrist, out var origPos)) return;

            if (isGrabbing)
            {
                m_Wrist.localRotation = origRot * Quaternion.Euler(m_GrabWristPose.rotationEuler);
                m_Wrist.localPosition = origPos + m_GrabWristPose.positionOffset/100f;
            }
            else
            {
                m_Wrist.localRotation = origRot;
                m_Wrist.localPosition = origPos;
            }
        }

        void CacheOriginalRotations(FingerBones finger)
        {
            if (finger.joints == null) return;
            foreach (var joint in finger.joints)
            {
                if (joint != null && !m_OriginalRotations.ContainsKey(joint))
                    m_OriginalRotations[joint] = joint.localRotation;
            }
        }

        // ── Editor helpers ────────────────────────────────────────────────────────

#if UNITY_EDITOR
        [ContextMenu("Log Current Grab Pose Values")]
        void LogGrabPoseValues()
        {
            Debug.Log($"=== {name} Grab Pose ===");
            Debug.Log($"Wrist rotationEuler: {m_GrabWristPose.rotationEuler}");
            Debug.Log($"Wrist positionOffset: {m_GrabWristPose.positionOffset}");
            Debug.Log($"Thumb  grabCurlAngle: {m_Thumb.grabCurlAngle}");
            Debug.Log($"Index  grabCurlAngle: {m_Index.grabCurlAngle}");
            Debug.Log($"Middle grabCurlAngle: {m_Middle.grabCurlAngle}");
            Debug.Log($"Ring   grabCurlAngle: {m_Ring.grabCurlAngle}");
            Debug.Log($"Pinky  grabCurlAngle: {m_Pinky.grabCurlAngle}");
            Debug.Log("→ 'Copy Component' してから Play Mode を止め 'Paste Component Values' で保存できます");
        }
#endif
    }
}
