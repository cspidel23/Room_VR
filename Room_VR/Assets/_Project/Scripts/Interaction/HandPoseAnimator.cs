using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

namespace RoomVR.Interaction
{
    // Drives hand bone rotations based on controller Grip / Trigger input.
    // Attach to the root of a hand mesh (e.g. LeftHand.fbx instance).
    // Assign each finger's joint Transforms in the Inspector.
    //
    // Two separate poses are supported:
    //   - Air grip : fingers curl proportionally to Grip/Trigger input (maxCurlAngle)
    //   - Grab pose: fixed angles used when holding the controller prop (grabCurlAngle)
    //     ControllerGrabHandler calls SetGripOverride(1f) / ClearGripOverride().
    public class HandPoseAnimator : MonoBehaviour
    {
        [Serializable]
        public struct FingerBones
        {
            [Tooltip("Joints from proximal to distal.")]
            public Transform[] joints;

            [Tooltip("Axis around which joints curl. Usually Vector3.right for fingers, Vector3.forward for thumb.")]
            public Vector3 curlAxis;

            [Tooltip("Max curl angle for air grip (proportional to button pressure).")]
            [Range(0f, 180f)]
            public float maxCurlAngle;

            [Tooltip("Fixed curl angle used when holding the controller prop. 0 = use maxCurlAngle.")]
            [Range(0f, 180f)]
            public float grabCurlAngle;
        }

        [Header("Finger Bones")]
        [SerializeField] FingerBones m_Thumb  = new FingerBones { curlAxis = Vector3.forward, maxCurlAngle = 60f, grabCurlAngle = 45f };
        [SerializeField] FingerBones m_Index  = new FingerBones { curlAxis = Vector3.right,   maxCurlAngle = 80f, grabCurlAngle = 50f };
        [SerializeField] FingerBones m_Middle = new FingerBones { curlAxis = Vector3.right,   maxCurlAngle = 80f, grabCurlAngle = 70f };
        [SerializeField] FingerBones m_Ring   = new FingerBones { curlAxis = Vector3.right,   maxCurlAngle = 80f, grabCurlAngle = 70f };
        [SerializeField] FingerBones m_Pinky  = new FingerBones { curlAxis = Vector3.right,   maxCurlAngle = 80f, grabCurlAngle = 70f };

        [Header("Input")]
        [SerializeField]
        XRInputValueReader<float> m_GripInput = new XRInputValueReader<float>("Grip");

        [SerializeField]
        XRInputValueReader<float> m_TriggerInput = new XRInputValueReader<float>("Trigger");

        // -1 = no override (use physical input). 0..1 = holding controller prop.
        float m_GripOverride = -1f;

        readonly Dictionary<Transform, Quaternion> m_OriginalRotations = new();

        void Awake()
        {
            CacheOriginalRotations(m_Thumb);
            CacheOriginalRotations(m_Index);
            CacheOriginalRotations(m_Middle);
            CacheOriginalRotations(m_Ring);
            CacheOriginalRotations(m_Pinky);
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
            var trigger = isGrabbing ? 0f : m_TriggerInput?.ReadValue() ?? 0f;

            ApplyFinger(m_Thumb,  grip,              isGrabbing);
            ApplyFinger(m_Index,  Mathf.Max(grip, trigger), isGrabbing);
            ApplyFinger(m_Middle, grip,              isGrabbing);
            ApplyFinger(m_Ring,   grip,              isGrabbing);
            ApplyFinger(m_Pinky,  grip,              isGrabbing);
        }

        // Called by ControllerGrabHandler when both hands grab the controller prop.
        public void SetGripOverride(float t) => m_GripOverride = Mathf.Clamp01(t);

        // Called by ControllerGrabHandler when the prop is released.
        public void ClearGripOverride() => m_GripOverride = -1f;

        void CacheOriginalRotations(FingerBones finger)
        {
            if (finger.joints == null) return;
            foreach (var joint in finger.joints)
            {
                if (joint != null && !m_OriginalRotations.ContainsKey(joint))
                    m_OriginalRotations[joint] = joint.localRotation;
            }
        }

        void ApplyFinger(FingerBones finger, float t, bool isGrabbing)
        {
            if (finger.joints == null) return;

            // grabCurlAngle が 0 のときは maxCurlAngle にフォールバック
            var targetAngle = isGrabbing && finger.grabCurlAngle > 0f
                ? finger.grabCurlAngle
                : t * finger.maxCurlAngle;

            foreach (var joint in finger.joints)
            {
                if (joint == null) continue;
                if (!m_OriginalRotations.TryGetValue(joint, out var original)) continue;
                joint.localRotation = original * Quaternion.AngleAxis(targetAngle, finger.curlAxis);
            }
        }
    }
}
