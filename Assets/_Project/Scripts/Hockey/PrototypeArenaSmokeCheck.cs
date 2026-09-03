/*
 * IceClash Phase 1 local PvE smoke assertions.
 * Verifies the generated 5v5-plus-goalies slice, conventional C/LW/RW/LD/RD roles,
 * mirrored center-faceoff resets, one-human routing, possession-only automatic
 * nearest-defender control, recommended tap passing with dotted feedback,
 * manual switching, loose-puck nearest-player SWITCH, possession-visible offense,
 * and SWITCH/CHECK defense controls,
 * contextual body/pull checks, smooth camera retargeting, modular systems, safe multi-touch Unity UI controls,
 * circular visual/hit separation, main-character team coloring that excludes
 * replaceable wearables, integrated-skates marker/ice-contact/height contracts,
 * responsive puck possession, charge-speed-only
 * low/high airborne shots with Shooting-skilled open-lane aim, snapshots, and one-way goals.
 * Recent changes: validates the elongated rink,
 * compact physical hockey nets, rounded-rink puck containment, stationary-carrier and board-pressure turnovers,
 * physics-driven line crossing, distinct actor scales, and
 * single-chaser role spacing across both zones, larger non-overlapping action UI,
 * enlarged fixed joystick geometry, and full
 * visible-control containment inside the safe-area layout. Passing checks enforce
 * distance-scaled launch tuning, local intended-receiver capture, short/medium/long
 * and moving reception, automatic control transfer, and interceptable outcomes.
 * Opponent AI checks cover loose corner-puck pursuit, active carrier pressure and
 * dislodging checks, plus tactical pass and shot outcomes. Attribute checks cover
 * constrained builds, physical mappings, fatigue, dekes, actions, contests,
 * snapshots, and AI-difficulty separation. Offside checks cover mirrored warning
 * grids, tag-up/turnover clearing, swept zone entry, and neutral-dot restarts.
 * Modular-character checks cover ten bound integrated-skates humanoid skaters,
 * two safe idle goalie presentations, one integrated-skates marker per actor, gameplay-root
 * authority, real keyboard/joystick routing, Idle/temporary Running animation,
 * foot alignment, equipment, IK, and stick-blade alignment.
 * Rounded-corner containment measures tangential recovery against the local radial
 * board normal instead of an inaccurate fixed diagonal.
 */

using System.Collections.Generic;
using System.Linq;
using IceClash.AI;
using IceClash.CameraSystem;
using IceClash.Core;
using IceClash.Gameplay;
using IceClash.Hockey.Character;
using IceClash.Input;
using IceClash.Match;
using IceClash.Player;
using IceClash.Puck;
using IceClash.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IceClash.Hockey
{
    public static class PrototypeArenaSmokeCheck
    {
        public static void RunIntegratedSkatesOnly()
        {
            PrototypeArenaBootstrap bootstrap = Object.FindAnyObjectByType<PrototypeArenaBootstrap>();
            if (bootstrap == null)
                bootstrap = new GameObject("Gameplay Skates Smoke Arena").AddComponent<PrototypeArenaBootstrap>();
            bootstrap.BuildForValidation();
            PlayerController[] players = Object.FindObjectsByType<PlayerController>();
            HockeyGoalieAI[] goalies = Object.FindObjectsByType<HockeyGoalieAI>();
            if (players.Length != 10 || goalies.Length != 2)
                throw new System.InvalidOperationException(
                    $"Gameplay skates smoke expected 10 skaters and 2 goalies, found {players.Length} and {goalies.Length}.");
            for (int i = 0; i < players.Length; i++)
            {
                HockeyEquipmentLoadout loadout = players[i].GetComponent<HockeyEquipmentLoadout>();
                if (!VerifyIntegratedSkates(loadout, players[i].transform, false))
                    throw new System.InvalidOperationException(
                        $"Gameplay player {players[i].PlayerId} does not have integrated skates.");
            }
            for (int i = 0; i < goalies.Length; i++)
            {
                HockeyEquipmentLoadout loadout = goalies[i].GetComponent<HockeyEquipmentLoadout>();
                if (!VerifyIntegratedSkates(loadout, goalies[i].transform, false))
                    throw new System.InvalidOperationException(
                        $"Gameplay goalie {goalies[i].Team} does not have integrated skates.");
            }
            Debug.Log("INTEGRATED_SKATES_RUNTIME_PASS skaters=10/10 goalies=2/2");
        }

        public static void Run()
        {
            PrototypeArenaBootstrap bootstrap = Object.FindAnyObjectByType<PrototypeArenaBootstrap>();
            if (bootstrap == null) bootstrap = new GameObject("Phase 1 PvE Smoke Arena").AddComponent<PrototypeArenaBootstrap>();
            bootstrap.BuildForValidation();

            PlayerController[] players = Object.FindObjectsByType<PlayerController>();
            HockeyPlayerAI[] skaterAi = Object.FindObjectsByType<HockeyPlayerAI>();
            HockeyGoalieAI[] goalies = Object.FindObjectsByType<HockeyGoalieAI>();
            LocalMatchSetup setup = Object.FindAnyObjectByType<LocalMatchSetup>();
            PuckController puck = Object.FindAnyObjectByType<PuckController>();
            OffsideController offside = Object.FindAnyObjectByType<OffsideController>();
            if (offside != null) offside.SetRuleEnabledForValidation(false);
            MobileActionButton[] buttons = Object.FindObjectsByType<MobileActionButton>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            int humanRouted = 0;
            for (int i = 0; i < players.Length; i++) if (players[i].InputSource is PlayerInputController) humanRouted++;

            Canvas uiCanvas = Object.FindAnyObjectByType<Canvas>();
            Transform mobileControls = uiCanvas != null ? uiCanvas.transform.Find("MobileControls") : null;
            Transform joystickArea = mobileControls != null ? mobileControls.Find("JoystickArea") : null;
            Transform actionButtons = mobileControls != null ? mobileControls.Find("ActionButtons") : null;
            VirtualJoystick virtualJoystick = joystickArea != null ? joystickArea.GetComponent<VirtualJoystick>() : null;
            CanvasScaler scaler = uiCanvas != null ? uiCanvas.GetComponent<CanvasScaler>() : null;
            MobileActionButton passButton = actionButtons != null
                ? actionButtons.Find("PassButton")?.GetComponent<MobileActionButton>() : null;
            MobileActionButton dekeButton = actionButtons != null
                ? actionButtons.Find("DekeButton")?.GetComponent<MobileActionButton>() : null;
            MobileActionButton shootButton = actionButtons != null
                ? actionButtons.Find("ShootButton")?.GetComponent<MobileActionButton>() : null;
            Canvas.ForceUpdateCanvases();

            bool controlHierarchy = uiCanvas != null && uiCanvas.name == "Canvas" && mobileControls != null
                && mobileControls.GetComponent<SafeAreaFitter>() != null
                && joystickArea != null && joystickArea.Find("JoystickBackground/JoystickHandle") != null
                && actionButtons != null && actionButtons.Find("PassButton") != null
                && actionButtons.Find("DekeButton") != null && actionButtons.Find("ShootButton") != null;
            bool controlScaling = scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
                && scaler.referenceResolution == new Vector2(1920f, 1080f);
            bool actionLayout = buttons.Length == 3 && passButton != null && dekeButton != null && shootButton != null
                && ButtonArea(passButton) >= 264f * 264f
                && Mathf.Approximately(ButtonArea(passButton), ButtonArea(dekeButton))
                && Mathf.Approximately(ButtonArea(passButton), ButtonArea(shootButton));
            bool refinedControlVisuals = VerifyJoystickVisuals(joystickArea)
                && VerifyActionVisual(passButton) && VerifyActionVisual(dekeButton) && VerifyActionVisual(shootButton)
                && Mathf.Approximately(VisualArea(passButton), VisualArea(dekeButton))
                && Mathf.Approximately(VisualArea(passButton), VisualArea(shootButton))
                && RectTransformContains(mobileControls as RectTransform, passButton.GetComponent<RectTransform>())
                && RectTransformContains(mobileControls as RectTransform, dekeButton.GetComponent<RectTransform>())
                && RectTransformContains(mobileControls as RectTransform, shootButton.GetComponent<RectTransform>())
                && RectTransformContains(mobileControls as RectTransform, joystickArea as RectTransform)
                && !RectTransformsOverlap(passButton, dekeButton)
                && !RectTransformsOverlap(passButton, shootButton)
                && !RectTransformsOverlap(dekeButton, shootButton)
                && !WorldRectsOverlap(joystickArea as RectTransform, passButton.GetComponent<RectTransform>())
                && !WorldRectsOverlap(joystickArea as RectTransform, dekeButton.GetComponent<RectTransform>())
                && !WorldRectsOverlap(joystickArea as RectTransform, shootButton.GetComponent<RectTransform>());
            bool fixedJoystick = VerifyFixedJoystick(joystickArea);
            bool analogInput = virtualJoystick != null && virtualJoystick.Radius >= 156f
                && virtualJoystick.DeadZone >= 0.1f && virtualJoystick.DeadZone <= 0.15f
                && VirtualJoystick.ApplyDeadZone(new Vector2(0.05f, 0f), virtualJoystick.DeadZone) == Vector2.zero
                && Mathf.Abs(VirtualJoystick.ApplyDeadZone(Vector2.one, virtualJoystick.DeadZone).magnitude - 1f) < 0.001f;
            bool sourceSelection = PlayerInputController.SelectMoveInput(Vector2.one * 4f, Vector2.right * 0.5f).magnitude <= 1f
                && PlayerInputController.SelectMoveInput(Vector2.right * 0.2f, Vector2.up * 0.8f) == Vector2.up * 0.8f;
            bool pointerOwnership = VerifyIndependentPointers(virtualJoystick, dekeButton);
            bool hardwareActionContract = VerifyHardwareActionContract();
            bool cleanWASDInput = false;
            bool cleanJoystickInput = false;
            bool cleanCameraCollider = false;
            bool cleanAnimation = false;
            bool cleanFootAlignment = false;

            bool roster = players.Length == 10 && skaterAi.Length == 10 && goalies.Length == 2
                && CountTeam(players, TeamId.Blue) == 5 && CountTeam(players, TeamId.Red) == 5 && humanRouted == 1;
            bool smallerSkaters = AllSkatersUseScale(players, 0.68f);
            bool broaderGoalies = AllGoaliesAreBroader(goalies, players[0]);
            bool modular = Object.FindObjectsByType<PlayerMovementController>().Length == 10
                && Object.FindObjectsByType<StickPuckInteraction>().Length == 10
                && Object.FindObjectsByType<PassReceivingZone>().Length == 10
                && Object.FindObjectsByType<PassController>().Length == 10
                && Object.FindObjectsByType<PassTargetSelector>().Length == 10
                && Object.FindObjectsByType<ShootController>().Length == 10
                && Object.FindObjectsByType<DekeController>().Length == 10
                && setup != null && setup.SwitchController != null && setup.ControlManager != null
                && setup.DefenseController != null && setup.DefenseController.Tuning != null
                && setup.MatchController != null && setup.OffsideController == offside && offside != null;
            bool modularCharacter = VerifyModularCharacters(players, goalies);
            bool teamCharacterMaterials = VerifyCharacterTeamMaterials(players, goalies);
            bool presentation = Object.FindAnyObjectByType<HockeyCameraController>() != null
                && virtualJoystick != null
                && Object.FindAnyObjectByType<MatchHUD>() != null
                && controlHierarchy && controlScaling && actionLayout && refinedControlVisuals && fixedJoystick
                && analogInput && sourceSelection && pointerOwnership && hardwareActionContract;
            bool arenaPresentation = VerifyArenaPresentation();
            bool puckIndependent = puck != null && puck.transform.parent == null && puck.Body != null;
            bool puckBoardContainment = VerifyPuckBoardContainment(puck);
            bool snapshots = setup != null && setup.Data.BlueTeam.Players.Count == 5 && setup.Data.RedTeam.Players.Count == 5
                && !string.IsNullOrEmpty(setup.Data.ControlledPlayerId);
            setup.CaptureDataForValidation();
            Dictionary<string, SkaterRole> baselineRoles = CaptureRoleMap(players);
            bool roleDistribution = VerifyRoleDistribution(players, TeamId.Blue)
                && VerifyRoleDistribution(players, TeamId.Red);
            bool faceoffFormation = VerifyFaceoffFormation(players, goalies, puck);
            bool spacedRoleTargets = VerifySpacedRoleTargets();
            bool rolePersistence = RolesMatchActorsAndSnapshots(baselineRoles, players, setup.Data);

            setup.MatchController.StartPlayImmediatelyForValidation();
            HockeyCameraController hockeyCamera = Object.FindAnyObjectByType<HockeyCameraController>();
            PlayerController passer = FindPlayer(players, "blue-1");
            PlayerController receiver = FindPlayer(players, "blue-2");
            VerifyCleanPlayerInputRoutes(setup, virtualJoystick, out cleanWASDInput, out cleanJoystickInput);
            VerifyCleanPlayerPresentation(setup.SwitchController.ControlledPlayer, hockeyCamera,
                out cleanCameraCollider, out cleanAnimation, out cleanFootAlignment);
            PlayerController expectedDefender = FindPlayer(players, "blue-3");
            PlayerController opponentCarrier = FindPlayer(players, "red-1");
            Vector3 cameraBeforeAutoSwitch = hockeyCamera.transform.position;

            passer.Movement.ResetMotion(new Vector3(0f, 1f, -4f), Quaternion.identity);
            receiver.Movement.ResetMotion(new Vector3(0f, 1f, 1f), Quaternion.identity);
            expectedDefender.Movement.ResetMotion(new Vector3(4f, 1f, 0f), Quaternion.identity);
            Vector3 puckScale = puck.transform.localScale;
            Vector3 controlOffset = Vector3.ProjectOnPlane(passer.Stick.ControlPoint - passer.transform.position, Vector3.up);
            bool puckSizeAndPosition = puckScale.x <= 0.421f && puckScale.z <= 0.421f && puckScale.y <= 0.061f
                && puck.GetComponent<BoxCollider>() != null
                && (puck.GetComponent<CapsuleCollider>() == null || !puck.GetComponent<CapsuleCollider>().enabled)
                && Vector3.Dot(controlOffset, passer.transform.forward) >= 1.1f;
            Vector3 pickupPosition = passer.transform.position;
            pickupPosition.y = puck.Body.position.y;
            puck.ResetPuck(pickupPosition);
            puck.transform.position = pickupPosition;
            Physics.SyncTransforms();
            bool forgivingPickup = puck.TryClaim(passer, passer.Stick);
            Vector3 matchedVelocity = Vector3.forward * 6f;
            puck.Body.position = passer.Stick.ControlPoint;
            puck.Body.linearVelocity = matchedVelocity;
            bool velocityMatchedControl = puck.CalculateCarryAcceleration(puck.Body.position, matchedVelocity).sqrMagnitude < 0.001f;
            float tapShotPower = passer.Shoot.EvaluatePower(0f);
            float chargedShotPower = passer.Shoot.EvaluatePower(1f);
            const float representativeShotDistance = 20f;
            const float representativePuckHeight = 0.55f;
            float lowTapShotLift = ShootController.EvaluateBallisticLiftSpeed(0.75f, tapShotPower,
                representativeShotDistance, representativePuckHeight, 0.75f, 1.05f, 2.2f, 7.5f, 9.81f,
                puck.Body.linearDamping, Time.fixedDeltaTime);
            float highTapShotLift = ShootController.EvaluateBallisticLiftSpeed(1.05f, tapShotPower,
                representativeShotDistance, representativePuckHeight, 0.75f, 1.05f, 2.2f, 7.5f, 9.81f,
                puck.Body.linearDamping, Time.fixedDeltaTime);
            float lowChargedShotLift = ShootController.EvaluateBallisticLiftSpeed(0.75f, chargedShotPower,
                representativeShotDistance, representativePuckHeight, 0.75f, 1.05f, 2.2f, 7.5f, 9.81f,
                puck.Body.linearDamping, Time.fixedDeltaTime);
            float highChargedShotLift = ShootController.EvaluateBallisticLiftSpeed(1.05f, chargedShotPower,
                representativeShotDistance, representativePuckHeight, 0.75f, 1.05f, 2.2f, 7.5f, 9.81f,
                puck.Body.linearDamping, Time.fixedDeltaTime);
            float lowTapGoalHeight = ShootController.EvaluateDampedBallisticHeight(representativePuckHeight,
                tapShotPower, lowTapShotLift, representativeShotDistance, 9.81f,
                puck.Body.linearDamping, Time.fixedDeltaTime);
            float highTapGoalHeight = ShootController.EvaluateDampedBallisticHeight(representativePuckHeight,
                tapShotPower, highTapShotLift, representativeShotDistance, 9.81f,
                puck.Body.linearDamping, Time.fixedDeltaTime);
            float lowChargedGoalHeight = ShootController.EvaluateDampedBallisticHeight(representativePuckHeight,
                chargedShotPower, lowChargedShotLift, representativeShotDistance, 9.81f,
                puck.Body.linearDamping, Time.fixedDeltaTime);
            float highChargedGoalHeight = ShootController.EvaluateDampedBallisticHeight(representativePuckHeight,
                chargedShotPower, highChargedShotLift, representativeShotDistance, 9.81f,
                puck.Body.linearDamping, Time.fixedDeltaTime);
            Vector3[] laneBlockers = { new(0f, 0f, 18f), new(0.8f, 0f, 18f) };
            float openLaneX = ShootController.EvaluateOpenLaneTargetX(Vector3.zero, 20f, laneBlockers);
            bool hardShotTuning = tapShotPower >= 24f && chargedShotPower >= 49f
                && chargedShotPower > tapShotPower * 1.9f
                && passer.Shoot.FullChargeSeconds <= 0.65f
                && passer.Shoot.CooldownSeconds <= 0.3f
                && lowTapShotLift >= 2.2f && highTapShotLift > lowTapShotLift
                && lowChargedShotLift >= 2.2f && highChargedShotLift > lowChargedShotLift
                && lowTapGoalHeight >= 0.65f && lowTapGoalHeight <= 0.85f
                && highTapGoalHeight > lowTapGoalHeight && highTapGoalHeight <= 1.15f
                && lowChargedGoalHeight >= 0.65f && lowChargedGoalHeight <= 0.85f
                && highChargedGoalHeight > lowChargedGoalHeight && highChargedGoalHeight <= 1.15f
                && ShootController.EvaluateHighShotVariant("shot-test", 0)
                    != ShootController.EvaluateHighShotVariant("shot-test", 1)
                && ShootController.EvaluateLaneAssist(1f, 0.9f) > ShootController.EvaluateLaneAssist(0f, 0.9f)
                && openLaneX < -0.5f
                && ShootController.EvaluateShotDirection(Vector3.forward, chargedShotPower, highChargedShotLift).y > 0f
                && puck.Body.collisionDetectionMode == CollisionDetectionMode.ContinuousDynamic;
            bool airborneShotRelease = VerifyRuntimeAirborneShots(passer, puck);
            puck.ResetPuck(passer.Stick.ControlPoint);
            bool reliablePassTuning = passer.Pass.ShortPassDistance < passer.Pass.MediumPassDistance
                && passer.Pass.MediumPassDistance < passer.Pass.LongPassDistance
                && passer.Pass.ShortPassSpeed >= 10f && passer.Pass.ShortPassSpeed <= 14f
                && passer.Pass.MediumPassSpeed >= 14f && passer.Pass.MediumPassSpeed <= 20f
                && passer.Pass.LongPassSpeed >= 20f && passer.Pass.LongPassSpeed <= 26f
                && passer.Pass.CalculatePassSpeed(passer.Pass.ShortPassDistance)
                    < passer.Pass.CalculatePassSpeed(passer.Pass.MediumPassDistance)
                && passer.Pass.CalculatePassSpeed(passer.Pass.MediumPassDistance)
                    < passer.Pass.CalculatePassSpeed(passer.Pass.LongPassDistance)
                && receiver.PassReception.Radius >= 1f
                && receiver.PassReception.EntrySpeed > 0f
                && receiver.PassReception.EntrySpeed < passer.Pass.LongPassSpeed
                && PassController.EvaluateLeadSeconds(0f) < PassController.EvaluateLeadSeconds(1f)
                && PassController.EvaluateMaximumDeviation(0f) > PassController.EvaluateMaximumDeviation(1f);
            StagePuckAtStick(puck, passer);
            bool passerClaimed = puck.TryClaim(passer, passer.Stick);
            passer.Pass.Tick(false, true);
            PlayerController recommendationBeforeMove = passer.Pass.RecommendedTarget;
            bool recommendationShown = recommendationBeforeMove != null
                && passer.Pass.FeedbackVisible && passer.Pass.VisiblePathDotCount == 9;
            passer.Movement.SetInput(Vector2.left);
            passer.Pass.Tick(false, true);
            bool movementInputIndependent = passer.Pass.RecommendedTarget == recommendationBeforeMove;
            int releasesBeforePass = puck.ImpulseReleaseSequence;
            bool carriedBeforePassTap = puck.IsCarriedBy(passer);
            bool tapReleased = passer.Pass.Tick(true, true);
            bool recommendedPassReleased = tapReleased
                && puck.ImpulseReleaseSequence == releasesBeforePass + 1
                && puck.Carrier == null
                && puck.IntendedPassReceiver == recommendationBeforeMove
                && !passer.Pass.FeedbackVisible && passer.Pass.RecommendedTarget == null;
            bool recommendedPassFlow = passerClaimed
                && recommendationShown && movementInputIndependent && recommendedPassReleased;

            StagePuckAtStick(puck, receiver);
            bool receiverClaimed = puck.TryClaim(receiver, receiver.Stick);
            bool humanPossessionAutoControl = receiverClaimed
                && setup.SwitchController.ControlledPlayer == receiver
                && setup.ControlManager.LastAutomaticReason == AutomaticControlReason.HumanPossession
                && hockeyCamera.Target == receiver.transform
                && hockeyCamera.transform.position == cameraBeforeAutoSwitch;
            setup.CaptureDataForValidation();
            rolePersistence &= RolesMatchActorsAndSnapshots(baselineRoles, players, setup.Data);

            setup.SwitchController.SetControlled(passer);
            int automaticSelectionsBeforeRelease = setup.ControlManager.AutomaticSelectionCount;
            PlayerController expectedReleaseController = ClosestTeamPlayer(players, TeamId.Blue, puck.Body.position);
            bool releasedPass = puck.Release(receiver, (receiver.transform.forward + Vector3.up).normalized, 5f);
            bool genericReleaseVelocity = Mathf.Abs(puck.Body.linearVelocity.magnitude - 5f) < 0.01f
                && puck.Body.linearVelocity.y > 0f;
            int automaticSelectionsAfterRelease = setup.ControlManager.AutomaticSelectionCount;
            puck.Body.position = passer.transform.position;
            Physics.SyncTransforms();
            bool releaseSelectedClosestOnce = setup.SwitchController.ControlledPlayer == expectedReleaseController
                && setup.ControlManager.LastAutomaticReason == AutomaticControlReason.LoosePuck
                && automaticSelectionsAfterRelease == automaticSelectionsBeforeRelease + 1
                && setup.ControlManager.AutomaticSelectionCount == automaticSelectionsAfterRelease;
            puck.ResetPuck(passer.Stick.ControlPoint);
            puck.Body.linearVelocity = passer.transform.forward * (passer.Stick.MaximumClaimSpeed + 1f);
            bool highSpeedLoosePuckRejected = !puck.TryClaim(passer, passer.Stick);
            puck.ResetPuck(passer.Stick.ControlPoint + passer.transform.right * (passer.Stick.ClaimRadius + 0.5f));
            bool distantLoosePuckRejected = !puck.TryClaim(passer, passer.Stick);
            bool ordinaryClaimLimits = highSpeedLoosePuckRejected && distantLoosePuckRejected;
            PlayerController expectedLooseController = ClosestTeamPlayer(players, TeamId.Blue, puck.Body.position);
            bool loosePuckAutoControl = releasedPass && genericReleaseVelocity && releaseSelectedClosestOnce
                && ordinaryClaimLimits
                && puck.IntendedPassReceiver == null
                && setup.SwitchController.ControlledPlayer == expectedLooseController
                && setup.ControlManager.LastAutomaticReason == AutomaticControlReason.LoosePuck;

            opponentCarrier.Movement.ResetMotion(new Vector3(0f, 1f, 0f), Quaternion.Euler(0f, 90f, 0f));
            expectedDefender.Movement.ResetMotion(new Vector3(1.45f, 1f, 0f), Quaternion.identity);
            receiver.Movement.ResetMotion(new Vector3(0f, 1f, -1.25f), Quaternion.identity);
            StagePuckAtStick(puck, opponentCarrier);
            bool opponentClaimed = puck.TryClaim(opponentCarrier, opponentCarrier.Stick);
            float expectedDefenderDistance = Vector3.Distance(expectedDefender.transform.position, puck.Body.position);
            bool expectedDefenderClosest = expectedDefenderDistance < Vector3.Distance(passer.transform.position, puck.Body.position)
                && expectedDefenderDistance < Vector3.Distance(receiver.transform.position, puck.Body.position);
            bool opponentPossessionAutoDefense = opponentClaimed
                && expectedDefenderClosest
                && setup.SwitchController.ControlledPlayer == expectedDefender
                && setup.ControlManager.LastAutomaticReason == AutomaticControlReason.OpponentPossession
                && hockeyCamera.Target == expectedDefender.transform;
            setup.CaptureDataForValidation();
            rolePersistence &= RolesMatchActorsAndSnapshots(baselineRoles, players, setup.Data);

            PointerEventData heldOffensePointer = new(EventSystem.current)
            {
                pointerId = 81,
                button = PointerEventData.InputButton.Left
            };
            bool defensiveControlMode = setup.HumanInput.Mode == MobileActionMode.Defense
                && passButton.Label == "SWITCH" && shootButton.Label == "CHECK"
                && passButton.gameObject.activeSelf && shootButton.gameObject.activeSelf
                && !dekeButton.gameObject.activeSelf;
            shootButton.OnPointerDown(heldOffensePointer);
            puck.ResetPuck(Vector3.zero);
            shootButton.OnPointerUp(heldOffensePointer);
            bool heldTransitionCleared = setup.HumanInput.Mode == MobileActionMode.Loose
                && passButton.Label == "SWITCH" && shootButton.Label == "SHOOT"
                && passButton.gameObject.activeSelf && !dekeButton.gameObject.activeSelf
                && !shootButton.gameObject.activeSelf && !setup.HumanInput.ShootReleased
                && !setup.HumanInput.CheckPressed;

            StagePuckAtStick(puck, opponentCarrier);
            bool opponentReclaimedForSwitch = puck.TryClaim(opponentCarrier, opponentCarrier.Stick);
            PointerEventData switchPointer = new(EventSystem.current)
            {
                pointerId = 82,
                button = PointerEventData.InputButton.Left
            };
            passButton.OnPointerDown(switchPointer);
            bool touchSwitchRouted = opponentReclaimedForSwitch && setup.HumanInput.SwitchPressed;
            setup.SwitchController.SwitchToBest();
            passButton.OnPointerUp(switchPointer);

            bool manualOverride = setup.SwitchController.ControlledPlayer != expectedDefender
                && touchSwitchRouted
                && hockeyCamera.Target == setup.SwitchController.ControlledPlayer.transform;
            setup.CaptureDataForValidation();
            rolePersistence &= RolesMatchActorsAndSnapshots(baselineRoles, players, setup.Data);
            bool reliablePassOutcomes = VerifyPassOutcomes(passer, receiver, opponentCarrier, puck, players,
                out bool shortPassReceived, out bool mediumPassReceived, out bool longPassReceived,
                out bool movingPassReceived, out bool obstructedPassIntercepted, out bool missedPassStayedLoose);
            StagePuckAtStick(puck, receiver);
            bool receiverReclaimedAfterOutcomes = puck.TryClaim(receiver, receiver.Stick);
            bool passReceptionAutoControl = reliablePassOutcomes && receiverReclaimedAfterOutcomes
                && setup.SwitchController.ControlledPlayer == receiver
                && setup.ControlManager.LastAutomaticReason == AutomaticControlReason.HumanPossession;
            bool defensiveChecks = VerifyDefensiveChecks(setup, players, opponentCarrier, puck,
                out bool tuningBounds, out bool bodyCheck, out bool pullCheck, out bool sharedCooldown,
                out bool rejectedCheck, out bool impulseReset, out bool looseAfterCheck);
            bool repeatedControlTransitions = VerifyRepeatedControlTransitions(setup.HumanInput,
                passer, receiver, opponentCarrier, players, puck, setup.SwitchController, setup.ControlManager,
                hockeyCamera, passButton, dekeButton, shootButton);
            bool opponentPuckDecisions = VerifyOpponentPuckDecisions(players, puck, setup.DefenseController,
                out bool cornerPuckPursuit, out bool opponentPressureCheck,
                out bool tacticalPassIntent, out bool shotIntent, out bool singlePuckChaser);
            bool attributeSystem = VerifyAttributeSystem(setup, players, puck,
                out bool attributeBudget, out bool attributePresets, out bool attributeMovement,
                out bool attributeStamina, out bool attributeDeke, out bool attributePuckControl,
                out bool attributeShot, out bool attributePass, out bool attributeReception,
                out bool attributeChecks, out bool attributeSnapshots, out bool aiAttributeSeparation);

            bool offsideRule = VerifyOffsideRule(setup, players, goalies, puck, offside,
                out bool offsideWarningVisual, out bool offsideTagUp, out bool offsideLooseRetention,
                out bool offsideTurnoverClear, out bool blueOffsideRestart, out bool redOffsideRestart,
                out bool legalZoneEntry, out bool offsideFaceoffResume);

            GoalTrigger blueScoringGoal = FindGoal(TeamId.Blue);
            GoalTrigger redScoringGoal = FindGoal(TeamId.Red);
            bool hockeySizedGoals = GoalWidth(blueScoringGoal) >= 2.75f && GoalWidth(blueScoringGoal) <= 2.85f
                && GoalWidth(redScoringGoal) >= 2.75f && GoalWidth(redScoringGoal) <= 2.85f
                && GoalPostHalfWidth("Blue Goal") >= 1.45f && GoalPostHalfWidth("Blue Goal") <= 1.55f
                && GoalPostHalfWidth("Red Goal") >= 1.45f && GoalPostHalfWidth("Red Goal") <= 1.55f
                && GoalHasPhysicalNet("Blue Goal") && GoalHasPhysicalNet("Red Goal");
            bool physicalBackNetStopsPuck = VerifyBackSideNetImpact(
                "Red Goal", blueScoringGoal, puck, setup.MatchController);
            BoxCollider blueGoalVolume = blueScoringGoal.GetComponent<BoxCollider>();
            Vector3 blueGoalLine = blueScoringGoal.transform.position - blueScoringGoal.ScoringDirection * (blueGoalVolume.size.z * 0.5f);
            bool beforeGoalLineRejected = !blueScoringGoal.IsValidEntry(
                blueGoalLine - blueScoringGoal.ScoringDirection * 0.1f,
                blueScoringGoal.ScoringDirection * 5f);
            float stagedGoalOffset = PrototypeRinkGeometry.GoalDepth * 0.25f;
            puck.ResetPuck(blueScoringGoal.transform.position + blueScoringGoal.ScoringDirection * stagedGoalOffset);
            puck.Body.linearVelocity = -blueScoringGoal.ScoringDirection * 5f;
            bool backSideGoalRejected = !blueScoringGoal.TryRegisterGoal(puck)
                && setup.MatchController.BlueScore == 0 && setup.MatchController.RedScore == 0
                && setup.MatchController.State == MatchStateSnapshot.Playing;
            puck.Body.linearVelocity = blueScoringGoal.ScoringDirection * chargedShotPower;
            backSideGoalRejected = backSideGoalRejected && !blueScoringGoal.TryRegisterGoal(puck);
            blueScoringGoal.TickSweptGoalLineForValidation();
            backSideGoalRejected = backSideGoalRejected
                && setup.MatchController.BlueScore == 0 && setup.MatchController.RedScore == 0
                && setup.MatchController.State == MatchStateSnapshot.Playing;
            bool bothGoalDirectionsConfigured = blueScoringGoal.ScoringDirection == Vector3.forward
                && redScoringGoal.ScoringDirection == Vector3.back;

            bool frontSideGoalRegistered = SimulateGoalCrossing(
                blueScoringGoal, puck, setup.MatchController, chargedShotPower);
            bool immediatePostGoalReset = frontSideGoalRegistered
                && beforeGoalLineRejected && backSideGoalRejected && bothGoalDirectionsConfigured
                && setup.MatchController.BlueScore == 1 && setup.MatchController.RedScore == 0
                && setup.MatchController.State == MatchStateSnapshot.GoalPause
                && VerifyFaceoffFormation(players, goalies, puck);
            setup.CaptureDataForValidation();
            rolePersistence &= RolesMatchActorsAndSnapshots(baselineRoles, players, setup.Data);
            setup.MatchController.CompleteGoalPauseForValidation();
            bool postGoalFaceoffEntry = setup.MatchController.State == MatchStateSnapshot.Faceoff
                && VerifyFaceoffFormation(players, goalies, puck);
            setup.CaptureDataForValidation();
            rolePersistence &= RolesMatchActorsAndSnapshots(baselineRoles, players, setup.Data);
            bool firstGoalFlow = immediatePostGoalReset && postGoalFaceoffEntry;

            setup.MatchController.StartPlayImmediatelyForValidation();
            puck.ResetPuck(redScoringGoal.transform.position - redScoringGoal.ScoringDirection * stagedGoalOffset);
            puck.Body.linearVelocity = redScoringGoal.ScoringDirection * 5f;
            bool oppositeFrontSideGoalRegistered = redScoringGoal.TryRegisterGoal(puck);
            bool bothDirectionsScored = oppositeFrontSideGoalRegistered
                && setup.MatchController.BlueScore == 1 && setup.MatchController.RedScore == 1
                && setup.MatchController.State == MatchStateSnapshot.GoalPause;

            setup.MatchController.StartPlayImmediatelyForValidation();
            puck.ResetPuck(blueScoringGoal.transform.position - blueScoringGoal.ScoringDirection * stagedGoalOffset);
            puck.Body.linearVelocity = blueScoringGoal.ScoringDirection * 5f;
            bool winningGoalRegistered = blueScoringGoal.TryRegisterGoal(puck);
            bool goalFlow = firstGoalFlow && bothDirectionsScored && winningGoalRegistered
                && setup.MatchController.BlueScore == 2 && setup.MatchController.RedScore == 1
                && Vector3.Distance(puck.Body.position, new Vector3(0f, 0.55f, 0f)) < 0.05f;
            setup.MatchController.ExpireImmediatelyForValidation();
            bool resultFlow = setup.MatchController.State == MatchStateSnapshot.Finished
                && setup.MatchController.RemainingSeconds == 0f && setup.MatchController.ResultText == "HUMAN TEAM WINS";

            if (!cleanWASDInput || !cleanJoystickInput || !cleanCameraCollider || !cleanAnimation || !cleanFootAlignment)
                throw new System.InvalidOperationException(
                    $"CLEAN_PLAYER_SMOKE_FAIL wasd={cleanWASDInput} joystick={cleanJoystickInput} cameraCollider={cleanCameraCollider} animation={cleanAnimation} footAlignment={cleanFootAlignment}");

            if (!roster || !roleDistribution || !faceoffFormation || !spacedRoleTargets || !rolePersistence
                || !smallerSkaters || !broaderGoalies || !modular || !modularCharacter || !teamCharacterMaterials || !presentation || !arenaPresentation || !puckIndependent || !snapshots
                || !humanPossessionAutoControl || !loosePuckAutoControl || !opponentPossessionAutoDefense
                || !manualOverride || !recommendedPassFlow || !reliablePassTuning || !reliablePassOutcomes || !passReceptionAutoControl
                || !defensiveControlMode || !heldTransitionCleared || !defensiveChecks || !repeatedControlTransitions
                || !opponentPuckDecisions || !attributeSystem || !offsideRule || !puckBoardContainment
                || !puckSizeAndPosition || !forgivingPickup
                || !velocityMatchedControl || !hardShotTuning || !airborneShotRelease
                || !hockeySizedGoals || !physicalBackNetStopsPuck || !goalFlow || !resultFlow)
                throw new System.InvalidOperationException($"PHASE1_PVE_SMOKE_FAIL roster={roster} roleDistribution={roleDistribution} faceoffFormation={faceoffFormation} spacedRoleTargets={spacedRoleTargets} rolePersistence={rolePersistence} immediatePostGoalReset={immediatePostGoalReset} postGoalFaceoffEntry={postGoalFaceoffEntry} smallerSkaters={smallerSkaters} broaderGoalies={broaderGoalies} modular={modular} modularCharacter={modularCharacter} presentation={presentation} arenaPresentation={arenaPresentation} controlHierarchy={controlHierarchy} controlScaling={controlScaling} actionLayout={actionLayout} refinedControlVisuals={refinedControlVisuals} fixedJoystick={fixedJoystick} analogInput={analogInput} sourceSelection={sourceSelection} pointerOwnership={pointerOwnership} hardwareActionContract={hardwareActionContract} defensiveControlMode={defensiveControlMode} heldTransitionCleared={heldTransitionCleared} repeatedControlTransitions={repeatedControlTransitions} opponentPuckDecisions={opponentPuckDecisions} singlePuckChaser={singlePuckChaser} cornerPuckPursuit={cornerPuckPursuit} opponentPressureCheck={opponentPressureCheck} tacticalPassIntent={tacticalPassIntent} shotIntent={shotIntent} tuningBounds={tuningBounds} bodyCheck={bodyCheck} pullCheck={pullCheck} sharedCooldown={sharedCooldown} rejectedCheck={rejectedCheck} impulseReset={impulseReset} looseAfterCheck={looseAfterCheck} puckIndependent={puckIndependent} snapshots={snapshots} puckSizeAndPosition={puckSizeAndPosition} forgivingPickup={forgivingPickup} velocityMatchedControl={velocityMatchedControl} hardShotTuning={hardShotTuning} airborneShotRelease={airborneShotRelease} reliablePassTuning={reliablePassTuning} reliablePassOutcomes={reliablePassOutcomes} shortPassReceived={shortPassReceived} mediumPassReceived={mediumPassReceived} longPassReceived={longPassReceived} movingPassReceived={movingPassReceived} obstructedPassIntercepted={obstructedPassIntercepted} missedPassStayedLoose={missedPassStayedLoose} passReceptionAutoControl={passReceptionAutoControl} genericReleaseVelocity={genericReleaseVelocity} ordinaryClaimLimits={ordinaryClaimLimits} tapShotPower={tapShotPower} chargedShotPower={chargedShotPower} hockeySizedGoals={hockeySizedGoals} physicalBackNetStopsPuck={physicalBackNetStopsPuck} puckScale={puckScale} controlOffset={controlOffset} recommendedPassFlow={recommendedPassFlow} recommendationShown={recommendationShown} recommendedTarget={recommendationBeforeMove?.PlayerId} movementInputIndependent={movementInputIndependent} carriedBeforePassTap={carriedBeforePassTap} tapReleased={tapReleased} releaseSequenceBefore={releasesBeforePass} recommendedPassReleased={recommendedPassReleased} humanPossessionAutoControl={humanPossessionAutoControl} loosePuckAutoControl={loosePuckAutoControl} opponentPossessionAutoDefense={opponentPossessionAutoDefense} manualOverride={manualOverride} offsideRule={offsideRule} offsideWarningVisual={offsideWarningVisual} offsideTagUp={offsideTagUp} offsideLooseRetention={offsideLooseRetention} offsideTurnoverClear={offsideTurnoverClear} blueOffsideRestart={blueOffsideRestart} redOffsideRestart={redOffsideRestart} legalZoneEntry={legalZoneEntry} offsideFaceoffResume={offsideFaceoffResume} beforeGoalLineRejected={beforeGoalLineRejected} backSideGoalRejected={backSideGoalRejected} bothDirectionsConfigured={bothGoalDirectionsConfigured} frontSideGoalRegistered={frontSideGoalRegistered} oppositeFrontSideGoalRegistered={oppositeFrontSideGoalRegistered} bothDirectionsScored={bothDirectionsScored} winningGoalRegistered={winningGoalRegistered} goalFlow={goalFlow} resultFlow={resultFlow} attributeSystem={attributeSystem} attributeBudget={attributeBudget} attributePresets={attributePresets} attributeMovement={attributeMovement} attributeStamina={attributeStamina} attributeDeke={attributeDeke} attributePuckControl={attributePuckControl} attributeShot={attributeShot} attributePass={attributePass} attributeReception={attributeReception} attributeChecks={attributeChecks} attributeSnapshots={attributeSnapshots} aiAttributeSeparation={aiAttributeSeparation}");

            Debug.Log("PHASE1_PVE_SMOKE_PASS skaters=10 modularHumanoids=12 boundSkaters=10 idleGoalies=2 twoHandIK=true roles=C_LW_RW_LD_RD rolePersistence=true spacedRoleTargets=true singlePuckChaser=true centerFaceoff=true postGoalFaceoff=true offsideWarning=true offsideTagUp=true offsideTurnoverClear=true sweptOffsideOrdering=true mirroredOffside=true neutralDotRestart=true offsideFaceoffResume=true smallerSkaters=true goalies=2 broaderGoalies=true mobileArena=true elongatedRink=true layeredBoards=true dimensionalNets=true hockeySizedGoals=true alignedArenaAnchors=true closeCamera=true humanInputs=1 aiSkaters=9 controls=OFFENSE_PASS_DEKE_SHOOT_LOOSE_SWITCH_DEFENSE_SWITCH_CHECK adaptiveControls=true heldTransitionCleared=true repeatedControlTransitions=true hardwareActionContract=true bodyCheck=true pullCheck=true sharedCheckCooldown=true rejectedCheck=true boundedCheckImpulse=true resetClearsImpulse=true looseAfterCheck=true nonHomingCheckRelease=true opponentCornerPuckPursuit=true opponentPressureCheck=true opponentTacticalPass=true opponentShotIntent=true attributeBudget=true attributePresets=true attributeMovement=true attributeStamina=true attributeDeke=true attributePuckControl=true attributeShot=true deterministicShot=true attributePass=true deterministicPass=true attributeReception=true attributeChecks=true attributeSnapshots=true aiAttributeSeparation=true largerActionButtons=true equalActionSizes=true unityUI=true safeArea=true referenceResolution=1920x1080 fixedJoystick=true persistentJoystick=true circularControls=true separateHitVisuals=true nonOverlappingActions=true deadZone=true analog=true independentPointers=true movementClamped=true movementOnly=true recommendedPassTarget=true dottedPassPath=true tapPass=true distanceScaledPass=true configurableReceptionZone=true shortPassReceived=true mediumPassReceived=true longPassReceived=true movingPassReceived=true passReceptionAutoControl=true obstructedPassIntercepted=true missedPassStayedLoose=true genericReleaseVelocity=true ordinaryClaimLimits=true harderShots=true hardChargedShot=true continuousPuckCollision=true possessionAutoControl=true loosePuckAutoControl=true opponentAutoDefense=true touchSwitch=true keyboardSwitchOverride=true cameraRetargetSmooth=true puckIndependent=true smallerPuck=true frontPuckControl=true forgivingPickup=true velocityMatchedPuck=true oneWayGoals=true beforeGoalLineRejected=true bothDirectionsScored=true backSideGoalRejected=true goalReset=true timerResult=true");
        }

        private static bool VerifyCharacterTeamMaterials(PlayerController[] players, HockeyGoalieAI[] goalies)
        {
            Material blueTeamMaterial = null;
            Material redTeamMaterial = null;
            for (int i = 0; i < players.Length; i++)
            {
                HockeyCharacterPresentation presentation = players[i].GetComponent<HockeyCharacterPresentation>();
                HockeyEquipmentLoadout loadout = players[i].GetComponent<HockeyEquipmentLoadout>();
                Material teamMaterial = FirstCharacterMaterial(presentation);
                bool characterApplied = teamMaterial != null && AllCharacterRenderersUse(presentation, teamMaterial);
                bool wearablesExcluded = teamMaterial != null && WearablesExcludeMaterial(loadout, teamMaterial);
                if (!characterApplied || !wearablesExcluded)
                {
                    Debug.LogWarning($"TEAM_CHARACTER_MATERIAL_FAIL player={players[i].PlayerId} "
                        + $"material={teamMaterial?.name} characterApplied={characterApplied} wearablesExcluded={wearablesExcluded}");
                    return false;
                }
                if (players[i].Team == TeamId.Blue)
                {
                    blueTeamMaterial ??= teamMaterial;
                    if (teamMaterial != blueTeamMaterial) return false;
                }
                else
                {
                    redTeamMaterial ??= teamMaterial;
                    if (teamMaterial != redTeamMaterial) return false;
                }
            }
            for (int i = 0; i < goalies.Length; i++)
            {
                HockeyCharacterPresentation presentation = goalies[i].GetComponent<HockeyCharacterPresentation>();
                HockeyEquipmentLoadout loadout = goalies[i].GetComponent<HockeyEquipmentLoadout>();
                Material expectedMaterial = goalies[i].Team == TeamId.Blue ? blueTeamMaterial : redTeamMaterial;
                bool characterApplied = expectedMaterial != null
                    && AllCharacterRenderersUse(presentation, expectedMaterial);
                bool wearablesExcluded = expectedMaterial != null
                    && WearablesExcludeMaterial(loadout, expectedMaterial);
                if (!characterApplied || !wearablesExcluded)
                {
                    Debug.LogWarning($"TEAM_CHARACTER_MATERIAL_FAIL goalie={goalies[i].Team} "
                        + $"material={expectedMaterial?.name} characterApplied={characterApplied} wearablesExcluded={wearablesExcluded}");
                    return false;
                }
            }
            bool passed = blueTeamMaterial != null && redTeamMaterial != null
                && blueTeamMaterial != redTeamMaterial;
            if (passed) Debug.Log("TEAM_CHARACTER_MATERIAL_PASS teams=Blue,Red wearablesExcluded=true");
            return passed;
        }

        private static bool VerifyModularCharacters(PlayerController[] players, HockeyGoalieAI[] goalies)
        {
            HockeyCharacterPresentation[] presentations = Object.FindObjectsByType<HockeyCharacterPresentation>();
            HockeyEquipmentLoadout[] loadouts = Object.FindObjectsByType<HockeyEquipmentLoadout>();
            HockeyStickRig[] rigs = Object.FindObjectsByType<HockeyStickRig>();
            if (presentations.Length != 12 || loadouts.Length != 12 || rigs.Length != 12) return false;
            Material blueTeamMaterial = null;
            Material redTeamMaterial = null;
            for (int i = 0; i < players.Length; i++)
            {
                HockeyCharacterPresentation presentation = players[i].GetComponent<HockeyCharacterPresentation>();
                HockeyEquipmentLoadout loadout = players[i].GetComponent<HockeyEquipmentLoadout>();
                HockeyStickRig rig = players[i].GetComponent<HockeyStickRig>();
                Transform visualBlade = loadout != null
                    ? loadout.FindEquippedChild(HockeyEquipmentSlot.Stick, "Stick Blade") : null;
                Material teamMaterial = FirstCharacterMaterial(presentation);
                bool productionSkates = VerifyIntegratedSkates(loadout, players[i].transform, true);
                if (players[i].Team == TeamId.Blue)
                {
                    blueTeamMaterial ??= teamMaterial;
                    if (teamMaterial != blueTeamMaterial) return false;
                }
                else
                {
                    redTeamMaterial ??= teamMaterial;
                    if (teamMaterial != redTeamMaterial) return false;
                }
                bool valid = presentation != null && presentation.IsBound && presentation.BoundPlayer == players[i]
                    && presentation.Animator != null && presentation.Animator.avatar != null
                    && presentation.Animator.avatar.isHuman && presentation.Animator.avatar.isValid
                    && !presentation.Animator.applyRootMotion && loadout != null && loadout.IsComplete()
                    && teamMaterial != null && AllCharacterRenderersUse(presentation, teamMaterial)
                    && WearablesExcludeMaterial(loadout, teamMaterial)
                    && productionSkates
                    && rig != null && rig.HasValidReferences
                    && rig.LeftHandConstraint.weight >= 0.99f && rig.RightHandConstraint.weight >= 0.99f
                    && visualBlade != null && Vector3.Distance(visualBlade.position, players[i].ControlPoint) <= 0.25f;
                if (!valid)
                {
                    GameObject skateMarker = loadout?.GetEquipped(HockeyEquipmentSlot.Skates);
                    Transform integratedVisual = players[i].transform.Find("Visual/Male_Base_IntegratedSkates_Visual");
                    Animator integratedAnimator = integratedVisual?.GetComponentInChildren<Animator>(true);
                    throw new System.InvalidOperationException(
                        $"MODULAR_CHARACTER_PLAYER_FAIL player={players[i].PlayerId} bound={presentation?.IsBound} "
                        + $"animator={presentation?.Animator != null} loadout={loadout?.IsComplete()} "
                        + $"productionSkates={productionSkates} rig={rig?.HasValidReferences} "
                        + $"bladeDistance={(visualBlade != null ? Vector3.Distance(visualBlade.position, players[i].ControlPoint) : -1f):F4} "
                        + $"marker={skateMarker?.name}/{skateMarker?.activeInHierarchy} visual={integratedVisual != null} "
                        + $"markerChildren={skateMarker?.transform.childCount} markerRenderers={skateMarker?.GetComponentsInChildren<Renderer>(true).Length} "
                        + $"markerFollower={skateMarker?.GetComponent<HockeyPairedEquipmentFollower>() != null} renderers={integratedVisual?.GetComponentsInChildren<Renderer>(true).Length} "
                        + $"retired={players[i].GetComponentsInChildren<Transform>(true).Any(value => value.name == "Skate_L_v1" || value.name == "Skate_R_v1")} "
                        + $"feet={loadout?.LeftFoot == integratedAnimator?.GetBoneTransform(HumanBodyBones.LeftFoot)}/"
                        + $"{loadout?.RightFoot == integratedAnimator?.GetBoneTransform(HumanBodyBones.RightFoot)} "
                        + $"scale={players[i].transform.localScale} visualY={players[i].transform.Find("Visual")?.localPosition.y:F4}");
                }
            }
            for (int i = 0; i < goalies.Length; i++)
            {
                HockeyCharacterPresentation presentation = goalies[i].GetComponent<HockeyCharacterPresentation>();
                HockeyEquipmentLoadout loadout = goalies[i].GetComponent<HockeyEquipmentLoadout>();
                Material expectedMaterial = goalies[i].Team == TeamId.Blue ? blueTeamMaterial : redTeamMaterial;
                if (presentation == null || presentation.IsBound
                    || presentation.CurrentPresentationState != HockeyPresentationState.Idle
                    || expectedMaterial == null || !AllCharacterRenderersUse(presentation, expectedMaterial)
                    || !WearablesExcludeMaterial(loadout, expectedMaterial)
                    || !VerifyIntegratedSkates(loadout, goalies[i].transform, false)) return false;
            }
            bool passed = blueTeamMaterial != null && redTeamMaterial != null && blueTeamMaterial != redTeamMaterial;
            if (passed) Debug.Log("INTEGRATED_SKATES_RUNTIME_PASS skaters=10/10 goalies=2/2");
            return passed;
        }

        private static bool VerifyIntegratedSkates(HockeyEquipmentLoadout loadout,
            Transform actorRoot, bool requireIceContact)
        {
            GameObject item = loadout != null ? loadout.GetEquipped(HockeyEquipmentSlot.Skates) : null;
            Transform characterVisual = actorRoot != null
                ? actorRoot.Find("Visual/Male_Base_IntegratedSkates_Visual") : null;
            Animator animator = characterVisual != null
                ? characterVisual.GetComponentInChildren<Animator>(true) : null;
            Renderer[] renderers = characterVisual != null
                ? characterVisual.GetComponentsInChildren<Renderer>(true) : System.Array.Empty<Renderer>();
            Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : default;
            for (int index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            Transform visualRoot = actorRoot != null ? actorRoot.Find("Visual") : null;
            bool contact = !requireIceContact || (visualRoot != null
                && Mathf.Abs(visualRoot.localPosition.y - 0.160f) <= 0.002f);
            bool height = !requireIceContact || (actorRoot != null
                && Mathf.Abs(actorRoot.localScale.x - 0.68f) <= 0.001f
                && Mathf.Abs(actorRoot.localScale.y - 0.68f) <= 0.001f
                && Mathf.Abs(actorRoot.localScale.z - 0.68f) <= 0.001f);
            bool retiredObjects = actorRoot != null && actorRoot.GetComponentsInChildren<Transform>(true)
                .Any(transform => transform.name == "Skate_L_v1" || transform.name == "Skate_R_v1");
            bool valid = item != null && item.activeInHierarchy && item.name == "Integrated Skates"
                && item.transform.childCount == 0
                && item.GetComponentsInChildren<Renderer>(true).Length == 0
                && item.GetComponent<HockeyPairedEquipmentFollower>() == null
                && characterVisual != null && animator != null && renderers.Length > 0
                && loadout.LeftFoot == animator.GetBoneTransform(HumanBodyBones.LeftFoot)
                && loadout.RightFoot == animator.GetBoneTransform(HumanBodyBones.RightFoot)
                && !retiredObjects;
            if (!valid)
                Debug.LogWarning($"INTEGRATED_SKATES_DIAGNOSTIC actor={actorRoot?.name} marker={item?.name} "
                    + $"renderers={renderers.Length} contact={contact} minY={bounds.min.y:F4} "
                    + $"height={bounds.size.y:F4} retiredObjects={retiredObjects}");
            return valid;
        }

        private static int CountTriangles(Mesh mesh)
        {
            long indices = 0;
            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                indices += (long)mesh.GetIndexCount(subMesh);
            return (int)(indices / 3);
        }

        private static Material FirstCharacterMaterial(HockeyCharacterPresentation presentation)
        {
            if (presentation == null || presentation.CharacterRenderers.Count == 0
                || presentation.CharacterRenderers[0] == null) return null;
            Material[] materials = presentation.CharacterRenderers[0].sharedMaterials;
            return materials.Length > 0 ? materials[0] : null;
        }

        private static bool AllCharacterRenderersUse(HockeyCharacterPresentation presentation, Material material)
        {
            if (presentation == null || material == null || presentation.CharacterRenderers.Count == 0) return false;
            for (int i = 0; i < presentation.CharacterRenderers.Count; i++)
            {
                Renderer renderer = presentation.CharacterRenderers[i];
                if (renderer == null || renderer.sharedMaterials.Length == 0) return false;
                for (int materialIndex = 0; materialIndex < renderer.sharedMaterials.Length; materialIndex++)
                    if (renderer.sharedMaterials[materialIndex] != material) return false;
            }
            return true;
        }

        private static bool WearablesExcludeMaterial(HockeyEquipmentLoadout loadout, Material material)
        {
            if (loadout == null || material == null) return false;
            HockeyEquipmentSlot[] wearableSlots =
            {
                HockeyEquipmentSlot.Helmet,
                HockeyEquipmentSlot.Visor,
                HockeyEquipmentSlot.Gloves,
                HockeyEquipmentSlot.Skates
            };
            for (int slotIndex = 0; slotIndex < wearableSlots.Length; slotIndex++)
            {
                GameObject equipment = loadout.GetEquipped(wearableSlots[slotIndex]);
                if (equipment == null) return false;
                Renderer[] renderers = equipment.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) return false;
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                    for (int materialIndex = 0; materialIndex < renderers[rendererIndex].sharedMaterials.Length; materialIndex++)
                        if (renderers[rendererIndex].sharedMaterials[materialIndex] == material) return false;
            }
            return true;
        }

        private static bool VerifyPuckBoardContainment(PuckController puck)
        {
            float clearance = puck.ConfiguredRinkClearance;
            Vector3 interior = new(2f, PrototypeRinkGeometry.PuckY, 3f);
            Vector3 unchangedInterior = PuckController.ClampToRoundedRink(interior,
                PrototypeRinkGeometry.Width, PrototypeRinkGeometry.Length,
                PrototypeRinkGeometry.CornerRadius, clearance);
            Vector3 straightBoard = PuckController.ClampToRoundedRink(
                new Vector3(0f, PrototypeRinkGeometry.PuckY, PrototypeRinkGeometry.Length),
                PrototypeRinkGeometry.Width, PrototypeRinkGeometry.Length,
                PrototypeRinkGeometry.CornerRadius, clearance);
            Vector3 cornerBoard = PuckController.ClampToRoundedRink(
                new Vector3(PrototypeRinkGeometry.Width, PrototypeRinkGeometry.PuckY, PrototypeRinkGeometry.Length),
                PrototypeRinkGeometry.Width, PrototypeRinkGeometry.Length,
                PrototypeRinkGeometry.CornerRadius, clearance);
            Vector3 outwardCarryVelocity = Vector3.forward * 9.6f;
            Vector3 constrainedCarryTarget = puck.ConstrainCarryMotionForValidation(
                new Vector3(0f, PrototypeRinkGeometry.PuckY, PrototypeRinkGeometry.Length),
                ref outwardCarryVelocity);
            Vector3 cornerOutward = new Vector3(1f, 0f, 1f).normalized * 9.6f;
            Vector3 constrainedCornerTarget = puck.ConstrainCarryMotionForValidation(
                new Vector3(PrototypeRinkGeometry.Width, PrototypeRinkGeometry.PuckY, PrototypeRinkGeometry.Length),
                ref cornerOutward);

            float coreX = PrototypeRinkGeometry.Width * 0.5f - PrototypeRinkGeometry.CornerRadius;
            float coreZ = PrototypeRinkGeometry.Length * 0.5f - PrototypeRinkGeometry.CornerRadius;
            float innerRadius = PrototypeRinkGeometry.CornerRadius - clearance;
            Vector3 constrainedCornerNormal = new(
                constrainedCornerTarget.x - coreX, 0f, constrainedCornerTarget.z - coreZ);
            bool geometryContained = clearance >= 0.4f
                && Vector3.Distance(unchangedInterior, interior) < 0.001f
                && straightBoard.z <= PrototypeRinkGeometry.Length * 0.5f - clearance + 0.001f
                && new Vector2(cornerBoard.x - coreX, cornerBoard.z - coreZ).sqrMagnitude
                    <= innerRadius * innerRadius + 0.001f
                && constrainedCarryTarget.z <= PrototypeRinkGeometry.Length * 0.5f - clearance + 0.001f
                && outwardCarryVelocity.z <= 0.001f
                && new Vector2(constrainedCornerTarget.x - coreX, constrainedCornerTarget.z - coreZ).sqrMagnitude
                    <= innerRadius * innerRadius + 0.001f
                && Vector3.Dot(cornerOutward, constrainedCornerNormal.normalized) <= 0.001f;

            puck.ResetPuck(new Vector3(0f, PrototypeRinkGeometry.PuckY, PrototypeRinkGeometry.Length));
            puck.Body.linearVelocity = Vector3.forward * 8f;
            puck.TickRinkContainmentForValidation();
            bool liveRecovery = puck.Body.position.z
                    <= PrototypeRinkGeometry.Length * 0.5f - clearance + 0.001f
                && puck.Body.linearVelocity.z <= 0.001f;
            puck.ResetPuck(new Vector3(
                PrototypeRinkGeometry.Width, PrototypeRinkGeometry.PuckY, PrototypeRinkGeometry.Length));
            puck.Body.linearVelocity = new Vector3(6f, 0f, 6f);
            puck.TickRinkContainmentForValidation();
            bool liveCornerRecovery = new Vector2(
                    puck.Body.position.x - coreX, puck.Body.position.z - coreZ).sqrMagnitude
                    <= innerRadius * innerRadius + 0.001f
                && Vector3.Dot(puck.Body.linearVelocity, new Vector3(
                    puck.Body.position.x - coreX, 0f, puck.Body.position.z - coreZ).normalized) <= 0.001f;
            puck.ResetPuck(new Vector3(0f, PrototypeRinkGeometry.PuckY, 0f));
            return geometryContained && liveRecovery && liveCornerRecovery;
        }

        private static bool VerifyOffsideRule(LocalMatchSetup setup, PlayerController[] players,
            HockeyGoalieAI[] goalies, PuckController puck, OffsideController offside,
            out bool warningVisual, out bool tagUp, out bool looseRetention, out bool turnoverClear,
            out bool blueRestart, out bool redRestart, out bool legalEntry, out bool faceoffResume)
        {
            warningVisual = tagUp = looseRetention = turnoverClear = false;
            blueRestart = redRestart = legalEntry = faceoffResume = false;
            if (setup == null || offside == null || puck == null) return false;
            offside.SetRuleEnabledForValidation(true);

            float line = PrototypeRinkGeometry.AttackingBlueLineDistance;
            bool exactLineOutside = !OffsideController.IsInsideOffensiveZone(
                    TeamId.Blue, new Vector3(0f, 1f, line))
                && !OffsideController.IsInsideOffensiveZone(
                    TeamId.Red, new Vector3(0f, 1f, -line));
            bool sweptBothDirections = OffsideController.CrossedAttackingBlueLine(
                    TeamId.Blue, new Vector3(0f, 0f, line), new Vector3(0f, 0f, line + 0.1f))
                && OffsideController.CrossedAttackingBlueLine(
                    TeamId.Red, new Vector3(0f, 0f, -line), new Vector3(0f, 0f, -line - 0.1f));
            warningVisual = WarningGridIsRedAndColliderFree(offside.BlueWarningObject)
                && WarningGridIsRedAndColliderFree(offside.RedWarningObject);

            PlayerController blueCarrier = FindPlayer(players, "blue-1");
            PlayerController bluePremature = FindPlayer(players, "blue-2");
            PlayerController redCarrier = FindPlayer(players, "red-1");
            PlayerController redPremature = FindPlayer(players, "red-2");
            int blueScore = setup.MatchController.BlueScore;
            int redScore = setup.MatchController.RedScore;

            bool blueWarning = PrepareOffsideWarning(players, puck, offside, blueCarrier, bluePremature,
                new Vector3(3f, 1f, line - 2.5f), new Vector3(0f, 1f, line + 1f))
                && offside.WarnedTeam == TeamId.Blue && offside.BlueWarningVisible && !offside.RedWarningVisible;
            bluePremature.Movement.ResetMotion(new Vector3(0f, 1f, line), Quaternion.identity);
            offside.TickForValidation(puck.Body.position);
            tagUp = blueWarning && !offside.WarnedTeam.HasValue
                && !offside.BlueWarningVisible && setup.MatchController.State == MatchStateSnapshot.Playing;

            bool rearmedForLoose = PrepareOffsideWarning(players, puck, offside, blueCarrier, bluePremature,
                new Vector3(3f, 1f, line - 2.5f), new Vector3(0f, 1f, line + 1f));
            Vector3 looseStart = puck.Body.position;
            bool released = puck.Release(blueCarrier, Vector3.forward, 5f);
            offside.TickForValidation(looseStart);
            looseRetention = rearmedForLoose && released && puck.Carrier == null
                && !puck.PossessionTeam.HasValue && offside.WarnedTeam == TeamId.Blue
                && offside.BlueWarningVisible;
            puck.Body.position = bluePremature.Stick.ControlPoint;
            puck.transform.position = puck.Body.position;
            puck.Body.linearVelocity = Vector3.zero;
            Physics.SyncTransforms();
            bool prematureReceiverClaimed = puck.TryClaim(bluePremature, bluePremature.Stick);
            offside.TickForValidation(looseStart);
            looseRetention = looseRetention && prematureReceiverClaimed
                && setup.MatchController.State == MatchStateSnapshot.Faceoff
                && setup.MatchController.BlueScore == blueScore && setup.MatchController.RedScore == redScore;
            setup.MatchController.CompleteFaceoffForValidation();

            bool rearmedForTurnover = PrepareOffsideWarning(players, puck, offside, blueCarrier, bluePremature,
                new Vector3(3f, 1f, line - 2.5f), new Vector3(0f, 1f, line + 1f));
            puck.ForceRelease(blueCarrier);
            redCarrier.Movement.ResetMotion(new Vector3(3f, 1f, line - 2.5f), Quaternion.Euler(0f, 180f, 0f));
            puck.Body.position = redCarrier.Stick.ControlPoint;
            puck.transform.position = puck.Body.position;
            Physics.SyncTransforms();
            bool opponentClaimed = puck.TryClaim(redCarrier, redCarrier.Stick);
            offside.TickForValidation(puck.Body.position);
            turnoverClear = rearmedForTurnover && opponentClaimed && puck.PossessionTeam == TeamId.Red
                && !offside.WarnedTeam.HasValue && !offside.BlueWarningVisible && !offside.RedWarningVisible
                && setup.MatchController.State == MatchStateSnapshot.Playing;

            bool armedForSweptTagUp = PrepareOffsideWarning(players, puck, offside, blueCarrier, bluePremature,
                new Vector3(2f, 1f, line - 2.5f), new Vector3(0f, 1f, line + 1f));
            Vector3 sweptTagUpPrevious = new(2f, PrototypeRinkGeometry.PuckY, line - 0.2f);
            puck.Body.position = new Vector3(2f, PrototypeRinkGeometry.PuckY, line + 0.2f);
            puck.transform.position = puck.Body.position;
            bluePremature.Movement.ResetMotion(new Vector3(0f, 1f, line - 0.2f), Quaternion.identity);
            offside.TickForValidation(sweptTagUpPrevious);
            bool sweptTagUpOrdering = armedForSweptTagUp
                && setup.MatchController.State == MatchStateSnapshot.Faceoff
                && setup.MatchController.BlueScore == blueScore && setup.MatchController.RedScore == redScore;
            setup.MatchController.CompleteFaceoffForValidation();

            ResetSkatersToCenter(players);
            blueCarrier.Movement.ResetMotion(new Vector3(-2f, 1f, line - 2.5f), Quaternion.identity);
            bluePremature.Movement.ResetMotion(new Vector3(0f, 1f, line - 0.2f), Quaternion.identity);
            StagePuckAtStick(puck, blueCarrier);
            bool sameTickCarrierClaimed = puck.TryClaim(blueCarrier, blueCarrier.Stick);
            Vector3 sameTickPrevious = new(-2f, PrototypeRinkGeometry.PuckY, line - 0.2f);
            offside.TickForValidation(sameTickPrevious);
            bool initiallyUnwarned = !offside.WarnedTeam.HasValue;
            bluePremature.Movement.ResetMotion(new Vector3(0f, 1f, line + 1f), Quaternion.identity);
            puck.Body.position = new Vector3(-2f, PrototypeRinkGeometry.PuckY, line + 0.2f);
            puck.transform.position = puck.Body.position;
            offside.TickForValidation(sameTickPrevious);
            bool sameTickEntry = sameTickCarrierClaimed && initiallyUnwarned
                && setup.MatchController.State == MatchStateSnapshot.Faceoff
                && setup.MatchController.BlueScore == blueScore && setup.MatchController.RedScore == redScore;
            setup.MatchController.CompleteFaceoffForValidation();

            bool armedForBlueEntry = PrepareOffsideWarning(players, puck, offside, blueCarrier, bluePremature,
                new Vector3(3f, 1f, line - 2.5f), new Vector3(0f, 1f, line + 1f));
            Vector3 bluePrevious = puck.Body.position;
            puck.Body.position = new Vector3(3f, PrototypeRinkGeometry.PuckY, line + 0.25f);
            puck.transform.position = puck.Body.position;
            offside.TickForValidation(bluePrevious);
            Vector3 expectedBlueFaceoff = OffsideController.NearestOffsideFaceoffPoint(TeamId.Blue, 3f);
            bool blueFaceoffState = armedForBlueEntry
                && setup.MatchController.State == MatchStateSnapshot.Faceoff
                && setup.MatchController.BlueScore == blueScore && setup.MatchController.RedScore == redScore
                && Vector3.Distance(setup.MatchController.LastFaceoffPosition, expectedBlueFaceoff) < 0.02f
                && Vector3.Distance(puck.Body.position, expectedBlueFaceoff) < 0.02f
                && !offside.BlueWarningVisible && !offside.RedWarningVisible
                && AllPlayersGameplayEnabled(players, false)
                && VerifyTranslatedFaceoff(players, goalies, expectedBlueFaceoff);
            offside.TickForValidation(bluePrevious);
            blueRestart = blueFaceoffState && setup.MatchController.State == MatchStateSnapshot.Faceoff
                && setup.MatchController.BlueScore == blueScore && setup.MatchController.RedScore == redScore;
            setup.MatchController.CompleteFaceoffForValidation();
            bool blueResumed = setup.MatchController.State == MatchStateSnapshot.Playing
                && AllPlayersGameplayEnabled(players, true);

            bool armedForRedEntry = PrepareOffsideWarning(players, puck, offside, redCarrier, redPremature,
                new Vector3(-3f, 1f, -line + 2.5f), new Vector3(0f, 1f, -line - 1f))
                && offside.WarnedTeam == TeamId.Red && offside.RedWarningVisible && !offside.BlueWarningVisible;
            Vector3 redPrevious = puck.Body.position;
            puck.Body.position = new Vector3(-3f, PrototypeRinkGeometry.PuckY, -line - 0.25f);
            puck.transform.position = puck.Body.position;
            offside.TickForValidation(redPrevious);
            Vector3 expectedRedFaceoff = OffsideController.NearestOffsideFaceoffPoint(TeamId.Red, -3f);
            bool redFaceoffState = armedForRedEntry
                && setup.MatchController.State == MatchStateSnapshot.Faceoff
                && setup.MatchController.BlueScore == blueScore && setup.MatchController.RedScore == redScore
                && Vector3.Distance(setup.MatchController.LastFaceoffPosition, expectedRedFaceoff) < 0.02f
                && Vector3.Distance(puck.Body.position, expectedRedFaceoff) < 0.02f
                && !offside.BlueWarningVisible && !offside.RedWarningVisible
                && AllPlayersGameplayEnabled(players, false)
                && VerifyTranslatedFaceoff(players, goalies, expectedRedFaceoff);
            redRestart = redFaceoffState;
            setup.MatchController.CompleteFaceoffForValidation();
            bool redResumed = setup.MatchController.State == MatchStateSnapshot.Playing
                && AllPlayersGameplayEnabled(players, true);
            faceoffResume = blueResumed && redResumed;

            ResetSkatersToCenter(players);
            blueCarrier.Movement.ResetMotion(new Vector3(0f, 1f, line - 2.5f), Quaternion.identity);
            StagePuckAtStick(puck, blueCarrier);
            bool legalCarrierClaimed = puck.TryClaim(blueCarrier, blueCarrier.Stick);
            Vector3 legalPrevious = puck.Body.position;
            offside.TickForValidation(legalPrevious);
            bool stayedUnwarned = !offside.WarnedTeam.HasValue;
            puck.Body.position = new Vector3(0f, PrototypeRinkGeometry.PuckY, line + 0.25f);
            puck.transform.position = puck.Body.position;
            offside.TickForValidation(legalPrevious);
            legalEntry = legalCarrierClaimed && stayedUnwarned
                && setup.MatchController.State == MatchStateSnapshot.Playing
                && !offside.WarnedTeam.HasValue && !offside.BlueWarningVisible && !offside.RedWarningVisible
                && setup.MatchController.BlueScore == blueScore && setup.MatchController.RedScore == redScore;

            bool result = exactLineOutside && sweptBothDirections && warningVisual && tagUp && looseRetention
                && turnoverClear && sweptTagUpOrdering && sameTickEntry
                && blueRestart && redRestart && legalEntry && faceoffResume;
            offside.SetRuleEnabledForValidation(false);
            return result;
        }

        private static bool PrepareOffsideWarning(PlayerController[] players, PuckController puck,
            OffsideController offside, PlayerController carrier, PlayerController premature,
            Vector3 carrierPosition, Vector3 prematurePosition)
        {
            ResetSkatersToCenter(players);
            Quaternion carrierRotation = carrier.Team == TeamId.Blue
                ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
            carrier.Movement.ResetMotion(carrierPosition, carrierRotation);
            premature.Movement.ResetMotion(prematurePosition, carrierRotation);
            StagePuckAtStick(puck, carrier);
            bool claimed = puck.TryClaim(carrier, carrier.Stick);
            offside.TickForValidation(puck.Body.position);
            return claimed && offside.WarnedTeam == carrier.Team;
        }

        private static void ResetSkatersToCenter(PlayerController[] players)
        {
            for (int i = 0; i < players.Length; i++)
            {
                players[i].ResetActor();
                players[i].SetGameplayEnabled(true);
            }
        }

        private static bool AllPlayersGameplayEnabled(PlayerController[] players, bool expected)
        {
            for (int i = 0; i < players.Length; i++)
                if (players[i].GameplayEnabled != expected) return false;
            return true;
        }

        private static bool VerifyTranslatedFaceoff(PlayerController[] players, HockeyGoalieAI[] goalies,
            Vector3 faceoffPoint)
        {
            Vector3 offset = new(faceoffPoint.x, 0f, faceoffPoint.z);
            for (int i = 0; i < players.Length; i++)
            {
                Vector3 expected = AIFormationController.Home(players[i].Team, players[i].Role) + offset;
                if (Vector3.Distance(players[i].transform.position, expected) > 0.02f) return false;
            }
            for (int i = 0; i < goalies.Length; i++)
                if (Vector3.Distance(goalies[i].transform.position, goalies[i].Anchor) > 0.02f) return false;
            return true;
        }

        private static bool WarningGridIsRedAndColliderFree(GameObject warning)
        {
            if (warning == null) return false;
            Collider[] colliders = warning.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i].enabled) return false;
            Renderer[] renderers = warning.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Color color = renderers[i].sharedMaterial.color;
                if (color.r < 0.8f || color.g > 0.2f || color.b > 0.2f || color.a >= 0.8f) return false;
            }
            return true;
        }

        private static bool VerifyAttributeSystem(LocalMatchSetup setup, PlayerController[] players, PuckController puck,
            out bool budget, out bool presets, out bool movement, out bool stamina, out bool deke,
            out bool puckControl, out bool shot, out bool pass, out bool reception, out bool checks,
            out bool snapshots, out bool aiSeparation)
        {
            PlayerAttributeBuild allocation = new(25);
            bool setSeventy = allocation.TrySet(PlayerAttribute.Speed, 70);
            int spentBeforeInvalid = allocation.SpentPoints;
            bool invalidRejected = !allocation.TrySet(PlayerAttribute.Speed, 96)
                && !allocation.TrySet(PlayerAttribute.Speed, 39)
                && allocation.SpentPoints == spentBeforeInvalid && allocation.Speed == 70;
            PlayerAttributeBuild noBudget = new(1);
            bool unaffordableRejected = !noBudget.TrySet(PlayerAttribute.Shooting, 41)
                && noBudget.Shooting == PlayerAttributeBuild.MinimumRating && noBudget.SpentPoints == 0;
            budget = allocation.PointBudget == 192 && setSeventy && allocation.SpentPoints == 31
                && invalidRejected && unaffordableRejected
                && PlayerAttributeBuild.BudgetForLevel(50) == 392
                && PlayerAttributeBuild.CostToRating(95) == 92
                && PlayerAttributeBuild.CostToRating(95) * 9 == 828;

            presets = VerifyPreset(PlayerBuildPreset.Speed, 175, new[] { 78, 75, 73, 58, 52, 45, 45, 45, 45 })
                && VerifyPreset(PlayerBuildPreset.Sniper, 192, new[] { 60, 60, 72, 55, 74, 78, 50, 43, 43 })
                && VerifyPreset(PlayerBuildPreset.Playmaker, 192, new[] { 62, 62, 72, 55, 74, 45, 76, 43, 48 })
                && VerifyPreset(PlayerBuildPreset.Power, 192, new[] { 55, 58, 50, 72, 60, 75, 45, 78, 41 })
                && VerifyPreset(PlayerBuildPreset.TwoWay, 192, new[] { 58, 58, 60, 68, 68, 49, 67, 55, 69 });
            for (int i = 0; i < players.Length && presets; i++)
            {
                PlayerAttributeBuild expected = PlayerAttributeBuild.CreatePreset(
                    PlayerAttributeBuild.PresetForRole(players[i].Role));
                presets &= BuildsMatch(expected, players[i].Attributes);
            }

            PlayerController sample = FindPlayer(players, "blue-1");
            PlayerAttributeBuild originalSampleBuild = sample.Attributes.Clone();
            PlayerAttributeBuild lowBuild = new(1);
            PlayerAttributeBuild highSpeedBuild = BuildWithMaximum(PlayerAttribute.Speed);
            PlayerAttributeBuild highAccelerationBuild = BuildWithMaximum(PlayerAttribute.Acceleration);
            PlayerAttributeBuild highAgilityBuild = BuildWithMaximum(PlayerAttribute.Agility);
            sample.ApplyBuild(lowBuild);
            float lowTerminalSpeed = sample.Movement.EffectiveMaximumSpeed;
            sample.Movement.ResetMotion(sample.transform.position, Quaternion.identity);
            sample.Movement.StepPlanarForValidation(Vector2.up, 0.1f);
            float lowAccelerationSpeed = sample.Movement.Velocity.magnitude;
            sample.Movement.ResetMotion(sample.transform.position, Quaternion.identity);
            sample.Movement.StepPlanarForValidation(Vector2.zero, 0.1f);
            bool zeroInputStill = sample.Movement.Velocity == Vector3.zero;
            sample.ApplyBuild(highSpeedBuild);
            float highTerminalSpeed = sample.Movement.EffectiveMaximumSpeed;
            sample.ApplyBuild(highAccelerationBuild);
            sample.Movement.ResetMotion(sample.transform.position, Quaternion.identity);
            sample.Movement.StepPlanarForValidation(Vector2.up, 0.1f);
            float highAccelerationSpeed = sample.Movement.Velocity.magnitude;
            sample.ApplyBuild(lowBuild);
            sample.Movement.SetPlanarVelocityForValidation(Vector3.forward * 5f);
            sample.Movement.StepPlanarForValidation(Vector2.right, 0.05f);
            float lowAgilityTurn = Vector3.Angle(Vector3.forward, sample.Movement.Velocity);
            sample.ApplyBuild(highAgilityBuild);
            sample.Movement.SetPlanarVelocityForValidation(Vector3.forward * 5f);
            sample.Movement.StepPlanarForValidation(Vector2.right, 0.05f);
            float highAgilityTurn = Vector3.Angle(Vector3.forward, sample.Movement.Velocity);
            Vector3 cameraForward = PlayerMovementController.CameraRelativeDirectionForValidation(Vector2.up);
            sample.Movement.SetPlanarVelocityForValidation(cameraForward * 5f);
            sample.Movement.StepPlanarForValidation(Vector2.down, 0.05f);
            float speedAfterReverseInput = Vector3.Dot(sample.Movement.Velocity, cameraForward);
            PlayerAttributeBuild mutableBuild = new(50);
            sample.ApplyBuild(mutableBuild);
            float beforeRuntimeAllocation = sample.Movement.EffectiveMaximumSpeed;
            bool runtimeAllocated = sample.Attributes.TrySet(PlayerAttribute.Speed, 95);
            float afterRuntimeAllocation = sample.Movement.EffectiveMaximumSpeed;
            sample.ApplyBuild(originalSampleBuild);
            movement = Nearly(PlayerMovementController.EvaluateMaximumSpeed(0f), 6.4f)
                && Nearly(PlayerMovementController.EvaluateMaximumSpeed(1f), 9.6f)
                && Nearly(PlayerMovementController.EvaluateAcceleration(0f), 13.5f)
                && Nearly(PlayerMovementController.EvaluateAcceleration(1f), 22.5f)
                && Nearly(PlayerMovementController.EvaluateLowSpeedTurnRate(0f), 12f)
                && Nearly(PlayerMovementController.EvaluateLowSpeedTurnRate(1f), 20f)
                && Nearly(PlayerMovementController.EvaluateHighSpeedTurnRate(0f), 6f)
                && Nearly(PlayerMovementController.EvaluateHighSpeedTurnRate(1f), 12f)
                && Nearly(lowTerminalSpeed, 6.4f) && Nearly(highTerminalSpeed, 9.6f)
                && highAccelerationSpeed > lowAccelerationSpeed && highAgilityTurn > lowAgilityTurn
                && speedAfterReverseInput >= 0f && speedAfterReverseInput < 5f
                && zeroInputStill && runtimeAllocated && Nearly(beforeRuntimeAllocation, 6.4f)
                && Nearly(afterRuntimeAllocation, 9.6f);

            PlayerAttributeBuild highStaminaBuild = BuildWithMaximum(PlayerAttribute.Stamina);
            sample.ApplyBuild(lowBuild);
            sample.SetStaminaForValidation(100f);
            sample.TickStaminaForValidation(1f, 1f);
            float lowStaminaAfterDrain = sample.Stamina;
            sample.SetStaminaForValidation(50f);
            sample.TickStaminaForValidation(0f, 1f);
            float lowStaminaAfterRecovery = sample.Stamina;
            sample.ApplyBuild(highStaminaBuild);
            sample.SetStaminaForValidation(100f);
            sample.TickStaminaForValidation(1f, 1f);
            float highStaminaAfterDrain = sample.Stamina;
            sample.SetStaminaForValidation(50f);
            sample.TickStaminaForValidation(0f, 1f);
            float highStaminaAfterRecovery = sample.Stamina;
            sample.ApplyBuild(originalSampleBuild);
            sample.SetStaminaForValidation(50f);
            float beforeDrain = sample.Stamina;
            sample.TickStaminaForValidation(1f, 1f);
            bool runtimeDrain = sample.Stamina < beforeDrain;
            float beforeRecovery = sample.Stamina;
            sample.TickStaminaForValidation(0f, 1f);
            bool runtimeRecovery = sample.Stamina > beforeRecovery;
            sample.SetStaminaForValidation(0f);
            bool fatigueFloor = Nearly(sample.PerformanceFactor, 0.68f);
            sample.ResetActor();
            bool resetStamina = Nearly(sample.Stamina, 100f) && sample.Movement.Velocity == Vector3.zero;
            stamina = Nearly(PlayerController.EvaluateStaminaDrainRate(0f), 10f)
                && Nearly(PlayerController.EvaluateStaminaDrainRate(1f), 4f)
                && Nearly(PlayerController.EvaluateStaminaRecoveryRate(0f), 9f)
                && Nearly(PlayerController.EvaluateStaminaRecoveryRate(1f), 13f)
                && runtimeDrain && runtimeRecovery && fatigueFloor && resetStamina
                && Nearly(lowStaminaAfterDrain, 90f) && Nearly(highStaminaAfterDrain, 96f)
                && Nearly(lowStaminaAfterRecovery, 59f) && Nearly(highStaminaAfterRecovery, 63f);

            sample.Movement.ResetMotion(sample.transform.position, sample.transform.rotation);
            StagePuckAtStick(puck, sample);
            bool claimedForDeke = puck.TryClaim(sample, sample.Stick);
            int dekesBefore = sample.Deke.StartedCount;
            Vector3 velocityBeforeDeke = sample.Movement.Velocity;
            AttributeValidationInput dekeInput = new();
            sample.TickInputForValidation(dekeInput, 0.016f);
            bool noAutomaticDeke = sample.Deke.StartedCount == dekesBefore;
            Vector3 neutralBase = sample.transform.position + sample.transform.forward * 1.15f + Vector3.up * 0.28f;
            dekeInput.DekePressed = true;
            sample.TickInputForValidation(dekeInput, 0.016f);
            float neutralDekeOffset = Vector3.Dot(sample.Stick.ControlPoint - neutralBase, sample.transform.right);
            bool explicitDeke = sample.Deke.StartedCount == dekesBefore + 1
                && sample.Deke.IsActive && sample.Movement.Velocity == velocityBeforeDeke;
            sample.Deke.ResetAction();
            dekeInput.Move = Vector2.right;
            sample.TickInputForValidation(dekeInput, 0f);
            float rightDekeOffset = Vector3.Dot(sample.Stick.ControlPoint - neutralBase, sample.transform.right);
            sample.Deke.ResetAction();
            dekeInput.Move = Vector2.left;
            sample.TickInputForValidation(dekeInput, 0f);
            float leftDekeOffset = Vector3.Dot(sample.Stick.ControlPoint - neutralBase, sample.transform.right);
            float dekeBonusSlowFatigued = sample.Deke.EvaluateProtectionBonusForValidation(Time.time, 0f, 0.68f);
            float dekeBonusFastFresh = sample.Deke.EvaluateProtectionBonusForValidation(Time.time, 1f, 1f);
            float dekeBonusExpired = sample.Deke.EvaluateProtectionBonusForValidation(
                Time.time + sample.Deke.EvaluateWindowSeconds() + 0.01f, 1f, 1f);
            deke = claimedForDeke && noAutomaticDeke && explicitDeke
                && Nearly(neutralDekeOffset, 0f) && rightDekeOffset > 0f && leftDekeOffset < 0f
                && Nearly(DekeController.EvaluateWindowSeconds(0f, 0f), 0.18f)
                && Nearly(DekeController.EvaluateWindowSeconds(1f, 1f), 0.42f)
                && dekeBonusFastFresh > dekeBonusSlowFatigued && Nearly(dekeBonusExpired, 0f);
            puck.ResetPuck(new Vector3(0f, 0.55f, 0f));

            sample.ApplyBuild(lowBuild);
            StagePuckAtStick(puck, sample);
            bool lowControlClaimed = puck.TryClaim(sample, sample.Stick);
            float lowLiveClaimRadius = sample.Stick.ClaimRadius;
            float lowLiveClaimSpeed = sample.Stick.MaximumClaimSpeed;
            puck.Body.position = sample.ControlPoint - sample.transform.forward;
            puck.Body.linearVelocity = Vector3.zero;
            float lowCarryAcceleration = puck.CalculateCarryAcceleration(sample.ControlPoint, Vector3.zero).magnitude;
            puck.ResetPuck(new Vector3(0f, 0.55f, 0f));
            PlayerAttributeBuild highControlBuild = BuildWithMaximum(PlayerAttribute.Control);
            sample.ApplyBuild(highControlBuild);
            StagePuckAtStick(puck, sample);
            bool highControlClaimed = puck.TryClaim(sample, sample.Stick);
            float highLiveClaimRadius = sample.Stick.ClaimRadius;
            float highLiveClaimSpeed = sample.Stick.MaximumClaimSpeed;
            puck.Body.position = sample.ControlPoint - sample.transform.forward;
            puck.Body.linearVelocity = Vector3.zero;
            float highCarryAcceleration = puck.CalculateCarryAcceleration(sample.ControlPoint, Vector3.zero).magnitude;
            sample.ApplyBuild(originalSampleBuild);
            puck.ResetPuck(new Vector3(0f, 0.55f, 0f));
            puckControl = Nearly(StickPuckInteraction.EvaluateClaimRadius(0f), 1.25f)
                && Nearly(StickPuckInteraction.EvaluateClaimRadius(1f), 1.85f)
                && Nearly(StickPuckInteraction.EvaluateMaximumClaimSpeed(0f), 12f)
                && Nearly(StickPuckInteraction.EvaluateMaximumClaimSpeed(1f), 17f)
                && Nearly(StickPuckInteraction.EvaluateCarryControlMultiplier(0f), 0.75f)
                && Nearly(StickPuckInteraction.EvaluateCarryControlMultiplier(1f), 1.25f)
                && lowControlClaimed && highControlClaimed
                && Nearly(lowLiveClaimRadius, 1.25f) && Nearly(highLiveClaimRadius, 1.85f)
                && Nearly(lowLiveClaimSpeed, 12f) && Nearly(highLiveClaimSpeed, 17f)
                && highCarryAcceleration > lowCarryAcceleration;

            Vector3 savedSamplePosition = sample.transform.position;
            Quaternion savedSampleRotation = sample.transform.rotation;
            sample.ApplyBuild(originalSampleBuild);
            sample.SetStaminaForValidation(100f);
            sample.Movement.ResetMotion(Vector3.up, Quaternion.identity);
            puck.Body.position = sample.ControlPoint;
            float runtimeShotBaseline = sample.Shoot.EvaluateRuntimeSituationChallengeForValidation(1f);
            float runtimeShotRepeat = sample.Shoot.EvaluateRuntimeSituationChallengeForValidation(1f);
            float runtimeMissingCharge = sample.Shoot.EvaluateRuntimeSituationChallengeForValidation(0f);
            sample.Movement.ResetMotion(Vector3.up, Quaternion.Euler(0f, 90f, 0f));
            puck.Body.position = sample.ControlPoint;
            float runtimeFacing = sample.Shoot.EvaluateRuntimeSituationChallengeForValidation(1f);
            sample.Movement.ResetMotion(new Vector3(0f, 1f, -15f), Quaternion.identity);
            puck.Body.position = sample.ControlPoint;
            float runtimeDistance = sample.Shoot.EvaluateRuntimeSituationChallengeForValidation(1f);
            sample.Movement.ResetMotion(Vector3.up, Quaternion.identity);
            puck.Body.position = sample.ControlPoint + Vector3.right * sample.Stick.ClaimRadius;
            float runtimePuckError = sample.Shoot.EvaluateRuntimeSituationChallengeForValidation(1f);
            puck.Body.position = sample.ControlPoint;
            sample.Movement.SetPlanarVelocityForValidation(Vector3.right * sample.Movement.EffectiveMaximumSpeed);
            float runtimeLateral = sample.Shoot.EvaluateRuntimeSituationChallengeForValidation(1f);
            sample.Movement.SetPlanarVelocityForValidation(Vector3.zero);
            sample.SetStaminaForValidation(0f);
            float runtimeFatigue = sample.Shoot.EvaluateRuntimeSituationChallengeForValidation(1f);
            sample.SetStaminaForValidation(100f);
            sample.Movement.ResetMotion(savedSamplePosition, savedSampleRotation);
            sample.ApplyBuild(lowBuild);
            sample.SetStaminaForValidation(100f);
            sample.Movement.ResetMotion(Vector3.up, Quaternion.Euler(0f, 90f, 0f));
            puck.Body.position = sample.ControlPoint;
            float lowLiveShotPower = sample.Shoot.EvaluatePower(0.7f);
            float lowLiveShotDeviation = Mathf.Abs(sample.Shoot.EvaluateRuntimeDeviationForValidation(0.7f, 1f));
            PlayerAttributeBuild highShooting = BuildWithMaximum(PlayerAttribute.Shooting);
            sample.ApplyBuild(highShooting);
            float highLiveShotPower = sample.Shoot.EvaluatePower(0.7f);
            float highLiveShotDeviation = Mathf.Abs(sample.Shoot.EvaluateRuntimeDeviationForValidation(0.7f, 1f));
            sample.ApplyBuild(originalSampleBuild);
            sample.Movement.ResetMotion(savedSamplePosition, savedSampleRotation);
            shot = Nearly(ShootController.EvaluatePowerMultiplier(0f), 0.85f)
                && Nearly(ShootController.EvaluatePowerMultiplier(1f), 1.2f)
                && Nearly(ShootController.EvaluateMaximumDeviation(0f), 6f)
                && Nearly(ShootController.EvaluateMaximumDeviation(1f), 1f)
                && Nearly(ShootController.EvaluateSituationChallenge(1f, 0f, 0f, 0f, 0f, 0f), 0.25f)
                && Nearly(ShootController.EvaluateSituationChallenge(0f, 1f, 0f, 0f, 0f, 0f), 0.2f)
                && Nearly(ShootController.EvaluateSituationChallenge(0f, 0f, 1f, 0f, 0f, 0f), 0.2f)
                && Nearly(ShootController.EvaluateSituationChallenge(0f, 0f, 0f, 1f, 0f, 0f), 0.15f)
                && Nearly(ShootController.EvaluateSituationChallenge(0f, 0f, 0f, 0f, 1f, 0f), 0.1f)
                && Nearly(ShootController.EvaluateSituationChallenge(0f, 0f, 0f, 0f, 0f, 1f), 0.1f)
                && Nearly(runtimeShotBaseline, runtimeShotRepeat)
                && Nearly(runtimeMissingCharge, runtimeShotBaseline) && runtimeFacing > runtimeShotBaseline
                && runtimeDistance > runtimeShotBaseline && runtimePuckError > runtimeShotBaseline
                && runtimeLateral > runtimeShotBaseline && runtimeFatigue > runtimeShotBaseline
                && highLiveShotPower > lowLiveShotPower
                && highLiveShotDeviation < lowLiveShotDeviation;

            Vector3 passDirection = Quaternion.Euler(0f, 35f, 0f) * sample.transform.forward;
            sample.ApplyBuild(lowBuild);
            float lowLivePassSpeed = sample.Pass.EvaluateLaunchSpeedForValidation(12f);
            float runtimePassFirst = sample.Pass.EvaluateRuntimeDeviationForValidation(passDirection, 12f);
            float runtimePassSecond = sample.Pass.EvaluateRuntimeDeviationForValidation(passDirection, 12f);
            PlayerAttributeBuild maximumPassing = BuildWithMaximum(PlayerAttribute.Passing);
            sample.ApplyBuild(maximumPassing);
            float highLivePassSpeed = sample.Pass.EvaluateLaunchSpeedForValidation(12f);
            float highLivePassDeviation = sample.Pass.EvaluateRuntimeDeviationForValidation(passDirection, 12f);
            sample.ApplyBuild(originalSampleBuild);
            pass = Nearly(PassController.EvaluatePaceMultiplier(0f), 0.88f)
                && Nearly(PassController.EvaluatePaceMultiplier(1f), 1.08f)
                && Nearly(PassController.EvaluateMaximumDeviation(0f), 5f)
                && Nearly(PassController.EvaluateMaximumDeviation(1f), 0.5f)
                && Nearly(PassController.EvaluateLeadSeconds(0f), 0.32f)
                && Nearly(PassController.EvaluateLeadSeconds(1f), 0.55f)
                && Nearly(PassController.EvaluateDeviationDegrees(0f, 0f, 1f), 0f)
                && Nearly(runtimePassFirst, runtimePassSecond) && Mathf.Abs(runtimePassFirst) > 0f
                && highLivePassSpeed > lowLivePassSpeed
                && Mathf.Abs(highLivePassDeviation) < Mathf.Abs(runtimePassFirst);

            float mixedReception = PassReceivingZone.EvaluateReceptionQuality(1f, 0f);
            PlayerController receiver = FindPlayer(players, "blue-2");
            PlayerController nonTarget = FindPlayer(players, "blue-3");
            PlayerAttributeBuild originalReceiverBuild = receiver.Attributes.Clone();
            PlayerAttributeBuild originalNonTargetBuild = nonTarget.Attributes.Clone();
            sample.ApplyBuild(lowBuild);
            receiver.ApplyBuild(lowBuild);
            nonTarget.ApplyBuild(lowBuild);
            sample.Movement.ResetMotion(Vector3.up, Quaternion.identity);
            receiver.Movement.ResetMotion(new Vector3(0f, 1f, 3f), Quaternion.identity);
            nonTarget.Movement.ResetMotion(new Vector3(1f, 1f, 1.5f), Quaternion.identity);
            StagePuckAtStick(puck, sample);
            bool lowPassReleased = puck.TryClaim(sample, sample.Stick)
                && sample.Pass.ReleaseForValidation(receiver, Vector3.zero, 0f);
            float lowLiveRadius = receiver.PassReception.Radius;
            float lowLiveEntry = receiver.PassReception.EntrySpeed;
            puck.Body.position = nonTarget.Stick.ControlPoint;
            bool teammateBypassRejected = !puck.TryClaim(nonTarget, nonTarget.Stick);
            PlayerAttributeBuild highPassing = BuildWithMaximum(PlayerAttribute.Passing);
            PlayerAttributeBuild highControl = BuildWithMaximum(PlayerAttribute.Control);
            sample.ApplyBuild(highPassing);
            receiver.ApplyBuild(highControl);
            StagePuckAtStick(puck, sample);
            bool highPassReleased = puck.TryClaim(sample, sample.Stick)
                && sample.Pass.ReleaseForValidation(receiver, Vector3.zero, 0f);
            float highLiveRadius = receiver.PassReception.Radius;
            float highLiveEntry = receiver.PassReception.EntrySpeed;
            sample.ApplyBuild(originalSampleBuild);
            receiver.ApplyBuild(originalReceiverBuild);
            nonTarget.ApplyBuild(originalNonTargetBuild);
            puck.ResetPuck(new Vector3(0f, 0.55f, 0f));
            reception = Nearly(PassReceivingZone.EvaluateReceptionQuality(0f, 0f), 0f)
                && Nearly(PassReceivingZone.EvaluateReceptionQuality(1f, 1f), 1f)
                && Nearly(mixedReception, 0.6f)
                && Nearly(PassReceivingZone.EvaluateRadius(0f), 1.4f)
                && Nearly(PassReceivingZone.EvaluateRadius(1f), 2.1f)
                && Nearly(PassReceivingZone.EvaluateEntrySpeed(0f), 4.5f)
                && Nearly(PassReceivingZone.EvaluateEntrySpeed(1f), 7.5f)
                && lowPassReleased && highPassReleased && teammateBypassRejected
                && Nearly(lowLiveRadius, 1.4f) && Nearly(lowLiveEntry, 4.5f)
                && Nearly(highLiveRadius, 2.1f) && Nearly(highLiveEntry, 7.5f);

            bool geometry = Nearly(DefensiveCheckController.NormalizeApproachSpeed(Vector3.forward * 8f,
                    Vector3.zero, Vector3.forward), 1f)
                && Nearly(DefensiveCheckController.NormalizeApproachSpeed(Vector3.zero,
                    Vector3.zero, Vector3.forward), 0f)
                && Nearly(DefensiveCheckController.NormalizeBodyAlignment(Vector3.back, Vector3.forward), 0f)
                && Nearly(DefensiveCheckController.NormalizeBodyAlignment(Vector3.forward, Vector3.forward), 1f)
                && Nearly(DefensiveCheckController.NormalizePullAlignment(Vector3.forward, Vector3.forward, 0.25f), 1f)
                && Nearly(DefensiveCheckController.NormalizeContactPosition(Vector3.forward, Vector3.forward), 1f)
                && Nearly(DefensiveCheckController.NormalizeContactPosition(Vector3.forward, Vector3.back), 0f);
            DefensiveCheckController.ContestScores strongBody = DefensiveCheckController.EvaluateContest(false,
                1f, 1f, 1f, 1f, 0f, 0f, 0f, 0f, 1f, 1f, 0f, 0f, 0f);
            DefensiveCheckController.ContestScores weakBody = DefensiveCheckController.EvaluateContest(false,
                0f, 0f, 0f, 0f, 1f, 1f, 1f, 1f, 0f, 0f, 1f, 1f, 0.15f);
            DefensiveCheckController.ContestScores strongPull = DefensiveCheckController.EvaluateContest(true,
                1f, 1f, 1f, 1f, 0f, 0f, 0f, 0f, 1f, 1f, 0f, 0f, 0f);
            DefensiveCheckController.ContestScores weakPull = DefensiveCheckController.EvaluateContest(true,
                0f, 0f, 0f, 0f, 1f, 1f, 1f, 1f, 0f, 0f, 1f, 1f, 0.15f);
            DefensiveCheckController.ContestScores boardContest = DefensiveCheckController.EvaluateContest(false,
                0.55f, 0.25f, 0.3f, 0.3f, 0.65f, 0.55f, 0.55f, 0.3f,
                0f, 1f, 0.55f, 0f, 0f);
            float centerPressure = DefensiveCheckController.EvaluateBoardPressure(Vector3.zero);
            float straightBoardPressure = DefensiveCheckController.EvaluateBoardPressure(
                new Vector3(PrototypeRinkGeometry.Width * 0.5f - 0.8f, 1f, 0f));
            float cornerBoardPressure = DefensiveCheckController.EvaluateBoardPressure(
                RoundedBoardPoint(0.8f));
            PlayerController liveChecker = FindPlayer(players, "blue-4");
            PlayerController liveCarrier = FindPlayer(players, "red-4");
            PlayerAttributeBuild originalCheckerBuild = liveChecker.Attributes.Clone();
            PlayerAttributeBuild originalCarrierBuild = liveCarrier.Attributes.Clone();
            PlayerAttributeBuild maximumAttack = BuildWithMaximum(PlayerAttribute.Strength,
                PlayerAttribute.Defense, PlayerAttribute.Speed, PlayerAttribute.Agility);
            PlayerAttributeBuild maximumProtection = BuildWithMaximum(PlayerAttribute.Control,
                PlayerAttribute.Strength, PlayerAttribute.Agility, PlayerAttribute.Speed);
            setup.DefenseController.SetGameplayEnabledForValidation(true);
            setup.DefenseController.ResetCooldownForValidation();
            liveChecker.ApplyBuild(maximumAttack);
            liveCarrier.ApplyBuild(lowBuild);
            liveChecker.Movement.ResetMotion(new Vector3(0f, 1f, 1f), Quaternion.Euler(0f, 180f, 0f));
            liveChecker.Movement.SetPlanarVelocityForValidation(Vector3.back * 8f);
            liveCarrier.Movement.ResetMotion(Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
            StagePuckAtStick(puck, liveCarrier);
            bool strongLiveClaim = puck.TryClaim(liveCarrier, liveCarrier.Stick);
            bool strongLiveSucceeded = strongLiveClaim
                && setup.DefenseController.TryCheck(liveChecker) == DefensiveCheckResult.BodyCheck;
            setup.DefenseController.ResetCooldownForValidation();
            liveChecker.ApplyBuild(lowBuild);
            liveCarrier.ApplyBuild(maximumProtection);
            liveChecker.Movement.ResetMotion(new Vector3(0f, 1f, 1f), Quaternion.Euler(0f, 180f, 0f));
            liveChecker.Movement.SetPlanarVelocityForValidation(Vector3.back * 8f);
            liveCarrier.Movement.ResetMotion(Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
            StagePuckAtStick(puck, liveCarrier);
            bool protectedLiveClaim = puck.TryClaim(liveCarrier, liveCarrier.Stick);
            bool protectionDeke = liveCarrier.Deke.Tick(true);
            bool weakLiveResisted = protectedLiveClaim && protectionDeke
                && setup.DefenseController.TryCheck(liveChecker) == DefensiveCheckResult.None
                && puck.Carrier == liveCarrier;

            setup.DefenseController.ResetCooldownForValidation();
            liveChecker.ApplyBuild(maximumAttack);
            liveCarrier.ApplyBuild(lowBuild);
            liveChecker.Movement.ResetMotion(Vector3.up, Quaternion.identity);
            liveChecker.Movement.SetPlanarVelocityForValidation(Vector3.forward * 8f);
            liveCarrier.Movement.ResetMotion(new Vector3(0f, 1f, 2.2f), Quaternion.identity);
            StagePuckAtStick(puck, liveCarrier);
            bool strongPullClaim = puck.TryClaim(liveCarrier, liveCarrier.Stick);
            bool strongLivePull = strongPullClaim
                && setup.DefenseController.TryCheck(liveChecker) == DefensiveCheckResult.PullCheck;

            setup.DefenseController.ResetCooldownForValidation();
            liveChecker.ApplyBuild(lowBuild);
            liveCarrier.ApplyBuild(maximumProtection);
            liveChecker.Movement.ResetMotion(Vector3.up, Quaternion.identity);
            liveChecker.Movement.SetPlanarVelocityForValidation(Vector3.forward * 8f);
            liveCarrier.Movement.ResetMotion(new Vector3(0f, 1f, 2.2f), Quaternion.identity);
            StagePuckAtStick(puck, liveCarrier);
            bool weakPullClaim = puck.TryClaim(liveCarrier, liveCarrier.Stick);
            bool weakLivePullResisted = weakPullClaim
                && setup.DefenseController.TryCheck(liveChecker) == DefensiveCheckResult.None
                && puck.Carrier == liveCarrier;

            setup.DefenseController.ResetCooldownForValidation();
            liveChecker.ApplyBuild(maximumAttack);
            liveCarrier.ApplyBuild(lowBuild);
            liveChecker.Movement.ResetMotion(Vector3.up, Quaternion.identity);
            liveChecker.Movement.SetPlanarVelocityForValidation(Vector3.forward * 8f);
            liveCarrier.Movement.ResetMotion(new Vector3(0f, 1f, 3.2f), Quaternion.identity);
            StagePuckAtStick(puck, liveCarrier);
            bool rangeClaim = puck.TryClaim(liveCarrier, liveCarrier.Stick);
            bool maximumRatingRangeRejected = rangeClaim
                && setup.DefenseController.TryCheck(liveChecker) == DefensiveCheckResult.None
                && puck.Carrier == liveCarrier;

            setup.DefenseController.ResetCooldownForValidation();
            liveChecker.Movement.ResetMotion(Vector3.up, Quaternion.Euler(0f, 180f, 0f));
            liveChecker.Movement.SetPlanarVelocityForValidation(Vector3.forward * 8f);
            liveCarrier.Movement.ResetMotion(new Vector3(0f, 1f, 2.2f), Quaternion.identity);
            StagePuckAtStick(puck, liveCarrier);
            bool coneClaim = puck.TryClaim(liveCarrier, liveCarrier.Stick);
            bool maximumRatingConeRejected = coneClaim
                && setup.DefenseController.TryCheck(liveChecker) == DefensiveCheckResult.None
                && puck.Carrier == liveCarrier;
            liveChecker.ApplyBuild(originalCheckerBuild);
            liveCarrier.ApplyBuild(originalCarrierBuild);
            puck.ResetPuck(new Vector3(0f, 0.55f, 0f));
            checks = geometry && strongBody.Succeeds && !weakBody.Succeeds
                && strongPull.Succeeds && !weakPull.Succeeds
                && Nearly(centerPressure, 0f) && straightBoardPressure > 0.99f
                && cornerBoardPressure > 0.99f && !boardContest.Succeeds
                && DefensiveCheckController.ContestSucceeds(boardContest, straightBoardPressure, 8f, false, true)
                && !DefensiveCheckController.ContestSucceeds(boardContest, straightBoardPressure, 8f, false, false)
                && DefensiveCheckController.ContestSucceeds(boardContest, centerPressure, 0f, false, true)
                && !DefensiveCheckController.ContestSucceeds(boardContest, centerPressure, 0f, true, true)
                && !DefensiveCheckController.ContestSucceeds(boardContest, centerPressure, 8f, false, true)
                && !DefensiveCheckController.ContestSucceeds(boardContest, centerPressure, 0f, false, false)
                && strongLiveSucceeded && weakLiveResisted
                && strongLivePull && weakLivePullResisted
                && maximumRatingRangeRejected && maximumRatingConeRejected;

            setup.CaptureDataForValidation();
            snapshots = SnapshotAttributesMatch(players, setup.Data);

            PlayerController blue = FindRole(players, TeamId.Blue, SkaterRole.RightDefense);
            PlayerController red = FindRole(players, TeamId.Red, SkaterRole.RightDefense);
            HockeyPlayerAI blueAi = blue.GetComponent<HockeyPlayerAI>();
            HockeyPlayerAI redAi = red.GetComponent<HockeyPlayerAI>();
            blueAi.Configure(blue, puck, 4, 5, AIDifficulty.Easy);
            redAi.Configure(red, puck, 4, 5, AIDifficulty.Normal);
            int humanInputs = 0;
            for (int i = 0; i < players.Length; i++) if (players[i].InputSource is PlayerInputController) humanInputs++;
            aiSeparation = blueAi.Difficulty != redAi.Difficulty
                && BuildsMatch(blue.Attributes, red.Attributes)
                && Nearly(blue.Movement.EffectiveMaximumSpeed, red.Movement.EffectiveMaximumSpeed)
                && Nearly(blue.Pass.EvaluateLaunchSpeedForValidation(8f), red.Pass.EvaluateLaunchSpeedForValidation(8f))
                && Nearly(blue.Shoot.EvaluatePower(0.65f), red.Shoot.EvaluatePower(0.65f))
                && blueAi.DecisionInterval > redAi.DecisionInterval
                && blueAi.TargetErrorRadius > redAi.TargetErrorRadius
                && blueAi.MaximumShotChargeSeconds < redAi.MaximumShotChargeSeconds
                && blueAi.PassProgressThreshold > redAi.PassProgressThreshold
                && humanInputs == 1;

            return budget && presets && movement && stamina && deke && puckControl && shot && pass
                && reception && checks && snapshots && aiSeparation;
        }

        private static bool VerifyPreset(PlayerBuildPreset preset, int expectedCost, int[] expectedRatings)
        {
            PlayerAttributeBuild build = PlayerAttributeBuild.CreatePreset(preset);
            if (!build.IsValid || build.Level != 25 || build.SpentPoints != expectedCost || expectedRatings.Length != 9)
                return false;
            for (int i = 0; i < expectedRatings.Length; i++)
                if (build.Get((PlayerAttribute)i) != expectedRatings[i]) return false;
            return true;
        }

        private static PlayerAttributeBuild BuildWithMaximum(params PlayerAttribute[] attributes)
        {
            PlayerAttributeBuild build = new(50);
            for (int i = 0; i < attributes.Length; i++)
                if (!build.TrySet(attributes[i], PlayerAttributeBuild.MaximumRating))
                    throw new System.InvalidOperationException($"Could not maximize {attributes[i]} in validation build.");
            return build;
        }

        private static bool BuildsMatch(PlayerAttributeBuild first, PlayerAttributeBuild second)
        {
            if (first == null || second == null || first.Level != second.Level) return false;
            foreach (PlayerAttribute attribute in System.Enum.GetValues(typeof(PlayerAttribute)))
                if (first.Get(attribute) != second.Get(attribute)) return false;
            return true;
        }

        private static bool SnapshotAttributesMatch(PlayerController[] players, MatchData data)
        {
            if (data == null) return false;
            for (int i = 0; i < players.Length; i++)
            {
                IReadOnlyList<PlayerData> team = players[i].Team == TeamId.Blue
                    ? data.BlueTeam.Players : data.RedTeam.Players;
                PlayerData snapshot = null;
                for (int j = 0; j < team.Count; j++) if (team[j].PlayerId == players[i].PlayerId) snapshot = team[j];
                if (snapshot == null || !BuildsMatch(players[i].Attributes, snapshot.Attributes)
                    || !Nearly(players[i].Stamina, snapshot.Stamina)) return false;
            }
            return true;
        }

        private static bool Nearly(float first, float second) => Mathf.Abs(first - second) < 0.001f;

        private sealed class AttributeValidationInput : IPlayerInput
        {
            public Vector2 Move { get; set; }
            public bool PassPressed { get; set; }
            public bool DekePressed { get; set; }
            public bool ShootHeld { get; set; }
            public bool ShootReleased { get; set; }
            public bool SwitchPressed => false;
            public bool CheckPressed => false;
        }

        private static bool VerifyOpponentPuckDecisions(PlayerController[] players, PuckController puck,
            DefensiveCheckController defensiveChecks, out bool cornerPuckPursuit,
            out bool opponentPressureCheck, out bool tacticalPassIntent, out bool shotIntent,
            out bool singlePuckChaser)
        {
            PlayerController carrier = FindPlayer(players, "red-4");
            PlayerController teammate = FindPlayer(players, "red-2");
            PlayerController otherTeammate = FindPlayer(players, "red-3");
            PlayerController pressure = FindPlayer(players, "blue-4");
            HockeyPlayerAI carrierAi = carrier.InputSource as HockeyPlayerAI;

            Vector3 cornerPuckPosition = new(9.5f, 0.55f, 20f);
            puck.ResetPuck(cornerPuckPosition);
            puck.transform.position = cornerPuckPosition;
            Physics.SyncTransforms();
            carrier.Movement.ResetMotion(new Vector3(8.5f, 1f, 18f), Quaternion.identity);
            teammate.Movement.ResetMotion(new Vector3(0f, 1f, 5f), Quaternion.identity);
            otherTeammate.Movement.ResetMotion(new Vector3(-5f, 1f, 5f), Quaternion.identity);
            cornerPuckPursuit = SimulateAiPuckPickup(carrier, carrierAi, puck);

            opponentPressureCheck = SimulateAiCarrierBattle(players, carrier, carrierAi, teammate,
                otherTeammate, pressure, puck, defensiveChecks);

            carrier.Movement.ResetMotion(new Vector3(0f, 1f, 8f), Quaternion.Euler(0f, 180f, 0f));
            teammate.Movement.ResetMotion(new Vector3(3f, 1f, -2f), Quaternion.Euler(0f, 180f, 0f));
            pressure.Movement.ResetMotion(new Vector3(0.5f, 1f, 8f), Quaternion.identity);
            StagePuckAtStick(puck, carrier);
            bool claimedForPass = puck.TryClaim(carrier, carrier.Stick);
            carrier.Pass.Tick(false, false);
            carrierAi.DecideForValidation();
            int releasesBeforePass = puck.ImpulseReleaseSequence;
            PlayerController intendedReceiver = carrier.Pass.RecommendedTarget;
            bool passReleased = carrier.Pass.Tick(carrierAi.PassPressed, false);
            tacticalPassIntent = claimedForPass && carrierAi.CurrentState == HockeyAIState.Attack
                && carrierAi.PassPressed && passReleased && intendedReceiver != null
                && puck.Carrier == null && puck.IntendedPassReceiver == intendedReceiver
                && puck.ImpulseReleaseSequence == releasesBeforePass + 1;

            puck.ResetPuck(Vector3.zero);
            carrier.Movement.ResetMotion(new Vector3(0f, 1f, -16f), Quaternion.Euler(0f, 180f, 0f));
            StagePuckAtStick(puck, carrier);
            bool claimedForShot = puck.TryClaim(carrier, carrier.Stick);
            carrierAi.DecideForValidation();
            carrier.Shoot.Tick(carrierAi.ShootHeld, carrierAi.ShootReleased);
            int releasesBeforeShot = puck.ImpulseReleaseSequence;
            carrierAi.CompleteShotChargeForValidation();
            carrier.Shoot.Tick(carrierAi.ShootHeld, carrierAi.ShootReleased);
            shotIntent = claimedForShot && carrierAi.CurrentState == HockeyAIState.Shoot
                && carrierAi.ShootReleased && puck.Carrier == null
                && puck.ImpulseReleaseSequence == releasesBeforeShot + 1
                && puck.LastImpulseReleasePlayerId == carrier.PlayerId;

            singlePuckChaser = VerifySingleLoosePuckChaser(players, puck);

            return cornerPuckPursuit && opponentPressureCheck && tacticalPassIntent && shotIntent
                && singlePuckChaser;
        }

        private static bool VerifySingleLoosePuckChaser(PlayerController[] players, PuckController puck)
        {
            Vector3[] positions = new Vector3[players.Length];
            Quaternion[] rotations = new Quaternion[players.Length];
            Vector3 puckPosition = puck.transform.position;
            Vector3 puckVelocity = puck.Body.linearVelocity;
            puck.ResetPuck(Vector3.zero);
            for (int i = 0; i < players.Length; i++)
            {
                positions[i] = players[i].transform.position;
                rotations[i] = players[i].transform.rotation;
                float tiedDepth = players[i].Team == TeamId.Blue ? -5f : 5f;
                players[i].Movement.ResetMotion(new Vector3(0f, 1f, tiedDepth), rotations[i]);
            }
            Physics.SyncTransforms();

            int blueChasers = 0;
            int redChasers = 0;
            string blueChaserId = null;
            string redChaserId = null;
            for (int i = 0; i < players.Length; i++)
            {
                HockeyPlayerAI ai = players[i].GetComponent<HockeyPlayerAI>();
                ai.DecideForValidation();
                if (ai.CurrentState != HockeyAIState.ChasePuck) continue;
                if (players[i].Team == TeamId.Blue)
                {
                    blueChasers++;
                    blueChaserId = players[i].PlayerId;
                }
                else
                {
                    redChasers++;
                    redChaserId = players[i].PlayerId;
                }
            }

            for (int i = 0; i < players.Length; i++)
                players[i].Movement.ResetMotion(positions[i], rotations[i]);
            puck.ResetPuck(puckPosition);
            puck.Body.linearVelocity = puckVelocity;
            Physics.SyncTransforms();
            bool symmetricNearTie = !HockeyPlayerAI.PreferPuckChaser(
                    25.00001f, "blue-2", 25f, "blue-1")
                && HockeyPlayerAI.PreferPuckChaser(
                    25f, "blue-1", 25.00001f, "blue-2");
            return blueChasers == 1 && redChasers == 1
                && blueChaserId == "blue-1" && redChaserId == "red-1"
                && symmetricNearTie;
        }

        private static bool SimulateAiCarrierBattle(PlayerController[] players, PlayerController challenger, HockeyPlayerAI ai,
            PlayerController teammate, PlayerController otherTeammate, PlayerController carrier,
            PuckController puck, DefensiveCheckController defensiveChecks)
        {
            defensiveChecks.ResetCooldownForValidation();
            defensiveChecks.SetGameplayEnabledForValidation(true);

            carrier.Movement.ResetMotion(new Vector3(0f, 1f, 1f), Quaternion.Euler(0f, 180f, 0f));
            carrier.Movement.SetPlanarVelocityForValidation(Vector3.back * 8f);
            challenger.Movement.ResetMotion(Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
            StagePuckAtStick(puck, challenger);
            bool opponentClaimedForHumanCheck = puck.TryClaim(challenger, challenger.Stick);
            bool humanCheckSucceeded = opponentClaimedForHumanCheck
                && defensiveChecks.TryCheck(carrier) != DefensiveCheckResult.None;
            bool humanCooldownActive = defensiveChecks.NextCheckTime > Time.time;

            HockeyPlayerAI teammateAi = teammate.InputSource as HockeyPlayerAI;
            HockeyPlayerAI otherTeammateAi = otherTeammate.InputSource as HockeyPlayerAI;
            HockeyPlayerAI[] opponentAi = new HockeyPlayerAI[CountTeam(players, challenger.Team)];
            int opponentIndex = 0;
            int reserveIndex = 0;
            for (int i = 0; i < players.Length; i++)
            {
                PlayerController candidate = players[i];
                if (candidate.Team != challenger.Team) continue;
                opponentAi[opponentIndex++] = candidate.InputSource as HockeyPlayerAI;
                if (candidate == challenger || candidate == teammate || candidate == otherTeammate) continue;
                float side = reserveIndex++ % 2 == 0 ? 1f : -1f;
                candidate.Movement.ResetMotion(new Vector3(side * 10f, 1f, 19f), Quaternion.Euler(0f, 180f, 0f));
            }

            puck.ResetPuck(Vector3.zero);
            carrier.Movement.ResetMotion(Vector3.zero, Quaternion.identity);
            challenger.Movement.ResetMotion(new Vector3(0f, 1f, -5.5f), Quaternion.identity);
            teammate.Movement.ResetMotion(new Vector3(6f, 1f, 0f), Quaternion.Euler(0f, 180f, 0f));
            otherTeammate.Movement.ResetMotion(new Vector3(-6f, 1f, 0f), Quaternion.Euler(0f, 180f, 0f));
            TickAll(opponentAi);
            StagePuckAtStick(puck, carrier);
            if (!puck.TryClaim(carrier, carrier.Stick)) return false;

            TickAll(opponentAi);
            bool deterministicTie = CountForecheckers(opponentAi) == 1 && ai.IsForechecking
                && !teammateAi.IsForechecking && !otherTeammateAi.IsForechecking;

            challenger.Movement.ResetMotion(new Vector3(0f, 1f, -5.5f), Quaternion.identity);
            teammate.Movement.ResetMotion(new Vector3(0f, 1f, 3f), Quaternion.Euler(0f, 180f, 0f));
            otherTeammate.Movement.ResetMotion(new Vector3(-8f, 1f, 15f), Quaternion.Euler(0f, 180f, 0f));
            TickAll(opponentAi);
            bool uniqueHandoff = CountForecheckers(opponentAi) == 1 && !ai.IsForechecking
                && teammateAi.IsForechecking && !otherTeammateAi.IsForechecking;

            challenger.Movement.ResetMotion(new Vector3(0f, 1f, -5.5f), Quaternion.identity);
            teammate.Movement.ResetMotion(new Vector3(8f, 1f, 15f), Quaternion.Euler(0f, 180f, 0f));
            otherTeammate.Movement.ResetMotion(new Vector3(-8f, 1f, 15f), Quaternion.Euler(0f, 180f, 0f));
            TickAll(opponentAi);
            bool supportingFormation = CountForecheckers(opponentAi) == 1 && ai.IsForechecking && !teammateAi.IsForechecking
                && !otherTeammateAi.IsForechecking
                && teammateAi.CurrentState == HockeyAIState.Defend
                && otherTeammateAi.CurrentState == HockeyAIState.Defend;

            const float stepDistance = 0.25f;
            const int maximumSteps = 40;
            for (int step = 0; step < maximumSteps && puck.Carrier == carrier; step++)
            {
                ai.DecideAndActForValidation();
                if (puck.Carrier == null) break;
                if (ai.CurrentState != HockeyAIState.Defend) return false;
                Vector3 direction = CameraRelativeDirection(ai.Move);
                if (direction.sqrMagnitude < 0.01f) return false;
                challenger.Movement.ResetMotion(challenger.transform.position + direction * stepDistance,
                    Quaternion.LookRotation(direction, Vector3.up));
                challenger.Movement.SetPlanarVelocityForValidation(direction * 8f);
                Physics.SyncTransforms();
            }

            bool openIceRecovery = humanCheckSucceeded && humanCooldownActive && deterministicTie && uniqueHandoff
                && supportingFormation && puck.Carrier == null
                && puck.LastPlayerTouchId == challenger.PlayerId
                && defensiveChecks.LastResult != DefensiveCheckResult.None;

            defensiveChecks.ResetCooldownForValidation();
            puck.ResetPuck(Vector3.zero);
            PlayerAttributeBuild originalChallengerBuild = challenger.Attributes.Clone();
            PlayerAttributeBuild originalBoardCarrierBuild = carrier.Attributes.Clone();
            challenger.ApplyBuild(new PlayerAttributeBuild());
            carrier.ApplyBuild(BuildWithMaximum(PlayerAttribute.Control, PlayerAttribute.Strength,
                PlayerAttribute.Agility, PlayerAttribute.Speed));
            carrier.Movement.ResetMotion(Vector3.zero, Quaternion.identity);
            challenger.Movement.ResetMotion(new Vector3(0f, 1f, -1.2f), Quaternion.identity);
            StagePuckAtStick(puck, carrier);
            bool stationaryCarrierClaimed = puck.TryClaim(carrier, carrier.Stick);
            ai.DecideAndActForValidation();
            bool stationaryRecovery = stationaryCarrierClaimed && puck.Carrier == null
                && puck.LastPlayerTouchId == challenger.PlayerId
                && defensiveChecks.LastResult == DefensiveCheckResult.BodyCheck;

            defensiveChecks.ResetCooldownForValidation();
            puck.ResetPuck(Vector3.zero);
            carrier.Deke.ResetAction();
            carrier.Movement.ResetMotion(Vector3.zero, Quaternion.identity);
            challenger.Movement.ResetMotion(new Vector3(0f, 1f, -1.2f), Quaternion.identity);
            StagePuckAtStick(puck, carrier);
            bool stationaryDekeCarrierClaimed = puck.TryClaim(carrier, carrier.Stick);
            bool stationaryDekeStarted = carrier.Deke.Tick(true);
            ai.DecideAndActForValidation();
            bool stationaryDekeResisted = stationaryDekeCarrierClaimed && stationaryDekeStarted
                && puck.Carrier == carrier
                && defensiveChecks.LastResult == DefensiveCheckResult.None;

            defensiveChecks.ResetCooldownForValidation();
            puck.ResetPuck(Vector3.zero);
            Vector3 boardCarrierPosition = new(PrototypeRinkGeometry.Width * 0.5f - 0.8f, 1f, 0f);
            carrier.Movement.ResetMotion(boardCarrierPosition, Quaternion.identity);
            carrier.Movement.SetPlanarVelocityForValidation(Vector3.forward * 8f);
            challenger.Movement.ResetMotion(boardCarrierPosition + Vector3.back * 1.2f, Quaternion.identity);
            challenger.Movement.SetPlanarVelocityForValidation(Vector3.forward * 8f);
            StagePuckAtStick(puck, carrier);
            bool boardCarrierClaimed = puck.TryClaim(carrier, carrier.Stick);
            ai.DecideAndActForValidation();
            bool boardRecovery = boardCarrierClaimed && puck.Carrier == null
                && puck.LastPlayerTouchId == challenger.PlayerId
                && defensiveChecks.LastResult == DefensiveCheckResult.BodyCheck;

            defensiveChecks.ResetCooldownForValidation();
            puck.ResetPuck(Vector3.zero);
            Vector3 cornerCarrierPosition = RoundedBoardPoint(0.8f);
            Vector3 cornerCenter = new(
                PrototypeRinkGeometry.Width * 0.5f - PrototypeRinkGeometry.CornerRadius,
                1f,
                PrototypeRinkGeometry.Length * 0.5f - PrototypeRinkGeometry.CornerRadius);
            Vector3 cornerOutward = Vector3.ProjectOnPlane(cornerCarrierPosition - cornerCenter, Vector3.up).normalized;
            Vector3 cornerTangent = new(-cornerOutward.z, 0f, cornerOutward.x);
            Quaternion cornerFacing = Quaternion.LookRotation(cornerTangent, Vector3.up);
            carrier.Movement.ResetMotion(cornerCarrierPosition, cornerFacing);
            carrier.Movement.SetPlanarVelocityForValidation(cornerTangent * 8f);
            challenger.Movement.ResetMotion(cornerCarrierPosition - cornerTangent * 1.2f, cornerFacing);
            challenger.Movement.SetPlanarVelocityForValidation(cornerTangent * 8f);
            StagePuckAtStick(puck, carrier);
            bool cornerCarrierClaimed = puck.TryClaim(carrier, carrier.Stick);
            ai.DecideAndActForValidation();
            bool cornerRecovery = cornerCarrierClaimed && puck.Carrier == null
                && puck.LastPlayerTouchId == challenger.PlayerId
                && defensiveChecks.LastResult == DefensiveCheckResult.BodyCheck;
            challenger.ApplyBuild(originalChallengerBuild);
            carrier.ApplyBuild(originalBoardCarrierBuild);

            bool passed = openIceRecovery && stationaryRecovery && stationaryDekeResisted
                && boardRecovery && cornerRecovery;
            if (!passed) Debug.LogWarning($"AI_PRESSURE_FAIL humanCheck={humanCheckSucceeded} cooldown={humanCooldownActive} tie={deterministicTie} handoff={uniqueHandoff} support={supportingFormation} openIce={openIceRecovery} stationaryClaim={stationaryCarrierClaimed} stationaryRecovery={stationaryRecovery} stationaryDekeClaim={stationaryDekeCarrierClaimed} stationaryDekeStarted={stationaryDekeStarted} stationaryDekeResisted={stationaryDekeResisted} boardClaim={boardCarrierClaimed} boardRecovery={boardRecovery} cornerClaim={cornerCarrierClaimed} cornerRecovery={cornerRecovery} carrier={puck.Carrier?.PlayerId} lastTouch={puck.LastPlayerTouchId} result={defensiveChecks.LastResult}");
            return passed;
        }

        private static Vector3 RoundedBoardPoint(float clearance)
        {
            Vector3 cornerCenter = new(
                PrototypeRinkGeometry.Width * 0.5f - PrototypeRinkGeometry.CornerRadius,
                1f,
                PrototypeRinkGeometry.Length * 0.5f - PrototypeRinkGeometry.CornerRadius);
            return cornerCenter + new Vector3(1f, 0f, 1f).normalized
                * (PrototypeRinkGeometry.CornerRadius - clearance);
        }

        private static void TickAll(HockeyPlayerAI[] players)
        {
            for (int i = 0; i < players.Length; i++) players[i].TickDecisionForValidation();
        }

        private static int CountForecheckers(HockeyPlayerAI[] players)
        {
            int count = 0;
            for (int i = 0; i < players.Length; i++) if (players[i].IsForechecking) count++;
            return count;
        }

        private static bool SimulateAiPuckPickup(PlayerController player, HockeyPlayerAI ai, PuckController puck)
        {
            const float stepDistance = 0.25f;
            const int maximumSteps = 80;
            for (int step = 0; step < maximumSteps && puck.Carrier == null; step++)
            {
                ai.DecideForValidation();
                if (ai.CurrentState != HockeyAIState.ChasePuck) return false;
                Vector3 direction = CameraRelativeDirection(ai.Move);
                if (direction.sqrMagnitude < 0.01f) return false;
                player.Movement.ResetMotion(player.transform.position + direction * stepDistance,
                    Quaternion.LookRotation(direction, Vector3.up));
                Physics.SyncTransforms();
                puck.TryClaim(player, player.Stick);
            }
            return puck.Carrier == player;
        }

        private static Vector3 CameraRelativeDirection(Vector2 input)
        {
            Camera view = Camera.main;
            Vector3 forward = view != null
                ? Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized
                : Vector3.forward;
            Vector3 right = view != null
                ? Vector3.ProjectOnPlane(view.transform.right, Vector3.up).normalized
                : Vector3.right;
            return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
        }

        private static void VerifyCleanPlayerInputRoutes(LocalMatchSetup setup, VirtualJoystick joystick,
            out bool wasdPassed, out bool joystickPassed)
        {
            wasdPassed = false;
            joystickPassed = false;
            PlayerController player = setup?.SwitchController?.ControlledPlayer;
            PlayerInputController sharedInput = setup?.HumanInput;
            LocalPlayerInput hardware = Object.FindAnyObjectByType<LocalPlayerInput>();
            if (player == null || sharedInput == null || hardware == null || joystick == null
                || EventSystem.current == null) return;

            Vector3 originalPosition = player.transform.position;
            Quaternion originalRotation = player.transform.rotation;
            Keyboard keyboard = Keyboard.current;
            bool addedKeyboard = keyboard == null;
            if (addedKeyboard) keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                player.Movement.ResetMotion(originalPosition, originalRotation);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                InputSystem.Update();
                Vector2 hardwareMove = hardware.Move;
                Vector2 sharedHardwareMove = sharedInput.Move;
                player.TickInputForValidation(sharedInput, 0.1f);
                Vector2 routedHardwareMove = player.MoveInput;
                player.Movement.StepPlanarForValidation(routedHardwareMove, 0.1f);
                float acceleratedSpeed = player.Movement.Velocity.magnitude;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                player.TickInputForValidation(sharedInput, 0.1f);
                player.Movement.StepPlanarForValidation(player.MoveInput, 0.1f);
                float releasedSpeed = player.Movement.Velocity.magnitude;
                wasdPassed = hardwareMove.y > 0.99f && sharedHardwareMove.y > 0.99f
                    && routedHardwareMove.y > 0.99f && acceleratedSpeed > 0.1f
                    && releasedSpeed < acceleratedSpeed;
                if (wasdPassed) Debug.Log(
                    "CLEAN_PLAYER_INPUT_WASD_PASS source=Keyboard pipeline=LocalPlayerInput>PlayerInputController>PlayerController>PlayerMovementController");

                player.Movement.ResetMotion(originalPosition, originalRotation);
                RectTransform background = joystick.transform.Find("JoystickBackground") as RectTransform;
                if (background == null) return;
                PointerEventData pointer = new(EventSystem.current)
                {
                    pointerId = 141,
                    button = PointerEventData.InputButton.Left,
                    position = RectTransformUtility.WorldToScreenPoint(null, background.position)
                };
                ExecuteEvents.Execute(joystick.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                pointer.position += Vector2.right * joystick.Radius * 0.8f;
                ExecuteEvents.Execute(joystick.gameObject, pointer, ExecuteEvents.dragHandler);
                Vector2 joystickDirection = joystick.Direction;
                Vector2 sharedJoystickMove = sharedInput.Move;
                player.TickInputForValidation(sharedInput, 0.1f);
                Vector2 routedJoystickMove = player.MoveInput;
                player.Movement.StepPlanarForValidation(routedJoystickMove, 0.1f);
                float joystickSpeed = player.Movement.Velocity.magnitude;
                ExecuteEvents.Execute(joystick.gameObject, pointer, ExecuteEvents.pointerUpHandler);
                player.TickInputForValidation(sharedInput, 0.1f);
                player.Movement.StepPlanarForValidation(player.MoveInput, 0.1f);
                float joystickReleasedSpeed = player.Movement.Velocity.magnitude;
                joystickPassed = joystickDirection.x > 0.5f && sharedJoystickMove.x > 0.5f
                    && routedJoystickMove.x > 0.5f && joystickSpeed > 0.1f
                    && joystick.Direction == Vector2.zero && joystickReleasedSpeed < joystickSpeed;
                if (joystickPassed) Debug.Log(
                    "CLEAN_PLAYER_INPUT_JOYSTICK_PASS source=PointerEvent pipeline=VirtualJoystick>PlayerInputController>PlayerController>PlayerMovementController");
            }
            finally
            {
                if (keyboard != null)
                {
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    InputSystem.Update();
                    if (addedKeyboard) InputSystem.RemoveDevice(keyboard);
                }
                player.Movement.ResetMotion(originalPosition, originalRotation);
            }
        }

        private static void VerifyCleanPlayerPresentation(PlayerController player, HockeyCameraController camera,
            out bool cameraColliderPassed, out bool animationPassed, out bool footAlignmentPassed)
        {
            cameraColliderPassed = false;
            animationPassed = false;
            footAlignmentPassed = false;
            if (player == null || camera == null) return;
            CharacterController collider = player.GetComponent<CharacterController>();
            Transform visual = player.transform.Find("Visual");
            Transform clean = visual != null ? visual.Find("Male_Base_IntegratedSkates_Visual") : null;
            HockeyCharacterPresentation presentation = player.GetComponent<HockeyCharacterPresentation>();
            Animator animator = clean != null ? clean.GetComponentInChildren<Animator>(true) : null;
            bool visualPhysics = visual != null && (visual.GetComponentsInChildren<Collider>(true).Length > 0
                || visual.GetComponentsInChildren<Rigidbody>(true).Length > 0);
            cameraColliderPassed = camera.Target == player.transform && collider != null && collider.enabled
                && collider.center == Vector3.zero && Mathf.Approximately(collider.height, 2f)
                && Mathf.Approximately(collider.radius, 0.45f) && !visualPhysics;
            if (cameraColliderPassed)
                Debug.Log("CLEAN_PLAYER_CAMERA_COLLIDER_PASS target=GameplayRoot rootMotion=false");

            if (presentation != null && animator != null && animator.runtimeAnimatorController != null)
            {
                Vector3 position = player.transform.position;
                Quaternion rotation = player.transform.rotation;
                bool parameterContract = VerifyCleanAnimatorParameterContract(player, presentation, animator);
                presentation.SetPreviewState(HockeyPresentationState.Running);
                animator.Update(0.02f);
                animator.Update(0.2f);
                AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
                AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
                string[] required = { "Speed", "ForwardAmount", "TurnAmount", "IsMoving", "IsBackward",
                    "IsBraking", "IsSprinting", "CrossoverDirection" };
                animationPassed = parameterContract && !animator.applyRootMotion
                    && required.All(name => animator.parameters.Count(parameter => parameter.name == name) == 1)
                    && presentation.CurrentPresentationState == HockeyPresentationState.Running
                    && (current.IsName("Running") || next.IsName("Running"))
                    && Vector3.Distance(player.transform.position, position) < 0.0001f
                    && Quaternion.Angle(player.transform.rotation, rotation) < 0.01f;
                presentation.Bind(player);
                if (animationPassed) Debug.Log("CLEAN_PLAYER_ANIMATION_PASS states=Idle,Running");
            }

            if (presentation != null && animator != null)
            {
                presentation.SetPreviewState(HockeyPresentationState.Idle);
                animator.Play("Idle", 0, 0f);
                animator.Update(0.05f);
            }
            if (clean != null && animator != null
                && TryMeasureVisibleFootContacts(clean, animator, out float leftContact, out float rightContact))
            {
                float leftDistance = Mathf.Abs(leftContact - 0.2f);
                float rightDistance = Mathf.Abs(rightContact - 0.2f);
                Debug.Log($"CLEAN_PLAYER_FOOT_ALIGNMENT_MEASURE left={leftDistance:F4} right={rightDistance:F4} leftY={leftContact:F4} rightY={rightContact:F4} rootY={player.transform.position.y:F4} visualLocalY={visual.localPosition.y:F4} iceY=0.200");
                footAlignmentPassed = leftDistance <= 0.03f && rightDistance <= 0.03f;
                if (footAlignmentPassed) Debug.Log(
                    $"CLEAN_PLAYER_FOOT_ALIGNMENT_PASS left={leftDistance:F4} right={rightDistance:F4} iceY=0.200");
                else throw new System.InvalidOperationException(
                    $"CLEAN_PLAYER_FOOT_ALIGNMENT_FAIL left={leftDistance:F4} right={rightDistance:F4} "
                    + $"leftY={leftContact:F4} rightY={rightContact:F4} rootY={player.transform.position.y:F4}");
            }
            presentation?.ClearPreview();
        }

        private static bool VerifyCleanAnimatorParameterContract(PlayerController player,
            HockeyCharacterPresentation presentation, Animator animator)
        {
            AttributeValidationInput input = new();
            Vector3 originalVelocity = player.Movement.Velocity;
            try
            {
                bool idle = ApplyCleanAnimatorCase(player, presentation, animator, input,
                    Vector3.zero, Vector3.zero)
                    && Nearly(animator.GetFloat("Speed"), 0f)
                    && Nearly(animator.GetFloat("ForwardAmount"), 0f)
                    && Nearly(animator.GetFloat("TurnAmount"), 0f)
                    && !animator.GetBool("IsMoving") && !animator.GetBool("IsBackward")
                    && !animator.GetBool("IsBraking") && !animator.GetBool("IsSprinting")
                    && Nearly(animator.GetFloat("CrossoverDirection"), 0f);

                Vector3 forward = player.transform.forward * 4f;
                bool forwardCase = ApplyCleanAnimatorCase(player, presentation, animator, input,
                    forward, player.transform.forward)
                    && Nearly(animator.GetFloat("Speed"), 4f)
                    && Nearly(animator.GetFloat("ForwardAmount"), 1f)
                    && Nearly(animator.GetFloat("TurnAmount"), 0f)
                    && animator.GetBool("IsMoving") && !animator.GetBool("IsBackward")
                    && !animator.GetBool("IsBraking") && !animator.GetBool("IsSprinting");

                Vector3 right = player.transform.right * 3f;
                bool turn = ApplyCleanAnimatorCase(player, presentation, animator, input,
                    right, player.transform.right)
                    && Nearly(animator.GetFloat("ForwardAmount"), 0f)
                    && Nearly(animator.GetFloat("TurnAmount"), 0.5f)
                    && Nearly(animator.GetFloat("CrossoverDirection"), 0.5f)
                    && !animator.GetBool("IsBackward") && !animator.GetBool("IsBraking");

                Vector3 backward = -player.transform.forward * 2f;
                bool backwardCase = ApplyCleanAnimatorCase(player, presentation, animator, input,
                    backward, -player.transform.forward)
                    && Nearly(animator.GetFloat("ForwardAmount"), -1f)
                    && animator.GetBool("IsBackward") && !animator.GetBool("IsBraking");

                bool braking = ApplyCleanAnimatorCase(player, presentation, animator, input,
                    player.transform.forward * 3f, -player.transform.forward)
                    && Nearly(animator.GetFloat("ForwardAmount"), 1f)
                    && !animator.GetBool("IsBackward") && animator.GetBool("IsBraking");

                bool passed = idle && forwardCase && turn && backwardCase && braking;
                if (passed) Debug.Log("CLEAN_PLAYER_ANIMATOR_PARAMETERS_PASS cases=Idle,Forward,Turn,Backward,Braking");
                return passed;
            }
            finally
            {
                input.Move = Vector2.zero;
                player.TickInputForValidation(input, 0f);
                player.Movement.SetPlanarVelocityForValidation(originalVelocity);
                presentation.TickForValidation();
            }
        }

        private static bool ApplyCleanAnimatorCase(PlayerController player,
            HockeyCharacterPresentation presentation, Animator animator, AttributeValidationInput input,
            Vector3 velocity, Vector3 desiredWorldDirection)
        {
            input.Move = CameraInputForWorldDirection(desiredWorldDirection);
            player.TickInputForValidation(input, 0f);
            player.Movement.SetPlanarVelocityForValidation(velocity);
            presentation.TickForValidation();
            return !animator.applyRootMotion;
        }

        private static Vector2 CameraInputForWorldDirection(Vector3 worldDirection)
        {
            if (worldDirection.sqrMagnitude <= 0.0001f) return Vector2.zero;
            Camera view = Camera.main;
            Vector3 forward = view != null
                ? Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = view != null
                ? Vector3.ProjectOnPlane(view.transform.right, Vector3.up).normalized : Vector3.right;
            Vector3 direction = Vector3.ProjectOnPlane(worldDirection, Vector3.up).normalized;
            return Vector2.ClampMagnitude(new Vector2(
                Vector3.Dot(direction, right), Vector3.Dot(direction, forward)), 1f);
        }

        private static bool TryMeasureVisibleFootContacts(Transform clean, Animator animator,
            out float leftContact, out float rightContact)
        {
            leftContact = float.PositiveInfinity;
            rightContact = float.PositiveInfinity;
            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (leftFoot == null || rightFoot == null) return false;
            float ceiling = Mathf.Max(leftFoot.position.y, rightFoot.position.y) + 0.2f;
            foreach (SkinnedMeshRenderer renderer in clean.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh baked = new Mesh();
                renderer.BakeMesh(baked, true);
                Matrix4x4 bakedToWorld = Matrix4x4.TRS(
                    renderer.transform.position, renderer.transform.rotation, Vector3.one);
                foreach (Vector3 vertex in baked.vertices)
                {
                    Vector3 world = bakedToWorld.MultiplyPoint3x4(vertex);
                    if (world.y > ceiling) continue;
                    float leftDistance = Mathf.Abs(world.x - leftFoot.position.x)
                        + Mathf.Abs(world.z - leftFoot.position.z) * 0.25f;
                    float rightDistance = Mathf.Abs(world.x - rightFoot.position.x)
                        + Mathf.Abs(world.z - rightFoot.position.z) * 0.25f;
                    if (leftDistance <= rightDistance) leftContact = Mathf.Min(leftContact, world.y);
                    else rightContact = Mathf.Min(rightContact, world.y);
                }
                UnityEngine.Object.Destroy(baked);
            }
            return float.IsFinite(leftContact) && float.IsFinite(rightContact);
        }

        private static bool VerifyHardwareActionContract()
        {
            return Object.FindAnyObjectByType<LocalPlayerInput>() != null
                && LocalPlayerInput.PassKeyboardKey == Key.E
                && LocalPlayerInput.DekeKeyboardKey == Key.LeftShift
                && LocalPlayerInput.ShootKeyboardKey == Key.Space
                && LocalPlayerInput.SwitchKeyboardKey == Key.Q
                && LocalPlayerInput.CheckKeyboardKey == Key.F
                && LocalPlayerInput.PassGamepadButton == GamepadButton.West
                && LocalPlayerInput.DekeGamepadButton == GamepadButton.South
                && LocalPlayerInput.SwitchGamepadButton == GamepadButton.North
                && LocalPlayerInput.CheckGamepadButton == GamepadButton.East;
        }

        private static bool VerifyDefensiveChecks(LocalMatchSetup setup, PlayerController[] players,
            PlayerController opponent, PuckController puck, out bool tuningBounds, out bool bodyCheck,
            out bool pullCheck, out bool sharedCooldown, out bool rejectedCheck,
            out bool impulseReset, out bool looseAfterCheck)
        {
            DefensiveCheckTuning.Values malformed = DefensiveCheckTuning.Sanitize(-10f, -20f, 2f,
                0f, 100f, -1f, 100f, -20f);
            DefensiveCheckTuning.Values nonFinite = DefensiveCheckTuning.Sanitize(float.NaN, float.PositiveInfinity,
                float.NaN, float.NegativeInfinity, float.NaN, float.PositiveInfinity,
                float.NaN, float.NegativeInfinity);
            tuningBounds = malformed.BodyRange >= 0.5f && malformed.BodyRange <= 2f
                && malformed.PullRange > malformed.BodyRange && malformed.PullRange <= 3.5f
                && malformed.PullForwardDot >= 0f && malformed.PullForwardDot <= 1f
                && malformed.CooldownSeconds >= 0.2f && malformed.CooldownSeconds <= 2f
                && malformed.BodyPuckSpeed <= 15f && malformed.PullPuckSpeed >= 1f
                && malformed.BodyImpulse <= DefensiveCheckTuning.MaximumBodyImpulse
                && nonFinite.BodyRange >= 0.5f && nonFinite.BodyRange <= 2f
                && nonFinite.PullRange > nonFinite.BodyRange && nonFinite.PullRange <= 3.5f
                && nonFinite.PullForwardDot >= 0f && nonFinite.PullForwardDot <= 1f
                && nonFinite.CooldownSeconds >= 0.2f && nonFinite.CooldownSeconds <= 2f
                && nonFinite.BodyPuckSpeed >= 1f && nonFinite.BodyPuckSpeed <= 15f
                && nonFinite.PullPuckSpeed >= 1f && nonFinite.PullPuckSpeed <= 15f
                && nonFinite.BodyImpulse >= 0f
                && nonFinite.BodyImpulse <= DefensiveCheckTuning.MaximumBodyImpulse
                && nonFinite.ImpulseDecay >= 4f && nonFinite.ImpulseDecay <= 30f;

            DefensiveCheckController defense = setup.DefenseController;
            PlayerController checker = FindPlayer(players, "blue-5");
            PlayerController alternate = FindPlayer(players, "blue-2");
            defense.SetGameplayEnabledForValidation(true);
            defense.ResetCooldownForValidation();
            checker.Movement.ResetMotion(Vector3.zero, Quaternion.identity);
            checker.Movement.SetPlanarVelocityForValidation(Vector3.forward * 8f);
            opponent.Movement.ResetMotion(Vector3.forward, Quaternion.identity);
            StagePuckAtStick(puck, opponent);
            bool bodyClaimed = puck.TryClaim(opponent, opponent.Stick);
            setup.SwitchController.SetControlled(checker);
            DefensiveCheckResult bodyResult = defense.TryCheck();
            bodyCheck = bodyClaimed && bodyResult == DefensiveCheckResult.BodyCheck
                && checker.Movement.ExternalVelocity.magnitude <= DefensiveCheckTuning.MaximumBodyImpulse
                && opponent.Movement.ExternalVelocity.magnitude <= DefensiveCheckTuning.MaximumBodyImpulse;
            looseAfterCheck = bodyCheck && puck.Carrier == null && puck.LastPlayerTouchId == checker.PlayerId;

            StagePuckAtStick(puck, opponent);
            bool cooldownCarrier = puck.TryClaim(opponent, opponent.Stick);
            setup.SwitchController.SetControlled(alternate);
            sharedCooldown = cooldownCarrier && defense.TryCheck() == DefensiveCheckResult.None
                && puck.Carrier == opponent;

            defense.ResetCooldownForValidation();
            checker.Movement.ResetMotion(Vector3.zero, Quaternion.identity);
            checker.Movement.SetPlanarVelocityForValidation(Vector3.forward * 8f);
            opponent.Movement.ResetMotion(Vector3.forward * 2.25f, Quaternion.identity);
            StagePuckAtStick(puck, opponent);
            bool pullClaimed = puck.TryClaim(opponent, opponent.Stick);
            setup.SwitchController.SetControlled(checker);
            Vector3 towardChecker = Vector3.ProjectOnPlane(checker.transform.position - opponent.transform.position, Vector3.up).normalized;
            DefensiveCheckResult pullResult = defense.TryCheck();
            Vector3 pullVelocity = Vector3.ProjectOnPlane(puck.Body.linearVelocity, Vector3.up);
            for (int i = 0; i < 3; i++) puck.TickPassReception();
            Vector3 pullVelocityAfterTicks = Vector3.ProjectOnPlane(puck.Body.linearVelocity, Vector3.up);
            pullCheck = pullClaimed && pullResult == DefensiveCheckResult.PullCheck && puck.Carrier == null
                && Vector3.Dot(pullVelocity.normalized, towardChecker) > 0.99f
                && puck.IntendedPassReceiver == null
                && Vector3.Distance(pullVelocity, pullVelocityAfterTicks) < 0.001f;

            setup.MatchController.BeginFaceoff();
            bool faceoffResetCooldown = defense.NextCheckTime <= Time.time
                && checker.Movement.ExternalVelocity == Vector3.zero
                && opponent.Movement.ExternalVelocity == Vector3.zero;
            setup.MatchController.StartPlayImmediatelyForValidation();

            defense.ResetCooldownForValidation();
            opponent.Movement.ResetMotion(Vector3.back * 4f, Quaternion.identity);
            StagePuckAtStick(puck, opponent);
            bool rejectedCarrier = puck.TryClaim(opponent, opponent.Stick);
            setup.SwitchController.SetControlled(checker);
            rejectedCheck = rejectedCarrier && defense.TryCheck() == DefensiveCheckResult.None
                && puck.Carrier == opponent;
            opponent.Movement.ResetMotion(Vector3.forward, Quaternion.identity);
            defense.SetGameplayEnabledForValidation(false);
            bool disabledCheckRejected = defense.TryCheck() == DefensiveCheckResult.None
                && puck.Carrier == opponent;
            defense.SetGameplayEnabledForValidation(true);
            rejectedCheck = rejectedCheck && disabledCheckRejected;

            opponent.Movement.ApplyExternalImpulse(Vector3.right * 6f, 6f, 14f);
            opponent.SetGameplayEnabled(false);
            bool disabledCleared = opponent.Movement.ExternalVelocity == Vector3.zero;
            opponent.SetGameplayEnabled(true);
            opponent.Movement.ApplyExternalImpulse(Vector3.right * 6f, 6f, 14f);
            opponent.Movement.ResetMotion(opponent.transform.position, opponent.transform.rotation);
            impulseReset = disabledCleared && opponent.Movement.ExternalVelocity == Vector3.zero
                && faceoffResetCooldown;
            defense.SetGameplayEnabledForValidation(true);
            return tuningBounds && bodyCheck && pullCheck && sharedCooldown && rejectedCheck
                && impulseReset && looseAfterCheck;
        }

        private static bool VerifyRepeatedControlTransitions(PlayerInputController input,
            PlayerController human, PlayerController looseNearest, PlayerController opponent,
            PlayerController[] players, PuckController puck,
            PlayerSwitchController switchController, PlayerControlManager controlManager,
            HockeyCameraController camera, MobileActionButton pass, MobileActionButton deke,
            MobileActionButton shoot)
        {
            int stagedIndex = 0;
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].Team != TeamId.Blue || players[i] == looseNearest) continue;
                players[i].Movement.ResetMotion(new Vector3(8f + stagedIndex++, 1f, 0f), Quaternion.identity);
            }
            looseNearest.Movement.ResetMotion(new Vector3(1f, 1f, 0f), Quaternion.identity);
            puck.ResetPuck(Vector3.zero);
            bool looseClosestControlled = switchController.ControlledPlayer == looseNearest
                && controlManager.LastAutomaticReason == AutomaticControlReason.LoosePuck;
            PointerEventData looseSwitchPointer = new(EventSystem.current)
            {
                pointerId = 91,
                button = PointerEventData.InputButton.Left
            };
            pass.OnPointerDown(looseSwitchPointer);
            switchController.ResetCooldownForValidation();
            switchController.TickForValidation();
            PlayerController manualLooseController = switchController.ControlledPlayer;
            pass.OnPointerUp(looseSwitchPointer);
            pass.ResetInput();
            bool looseMode = input.Mode == MobileActionMode.Loose && pass.Label == "SWITCH"
                && shoot.Label == "SHOOT" && pass.gameObject.activeSelf
                && !deke.gameObject.activeSelf && !shoot.gameObject.activeSelf;
            bool looseSwitchRouted = manualLooseController != null && manualLooseController != looseNearest
                && camera.Target == manualLooseController.transform;
            int selectionsAfterManualSwitch = controlManager.AutomaticSelectionCount;
            puck.Body.position = looseNearest.transform.position;
            Physics.SyncTransforms();
            bool looseManualOverridePersists = switchController.ControlledPlayer == manualLooseController
                && controlManager.AutomaticSelectionCount == selectionsAfterManualSwitch;
            StagePuckAtStick(puck, human);
            bool blueClaimed = puck.TryClaim(human, human.Stick);
            bool blueOffense = blueClaimed && input.Mode == MobileActionMode.Offense
                && pass.gameObject.activeSelf && deke.gameObject.activeSelf && shoot.gameObject.activeSelf
                && pass.Label == "PASS" && deke.Label == "DEKE" && shoot.Label == "SHOOT";
            puck.ResetPuck(Vector3.zero);
            StagePuckAtStick(puck, opponent);
            bool redClaimed = puck.TryClaim(opponent, opponent.Stick);
            bool redDefense = redClaimed && input.Mode == MobileActionMode.Defense
                && pass.Label == "SWITCH" && pass.gameObject.activeSelf
                && !deke.gameObject.activeSelf && shoot.Label == "CHECK" && shoot.gameObject.activeSelf;
            return looseMode && looseClosestControlled && looseSwitchRouted && looseManualOverridePersists
                && blueOffense && redDefense && !input.PassPressed
                && !input.SwitchPressed && !input.CheckPressed && !input.ShootReleased;
        }

        private static bool AllSkatersUseScale(PlayerController[] players, float expectedScale)
        {
            Vector3 expected = Vector3.one * expectedScale;
            for (int i = 0; i < players.Length; i++)
            {
                if ((players[i].transform.localScale - expected).sqrMagnitude > 0.0001f) return false;
            }
            return true;
        }

        private static bool AllGoaliesAreBroader(HockeyGoalieAI[] goalies, PlayerController referencePlayer)
        {
            Renderer referenceRenderer = referencePlayer != null ? referencePlayer.GetComponentInChildren<Renderer>() : null;
            CharacterController referenceController = referencePlayer != null ? referencePlayer.GetComponent<CharacterController>() : null;
            if (referenceRenderer == null || referenceController == null) return false;

            for (int i = 0; i < goalies.Length; i++)
            {
                GameObject goalie = goalies[i].gameObject;
                Renderer goalieRenderer = goalie.GetComponentInChildren<Renderer>();
                CharacterController goalieController = goalie.GetComponent<CharacterController>();
                if (goalieRenderer == null || goalieController == null
                    || goalieRenderer.bounds.size.x < referenceRenderer.bounds.size.x * 1.2f
                    || goalieRenderer.bounds.size.z < referenceRenderer.bounds.size.z * 1.1f
                    || goalieRenderer.bounds.size.y < referenceRenderer.bounds.size.y
                    || goalieRenderer.bounds.size.y > referenceRenderer.bounds.size.y * 1.12f
                    || Mathf.Abs(goalieController.height - referenceController.height) > 0.0001f
                    || Mathf.Abs(goalieController.radius - referenceController.radius) > 0.0001f)
                    return false;
            }
            return true;
        }

        private static bool VerifyArenaPresentation()
        {
            GameObject centerLine = GameObject.Find("Center Line");
            GameObject blueGoal = GameObject.Find("Blue Goal Post A");
            GameObject redGoal = GameObject.Find("Red Goal Post A");
            GameObject board = GameObject.Find("Rink Board 00");
            GameObject kickplate = GameObject.Find("Yellow Kickplate 00");
            GameObject rail = GameObject.Find("Blue Top Rail 00");
            GameObject glass = GameObject.Find("Rink Glass 00");
            GameObject rearPost = GameObject.Find("Blue Goal Rear Post A");
            GameObject roofNet = GameObject.Find("Blue Goal Net Roof Longitudinal 4");
            GameObject sideNet = GameObject.Find("Blue Goal Net Side A Vertical 1");
            GoalTrigger blueTrigger = GameObject.Find("Blue Goal Trigger")?.GetComponent<GoalTrigger>();
            GoalTrigger redTrigger = GameObject.Find("Red Goal Trigger")?.GetComponent<GoalTrigger>();
            HockeyGoalieAI[] goalies = Object.FindObjectsByType<HockeyGoalieAI>();
            HockeyCameraController controller = Object.FindAnyObjectByType<HockeyCameraController>();
            Camera view = controller != null ? controller.GetComponent<Camera>() : null;

            bool elongatedRink = centerLine != null && centerLine.transform.localScale.x >= 23f
                && blueGoal != null && redGoal != null
                && Mathf.Abs(blueGoal.transform.position.z) >= 20f
                && Mathf.Abs(redGoal.transform.position.z) >= 20f;
            bool layeredBoards = board != null && board.GetComponent<Collider>() != null
                && board.transform.localScale.y <= 1.1f
                && kickplate != null && !HasEnabledCollider(kickplate)
                && rail != null && !HasEnabledCollider(rail)
                && glass != null && !HasEnabledCollider(glass)
                && glass.transform.localScale.y >= 1.2f;
            bool dimensionalNets = rearPost != null && roofNet != null && sideNet != null
                && Mathf.Abs(rearPost.transform.position.z - blueGoal.transform.position.z) >= 0.85f;
            HockeyGoalieAI blueGoalie = FindGoalie(goalies, TeamId.Blue);
            HockeyGoalieAI redGoalie = FindGoalie(goalies, TeamId.Red);
            Vector3 blueDefense = AIFormationController.Defend(TeamId.Blue, SkaterRole.LeftDefense, new Vector3(0f, 1f, -PrototypeRinkGeometry.GoalieAnchor));
            Vector3 redDefense = AIFormationController.Defend(TeamId.Red, SkaterRole.LeftDefense, new Vector3(0f, 1f, PrototypeRinkGeometry.GoalieAnchor));
            bool alignedArenaAnchors = blueTrigger != null && redTrigger != null
                && Mathf.Abs(blueTrigger.transform.position.z + PrototypeRinkGeometry.GoalLineDistance + PrototypeRinkGeometry.GoalDepth * 0.5f) < 0.01f
                && Mathf.Abs(redTrigger.transform.position.z - PrototypeRinkGeometry.GoalLineDistance - PrototypeRinkGeometry.GoalDepth * 0.5f) < 0.01f
                && Mathf.Abs(blueTrigger.GetComponent<BoxCollider>().size.z - PrototypeRinkGeometry.GoalDepth) < 0.01f
                && Mathf.Abs(redTrigger.GetComponent<BoxCollider>().size.z - PrototypeRinkGeometry.GoalDepth) < 0.01f
                && blueGoalie != null && Mathf.Abs(blueGoalie.Anchor.z + PrototypeRinkGeometry.GoalieAnchor) < 0.01f
                && redGoalie != null && Mathf.Abs(redGoalie.Anchor.z - PrototypeRinkGeometry.GoalieAnchor) < 0.01f
                && blueDefense.z < -12f && redDefense.z > 12f
                && Mathf.Abs(blueDefense.z + redDefense.z) < 0.01f;
            bool closeCamera = view != null && view.fieldOfView <= 46.1f
                && controller.Target != null
                && view.transform.position.y - controller.Target.position.y <= 12.1f
                && Mathf.Abs(view.transform.position.z - controller.Target.position.z) <= 15.1f;
            bool passed = elongatedRink && layeredBoards && dimensionalNets && alignedArenaAnchors && closeCamera;
            if (!passed) Debug.LogWarning($"ARENA_PRESENTATION_FAIL elongated={elongatedRink} boards={layeredBoards} nets={dimensionalNets} anchors={alignedArenaAnchors} camera={closeCamera} boardY={board?.transform.localScale.y} boardCollider={board?.GetComponent<Collider>() != null} kickCollider={kickplate?.GetComponent<Collider>() != null} railCollider={rail?.GetComponent<Collider>() != null} glassCollider={glass?.GetComponent<Collider>() != null} glassY={glass?.transform.localScale.y}");
            return passed;
        }

        private static HockeyGoalieAI FindGoalie(HockeyGoalieAI[] goalies, TeamId team)
        {
            for (int i = 0; i < goalies.Length; i++) if (goalies[i].Team == team) return goalies[i];
            return null;
        }

        private static float GoalWidth(GoalTrigger goal)
        {
            BoxCollider volume = goal != null ? goal.GetComponent<BoxCollider>() : null;
            return volume != null ? volume.size.x : 0f;
        }

        private static bool HasEnabledCollider(GameObject target)
        {
            Collider targetCollider = target != null ? target.GetComponent<Collider>() : null;
            return targetCollider != null && targetCollider.enabled;
        }

        private static bool SimulateGoalCrossing(GoalTrigger goal, PuckController puck,
            MatchController match, float shotSpeed)
        {
            BoxCollider volume = goal.GetComponent<BoxCollider>();
            Vector3 goalLine = goal.transform.position - goal.ScoringDirection * (volume.size.z * 0.5f);
            int blueBefore = match.BlueScore;
            int redBefore = match.RedScore;
            SimulationMode previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            try
            {
                Vector3 startPosition = goalLine - goal.ScoringDirection * 0.35f;
                puck.ResetPuck(startPosition);
                puck.transform.position = startPosition;
                Physics.SyncTransforms();
                puck.Body.linearVelocity = goal.ScoringDirection * shotSpeed;
                for (int step = 0; step < 30 && match.State == MatchStateSnapshot.Playing; step++)
                {
                    Physics.Simulate(Time.fixedDeltaTime);
                    goal.TickSweptGoalLineForValidation();
                }
            }
            finally
            {
                Physics.simulationMode = previousMode;
            }

            return match.State == MatchStateSnapshot.GoalPause
                && (match.BlueScore == blueBefore + 1 || match.RedScore == redBefore + 1)
                && match.BlueScore + match.RedScore == blueBefore + redBefore + 1;
        }

        private static bool VerifyPassOutcomes(PlayerController passer, PlayerController receiver,
            PlayerController interceptor, PuckController puck, PlayerController[] players,
            out bool shortReceived, out bool mediumReceived, out bool longReceived,
            out bool movingReceived, out bool obstructedIntercepted, out bool missedPassStayedLoose)
        {
            Vector3[] positions = new Vector3[players.Length];
            Quaternion[] rotations = new Quaternion[players.Length];
            bool[] controllerStates = new bool[players.Length];
            SimulationMode previousMode = Physics.simulationMode;
            for (int i = 0; i < players.Length; i++)
            {
                positions[i] = players[i].transform.position;
                rotations[i] = players[i].transform.rotation;
                CharacterController controller = players[i].GetComponent<CharacterController>();
                controllerStates[i] = controller != null && controller.enabled;
                if (controller != null) controller.enabled = false;
            }

            Physics.simulationMode = SimulationMode.Script;
            try
            {
                obstructedIntercepted = SimulatePassOutcome(passer, receiver, interceptor, interceptor,
                    puck, 8f, Vector3.zero, 0f);
                shortReceived = SimulatePassOutcome(passer, receiver, null, receiver,
                    puck, 3f, Vector3.zero, 1f);
                mediumReceived = SimulatePassOutcome(passer, receiver, null, receiver,
                    puck, 9f, Vector3.zero, -1f);
                longReceived = SimulatePassOutcome(passer, receiver, null, receiver,
                    puck, 16f, Vector3.zero, 1f);
                missedPassStayedLoose = SimulateMissedPassOutcome(passer, receiver, puck);
                movingReceived = SimulatePassOutcome(passer, receiver, null, receiver,
                    puck, 9f, Vector3.right * 1.5f, -1f);
                return shortReceived && mediumReceived && longReceived && movingReceived
                    && obstructedIntercepted && missedPassStayedLoose;
            }
            finally
            {
                Physics.simulationMode = previousMode;
                puck.ResetPuck(new Vector3(0f, 0.55f, 0f));
                for (int i = 0; i < players.Length; i++)
                {
                    players[i].Movement.ResetMotion(positions[i], rotations[i]);
                    CharacterController controller = players[i].GetComponent<CharacterController>();
                    if (controller != null) controller.enabled = controllerStates[i];
                }
                Physics.SyncTransforms();
            }
        }

        private static bool SimulatePassOutcome(PlayerController passer, PlayerController receiver,
            PlayerController interceptor, PlayerController expectedCarrier, PuckController puck,
            float passDistance, Vector3 receiverVelocity, float normalizedError)
        {
            Vector3 passerPosition = new(0f, 1f, -3f);
            Vector3 receiverPosition = passerPosition + Vector3.forward * passDistance;
            passer.Movement.ResetMotion(passerPosition, Quaternion.identity);
            receiver.Movement.ResetMotion(receiverPosition, Quaternion.identity);
            if (interceptor != null)
                interceptor.Movement.ResetMotion(Vector3.Lerp(passerPosition, receiverPosition, 0.5f), Quaternion.identity);

            Vector3 stagedPuckPosition = passer.Stick.ControlPoint;
            stagedPuckPosition.y = 0.55f;
            puck.ResetPuck(stagedPuckPosition);
            puck.transform.position = stagedPuckPosition;
            puck.Body.position = stagedPuckPosition;
            Physics.SyncTransforms();
            Physics.Simulate(Time.fixedDeltaTime);
            puck.ResetPuck(stagedPuckPosition);
            puck.transform.position = stagedPuckPosition;
            puck.Body.position = stagedPuckPosition;
            Physics.SyncTransforms();
            float passing = passer.Attributes.Normalized(PlayerAttribute.Passing);
            float expectedLaunchSpeed = passer.Pass.CalculatePassSpeed(
                Vector3.ProjectOnPlane(receiver.Stick.ControlPoint + receiverVelocity * PassController.EvaluateLeadSeconds(passing)
                    - puck.Body.position, Vector3.up).magnitude) * PassController.EvaluatePaceMultiplier(passing)
                * passer.PerformanceFactor;
            if (!puck.TryClaim(passer, passer.Stick))
            {
                Debug.LogWarning($"PASS_OUTCOME_LAUNCH_FAIL distance={passDistance} reason=claim");
                return false;
            }
            if (!passer.Pass.ReleaseForValidation(receiver, receiverVelocity, normalizedError))
            {
                Debug.LogWarning($"PASS_OUTCOME_LAUNCH_FAIL distance={passDistance} reason=release");
                return false;
            }
            if (Mathf.Abs(puck.Body.linearVelocity.magnitude - expectedLaunchSpeed) > 0.05f)
            {
                Debug.LogWarning($"PASS_OUTCOME_LAUNCH_FAIL distance={passDistance} reason=speed expected={expectedLaunchSpeed:F2} actual={puck.Body.linearVelocity.magnitude:F2}");
                return false;
            }

            const int maximumSteps = 90;
            float nearestReceiverDistance = float.PositiveInfinity;
            for (int step = 0; step < maximumSteps && puck.Carrier == null; step++)
            {
                if (receiverVelocity.sqrMagnitude > 0f)
                    receiver.Movement.ResetMotion(receiver.transform.position + receiverVelocity * Time.fixedDeltaTime,
                        receiver.transform.rotation);
                Physics.SyncTransforms();
                Physics.Simulate(Time.fixedDeltaTime);
                nearestReceiverDistance = Mathf.Min(nearestReceiverDistance,
                    Vector3.ProjectOnPlane(receiver.Stick.ControlPoint - puck.Body.position, Vector3.up).magnitude);
                if (interceptor != null) puck.TryClaim(interceptor, interceptor.Stick);
                if (puck.Carrier == null) receiver.PassReception.TryReceive(puck);
                if (puck.Carrier == null) puck.TryClaim(receiver, receiver.Stick);
            }

            if (puck.Carrier != expectedCarrier)
                Debug.LogWarning($"PASS_OUTCOME_FAIL distance={passDistance} expected={expectedCarrier?.PlayerId} actual={puck.Carrier?.PlayerId} intended={puck.IntendedPassReceiver?.PlayerId} nearestStick={nearestReceiverDistance:F2} playerDistance={Vector3.ProjectOnPlane(receiver.transform.position - puck.Body.position, Vector3.up).magnitude:F2} radius={receiver.PassReception.Radius:F2} puckPosition={puck.Body.position} receiverPosition={receiver.transform.position} receiverControl={receiver.Stick.ControlPoint} speed={puck.Body.linearVelocity.magnitude:F2}");
            return puck.Carrier == expectedCarrier;
        }

        private static bool SimulateMissedPassOutcome(PlayerController passer, PlayerController receiver,
            PuckController puck)
        {
            Vector3 passerPosition = new(0f, 1f, -3f);
            Vector3 receiverPosition = new(0f, 1f, 5f);
            passer.Movement.ResetMotion(passerPosition, Quaternion.identity);
            receiver.Movement.ResetMotion(receiverPosition, Quaternion.identity);
            StagePuckAtStick(puck, passer);
            if (!puck.TryClaim(passer, passer.Stick)
                || !passer.Pass.ReleaseForValidation(receiver, Vector3.zero, 0f)) return false;

            puck.Body.position = receiverPosition + Vector3.forward * (receiver.PassReception.Radius + 0.5f);
            puck.Body.linearVelocity = Vector3.forward * 8f;
            puck.TickPassReception();
            bool eligibilityCleared = puck.IntendedPassReceiver == null;
            puck.Body.position = receiverPosition;
            puck.Body.linearVelocity = Vector3.zero;
            bool lateReentryRejected = !receiver.PassReception.TryReceive(puck);
            return eligibilityCleared && lateReentryRejected && puck.Carrier == null;
        }

        private static float GoalPostHalfWidth(string goalName)
        {
            GameObject first = GameObject.Find(goalName + " Post A");
            GameObject second = GameObject.Find(goalName + " Post B");
            return first != null && second != null ? Mathf.Abs(second.transform.position.x - first.transform.position.x) * 0.5f : 0f;
        }

        private static bool GoalHasPhysicalNet(string goalName)
        {
            BoxCollider back = GameObject.Find(goalName + " Net Back Collision")?.GetComponent<BoxCollider>();
            BoxCollider sideA = GameObject.Find(goalName + " Net Side A Collision")?.GetComponent<BoxCollider>();
            BoxCollider sideB = GameObject.Find(goalName + " Net Side B Collision")?.GetComponent<BoxCollider>();
            BoxCollider roof = GameObject.Find(goalName + " Net Roof Collision")?.GetComponent<BoxCollider>();
            return back != null && back.enabled && back.size.x >= 3f && back.size.y >= 1.25f
                && sideA != null && sideA.enabled && sideA.size.z >= PrototypeRinkGeometry.GoalDepth
                && sideB != null && sideB.enabled && sideB.size.z >= PrototypeRinkGeometry.GoalDepth
                && roof != null && roof.enabled && roof.size.x >= 3f
                && sideA.bounds.max.x <= -1.5f + 0.001f
                && sideB.bounds.min.x >= 1.5f - 0.001f
                && roof.bounds.min.y >= 1.25f - 0.001f;
        }

        private readonly struct AirborneShotResult
        {
            public readonly bool Released;
            public readonly bool CrossedGoalLine;
            public readonly bool HighShot;
            public readonly float LaunchSpeed;
            public readonly float Rise;
            public readonly float GoalHeight;

            public AirborneShotResult(bool released, bool crossedGoalLine, bool highShot,
                float launchSpeed, float rise, float goalHeight)
            {
                Released = released;
                CrossedGoalLine = crossedGoalLine;
                HighShot = highShot;
                LaunchSpeed = launchSpeed;
                Rise = rise;
                GoalHeight = goalHeight;
            }
        }

        private static bool VerifyRuntimeAirborneShots(PlayerController player, PuckController puck)
        {
            Vector3 previousPlayerPosition = player.transform.position;
            Quaternion previousPlayerRotation = player.transform.rotation;
            float attackSign = player.Team == TeamId.Blue ? 1f : -1f;
            float goalZ = attackSign * PrototypeRinkGeometry.GoalLineDistance;
            Vector3 shotPosition = new(0f, previousPlayerPosition.y, goalZ - attackSign * 20f);
            Quaternion shotRotation = Quaternion.LookRotation(Vector3.forward * attackSign, Vector3.up);
            Scene trajectoryScene = SceneManager.CreateScene("Airborne Shot Validation",
                new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            PhysicsScene trajectoryPhysics = trajectoryScene.GetPhysicsScene();
            try
            {
                player.Movement.ResetMotion(shotPosition, shotRotation);
                AirborneShotResult tapA = SimulateAirborneShot(
                    player, puck, 0f, goalZ, trajectoryScene, trajectoryPhysics);
                AirborneShotResult tapB = SimulateAirborneShot(
                    player, puck, 0f, goalZ, trajectoryScene, trajectoryPhysics);
                AirborneShotResult chargedA = SimulateAirborneShot(
                    player, puck, 1f, goalZ, trajectoryScene, trajectoryPhysics);
                AirborneShotResult chargedB = SimulateAirborneShot(
                    player, puck, 1f, goalZ, trajectoryScene, trajectoryPhysics);

                return ValidateAirbornePair(tapA, tapB)
                    && ValidateAirbornePair(chargedA, chargedB)
                    && Mathf.Min(chargedA.LaunchSpeed, chargedB.LaunchSpeed)
                        > Mathf.Max(tapA.LaunchSpeed, tapB.LaunchSpeed) * 1.9f;
            }
            finally
            {
                player.Movement.ResetMotion(previousPlayerPosition, previousPlayerRotation);
                SceneManager.UnloadSceneAsync(trajectoryScene);
            }
        }

        private static AirborneShotResult SimulateAirborneShot(PlayerController player,
            PuckController puck, float charge, float goalZ, Scene trajectoryScene, PhysicsScene trajectoryPhysics)
        {
            StageShotPuckOnIce(puck, player);
            bool claimed = puck.TryClaim(player, player.Stick);
            Vector3 start = puck.Body.position;
            bool highShot = ShootController.EvaluateHighShotVariant(player.PlayerId, puck.ImpulseReleaseSequence);
            bool released = claimed && player.Shoot.ReleaseForValidation(charge);
            Vector3 launchVelocity = puck.Body.linearVelocity;
            float launchSpeed = launchVelocity.magnitude;
            Vector3 horizontalDirection = Vector3.ProjectOnPlane(launchVelocity, Vector3.up).normalized;
            float goalDistance = Mathf.Abs(goalZ - start.z) / Mathf.Max(Mathf.Abs(horizontalDirection.z), 0.01f);

            GameObject simulatedPuck = new("Damped Shot Body");
            SceneManager.MoveGameObjectToScene(simulatedPuck, trajectoryScene);
            Rigidbody simulatedBody = simulatedPuck.AddComponent<Rigidbody>();
            simulatedBody.mass = puck.Body.mass;
            simulatedBody.useGravity = true;
            simulatedBody.linearDamping = puck.Body.linearDamping;
            simulatedBody.linearVelocity = launchVelocity;
            Vector3 previous = simulatedBody.position;
            float maximumHeight = 0f;
            bool rose = false;
            for (int step = 0; released && step < 250; step++)
            {
                trajectoryPhysics.Simulate(Time.fixedDeltaTime);
                Vector3 current = simulatedBody.position;
                maximumHeight = Mathf.Max(maximumHeight, current.y);
                rose |= current.y > 0.01f;
                float previousDistance = Vector3.Dot(previous, horizontalDirection);
                float currentDistance = Vector3.Dot(current, horizontalDirection);
                if (previousDistance < goalDistance && currentDistance >= goalDistance)
                {
                    float crossing = Mathf.InverseLerp(previousDistance, currentDistance, goalDistance);
                    float goalHeight = start.y + Mathf.Lerp(previous.y, current.y, crossing);
                    Object.DestroyImmediate(simulatedPuck);
                    return new AirborneShotResult(true, true, highShot, launchSpeed,
                        maximumHeight, goalHeight);
                }
                if (rose && start.y + current.y <= PrototypeRinkGeometry.PuckY) break;
                previous = current;
            }
            float finalHeight = start.y + simulatedBody.position.y;
            Object.DestroyImmediate(simulatedPuck);
            return new AirborneShotResult(released, false, highShot, launchSpeed,
                maximumHeight, finalHeight);
        }

        private static bool ValidateAirbornePair(AirborneShotResult first, AirborneShotResult second)
        {
            AirborneShotResult low = first.HighShot ? second : first;
            AirborneShotResult high = first.HighShot ? first : second;
            return first.HighShot != second.HighShot
                && low.Released && high.Released && low.CrossedGoalLine && high.CrossedGoalLine
                && low.Rise >= 0.12f && high.Rise > low.Rise
                && low.GoalHeight >= 0.65f && low.GoalHeight <= 0.9f
                && high.GoalHeight > low.GoalHeight && high.GoalHeight <= 1.15f;
        }

        private static bool VerifyBackSideNetImpact(string goalName, GoalTrigger goal,
            PuckController puck, MatchController match)
        {
            BoxCollider back = GameObject.Find(goalName + " Net Back Collision")?.GetComponent<BoxCollider>();
            if (back == null || back.isTrigger) return false;

            Vector3 backCenter = back.transform.TransformPoint(back.center);
            Vector3 outsidePosition = backCenter + goal.ScoringDirection * 0.7f;
            outsidePosition.y = PrototypeRinkGeometry.PuckY;
            int blueBefore = match.BlueScore;
            int redBefore = match.RedScore;
            puck.ResetPuck(outsidePosition);
            puck.transform.position = outsidePosition;
            Physics.SyncTransforms();
            SimulationMode previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            try
            {
                puck.Body.linearVelocity = -goal.ScoringDirection * 12f;
                for (int step = 0; step < 4; step++) Physics.Simulate(Time.fixedDeltaTime);
            }
            finally
            {
                Physics.simulationMode = previousMode;
            }

            float outsideDistance = Vector3.Dot(puck.Body.position - backCenter, goal.ScoringDirection);
            bool stoppedOutside = outsideDistance > 0.1f
                && Vector3.Dot(puck.Body.linearVelocity, -goal.ScoringDirection) <= 0.05f
                && match.BlueScore == blueBefore && match.RedScore == redBefore
                && match.State == MatchStateSnapshot.Playing;
            Vector3 centerPosition = new(0f, PrototypeRinkGeometry.PuckY, 0f);
            puck.ResetPuck(centerPosition);
            puck.transform.position = centerPosition;
            return stoppedOutside;
        }

        private static int CountTeam(PlayerController[] players, TeamId team)
        {
            int count = 0;
            for (int i = 0; i < players.Length; i++) if (players[i].Team == team) count++;
            return count;
        }

        private static PlayerController ClosestTeamPlayer(PlayerController[] players, TeamId team, Vector3 position)
        {
            PlayerController closest = null;
            float closestDistanceSquared = float.PositiveInfinity;
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].Team != team) continue;
                float distanceSquared = (players[i].transform.position - position).sqrMagnitude;
                if (distanceSquared >= closestDistanceSquared) continue;
                closest = players[i];
                closestDistanceSquared = distanceSquared;
            }
            return closest;
        }

        private static float ButtonArea(MobileActionButton button)
        {
            RectTransform rect = button != null ? button.GetComponent<RectTransform>() : null;
            return rect != null ? rect.rect.width * rect.rect.height : 0f;
        }

        private static float VisualArea(MobileActionButton button)
        {
            RectTransform visual = button != null ? button.transform.Find("Visual") as RectTransform : null;
            return visual != null ? visual.rect.width * visual.rect.height : 0f;
        }

        private static bool VerifyJoystickVisuals(Transform joystickArea)
        {
            Image hitArea = joystickArea != null ? joystickArea.GetComponent<Image>() : null;
            Image background = joystickArea != null
                ? joystickArea.Find("JoystickBackground")?.GetComponent<Image>() : null;
            Image handle = joystickArea != null
                ? joystickArea.Find("JoystickBackground/JoystickHandle")?.GetComponent<Image>() : null;
            return hitArea != null && hitArea.raycastTarget && hitArea.color.a == 0f
                && background != null && background.sprite != null && background.preserveAspect && !background.raycastTarget
                && background.gameObject.activeSelf
                && handle != null && handle.sprite != null && handle.preserveAspect && !handle.raycastTarget;
        }

        private static bool VerifyFixedJoystick(Transform joystickArea)
        {
            RectTransform area = joystickArea as RectTransform;
            RectTransform background = joystickArea != null
                ? joystickArea.Find("JoystickBackground") as RectTransform : null;
            RectTransform handle = joystickArea != null
                ? joystickArea.Find("JoystickBackground/JoystickHandle") as RectTransform : null;
            if (area == null || background == null || handle == null) return false;

            Rect areaBounds = OffsetRect(area.rect, area.anchoredPosition);
            return area.anchorMin == Vector2.zero && area.anchorMax == Vector2.zero
                && Mathf.Abs(area.rect.width - area.rect.height) < 0.01f
                && area.rect.width >= 432f
                && area.rect.width > background.rect.width
                && background.rect.width >= 312f && background.rect.height >= 312f
                && handle.rect.width >= 132f && handle.rect.height >= 132f
                && areaBounds.xMin >= 0f && areaBounds.yMin >= 0f
                && background.anchorMin == new Vector2(0.5f, 0.5f)
                && background.anchorMax == new Vector2(0.5f, 0.5f)
                && background.anchoredPosition == Vector2.zero && background.gameObject.activeSelf
                && handle.anchoredPosition == Vector2.zero;
        }

        private static bool VerifyActionVisual(MobileActionButton action)
        {
            if (action == null) return false;
            Image hitArea = action.GetComponent<Image>();
            Button button = action.GetComponent<Button>();
            RectTransform visual = action.transform.Find("Visual") as RectTransform;
            Image visualImage = visual != null ? visual.GetComponent<Image>() : null;
            Text label = visual != null ? visual.Find("Label")?.GetComponent<Text>() : null;
            RectTransform hitRect = action.GetComponent<RectTransform>();
            return hitArea != null && hitArea.raycastTarget && hitArea.color.a == 0f
                && button != null && button.targetGraphic == visualImage
                && visualImage != null && visualImage.sprite != null && visualImage.preserveAspect
                && !visualImage.raycastTarget && label != null && !label.raycastTarget
                && visual.rect.width >= 240f && visual.rect.height >= 240f && label.fontSize >= 48
                && hitRect.rect.width >= visual.rect.width && hitRect.rect.height >= visual.rect.height;
        }

        private static bool RectTransformContains(RectTransform container, RectTransform child)
        {
            if (container == null || child == null) return false;
            Rect containerBounds = WorldRect(container);
            Rect childBounds = WorldRect(child);
            return containerBounds.Contains(childBounds.min) && containerBounds.Contains(childBounds.max);
        }

        private static bool WorldRectsOverlap(RectTransform first, RectTransform second)
        {
            return first == null || second == null || WorldRect(first).Overlaps(WorldRect(second));
        }

        private static Rect WorldRect(RectTransform rectTransform)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static bool RectTransformsOverlap(MobileActionButton first, MobileActionButton second)
        {
            if (first == null || second == null) return true;
            RectTransform firstRect = first.GetComponent<RectTransform>();
            RectTransform secondRect = second.GetComponent<RectTransform>();
            Rect firstBounds = OffsetRect(firstRect.rect, firstRect.anchoredPosition);
            Rect secondBounds = OffsetRect(secondRect.rect, secondRect.anchoredPosition);
            return firstBounds.Overlaps(secondBounds);
        }

        private static Rect OffsetRect(Rect rect, Vector2 offset)
        {
            return new Rect(rect.position + offset, rect.size);
        }

        private static bool VerifyIndependentPointers(VirtualJoystick joystick, MobileActionButton action)
        {
            if (joystick == null || action == null || EventSystem.current == null) return false;
            RectTransform background = joystick.transform.Find("JoystickBackground") as RectTransform;
            RectTransform handle = joystick.transform.Find("JoystickBackground/JoystickHandle") as RectTransform;
            if (background == null || handle == null) return false;
            Vector2 fixedOrigin = background.anchoredPosition;
            PointerEventData joystickPointer = new(EventSystem.current)
            {
                pointerId = 11,
                button = PointerEventData.InputButton.Left,
                position = new Vector2(Screen.width * 0.15f, Screen.height * 0.15f)
            };
            joystick.OnPointerDown(joystickPointer);
            joystickPointer.position += Vector2.right * Mathf.Max(Screen.width, 500f);
            joystick.OnDrag(joystickPointer);

            PointerEventData actionPointer = new(EventSystem.current)
            {
                pointerId = 22,
                button = PointerEventData.InputButton.Left
            };
            action.OnPointerDown(actionPointer);
            bool simultaneous = joystick.ActivePointerId == 11 && action.ActivePointerId == 22
                && joystick.Direction.magnitude <= 1f && joystick.Direction.sqrMagnitude > 0f
                && background.anchoredPosition == fixedOrigin && background.gameObject.activeSelf;

            PointerEventData unrelatedPointer = new(EventSystem.current)
            {
                pointerId = 33,
                button = PointerEventData.InputButton.Left
            };
            joystick.OnPointerUp(unrelatedPointer);
            bool retained = joystick.ActivePointerId == 11;
            action.OnPointerUp(actionPointer);
            joystick.OnPointerUp(joystickPointer);
            bool resetAtFixedOrigin = joystick.Direction == Vector2.zero
                && handle.anchoredPosition == Vector2.zero
                && background.anchoredPosition == fixedOrigin && background.gameObject.activeSelf;
            return simultaneous && retained && resetAtFixedOrigin;
        }

        private static PlayerController FindPlayer(PlayerController[] players, string playerId)
        {
            for (int i = 0; i < players.Length; i++) if (players[i].PlayerId == playerId) return players[i];
            throw new System.InvalidOperationException($"Smoke check could not find player '{playerId}'.");
        }

        private static Dictionary<string, SkaterRole> CaptureRoleMap(PlayerController[] players)
        {
            Dictionary<string, SkaterRole> roles = new();
            for (int i = 0; i < players.Length; i++) roles.Add(players[i].PlayerId, players[i].Role);
            return roles;
        }

        private static bool VerifyRoleDistribution(PlayerController[] players, TeamId team)
        {
            int center = 0, leftWing = 0, rightWing = 0, leftDefense = 0, rightDefense = 0;
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].Team != team) continue;
                switch (players[i].Role)
                {
                    case SkaterRole.Center: center++; break;
                    case SkaterRole.LeftWing: leftWing++; break;
                    case SkaterRole.RightWing: rightWing++; break;
                    case SkaterRole.LeftDefense: leftDefense++; break;
                    case SkaterRole.RightDefense: rightDefense++; break;
                }
            }
            return center == 1 && leftWing == 1 && rightWing == 1
                && leftDefense == 1 && rightDefense == 1;
        }

        private static bool RolesMatchActorsAndSnapshots(
            IReadOnlyDictionary<string, SkaterRole> expected, PlayerController[] players, MatchData data)
        {
            if (expected.Count != players.Length || data == null) return false;
            for (int i = 0; i < players.Length; i++)
                if (!expected.TryGetValue(players[i].PlayerId, out SkaterRole role) || players[i].Role != role)
                    return false;
            return SnapshotRolesMatch(expected, data.BlueTeam.Players)
                && SnapshotRolesMatch(expected, data.RedTeam.Players);
        }

        private static bool SnapshotRolesMatch(
            IReadOnlyDictionary<string, SkaterRole> expected, IReadOnlyList<PlayerData> snapshots)
        {
            for (int i = 0; i < snapshots.Count; i++)
                if (!expected.TryGetValue(snapshots[i].PlayerId, out SkaterRole role) || snapshots[i].Role != role)
                    return false;
            return snapshots.Count == 5;
        }

        private static bool VerifyFaceoffFormation(
            PlayerController[] players, HockeyGoalieAI[] goalies, PuckController puck)
        {
            const float positionTolerance = 0.02f;
            const float minimumSkaterClearance = 1.25f;
            const float minimumPuckClearance = 0.75f;
            float halfWidth = PrototypeRinkGeometry.Width * 0.5f;
            float halfLength = PrototypeRinkGeometry.Length * 0.5f;
            if (players.Length != 10 || goalies.Length != 2 || puck == null
                || Vector3.Distance(puck.Body.position, new Vector3(0f, 0.55f, 0f)) > positionTolerance)
                return false;

            for (int i = 0; i < players.Length; i++)
            {
                PlayerController player = players[i];
                Vector3 expected = AIFormationController.Home(player.Team, player.Role);
                Vector3 position = player.transform.position;
                float attack = player.Team == TeamId.Blue ? 1f : -1f;
                float lateral = position.x * attack;
                if (Vector3.Distance(position, expected) > positionTolerance
                    || Vector3.Dot(player.transform.forward, Vector3.forward * attack) < 0.999f
                    || Mathf.Abs(position.x) > halfWidth - 0.5f
                    || Mathf.Abs(position.z) > halfLength - 0.5f
                    || Vector3.ProjectOnPlane(position - puck.Body.position, Vector3.up).magnitude < minimumPuckClearance)
                    return false;
                if ((player.Role == SkaterRole.LeftWing || player.Role == SkaterRole.LeftDefense) && lateral >= 0f)
                    return false;
                if ((player.Role == SkaterRole.RightWing || player.Role == SkaterRole.RightDefense) && lateral <= 0f)
                    return false;
                if ((player.Role == SkaterRole.LeftWing || player.Role == SkaterRole.RightWing)
                    && Vector3.ProjectOnPlane(position, Vector3.up).magnitude <= PrototypeRinkGeometry.CenterFaceoffCircleRadius)
                    return false;

                for (int goalieIndex = 0; goalieIndex < goalies.Length; goalieIndex++)
                    if (Vector3.ProjectOnPlane(position - goalies[goalieIndex].Anchor, Vector3.up).magnitude < 2f)
                        return false;
                for (int other = i + 1; other < players.Length; other++)
                    if (Vector3.ProjectOnPlane(position - players[other].transform.position, Vector3.up).magnitude < minimumSkaterClearance)
                        return false;
            }

            foreach (SkaterRole role in System.Enum.GetValues(typeof(SkaterRole)))
            {
                PlayerController blue = FindRole(players, TeamId.Blue, role);
                PlayerController red = FindRole(players, TeamId.Red, role);
                Vector3 mirrored = blue.transform.position + red.transform.position;
                if (Mathf.Abs(mirrored.x) > positionTolerance || Mathf.Abs(mirrored.z) > positionTolerance)
                    return false;
            }

            if (!VerifyTeamFaceoffDepth(players, TeamId.Blue) || !VerifyTeamFaceoffDepth(players, TeamId.Red))
                return false;
            for (int i = 0; i < goalies.Length; i++)
                if (Vector3.Distance(goalies[i].transform.position, goalies[i].Anchor) > positionTolerance)
                    return false;
            return true;
        }

        private static bool VerifyTeamFaceoffDepth(PlayerController[] players, TeamId team)
        {
            float attack = team == TeamId.Blue ? 1f : -1f;
            PlayerController center = FindRole(players, team, SkaterRole.Center);
            PlayerController leftWing = FindRole(players, team, SkaterRole.LeftWing);
            PlayerController rightWing = FindRole(players, team, SkaterRole.RightWing);
            PlayerController leftDefense = FindRole(players, team, SkaterRole.LeftDefense);
            PlayerController rightDefense = FindRole(players, team, SkaterRole.RightDefense);
            float centerDistance = Vector3.ProjectOnPlane(center.transform.position, Vector3.up).magnitude;
            float shallowestForward = Mathf.Min(center.transform.position.z * attack,
                Mathf.Min(leftWing.transform.position.z * attack, rightWing.transform.position.z * attack));
            float deepestDefense = Mathf.Max(leftDefense.transform.position.z * attack, rightDefense.transform.position.z * attack);
            return centerDistance < Vector3.ProjectOnPlane(leftWing.transform.position, Vector3.up).magnitude
                && centerDistance < Vector3.ProjectOnPlane(rightWing.transform.position, Vector3.up).magnitude
                && deepestDefense < shallowestForward;
        }

        private static bool VerifySpacedRoleTargets()
        {
            Vector3 attackingCarrier = new(1f, 1f, 16f);
            Vector3 blueWingSupport = AIFormationController.Support(
                TeamId.Blue, SkaterRole.LeftWing, attackingCarrier);
            Vector3 blueDefenseSupport = AIFormationController.Support(
                TeamId.Blue, SkaterRole.LeftDefense, attackingCarrier);
            Vector3 redDefenseSupport = AIFormationController.Support(
                TeamId.Red, SkaterRole.LeftDefense, -attackingCarrier);
            Vector3 ownZoneCarrier = new(-1f, 1f, -16f);
            Vector3 blueWingOwnZoneSupport = AIFormationController.Support(
                TeamId.Blue, SkaterRole.LeftWing, ownZoneCarrier);

            Vector3 ownZoneThreat = new(0f, 1f, -12f);
            Vector3 blueWingDefense = AIFormationController.Defend(
                TeamId.Blue, SkaterRole.LeftWing, ownZoneThreat);
            Vector3 blueCenterDefense = AIFormationController.Defend(
                TeamId.Blue, SkaterRole.Center, ownZoneThreat);
            Vector3 blueDefenseDefense = AIFormationController.Defend(
                TeamId.Blue, SkaterRole.LeftDefense, ownZoneThreat);
            Vector3 redWingDefense = AIFormationController.Defend(
                TeamId.Red, SkaterRole.LeftWing, -ownZoneThreat);

            return blueDefenseSupport.z > 0f
                && blueWingSupport.z > blueDefenseSupport.z + 5f
                && blueWingOwnZoneSupport.z < 0f
                && Mathf.Abs(blueDefenseSupport.z + redDefenseSupport.z) < 0.01f
                && blueWingDefense.z < 0f
                && blueWingDefense.z > blueCenterDefense.z
                && blueCenterDefense.z > blueDefenseDefense.z
                && Mathf.Abs(blueWingDefense.z + redWingDefense.z) < 0.01f;
        }

        private static PlayerController FindRole(PlayerController[] players, TeamId team, SkaterRole role)
        {
            for (int i = 0; i < players.Length; i++)
                if (players[i].Team == team && players[i].Role == role) return players[i];
            throw new System.InvalidOperationException($"Smoke check could not find {team} {role}.");
        }

        private static GoalTrigger FindGoal(TeamId scoringTeam)
        {
            GoalTrigger[] goals = Object.FindObjectsByType<GoalTrigger>();
            for (int i = 0; i < goals.Length; i++) if (goals[i].ScoringTeam == scoringTeam) return goals[i];
            throw new System.InvalidOperationException($"Smoke check could not find the goal scored by '{scoringTeam}'.");
        }

        private static void StagePuckAtStick(PuckController puck, PlayerController player)
        {
            Vector3 position = player.Stick.ControlPoint;
            puck.ResetPuck(position);
            puck.transform.position = position;
            puck.Body.position = position;
            Physics.SyncTransforms();
        }

        private static void StageShotPuckOnIce(PuckController puck, PlayerController player)
        {
            Vector3 position = player.Stick.ControlPoint;
            position.y = PrototypeRinkGeometry.PuckY;
            puck.ResetPuck(position);
            puck.transform.position = position;
            puck.Body.position = position;
            Physics.SyncTransforms();
        }
    }
}
