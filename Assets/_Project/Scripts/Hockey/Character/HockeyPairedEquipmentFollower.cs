/*
 * Keeps paired modular presentation pieces attached to their Humanoid bones.
 * The equipment item remains a single replaceable slot object while its two
 * visual children follow independently animated hands or feet.
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
            Apply(firstBone, RotationReference(firstBone), firstVisual, firstPositionOffset, firstRotationOffset);
            Apply(secondBone, RotationReference(secondBone), secondVisual, secondPositionOffset, secondRotationOffset);
        }

        public bool IsAligned(float tolerance)
        {
            return IsAligned(firstBone, RotationReference(firstBone), firstVisual,
                    firstPositionOffset, firstRotationOffset, tolerance)
                && IsAligned(secondBone, RotationReference(secondBone), secondVisual,
                    secondPositionOffset, secondRotationOffset, tolerance);
        }

        private void LateUpdate() => RefreshPose();

        private Transform RotationReference(Transform bone) => keepUpright && orientationRoot != null ? orientationRoot : bone;

        private static void Apply(Transform bone, Transform rotationReference, Transform visual,
            Vector3 position, Quaternion rotation)
        {
            if (bone == null || visual == null) return;
            visual.SetPositionAndRotation(bone.TransformPoint(position), rotationReference.rotation * rotation);
        }

        private static bool IsAligned(Transform bone, Transform rotationReference, Transform visual,
            Vector3 position, Quaternion rotation, float tolerance)
        {
            return bone != null && visual != null
                && Vector3.Distance(visual.position, bone.TransformPoint(position)) <= tolerance
                && Quaternion.Angle(visual.rotation, rotationReference.rotation * rotation) <= 1f;
        }
    }
}
