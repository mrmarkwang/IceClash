/*
 * IceClash modular character scene validation harness.
 * Exercises ten humanoids, independent equipment replacement, preview animation,
 * two-hand stick IK, and claim/carry/release through the existing puck system.
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
            yield return null;
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
            }

            ValidateTwoHandStickPose(players[0]);
            players[0].SetPreviewState(HockeyPresentationState.Idle);
            yield return new WaitForSeconds(0.12f);
            Transform leftUpperLeg = players[0].Animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            Transform spine = players[0].Animator.GetBoneTransform(HumanBodyBones.Spine);
            Transform chest = players[0].Animator.GetBoneTransform(HumanBodyBones.Chest);
            if (leftUpperLeg == null || spine == null || chest == null) throw Fail("Humanoid motion test bones are missing.");
            Quaternion idleLegRotation = leftUpperLeg.localRotation;
            players[0].SetPreviewState(HockeyPresentationState.Skating);
            yield return new WaitForSeconds(0.2f);
            if (players[0].CurrentPresentationState != HockeyPresentationState.Skating)
                throw Fail("Skating preview state did not apply.");
            if (!AnimatorIsInOrTransitioningTo(players[0].Animator, "Skate"))
                throw Fail("Animator did not enter the placeholder Skate state.");
            if (Quaternion.Angle(idleLegRotation, leftUpperLeg.localRotation) < 2f)
                throw Fail("Placeholder Skate state did not visibly move a Humanoid leg bone.");
            ValidatePairedEquipment(players[0]);
            Quaternion preShotSpineRotation = spine.localRotation;
            Quaternion preShotChestRotation = chest.localRotation;
            players[0].SetPreviewState(HockeyPresentationState.Shooting);
            yield return new WaitForSeconds(0.12f);
            if (players[0].CurrentPresentationState != HockeyPresentationState.Shooting)
                throw Fail("Shooting preview state did not apply.");
            if (!AnimatorIsInOrTransitioningTo(players[0].Animator, "Shoot"))
                throw Fail("Animator did not enter the placeholder Shoot state.");
            if (Mathf.Max(Quaternion.Angle(preShotSpineRotation, spine.localRotation),
                    Quaternion.Angle(preShotChestRotation, chest.localRotation)) < 2f)
                throw Fail("Placeholder Shoot state did not visibly move a Humanoid torso bone.");

            ValidateEquipmentIndependence(players[0].Equipment);

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

        private static void ValidateEquipmentIndependence(HockeyEquipmentLoadout loadout)
        {
            HockeyStickRig rig = loadout.GetComponent<HockeyStickRig>();
            Transform stableLeftTarget = rig != null ? rig.LeftHandTarget : null;
            Transform stableRightTarget = rig != null ? rig.RightHandTarget : null;
            GameObject jersey = loadout.GetEquipped(HockeyEquipmentSlot.Jersey);
            loadout.Equip(HockeyEquipmentSlot.Helmet, jersey);
            if (loadout.GetEquipped(HockeyEquipmentSlot.Jersey) != jersey
                || loadout.GetEquipped(HockeyEquipmentSlot.Helmet) == jersey)
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
                    || rig.LeftHandTarget != stableLeftTarget || rig.RightHandTarget != stableRightTarget))
                    throw new InvalidOperationException("Clearing Stick did not safely disable the stable two-hand IK rig.");
                GameObject replacement = slot == HockeyEquipmentSlot.Gloves || slot == HockeyEquipmentSlot.Skates
                    ? CreatePairedReplacement(slot)
                    : GameObject.CreatePrimitive(PrimitiveType.Cube);
                replacement.name = $"Validation {slot}";
                Collider collider = replacement.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
                loadout.Equip(slot, replacement);
                foreach (HockeyEquipmentSlot candidate in Enum.GetValues(typeof(HockeyEquipmentSlot)))
                    if (candidate != slot && loadout.GetEquipped(candidate) != before[candidate])
                        throw new InvalidOperationException($"Replacing {slot} changed {candidate}.");
                if (slot == HockeyEquipmentSlot.Stick && (rig.LeftHandConstraint.weight < 0.99f
                    || rig.RightHandConstraint.weight < 0.99f || rig.LeftHandTarget != stableLeftTarget
                    || rig.RightHandTarget != stableRightTarget))
                    throw new InvalidOperationException("Replacing Stick did not restore the stable two-hand IK rig.");
                if (slot == HockeyEquipmentSlot.Gloves || slot == HockeyEquipmentSlot.Skates)
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
            float targetDistance = DistanceToSegment(rig.LeftHandTarget.position, rig.RightHandTarget.position,
                rig.ShaftEndReference.position);
            if (targetDistance > 0.03f)
                throw new InvalidOperationException($"Left-hand target is {targetDistance:F3}m away from the stick shaft.");
            Transform visibleShaft = player.Equipment.FindEquippedChild(HockeyEquipmentSlot.Stick, "Stick Shaft");
            if (visibleShaft == null || !ContainsCubePoint(visibleShaft, rig.LeftHandTarget.position)
                || !ContainsCubePoint(visibleShaft, rig.RightHandTarget.position))
                throw new InvalidOperationException("Both hand targets are not on the rendered stick shaft.");
            Transform leftHand = player.Animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightHand = player.Animator.GetBoneTransform(HumanBodyBones.RightHand);
            float leftDistance = leftHand != null ? Vector3.Distance(leftHand.position, rig.LeftHandTarget.position) : float.PositiveInfinity;
            float rightDistance = rightHand != null ? Vector3.Distance(rightHand.position, rig.RightHandTarget.position) : float.PositiveInfinity;
            if (leftDistance > 0.1f || rightDistance > 0.1f)
                throw new InvalidOperationException(
                    $"Animation Rigging did not place both hands on their stick targets (left={leftDistance:F3} {leftHand?.position}->{rig.LeftHandTarget.position}, right={rightDistance:F3} {rightHand?.position}->{rig.RightHandTarget.position}).");
        }

        private static void ValidatePairedEquipment(HockeyCharacterPresentation player)
        {
            HockeyEquipmentLoadout loadout = player.Equipment;
            HockeyPairedEquipmentFollower gloves = loadout.GetEquipped(HockeyEquipmentSlot.Gloves)
                ?.GetComponent<HockeyPairedEquipmentFollower>();
            HockeyPairedEquipmentFollower skates = loadout.GetEquipped(HockeyEquipmentSlot.Skates)
                ?.GetComponent<HockeyPairedEquipmentFollower>();
            if (gloves == null || skates == null || !gloves.IsAligned(0.03f) || !skates.IsAligned(0.03f))
                throw new InvalidOperationException("Paired gloves or skates did not follow their animated Humanoid bones.");
            Transform leftHand = player.Animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightHand = player.Animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (leftHand == null || rightHand == null
                || Vector3.Distance(gloves.FirstVisual.position, leftHand.position) > 0.03f
                || Vector3.Distance(gloves.SecondVisual.position, rightHand.position) > 0.03f)
                throw new InvalidOperationException("Glove centers are not attached to the animated hand bones.");
            Transform leftSkate = loadout.FindEquippedChild(HockeyEquipmentSlot.Skates, "Skates L");
            Transform rightSkate = loadout.FindEquippedChild(HockeyEquipmentSlot.Skates, "Skates R");
            float leftVertical = leftSkate != null ? Mathf.Abs(Vector3.Dot(leftSkate.forward, Vector3.up)) : 1f;
            float rightVertical = rightSkate != null ? Mathf.Abs(Vector3.Dot(rightSkate.forward, Vector3.up)) : 1f;
            if (leftVertical > 0.5f || rightVertical > 0.5f)
                throw new InvalidOperationException(
                    $"Skate long axes are not oriented along the ice (left={leftVertical:F3}, right={rightVertical:F3}).");
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
