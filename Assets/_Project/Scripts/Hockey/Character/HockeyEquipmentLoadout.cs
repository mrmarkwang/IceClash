/*
 * IceClash modular hockey equipment registry.
 * Owns four independently replaceable wearables plus the gameplay stick.
 * Preserves retained serialized slot IDs, paired-equipment support, exposed
 * Humanoid hand/foot bindings, and left-hand IK rebinding to the equipped
 * stick's SecondaryGrip. Integrated skates use the stable skate slot without
 * swapping or masking the character's combined mesh.
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace IceClash.Hockey.Character
{
    public enum HockeyEquipmentSlot
    {
        // Retained values are stable because prefab bindings serialize this enum as integers.
        Helmet = 0,
        Gloves = 2,
        Skates = 4,
        Stick = 5,
        Visor = 8
    }

    [Serializable]
    public sealed class HockeyEquipmentBinding
    {
        [SerializeField] private HockeyEquipmentSlot slot;
        [SerializeField] private Transform anchor;
        [SerializeField] private GameObject equipped;

        public HockeyEquipmentSlot Slot => slot;
        public Transform Anchor => anchor;
        public GameObject Equipped => equipped;

        public void Configure(HockeyEquipmentSlot value, Transform slotAnchor, GameObject item)
        {
            slot = value;
            anchor = slotAnchor;
            equipped = item;
        }

        public void SetEquipped(GameObject item) => equipped = item;
    }

    public sealed class HockeyEquipmentLoadout : MonoBehaviour
    {
        [SerializeField] private HockeyEquipmentBinding[] slots = Array.Empty<HockeyEquipmentBinding>();
        [SerializeField] private HockeyStickRig stickRig;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightHand;
        [SerializeField] private Transform leftFoot;
        [SerializeField] private Transform rightFoot;
        [SerializeField] private Vector3 leftSkatePositionOffset;
        [SerializeField] private Vector3 rightSkatePositionOffset;
        [SerializeField] private Quaternion leftSkateRotationOffset = Quaternion.identity;
        [SerializeField] private Quaternion rightSkateRotationOffset = Quaternion.identity;
        [SerializeField] private SkinnedMeshRenderer skateMaskedSkin;
        [SerializeField] private Mesh unmaskedCharacterMesh;
        [SerializeField] private Mesh maskedCharacterMesh;

        public IReadOnlyList<HockeyEquipmentBinding> Slots => slots;
        public int SlotCount => slots != null ? slots.Length : 0;
        public Transform LeftHand => leftHand;
        public Transform RightHand => rightHand;
        public Transform LeftFoot => leftFoot;
        public Transform RightFoot => rightFoot;

        public void Configure(HockeyEquipmentBinding[] bindings, HockeyStickRig rig,
            Transform handA, Transform handB, Transform footA, Transform footB)
        {
            slots = bindings ?? Array.Empty<HockeyEquipmentBinding>();
            stickRig = rig;
            leftHand = handA;
            rightHand = handB;
            leftFoot = footA;
            rightFoot = footB;
            CaptureSkateContract();
            BindAllPairedEquipment();
            NotifyStickState();
        }

        public Transform GetAnchor(HockeyEquipmentSlot slot) => Find(slot)?.Anchor;
        public GameObject GetEquipped(HockeyEquipmentSlot slot) => Find(slot)?.Equipped;
        public Transform FindEquippedChild(HockeyEquipmentSlot slot, string childName)
        {
            GameObject equipped = GetEquipped(slot);
            return equipped != null ? FindDescendant(equipped.transform, childName) : null;
        }

        public GameObject Equip(HockeyEquipmentSlot slot, GameObject replacement)
        {
            HockeyEquipmentBinding binding = Require(slot);
            GameObject previous = binding.Equipped;
            if (previous == replacement)
            {
                NotifyStickState();
                return previous;
            }

            GameObject equipped = null;
            if (replacement != null)
            {
                bool clone = !replacement.scene.IsValid() || IsEquippedElsewhere(slot, replacement)
                    || (previous != null && replacement.transform.IsChildOf(previous.transform));
                equipped = clone ? Instantiate(replacement) : replacement;
            }
            if (previous != null) DestroyEquipmentObject(previous);
            if (equipped != null)
            {
                equipped.name = replacement.name.Replace("(Clone)", string.Empty);
                equipped.transform.SetParent(binding.Anchor, false);
                equipped.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                equipped.transform.localScale = Vector3.one;
            }

            binding.SetEquipped(equipped);
            BindPairedEquipment(slot, equipped);
            if (slot == HockeyEquipmentSlot.Skates) ApplySkateMask(equipped != null);
            if (slot == HockeyEquipmentSlot.Stick) NotifyStickState();
            return equipped;
        }

        public void Clear(HockeyEquipmentSlot slot) => Equip(slot, null);

        public void ConfigureSkateMask(SkinnedMeshRenderer skin, Mesh unmasked, Mesh masked)
        {
            skateMaskedSkin = skin;
            unmaskedCharacterMesh = unmasked;
            maskedCharacterMesh = masked;
            ApplySkateMask(GetEquipped(HockeyEquipmentSlot.Skates) != null);
        }

        public bool IsComplete()
        {
            if (slots == null || slots.Length != Enum.GetValues(typeof(HockeyEquipmentSlot)).Length) return false;
            HashSet<HockeyEquipmentSlot> found = new();
            for (int i = 0; i < slots.Length; i++)
            {
                HockeyEquipmentBinding binding = slots[i];
                if (binding == null || binding.Anchor == null || !found.Add(binding.Slot)) return false;
            }
            return true;
        }

        private void OnEnable()
        {
            BindAllPairedEquipment();
            ApplySkateMask(GetEquipped(HockeyEquipmentSlot.Skates) != null);
        }

        private void BindAllPairedEquipment()
        {
            BindPairedEquipment(HockeyEquipmentSlot.Gloves, GetEquipped(HockeyEquipmentSlot.Gloves));
            BindPairedEquipment(HockeyEquipmentSlot.Skates, GetEquipped(HockeyEquipmentSlot.Skates));
        }

        private void BindPairedEquipment(HockeyEquipmentSlot slot, GameObject item)
        {
            if (item == null) return;
            HockeyPairedEquipmentFollower follower = item.GetComponent<HockeyPairedEquipmentFollower>();
            if (follower == null) return;
            if (slot == HockeyEquipmentSlot.Gloves)
                follower.BindBones(leftHand, rightHand, Vector3.zero, Vector3.zero,
                    Quaternion.identity, Quaternion.identity);
            else if (slot == HockeyEquipmentSlot.Skates)
                follower.BindBones(leftFoot, rightFoot, leftSkatePositionOffset, rightSkatePositionOffset,
                    leftSkateRotationOffset, rightSkateRotationOffset);
        }

        private void CaptureSkateContract()
        {
            GameObject skates = GetEquipped(HockeyEquipmentSlot.Skates);
            HockeyPairedEquipmentFollower follower = skates != null
                ? skates.GetComponent<HockeyPairedEquipmentFollower>() : null;
            if (follower == null || follower.FirstVisual == null || follower.SecondVisual == null
                || leftFoot == null || rightFoot == null) return;
            leftSkatePositionOffset = leftFoot.InverseTransformPoint(follower.FirstVisual.position);
            rightSkatePositionOffset = rightFoot.InverseTransformPoint(follower.SecondVisual.position);
            leftSkateRotationOffset = Quaternion.Inverse(leftFoot.rotation) * follower.FirstVisual.rotation;
            rightSkateRotationOffset = Quaternion.Inverse(rightFoot.rotation) * follower.SecondVisual.rotation;
        }

        private void ApplySkateMask(bool skatesEquipped)
        {
            if (skateMaskedSkin == null) return;
            Mesh target = skatesEquipped ? maskedCharacterMesh : unmaskedCharacterMesh;
            if (target != null) skateMaskedSkin.sharedMesh = target;
        }

        private HockeyEquipmentBinding Find(HockeyEquipmentSlot slot)
        {
            if (slots == null) return null;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null && slots[i].Slot == slot) return slots[i];
            return null;
        }

        private HockeyEquipmentBinding Require(HockeyEquipmentSlot slot)
        {
            HockeyEquipmentBinding binding = Find(slot);
            if (binding == null || binding.Anchor == null)
                throw new InvalidOperationException($"Hockey equipment slot {slot} is not configured on {name}.");
            return binding;
        }

        private bool IsEquippedElsewhere(HockeyEquipmentSlot destination, GameObject replacement)
        {
            if (slots == null) return false;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null && slots[i].Slot != destination && slots[i].Equipped == replacement) return true;
            return false;
        }

        private static Transform FindDescendant(Transform root, string childName)
        {
            if (root.name == childName) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDescendant(root.GetChild(i), childName);
                if (found != null) return found;
            }
            return null;
        }

        private void NotifyStickState()
        {
            if (stickRig == null) return;
            GameObject stick = GetEquipped(HockeyEquipmentSlot.Stick);
            Transform secondaryGrip = stick != null ? FindDescendant(stick.transform, "SecondaryGrip") : null;
            stickRig.SetStickEquipped(stick != null, secondaryGrip);
        }

        private static void DestroyEquipmentObject(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }
    }
}
