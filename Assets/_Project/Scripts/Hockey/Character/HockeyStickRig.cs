/*
 * IceClash two-hand hockey stick rig presentation.
 * Keeps both arm constraints bound to stable targets outside replaceable stick
 * content and disables IK atomically whenever the Stick slot is empty.
 */

using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace IceClash.Hockey.Character
{
    public sealed class HockeyStickRig : MonoBehaviour
    {
        [SerializeField] private TwoBoneIKConstraint leftHandConstraint;
        [SerializeField] private TwoBoneIKConstraint rightHandConstraint;
        [SerializeField] private Transform leftHandTarget;
        [SerializeField] private Transform rightHandTarget;
        [SerializeField] private Transform leftElbowHint;
        [SerializeField] private Transform rightElbowHint;
        [SerializeField] private Transform shaftEndReference;
        [SerializeField] private Transform bladeReference;

        public TwoBoneIKConstraint LeftHandConstraint => leftHandConstraint;
        public TwoBoneIKConstraint RightHandConstraint => rightHandConstraint;
        public Transform LeftHandTarget => leftHandTarget;
        public Transform RightHandTarget => rightHandTarget;
        public Transform ShaftEndReference => shaftEndReference;
        public Transform BladeReference => bladeReference;
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
            float weight = equipped ? 1f : 0f;
            if (leftHandConstraint != null) leftHandConstraint.weight = weight;
            if (rightHandConstraint != null) rightHandConstraint.weight = weight;
        }
    }
}
