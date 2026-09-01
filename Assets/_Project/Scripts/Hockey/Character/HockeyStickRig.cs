/*
 * IceClash two-hand hockey stick rig presentation.
 * Keeps the right-hand pose independent while the stick follows RightHand via
 * StickSocket, synchronizes a stable left-hand IK target from the equipped
 * SecondaryGrip after rig evaluation, and disables IK whenever Stick is empty.
 */

using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace IceClash.Hockey.Character
{
    public sealed class HockeyStickRig : MonoBehaviour
    {
        private static readonly Vector3 LeftPalmGripOffsetValue = new(0.07f, 0f, 0f);
        [SerializeField] private TwoBoneIKConstraint leftHandConstraint;
        [SerializeField] private TwoBoneIKConstraint rightHandConstraint;
        [SerializeField] private Transform leftHandTarget;
        [SerializeField] private Transform rightHandTarget;
        [SerializeField] private Transform leftElbowHint;
        [SerializeField] private Transform rightElbowHint;
        [SerializeField] private Transform shaftEndReference;
        [SerializeField] private Transform bladeReference;
        [SerializeField] private Transform equippedSecondaryGrip;

        public TwoBoneIKConstraint LeftHandConstraint => leftHandConstraint;
        public TwoBoneIKConstraint RightHandConstraint => rightHandConstraint;
        public Transform LeftHandTarget => leftHandTarget;
        public Transform RightHandTarget => rightHandTarget;
        public Transform ShaftEndReference => shaftEndReference;
        public Transform BladeReference => bladeReference;
        public Transform EquippedSecondaryGrip => equippedSecondaryGrip;
        public Vector3 LeftPalmGripOffset => LeftPalmGripOffsetValue;
        public bool HasValidReferences => leftHandConstraint != null && rightHandConstraint != null
            && leftHandTarget != null && rightHandTarget != null
            && leftElbowHint != null && rightElbowHint != null
            && shaftEndReference != null && bladeReference != null;

        public void Configure(TwoBoneIKConstraint leftConstraint, TwoBoneIKConstraint rightConstraint,
            Transform leftTarget, Transform rightTarget, Transform leftHint, Transform rightHint,
            Transform shaftEnd, Transform blade)
        {
            leftHandConstraint = leftConstraint;
            rightHandConstraint = rightConstraint;
            leftHandTarget = leftTarget;
            rightHandTarget = rightTarget;
            leftElbowHint = leftHint;
            rightElbowHint = rightHint;
            shaftEndReference = shaftEnd;
            bladeReference = blade;
        }

        public void SetStickEquipped(bool equipped)
        {
            SetStickEquipped(equipped, equippedSecondaryGrip);
        }

        public void SetStickEquipped(bool equipped, Transform secondaryGrip)
        {
            equippedSecondaryGrip = secondaryGrip;
            if (leftHandConstraint != null)
            {
                TwoBoneIKConstraintData data = leftHandConstraint.data;
                data.target = leftHandTarget;
                leftHandConstraint.data = data;
                leftHandConstraint.weight = equipped && secondaryGrip != null ? 1f : 0f;
            }
            if (rightHandConstraint != null) rightHandConstraint.weight = equipped ? 1f : 0f;
        }

        private void LateUpdate()
        {
            if (equippedSecondaryGrip != null && leftHandTarget != null)
            {
                Quaternion rotation = equippedSecondaryGrip.rotation;
                leftHandTarget.SetPositionAndRotation(
                    equippedSecondaryGrip.position - rotation * LeftPalmGripOffsetValue, rotation);
            }
        }
    }
}
