/*
 * Keeps paired modular presentation pieces attached to their Humanoid bones.
 * The equipment item remains a single replaceable slot object while its two
 * visual children follow independently animated hands or feet. Keep-upright
 * items may keep rotation in orientation-root space or follow the bone rigidly.
 */

using UnityEngine;

namespace IceClash.Hockey.Character
{
    [ExecuteAlways]
    public sealed class HockeyPairedEquipmentFollower : MonoBehaviour
    {
        [SerializeField] private Transform firstBone;
        [SerializeField] private Transform secondBone;
        [SerializeField] private Transform firstVisual;
        [SerializeField] private Transform secondVisual;
        [SerializeField] private Vector3 firstPositionOffset;
        [SerializeField] private Vector3 secondPositionOffset;
        [SerializeField] private Quaternion firstRotationOffset = Quaternion.identity;
        [SerializeField] private Quaternion secondRotationOffset = Quaternion.identity;
        [SerializeField] private bool keepUpright;
        [SerializeField] private Transform orientationRoot;

        public Transform FirstVisual => firstVisual;
        public Transform SecondVisual => secondVisual;

        public void ConfigureVisuals(Transform visualA, Transform visualB)
        {
            firstVisual = visualA;
            secondVisual = visualB;
        }

        public void BindBones(Transform boneA, Transform boneB, Vector3 positionA, Vector3 positionB,
            Quaternion rotationA, Quaternion rotationB, bool upright = false, Transform uprightRoot = null)
        {
            firstBone = boneA;
            secondBone = boneB;
            keepUpright = upright;
            orientationRoot = uprightRoot;
            firstPositionOffset = positionA;
            secondPositionOffset = positionB;
            firstRotationOffset = rotationA;
            secondRotationOffset = rotationB;
            RefreshPose();
        }

        public void RefreshPose()
        {
            Transform firstReference = RotationReference(firstBone);
            Transform secondReference = RotationReference(secondBone);
            Vector3 firstPosition = TargetPosition(firstBone, firstReference, firstPositionOffset);
            Vector3 secondPosition = TargetPosition(secondBone, secondReference, secondPositionOffset);
            Apply(firstReference, firstVisual, firstPosition,
                firstRotationOffset);
            Apply(secondReference, secondVisual, secondPosition,
                secondRotationOffset);
        }

        public bool IsAligned(float tolerance)
        {
            Transform firstReference = RotationReference(firstBone);
            Transform secondReference = RotationReference(secondBone);
            Vector3 firstPosition = TargetPosition(firstBone, firstReference, firstPositionOffset);
            Vector3 secondPosition = TargetPosition(secondBone, secondReference, secondPositionOffset);
            return IsAligned(firstReference, firstVisual, firstPosition,
                    firstRotationOffset, tolerance)
                && IsAligned(secondReference, secondVisual, secondPosition,
                    secondRotationOffset, tolerance);
        }

        private void LateUpdate() => RefreshPose();

        private Transform RotationReference(Transform bone) => keepUpright && orientationRoot != null ? orientationRoot : bone;

        private static Vector3 TargetPosition(Transform bone, Transform rotationReference, Vector3 position)
        {
            if (bone == null) return Vector3.zero;
            return rotationReference == bone
                ? bone.TransformPoint(position)
                : bone.position + rotationReference.TransformVector(position);
        }

        private static void Apply(Transform rotationReference, Transform visual,
            Vector3 worldPosition, Quaternion rotation)
        {
            if (rotationReference == null || visual == null) return;
            visual.SetPositionAndRotation(worldPosition, rotationReference.rotation * rotation);
        }

        private static bool IsAligned(Transform rotationReference, Transform visual,
            Vector3 worldPosition, Quaternion rotation, float tolerance)
        {
            return rotationReference != null && visual != null
                && Vector3.Distance(visual.position, worldPosition) <= tolerance
                && Quaternion.Angle(visual.rotation, rotationReference.rotation * rotation) <= 1f;
        }
    }
}
