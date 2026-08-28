/*
 * IceClash Phase 1 local PvE smoke assertions.
 * Verifies the generated 3v3-plus-goalies slice, one-human routing, possession-only
 * automatic control, recommended tap passing with dotted feedback, manual switching,
 * smooth camera retargeting, modular systems, safe multi-touch Unity UI controls,
 * circular visual/hit separation, responsive puck possession, forceful charged
 * shots, snapshots, and one-way goals. Recent changes: validates the elongated rink,
 * compact hockey nets, physics-driven line crossing, distinct actor scales, and
 * larger non-overlapping action UI, enlarged fixed joystick geometry, and full
 * visible-control containment inside the safe-area layout. Passing checks enforce
 * reliable high-speed, low-error tuning plus stationary, moving, and interceptable
 * physics outcomes.
 */

using IceClash.AI;
using IceClash.CameraSystem;
using IceClash.Core;
using IceClash.Gameplay;
using IceClash.Input;
using IceClash.Match;
using IceClash.Player;
using IceClash.Puck;
using IceClash.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IceClash.Hockey
{
    public static class PrototypeArenaSmokeCheck
    {
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
            MobileActionButton[] buttons = Object.FindObjectsByType<MobileActionButton>();
            int humanRouted = 0;
            for (int i = 0; i < players.Length; i++) if (players[i].InputSource is PlayerInputController) humanRouted++;

            Canvas uiCanvas = Object.FindAnyObjectByType<Canvas>();
            Transform mobileControls = uiCanvas != null ? uiCanvas.transform.Find("MobileControls") : null;
            Transform joystickArea = mobileControls != null ? mobileControls.Find("JoystickArea") : null;
            Transform actionButtons = mobileControls != null ? mobileControls.Find("ActionButtons") : null;
            VirtualJoystick virtualJoystick = joystickArea != null ? joystickArea.GetComponent<VirtualJoystick>() : null;
            CanvasScaler scaler = uiCanvas != null ? uiCanvas.GetComponent<CanvasScaler>() : null;
            MobileActionButton passButton = FindButton(buttons, "PASS");
            MobileActionButton dekeButton = FindButton(buttons, "DEKE");
            MobileActionButton shootButton = FindButton(buttons, "SHOOT");
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

            bool roster = players.Length == 6 && skaterAi.Length == 6 && goalies.Length == 2
                && CountTeam(players, TeamId.Blue) == 3 && CountTeam(players, TeamId.Red) == 3 && humanRouted == 1;
            bool smallerSkaters = AllSkatersUseScale(players, 0.68f);
            bool broaderGoalies = AllGoaliesAreBroader(goalies, players[0]);
            bool modular = Object.FindObjectsByType<PlayerMovementController>().Length == 6
                && Object.FindObjectsByType<StickPuckInteraction>().Length == 6
                && Object.FindObjectsByType<PassController>().Length == 6
                && Object.FindObjectsByType<PassTargetSelector>().Length == 6
                && Object.FindObjectsByType<ShootController>().Length == 6
                && setup != null && setup.SwitchController != null && setup.ControlManager != null && setup.MatchController != null;
            bool presentation = Object.FindAnyObjectByType<HockeyCameraController>() != null
                && virtualJoystick != null
                && Object.FindAnyObjectByType<MatchHUD>() != null
                && controlHierarchy && controlScaling && actionLayout && refinedControlVisuals && fixedJoystick
                && analogInput && sourceSelection && pointerOwnership;
            bool arenaPresentation = VerifyArenaPresentation();
            bool puckIndependent = puck != null && puck.transform.parent == null && puck.Body != null;
            bool snapshots = setup != null && setup.Data.BlueTeam.Players.Count == 3 && setup.Data.RedTeam.Players.Count == 3
                && !string.IsNullOrEmpty(setup.Data.ControlledPlayerId);

            setup.MatchController.StartPlayImmediatelyForValidation();
            HockeyCameraController hockeyCamera = Object.FindAnyObjectByType<HockeyCameraController>();
            PlayerController passer = FindPlayer(players, "blue-1");
            PlayerController receiver = FindPlayer(players, "blue-2");
            PlayerController expectedDefender = FindPlayer(players, "blue-3");
            PlayerController opponentCarrier = FindPlayer(players, "red-1");
            Vector3 cameraBeforeAutoSwitch = hockeyCamera.transform.position;

            passer.Movement.ResetMotion(new Vector3(0f, 1f, -4f), Quaternion.identity);
            receiver.Movement.ResetMotion(new Vector3(0f, 1f, 1f), Quaternion.identity);
            expectedDefender.Movement.ResetMotion(new Vector3(4f, 1f, 0f), Quaternion.identity);
            Vector3 puckScale = puck.transform.localScale;
            Vector3 controlOffset = Vector3.ProjectOnPlane(passer.Stick.ControlPoint - passer.transform.position, Vector3.up);
            bool puckSizeAndPosition = puckScale.x <= 0.42f && puckScale.z <= 0.42f && puckScale.y <= 0.06f
                && puck.GetComponent<BoxCollider>() != null && puck.GetComponent<CapsuleCollider>() == null
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
            bool hardShotTuning = tapShotPower >= 20f && chargedShotPower >= 44f
                && chargedShotPower > tapShotPower * 2f
                && puck.Body.collisionDetectionMode == CollisionDetectionMode.ContinuousDynamic;
            bool reliablePassTuning = passer.Pass.PassSpeed >= 14.5f
                && passer.Pass.PassSpeed <= passer.Stick.MaximumClaimSpeed
                && passer.Pass.LeadSeconds >= 0.45f && passer.Pass.LeadSeconds <= 0.5f
                && passer.Pass.MaximumErrorDegrees >= 0.5f && passer.Pass.MaximumErrorDegrees <= 1f;
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
                && !passer.Pass.FeedbackVisible && passer.Pass.RecommendedTarget == null;
            bool recommendedPassFlow = passerClaimed
                && recommendationShown && movementInputIndependent && recommendedPassReleased;

            StagePuckAtStick(puck, receiver);
            bool receiverClaimed = puck.TryClaim(receiver, receiver.Stick);
            bool humanPossessionAutoControl = receiverClaimed
                && setup.SwitchController.ControlledPlayer == receiver
                && setup.ControlManager.LastAutomaticReason == AutomaticControlReason.HumanPossession
                && setup.ControlManager.AutomaticSelectionCount == 1
                && hockeyCamera.Target == receiver.transform
                && hockeyCamera.transform.position == cameraBeforeAutoSwitch;

            bool releasedPass = puck.Release(receiver, receiver.transform.forward, 5f);
            bool noTrajectorySwitch = releasedPass
                && setup.SwitchController.ControlledPlayer == receiver
                && setup.ControlManager.AutomaticSelectionCount == 1;

            opponentCarrier.Movement.ResetMotion(expectedDefender.transform.position + Vector3.forward * 1.6f, Quaternion.Euler(0f, 180f, 0f));
            StagePuckAtStick(puck, opponentCarrier);
            bool opponentClaimed = puck.TryClaim(opponentCarrier, opponentCarrier.Stick);
            bool opponentPossessionAutoDefense = opponentClaimed
                && setup.SwitchController.ControlledPlayer == expectedDefender
                && setup.ControlManager.LastAutomaticReason == AutomaticControlReason.OpponentPossession
                && setup.ControlManager.AutomaticSelectionCount == 2
                && hockeyCamera.Target == expectedDefender.transform;

            setup.SwitchController.SwitchToBest();
            bool manualOverride = setup.SwitchController.ControlledPlayer != expectedDefender
                && setup.ControlManager.AutomaticSelectionCount == 2
                && hockeyCamera.Target == setup.SwitchController.ControlledPlayer.transform;
            bool reliablePassOutcomes = VerifyPassOutcomes(passer, receiver, opponentCarrier, puck, players,
                out bool stationaryPassReceived, out bool movingPassReceived, out bool obstructedPassIntercepted);

            GoalTrigger blueScoringGoal = FindGoal(TeamId.Blue);
            GoalTrigger redScoringGoal = FindGoal(TeamId.Red);
            bool hockeySizedGoals = GoalWidth(blueScoringGoal) >= 2.75f && GoalWidth(blueScoringGoal) <= 2.85f
                && GoalWidth(redScoringGoal) >= 2.75f && GoalWidth(redScoringGoal) <= 2.85f
                && GoalPostHalfWidth("Blue Goal") >= 1.45f && GoalPostHalfWidth("Blue Goal") <= 1.55f
                && GoalPostHalfWidth("Red Goal") >= 1.45f && GoalPostHalfWidth("Red Goal") <= 1.55f;
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
            bool bothGoalDirectionsConfigured = blueScoringGoal.ScoringDirection == Vector3.forward
                && redScoringGoal.ScoringDirection == Vector3.back;

            bool frontSideGoalRegistered = SimulateGoalCrossing(blueScoringGoal, puck, setup.MatchController);
            bool firstGoalFlow = frontSideGoalRegistered
                && beforeGoalLineRejected && backSideGoalRejected && bothGoalDirectionsConfigured
                && setup.MatchController.BlueScore == 1 && setup.MatchController.RedScore == 0
                && setup.MatchController.State == MatchStateSnapshot.GoalPause
                && Vector3.Distance(puck.Body.position, new Vector3(0f, 0.55f, 0f)) < 0.05f;

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

            if (!roster || !smallerSkaters || !broaderGoalies || !modular || !presentation || !arenaPresentation || !puckIndependent || !snapshots
                || !humanPossessionAutoControl || !noTrajectorySwitch || !opponentPossessionAutoDefense
                || !manualOverride || !recommendedPassFlow || !reliablePassTuning || !reliablePassOutcomes
                || !puckSizeAndPosition || !forgivingPickup
                || !velocityMatchedControl || !hardShotTuning || !hockeySizedGoals || !goalFlow || !resultFlow)
                throw new System.InvalidOperationException($"PHASE1_PVE_SMOKE_FAIL roster={roster} smallerSkaters={smallerSkaters} broaderGoalies={broaderGoalies} modular={modular} presentation={presentation} arenaPresentation={arenaPresentation} controlHierarchy={controlHierarchy} controlScaling={controlScaling} actionLayout={actionLayout} refinedControlVisuals={refinedControlVisuals} fixedJoystick={fixedJoystick} analogInput={analogInput} sourceSelection={sourceSelection} pointerOwnership={pointerOwnership} puckIndependent={puckIndependent} snapshots={snapshots} puckSizeAndPosition={puckSizeAndPosition} forgivingPickup={forgivingPickup} velocityMatchedControl={velocityMatchedControl} hardShotTuning={hardShotTuning} reliablePassTuning={reliablePassTuning} reliablePassOutcomes={reliablePassOutcomes} stationaryPassReceived={stationaryPassReceived} movingPassReceived={movingPassReceived} obstructedPassIntercepted={obstructedPassIntercepted} tapShotPower={tapShotPower} chargedShotPower={chargedShotPower} hockeySizedGoals={hockeySizedGoals} puckScale={puckScale} controlOffset={controlOffset} recommendedPassFlow={recommendedPassFlow} recommendationShown={recommendationShown} recommendedTarget={recommendationBeforeMove?.PlayerId} movementInputIndependent={movementInputIndependent} carriedBeforePassTap={carriedBeforePassTap} tapReleased={tapReleased} releaseSequenceBefore={releasesBeforePass} recommendedPassReleased={recommendedPassReleased} humanPossessionAutoControl={humanPossessionAutoControl} noTrajectorySwitch={noTrajectorySwitch} opponentPossessionAutoDefense={opponentPossessionAutoDefense} manualOverride={manualOverride} beforeGoalLineRejected={beforeGoalLineRejected} backSideGoalRejected={backSideGoalRejected} bothGoalDirectionsConfigured={bothGoalDirectionsConfigured} frontSideGoalRegistered={frontSideGoalRegistered} oppositeFrontSideGoalRegistered={oppositeFrontSideGoalRegistered} bothDirectionsScored={bothDirectionsScored} winningGoalRegistered={winningGoalRegistered} goalFlow={goalFlow} resultFlow={resultFlow}");

            Debug.Log("PHASE1_PVE_SMOKE_PASS skaters=6 smallerSkaters=true goalies=2 broaderGoalies=true mobileArena=true elongatedRink=true layeredBoards=true dimensionalNets=true hockeySizedGoals=true alignedArenaAnchors=true closeCamera=true humanInputs=1 aiSkaters=5 controls=FIXED_JOYSTICK_PASS_DEKE_SHOOT largerActionButtons=true equalActionSizes=true unityUI=true safeArea=true referenceResolution=1920x1080 fixedJoystick=true persistentJoystick=true circularControls=true separateHitVisuals=true nonOverlappingActions=true deadZone=true analog=true independentPointers=true movementClamped=true movementOnly=true recommendedPassTarget=true dottedPassPath=true tapPass=true reliablePassTuning=true stationaryPassReceived=true movingPassReceived=true obstructedPassIntercepted=true imperfectNonHomingPass=true harderShots=true hardChargedShot=true continuousPuckCollision=true possessionAutoControl=true noTrajectorySwitch=true opponentAutoDefense=true keyboardSwitchOverride=true cameraRetargetSmooth=true puckIndependent=true smallerPuck=true frontPuckControl=true forgivingPickup=true velocityMatchedPuck=true oneWayGoals=true beforeGoalLineRejected=true bothDirectionsScored=true backSideGoalRejected=true goalReset=true timerResult=true");
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
                && kickplate != null && kickplate.GetComponent<Collider>() == null
                && rail != null && rail.GetComponent<Collider>() == null
                && glass != null && glass.GetComponent<Collider>() == null
                && glass.transform.localScale.y >= 1.2f;
            bool dimensionalNets = rearPost != null && roofNet != null && sideNet != null
                && Mathf.Abs(rearPost.transform.position.z - blueGoal.transform.position.z) >= 0.85f;
            HockeyGoalieAI blueGoalie = FindGoalie(goalies, TeamId.Blue);
            HockeyGoalieAI redGoalie = FindGoalie(goalies, TeamId.Red);
            Vector3 blueDefense = AIFormationController.Defend(TeamId.Blue, 0, 3, new Vector3(0f, 1f, -PrototypeRinkGeometry.GoalieAnchor));
            Vector3 redDefense = AIFormationController.Defend(TeamId.Red, 0, 3, new Vector3(0f, 1f, PrototypeRinkGeometry.GoalieAnchor));
            bool alignedArenaAnchors = blueTrigger != null && redTrigger != null
                && Mathf.Abs(blueTrigger.transform.position.z + PrototypeRinkGeometry.GoalLineDistance + PrototypeRinkGeometry.GoalDepth * 0.5f) < 0.01f
                && Mathf.Abs(redTrigger.transform.position.z - PrototypeRinkGeometry.GoalLineDistance - PrototypeRinkGeometry.GoalDepth * 0.5f) < 0.01f
                && Mathf.Abs(blueTrigger.GetComponent<BoxCollider>().size.z - PrototypeRinkGeometry.GoalDepth) < 0.01f
                && Mathf.Abs(redTrigger.GetComponent<BoxCollider>().size.z - PrototypeRinkGeometry.GoalDepth) < 0.01f
                && blueGoalie != null && Mathf.Abs(blueGoalie.Anchor.z + PrototypeRinkGeometry.GoalieAnchor) < 0.01f
                && redGoalie != null && Mathf.Abs(redGoalie.Anchor.z - PrototypeRinkGeometry.GoalieAnchor) < 0.01f
                && Mathf.Abs(blueDefense.z + PrototypeRinkGeometry.GoalieAnchor) < 0.01f
                && Mathf.Abs(redDefense.z - PrototypeRinkGeometry.GoalieAnchor) < 0.01f;
            bool closeCamera = view != null && view.fieldOfView <= 46.1f
                && controller.Target != null
                && view.transform.position.y - controller.Target.position.y <= 12.1f
                && Mathf.Abs(view.transform.position.z - controller.Target.position.z) <= 15.1f;
            return elongatedRink && layeredBoards && dimensionalNets && alignedArenaAnchors && closeCamera;
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

        private static bool SimulateGoalCrossing(GoalTrigger goal, PuckController puck, MatchController match)
        {
            BoxCollider volume = goal.GetComponent<BoxCollider>();
            Vector3 goalLine = goal.transform.position - goal.ScoringDirection * (volume.size.z * 0.5f);
            int blueBefore = match.BlueScore;
            int redBefore = match.RedScore;
            SimulationMode previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            try
            {
                puck.ResetPuck(goalLine - goal.ScoringDirection * 0.35f);
                puck.Body.linearVelocity = goal.ScoringDirection * 5f;
                Physics.SyncTransforms();
                for (int step = 0; step < 30 && match.State == MatchStateSnapshot.Playing; step++)
                    Physics.Simulate(Time.fixedDeltaTime);
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
            out bool stationaryReceived, out bool movingReceived, out bool obstructedIntercepted)
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
                stationaryReceived = SimulatePassOutcome(passer, receiver, null, receiver,
                    puck, Vector3.zero, 1f, 1f);
                movingReceived = SimulatePassOutcome(passer, receiver, null, receiver,
                    puck, Vector3.right * 1.5f, 1f, -1f);
                obstructedIntercepted = SimulatePassOutcome(passer, receiver, interceptor, interceptor,
                    puck, Vector3.zero, 1f, 0f);
                return stationaryReceived && movingReceived && obstructedIntercepted;
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
            Vector3 receiverVelocity, float quality, float normalizedError)
        {
            Vector3 passerPosition = new(-8f, 1f, -3f);
            Vector3 receiverPosition = new(-8f, 1f, 3f);
            passer.Movement.ResetMotion(passerPosition, Quaternion.identity);
            receiver.Movement.ResetMotion(receiverPosition, Quaternion.identity);
            if (interceptor != null)
                interceptor.Movement.ResetMotion(new Vector3(-8f, 1f, 0f), Quaternion.identity);

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
            if (!puck.TryClaim(passer, passer.Stick)
                || !passer.Pass.ReleaseForValidation(receiver, receiverVelocity, quality, normalizedError)) return false;

            const int maximumSteps = 90;
            for (int step = 0; step < maximumSteps && puck.Carrier == null; step++)
            {
                if (receiverVelocity.sqrMagnitude > 0f)
                    receiver.Movement.ResetMotion(receiver.transform.position + receiverVelocity * Time.fixedDeltaTime,
                        receiver.transform.rotation);
                Physics.SyncTransforms();
                Physics.Simulate(Time.fixedDeltaTime);
                if (interceptor != null) puck.TryClaim(interceptor, interceptor.Stick);
                if (puck.Carrier == null) puck.TryClaim(receiver, receiver.Stick);
            }

            return puck.Carrier == expectedCarrier;
        }

        private static float GoalPostHalfWidth(string goalName)
        {
            GameObject first = GameObject.Find(goalName + " Post A");
            GameObject second = GameObject.Find(goalName + " Post B");
            return first != null && second != null ? Mathf.Abs(second.transform.position.x - first.transform.position.x) * 0.5f : 0f;
        }

        private static int CountTeam(PlayerController[] players, TeamId team)
        {
            int count = 0;
            for (int i = 0; i < players.Length; i++) if (players[i].Team == team) count++;
            return count;
        }

        private static MobileActionButton FindButton(MobileActionButton[] buttons, string label)
        {
            for (int i = 0; i < buttons.Length; i++) if (buttons[i].Label == label) return buttons[i];
            return null;
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
    }
}
