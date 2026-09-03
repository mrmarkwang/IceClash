/*
 * IceClash modular clean-character scene validation harness.
 * Exercises ten integrated-skates Humanoids, Idle/temporary Running playback,
 * supported equipment and right-hand-authoritative two-hand IK contracts,
 * including live SecondaryGrip rebinding, main-visual material tinting, and
 * the puck system without requiring detachable skate renderers.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using IceClash.Core;
using IceClash.Player;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.Hockey.Character
{
    public sealed class ModularCharacterTestHarness : MonoBehaviour
    {
        [SerializeField] private PuckController puck;
        public bool Passed { get; private set; }
        public string Failure { get; private set; } = string.Empty;

        public void Configure(PuckController scenePuck) => puck = scenePuck;

        private IEnumerator Start()
        {
            // Allow the right-hand rig, SecondaryGrip follower, and left-hand rig
            // to settle across their ordered frame boundaries before measuring.
            yield return new WaitForSeconds(0.12f);
            IEnumerator validation = RunValidation();
            while (true)
            {
                object current;
                try
                {
                    if (!validation.MoveNext()) break;
                    current = validation.Current;
                }
                catch (Exception exception)
                {
                    Failure = exception.Message;
                    Debug.LogError("MODULAR_CHARACTER_SMOKE_FAIL " + Failure);
                    yield break;
                }
                yield return current;
            }
        }

        public IEnumerator RunValidation()
        {
            Passed = false;
            Failure = string.Empty;
            HockeyCharacterPresentation[] players = FindObjectsByType<HockeyCharacterPresentation>();
            if (players.Length != 10) throw Fail($"Expected 10 modular players, found {players.Length}.");
            if (puck == null || puck.Body == null) throw Fail("Test scene puck is not configured.");

            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].Animator == null || players[i].Animator.avatar == null
                    || !players[i].Animator.avatar.isHuman || !players[i].Animator.avatar.isValid)
                    throw Fail($"Player {i} has no valid Humanoid avatar.");
                if (players[i].Equipment == null || !players[i].Equipment.IsComplete())
                    throw Fail($"Player {i} equipment slots are incomplete.");
                foreach (HockeyEquipmentSlot slot in Enum.GetValues(typeof(HockeyEquipmentSlot)))
                {
                    GameObject equipped = players[i].Equipment.GetEquipped(slot);
                    if (equipped == null || !equipped.activeInHierarchy)
                        throw Fail($"Player {i} has no active {slot} item.");
                }
            }

            ValidatePairedEquipment(players[0]);
            ValidateEquipmentIndependence(players[1]);
            Debug.Log("SUPPORTED_WEARABLE_RUNTIME_PASS slots=Helmet,Visor,Gloves,Skates stickBindingPreserved=true");
            ValidateTwoHandStickPose(players[0]);
            players[0].SetPreviewState(HockeyPresentationState.Idle);
            yield return new WaitForSeconds(0.12f);
            Transform leftUpperLeg = players[0].Animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            if (leftUpperLeg == null) throw Fail("Humanoid motion test leg is missing.");
            Quaternion idleLegRotation = leftUpperLeg.localRotation;
            players[0].SetPreviewState(HockeyPresentationState.Running);
            yield return new WaitForSeconds(0.2f);
            if (players[0].CurrentPresentationState != HockeyPresentationState.Running)
                throw Fail("Running preview state did not apply.");
            if (!AnimatorIsInOrTransitioningTo(players[0].Animator, "Running"))
                throw Fail("Animator did not enter the temporary Running state.");
            if (Quaternion.Angle(idleLegRotation, leftUpperLeg.localRotation) < 2f)
                throw Fail("Temporary Running state did not visibly move a Humanoid leg bone.");
            PlayerController controller = players[0].GetComponent<PlayerController>();
            if (controller == null) controller = players[0].gameObject.AddComponent<PlayerController>();
            controller.Configure("modular-test", TeamId.Blue, SkaterRole.Center, null, puck,
                players[0].transform.position, new PlayerAttributeBuild());
            players[0].Bind(controller);
            puck.ResetPuck(controller.Stick.ControlPoint);
            if (!puck.TryClaim(controller, controller.Stick)) throw Fail("Existing puck system rejected the test player claim.");
            yield return new WaitForFixedUpdate();
            if (!puck.IsCarriedBy(controller)) throw Fail("Puck did not remain carried by the configured modular player.");
            controller.Movement.ResetMotion(controller.transform.position + controller.transform.forward * 0.45f,
                controller.transform.rotation);
            float initialFollowDistance = Vector3.ProjectOnPlane(
                puck.Body.position - controller.Stick.ControlPoint, Vector3.up).magnitude;
            for (int i = 0; i < 12; i++) yield return new WaitForFixedUpdate();
            float finalFollowDistance = Vector3.ProjectOnPlane(
                puck.Body.position - controller.Stick.ControlPoint, Vector3.up).magnitude;
            if (finalFollowDistance >= initialFollowDistance || finalFollowDistance > 0.35f)
                throw Fail($"Carried puck did not follow the moved control point ({initialFollowDistance:F3} -> {finalFollowDistance:F3}).");
            if (!puck.Release(controller, players[0].transform.forward, 4f)) throw Fail("Existing puck system rejected release.");
            if (puck.Carrier != null) throw Fail("Puck carrier was not cleared after release.");

            Passed = true;
            Debug.Log("MODULAR_CHARACTER_SMOKE_PASS players=10");
        }

        private static void ValidateEquipmentIndependence(HockeyCharacterPresentation presentation)
        {
            ValidateCharacterMaterial(presentation);
            HockeyEquipmentLoadout loadout = presentation.Equipment;
            HockeyStickRig rig = loadout.GetComponent<HockeyStickRig>();
            Transform stableLeftTarget = rig != null ? rig.LeftHandTarget : null;
            Transform stableRightTarget = rig != null ? rig.RightHandTarget : null;
            GameObject visor = loadout.GetEquipped(HockeyEquipmentSlot.Visor);
            loadout.Equip(HockeyEquipmentSlot.Helmet, visor);
            if (loadout.GetEquipped(HockeyEquipmentSlot.Visor) != visor
                || loadout.GetEquipped(HockeyEquipmentSlot.Helmet) == visor)
                throw new InvalidOperationException("Cross-slot equipment replacement stole the source slot item.");
            foreach (HockeyEquipmentSlot slot in Enum.GetValues(typeof(HockeyEquipmentSlot)))
            {
                Dictionary<HockeyEquipmentSlot, GameObject> before = new();
                foreach (HockeyEquipmentSlot candidate in Enum.GetValues(typeof(HockeyEquipmentSlot)))
                    before[candidate] = loadout.GetEquipped(candidate);
                HockeyPairedEquipmentFollower previousFollower = before[slot] != null
                    ? before[slot].GetComponent<HockeyPairedEquipmentFollower>() : null;
                Vector3 previousFirstPosition = previousFollower != null ? previousFollower.FirstVisual.position : Vector3.zero;
                Vector3 previousSecondPosition = previousFollower != null ? previousFollower.SecondVisual.position : Vector3.zero;
                loadout.Clear(slot);
                if (loadout.GetEquipped(slot) != null)
                    throw new InvalidOperationException($"Clearing {slot} left an equipped item.");
                foreach (HockeyEquipmentSlot candidate in Enum.GetValues(typeof(HockeyEquipmentSlot)))
                    if (candidate != slot && loadout.GetEquipped(candidate) != before[candidate])
                        throw new InvalidOperationException($"Clearing {slot} changed {candidate}.");
                if (slot == HockeyEquipmentSlot.Stick && (rig == null
                    || rig.LeftHandConstraint.weight > 0f || rig.RightHandConstraint.weight > 0f
                    || rig.LeftHandTarget != stableLeftTarget || rig.EquippedSecondaryGrip != null
                    || rig.RightHandTarget != stableRightTarget))
                    throw new InvalidOperationException("Clearing Stick did not safely disable and unbind the two-hand IK rig.");
                GameObject replacement = slot == HockeyEquipmentSlot.Gloves
                    ? CreatePairedReplacement(slot)
                    : slot == HockeyEquipmentSlot.Skates
                        ? new GameObject("Validation Integrated Skates")
                        : GameObject.CreatePrimitive(PrimitiveType.Cube);
                replacement.name = $"Validation {slot}";
                Collider collider = replacement.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
                if (slot == HockeyEquipmentSlot.Stick)
                {
                    GameObject secondaryGrip = new("SecondaryGrip");
                    secondaryGrip.transform.SetParent(replacement.transform, false);
                    secondaryGrip.transform.localPosition = new Vector3(0f, -0.35f, 0f);
                }
                loadout.Equip(slot, replacement);
                foreach (HockeyEquipmentSlot candidate in Enum.GetValues(typeof(HockeyEquipmentSlot)))
                    if (candidate != slot && loadout.GetEquipped(candidate) != before[candidate])
                        throw new InvalidOperationException($"Replacing {slot} changed {candidate}.");
                if (slot == HockeyEquipmentSlot.Stick && (rig.LeftHandConstraint.weight < 0.99f
                    || rig.RightHandConstraint.weight < 0.99f || rig.LeftHandTarget != stableLeftTarget
                    || rig.EquippedSecondaryGrip == null
                    || !rig.EquippedSecondaryGrip.IsChildOf(loadout.GetEquipped(HockeyEquipmentSlot.Stick).transform)
                    || rig.RightHandTarget != stableRightTarget))
                    throw new InvalidOperationException("Replacing Stick did not rebind the SecondaryGrip IK target.");
                if (slot == HockeyEquipmentSlot.Gloves)
                {
                    HockeyPairedEquipmentFollower replacementFollower = loadout.GetEquipped(slot)
                        .GetComponent<HockeyPairedEquipmentFollower>();
                    if (!replacementFollower.IsAligned(0.03f)
                        || Vector3.Distance(replacementFollower.FirstVisual.position, previousFirstPosition) > 0.05f
                        || Vector3.Distance(replacementFollower.SecondVisual.position, previousSecondPosition) > 0.05f)
                        throw new InvalidOperationException(
                            $"Replacement {slot} did not inherit the destination player's stable paired-slot pose.");
                }
            }
        }

        private static void ValidateCharacterMaterial(HockeyCharacterPresentation presentation)
        {
            if (presentation.CharacterRenderers.Count == 0 || presentation.CharacterRenderers[0] == null)
                throw new InvalidOperationException("Main character renderers are missing for team tint validation.");
            Material original = presentation.CharacterRenderers[0].sharedMaterial;
            Material wearableOriginal = presentation.Equipment.GetEquipped(HockeyEquipmentSlot.Helmet)
                ?.GetComponentInChildren<Renderer>(true)?.sharedMaterial;
            Material validationMaterial = new(original) { name = "Validation Team Material" };
            presentation.SetTeamMaterial(validationMaterial);
            for (int i = 0; i < presentation.CharacterRenderers.Count; i++)
            {
                Renderer renderer = presentation.CharacterRenderers[i];
                if (renderer == null || renderer.sharedMaterials.Length == 0)
                    throw new InvalidOperationException("A configured main character renderer is missing materials.");
                for (int materialIndex = 0; materialIndex < renderer.sharedMaterials.Length; materialIndex++)
                    if (renderer.sharedMaterials[materialIndex] != validationMaterial)
                        throw new InvalidOperationException("Team material did not apply to the main character visual.");
            }
            if (presentation.Equipment.GetEquipped(HockeyEquipmentSlot.Helmet)
                    ?.GetComponentInChildren<Renderer>(true)?.sharedMaterial != wearableOriginal)
                throw new InvalidOperationException("Main-character team tint changed a wearable material.");
            presentation.SetTeamMaterial(original);
            Destroy(validationMaterial);
        }

        private static GameObject CreatePairedReplacement(HockeyEquipmentSlot slot)
        {
            GameObject item = new($"Validation {slot}");
            GameObject first = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject second = GameObject.CreatePrimitive(PrimitiveType.Cube);
            first.name = slot + " L";
            second.name = slot + " R";
            first.transform.SetParent(item.transform, false);
            second.transform.SetParent(item.transform, false);
            first.transform.localPosition = new Vector3(-0.1f, 0.05f, 0.05f);
            second.transform.localPosition = new Vector3(0.1f, 0.05f, 0.05f);
            Collider firstCollider = first.GetComponent<Collider>();
            Collider secondCollider = second.GetComponent<Collider>();
            if (firstCollider != null) Destroy(firstCollider);
            if (secondCollider != null) Destroy(secondCollider);
            HockeyPairedEquipmentFollower follower = item.AddComponent<HockeyPairedEquipmentFollower>();
            follower.ConfigureVisuals(first.transform, second.transform);
            return item;
        }

        private static void ValidateTwoHandStickPose(HockeyCharacterPresentation player)
        {
            HockeyStickRig rig = player.GetComponent<HockeyStickRig>();
            if (rig == null || !rig.HasValidReferences)
                throw new InvalidOperationException("Two-hand stick rig is missing.");
            Transform primaryGrip = player.Equipment.FindEquippedChild(HockeyEquipmentSlot.Stick, "PrimaryGrip");
            Transform secondaryGrip = player.Equipment.FindEquippedChild(HockeyEquipmentSlot.Stick, "SecondaryGrip");
            if (primaryGrip == null || secondaryGrip == null || rig.EquippedSecondaryGrip != secondaryGrip
                || Vector3.Distance(rig.LeftHandTarget.TransformPoint(rig.LeftPalmGripOffset),
                    secondaryGrip.position) > 0.01f)
                throw new InvalidOperationException(
                    $"Two-hand stick rig is not bound to its authored grip markers (secondary="
                    + $"{rig.EquippedSecondaryGrip?.name}/{secondaryGrip?.name}, proxyDistance="
                    + $"{(secondaryGrip != null ? Vector3.Distance(rig.LeftHandTarget.TransformPoint(rig.LeftPalmGripOffset), secondaryGrip.position) : -1f):F3}).");
            Transform visibleShaft = player.Equipment.FindEquippedChild(HockeyEquipmentSlot.Stick, "Stick Shaft");
            Vector3 leftPalmGrip = rig.LeftHandTarget.TransformPoint(rig.LeftPalmGripOffset);
            if (visibleShaft == null || !ContainsCubePoint(visibleShaft, leftPalmGrip)
                || !ContainsCubePoint(visibleShaft, primaryGrip.position))
                throw new InvalidOperationException("Both hand targets are not on the rendered stick shaft: "
                    + $"shaft={visibleShaft?.name} leftLocal={visibleShaft?.InverseTransformPoint(leftPalmGrip)} "
                    + $"primaryLocal={visibleShaft?.InverseTransformPoint(primaryGrip.position)}.");
            Transform leftHand = player.Animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightHand = player.Animator.GetBoneTransform(HumanBodyBones.RightHand);
            float leftDistance = leftHand != null ? Vector3.Distance(leftHand.position, rig.LeftHandTarget.position) : float.PositiveInfinity;
            float rightDistance = rightHand != null ? Vector3.Distance(rightHand.position, rig.RightHandTarget.position) : float.PositiveInfinity;
            if (leftDistance > 0.1f || rightDistance > 0.1f)
            {
                Transform leftShoulder = player.Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                Transform rightShoulder = player.Animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                throw new InvalidOperationException(
                    $"Animation Rigging did not place both hands on their stick targets (left={leftDistance:F3} {leftHand?.position}->{rig.LeftHandTarget.position}, right={rightDistance:F3} {rightHand?.position}->{rig.RightHandTarget.position})."
                    + $" local leftShoulder={player.transform.InverseTransformPoint(leftShoulder.position)} "
                    + $"leftTarget={player.transform.InverseTransformPoint(rig.LeftHandTarget.position)} "
                    + $"rightShoulder={player.transform.InverseTransformPoint(rightShoulder.position)} "
                    + $"rightTarget={player.transform.InverseTransformPoint(rig.RightHandTarget.position)}");
            }
            ValidateNaturalArmBend(player.transform, player.Animator, HumanBodyBones.LeftUpperArm,
                HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, -1f, "left");
            ValidateNaturalArmBend(player.transform, player.Animator, HumanBodyBones.RightUpperArm,
                HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, 1f, "right");
        }

        private static void ValidateNaturalArmBend(Transform playerRoot, Animator animator,
            HumanBodyBones upperArmBone, HumanBodyBones lowerArmBone, HumanBodyBones handBone,
            float outwardDirection, string side)
        {
            Transform upperArm = animator.GetBoneTransform(upperArmBone);
            Transform elbow = animator.GetBoneTransform(lowerArmBone);
            Transform hand = animator.GetBoneTransform(handBone);
            if (upperArm == null || elbow == null || hand == null)
                throw new InvalidOperationException($"Professional {side}-arm pose is missing a Humanoid bone.");
            float bend = Vector3.Angle(upperArm.position - elbow.position, hand.position - elbow.position);
            float elbowX = playerRoot.InverseTransformPoint(elbow.position).x;
            float handX = playerRoot.InverseTransformPoint(hand.position).x;
            float outwardOffset = (elbowX - handX) * outwardDirection;
            float reach = Vector3.Distance(upperArm.position, hand.position);
            float armLength = Vector3.Distance(upperArm.position, elbow.position)
                + Vector3.Distance(elbow.position, hand.position);
            if (bend < 25f || bend > 165f || outwardOffset < 0.02f)
                throw new InvalidOperationException(
                    $"Professional {side}-arm pose is unnatural (bend={bend:F1}, outward={outwardOffset:F3}, "
                    + $"reach={reach:F3}/{armLength:F3}, shoulder={playerRoot.InverseTransformPoint(upperArm.position)}, "
                    + $"hand={playerRoot.InverseTransformPoint(hand.position)}).");
        }

        private static void ValidatePairedEquipment(HockeyCharacterPresentation player)
        {
            HockeyEquipmentLoadout loadout = player.Equipment;
            HockeyPairedEquipmentFollower gloves = loadout.GetEquipped(HockeyEquipmentSlot.Gloves)
                ?.GetComponent<HockeyPairedEquipmentFollower>();
            GameObject integratedSkates = loadout.GetEquipped(HockeyEquipmentSlot.Skates);
            if (gloves == null || !gloves.IsAligned(0.03f))
                throw new InvalidOperationException("Paired gloves did not follow their animated Humanoid hand bones.");
            Transform leftHand = player.Animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightHand = player.Animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (leftHand == null || rightHand == null
                || Vector3.Distance(gloves.FirstVisual.position, leftHand.position) > 0.03f
                || Vector3.Distance(gloves.SecondVisual.position, rightHand.position) > 0.03f)
                throw new InvalidOperationException("Glove centers are not attached to the animated hand bones.");
            if (integratedSkates == null || integratedSkates.name != "Integrated Skates"
                || integratedSkates.GetComponentsInChildren<Renderer>(true).Length != 0
                || integratedSkates.GetComponent<HockeyPairedEquipmentFollower>() != null
                || loadout.LeftFoot != player.Animator.GetBoneTransform(HumanBodyBones.LeftFoot)
                || loadout.RightFoot != player.Animator.GetBoneTransform(HumanBodyBones.RightFoot))
                throw new InvalidOperationException(
                    "Integrated skates did not preserve the active equipment marker and Humanoid foot bindings.");
        }

        private static bool ContainsCubePoint(Transform cube, Vector3 worldPoint)
        {
            Vector3 local = cube.InverseTransformPoint(worldPoint);
            return Mathf.Abs(local.x) <= 0.55f && Mathf.Abs(local.y) <= 0.55f && Mathf.Abs(local.z) <= 0.55f;
        }

        private static float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            Vector3 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared < 0.000001f) return Vector3.Distance(point, start);
            float t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared);
            return Vector3.Distance(point, start + segment * t);
        }

        private static bool AnimatorIsInOrTransitioningTo(Animator animator, string stateName)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return false;
            int expected = Animator.StringToHash(stateName);
            if (animator.GetCurrentAnimatorStateInfo(0).shortNameHash == expected) return true;
            return animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).shortNameHash == expected;
        }

        private Exception Fail(string message)
        {
            Failure = message;
            Debug.LogError("MODULAR_CHARACTER_SMOKE_FAIL " + message);
            return new InvalidOperationException(message);
        }
    }
}
