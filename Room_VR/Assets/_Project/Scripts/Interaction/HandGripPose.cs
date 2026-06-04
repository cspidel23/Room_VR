using System;
using UnityEngine;

namespace RoomVR.Interaction
{
    // Per-finger grab settings that can live on a grabbable object (not the hand bones).
    [Serializable]
    public class FingerGrip
    {
        [Tooltip("Curl axis for this finger during grab. Zero = fall back to the hand's own curl axis.")]
        public Vector3 curlAxis = Vector3.right;

        [Tooltip("Curl angle (degrees) for this finger during grab.")]
        [Range(-180f, 180f)]
        public float curlAngle = 60f;

        [Tooltip("Optional per-joint overrides. Each entry matches the hand's joints by index.")]
        public HandPoseAnimator.JointGrabPose[] grabJoints;
    }

    // A full hand grip pose. Put this on a grabbable object and it is applied to whichever
    // hand grabs it (via HandPoseAnimator.SetGripPose), so each object can have its own
    // hand shape — tunable in the Inspector just like the controller's grab pose.
    [Serializable]
    public class HandGripPose
    {
        public FingerGrip thumb  = new FingerGrip { curlAxis = Vector3.forward, curlAngle = 45f };
        public FingerGrip index  = new FingerGrip { curlAxis = Vector3.right,   curlAngle = 60f };
        public FingerGrip middle = new FingerGrip { curlAxis = Vector3.right,   curlAngle = 70f };
        public FingerGrip ring   = new FingerGrip { curlAxis = Vector3.right,   curlAngle = 70f };
        public FingerGrip pinky  = new FingerGrip { curlAxis = Vector3.right,   curlAngle = 70f };

        [Tooltip("Wrist euler offset applied during grab.")]
        public Vector3 wristRotationEuler;

        [Tooltip("Wrist local position offset during grab (divided by 100 internally, like the controller pose).")]
        public Vector3 wristPositionOffset;
    }
}
