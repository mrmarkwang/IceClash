/*
 * Compile-time compatibility boundary for editor consumers that referenced the
 * retired Male_Base_v1 production generator. Paths resolve to the integrated-
 * skates replacement, while legacy generation fails explicitly instead of
 * silently rebuilding gameplay with incompatible assumptions.
 */

#if UNITY_EDITOR
using System;
using UnityEngine;

namespace IceClash.CharacterValidation.Editor
{
    public static class MaleBaseV11GameplayIntegrationSetup
    {
        public const string CleanModelPath =
            "Assets/Characters/Male/Male_Base_v1/Models/Meshy_AI_Hockey_Player_Charact_biped_Character_output.fbx";
        public const string ControllerPath =
            "Assets/Characters/Male/Male_Base_v1/Animations/Air_Squat_Validation.controller";
        public const string VisualPrefabPath =
            "Assets/Characters/Male/Male_Base_v1/Prefabs/Male_Base_v2_IntegratedSkates_Test.prefab";

        public static void GenerateProductionAssets()
        {
            throw new InvalidOperationException(
                "The legacy Male_Base_v1 production generator was retired when the asset folder was replaced. " +
                "Migrate gameplay to the integrated-skates character in a dedicated integration task.");
        }

        public static float AlignVisualToGameplayIce(GameObject gameplayRoot, Transform visualRoot, Animator animator)
        {
            throw new InvalidOperationException(
                "Legacy gameplay alignment is unavailable for the integrated-skates replacement. " +
                "Calibrate the 2.126 m character scale and blade contact plane during gameplay migration.");
        }
    }
}
#endif
