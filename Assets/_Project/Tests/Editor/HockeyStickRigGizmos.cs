/*
 * IceClash editor-only hockey grip diagnostics.
 * Draws named PrimaryGrip, SecondaryGrip, BladeContact, left-hand IK target,
 * and LeftElbowHint markers without adding runtime debug renderers.
 */

#if UNITY_EDITOR
using IceClash.Hockey.Character;
using UnityEditor;
using UnityEngine;

namespace IceClash.Tests.Editor
{
    public static class HockeyStickRigGizmos
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        private static void Draw(HockeyStickRig rig, GizmoType gizmoType)
        {
            if (rig == null) return;
            DrawNamed(FindDescendant(rig.transform, "PrimaryGrip"), new Color(0.15f, 0.85f, 1f), "PrimaryGrip");
            DrawNamed(FindDescendant(rig.transform, "SecondaryGrip"), new Color(0.2f, 1f, 0.35f), "SecondaryGrip");
            DrawNamed(FindDescendant(rig.transform, "BladeContact"), new Color(1f, 0.25f, 0.2f), "BladeContact");
            DrawNamed(rig.LeftHandTarget, new Color(1f, 0.85f, 0.15f), "LeftHand IK Target");
            Transform hint = rig.LeftHandConstraint != null ? rig.LeftHandConstraint.data.hint : null;
            DrawNamed(hint, new Color(1f, 0.25f, 0.9f), "LeftElbowHint");
            if (rig.LeftHandTarget != null && hint != null)
            {
                Handles.color = new Color(1f, 0.25f, 0.9f, 0.65f);
                Handles.DrawDottedLine(hint.position, rig.LeftHandTarget.position, 4f);
            }
        }

        private static void DrawNamed(Transform marker, Color color, string label)
        {
            if (marker == null) return;
            float size = HandleUtility.GetHandleSize(marker.position) * 0.045f;
            Handles.color = color;
            Handles.SphereHandleCap(0, marker.position, marker.rotation, size, EventType.Repaint);
            Handles.Label(marker.position + Vector3.up * size * 1.4f, label);
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
    }
}
#endif
