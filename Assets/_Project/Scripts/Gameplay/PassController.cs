/*
 * IceClash recommended tap PASS controller.
 * Continuously previews one tactical teammate with pooled dotted-path feedback
 * during human possession, then releases an imperfect non-homing physics pass.
 * Recent change: launch pace now scales through configurable short, medium, and
 * long distance bands before local intended-receiver capture.
 */

using IceClash.Player;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.Gameplay
{
    [RequireComponent(typeof(PassTargetSelector))]
    public sealed class PassController : MonoBehaviour
    {
        private const int PathDotCount = 9;

        [Header("Distance-scaled pass pace")]
        [SerializeField, Min(0f)] private float shortPassDistance = 4f;
        [SerializeField, Min(0f)] private float shortPassSpeed = 12f;
        [SerializeField, Min(0f)] private float mediumPassDistance = 10f;
        [SerializeField, Min(0f)] private float mediumPassSpeed = 17f;
        [SerializeField, Min(0f)] private float longPassDistance = 17f;
        [SerializeField, Min(0f)] private float longPassSpeed = 24f;

        [Header("Pass behavior")]
        [SerializeField] private float cooldown = 0.28f;
        [SerializeField] private float leadSeconds = 0.45f;
        [SerializeField, Min(0f)] private float receptionGraceSeconds = 0.55f;
        [SerializeField, Range(0f, 12f)] private float errorDegrees = 1f;
        [SerializeField, Range(0.03f, 0.3f)] private float recommendationRefreshInterval = 0.1f;
        [SerializeField] private float pathDotSize = 0.11f;

        private readonly Renderer[] pathDots = new Renderer[PathDotCount];
        private PlayerController player;
        private PuckController puck;
        private PassTargetSelector selector;
        private PassTargetSelection recommendation;
        private float nextPassTime;
        private float nextRecommendationTime;
        private GameObject feedbackRoot;
        private GameObject targetMarker;
        private Material feedbackMaterial;

        public PlayerController RecommendedTarget => recommendation.SelectedTeammate;
        public bool FeedbackVisible => feedbackRoot != null && feedbackRoot.activeSelf;
        public int VisiblePathDotCount => FeedbackVisible ? pathDots.Length : 0;
        internal float ShortPassDistance => shortPassDistance;
        internal float MediumPassDistance => mediumPassDistance;
        internal float LongPassDistance => longPassDistance;
        internal float ShortPassSpeed => shortPassSpeed;
        internal float MediumPassSpeed => mediumPassSpeed;
        internal float LongPassSpeed => longPassSpeed;
        internal float LeadSeconds => leadSeconds;
        internal float MaximumErrorDegrees => errorDegrees;

        private void OnValidate()
        {
            shortPassDistance = Mathf.Max(0f, shortPassDistance);
            mediumPassDistance = Mathf.Max(shortPassDistance + 0.01f, mediumPassDistance);
            longPassDistance = Mathf.Max(mediumPassDistance + 0.01f, longPassDistance);
            shortPassSpeed = Mathf.Max(0f, shortPassSpeed);
            mediumPassSpeed = Mathf.Max(shortPassSpeed, mediumPassSpeed);
            longPassSpeed = Mathf.Max(mediumPassSpeed, longPassSpeed);
        }

        public void Configure(PlayerController owner, PuckController controlledPuck)
        {
            player = owner;
            puck = controlledPuck;
            selector = GetComponent<PassTargetSelector>();
        }

        public bool Tick(bool passPressed, bool showRecommendation, float quality = 1f)
        {
            if (player == null || puck == null || !puck.IsCarriedBy(player))
            {
                ClearRecommendation();
                return false;
            }

            if (Time.time >= nextRecommendationTime || !recommendation.IsValid)
            {
                recommendation = selector != null ? selector.Select(player) : default;
                nextRecommendationTime = Time.time + recommendationRefreshInterval;
            }

            if (showRecommendation && recommendation.IsValid) ShowRecommendation(recommendation.SelectedTeammate);
            else HideFeedback();

            if (!passPressed || Time.time < nextPassTime || !recommendation.IsValid) return false;
            bool released = Execute(recommendation, quality);
            if (released) ClearRecommendation();
            return released;
        }

        public void Cancel() => ClearRecommendation();

        private bool Execute(PassTargetSelection selection, float quality)
        {
            PlayerController target = selection.SelectedTeammate;
            if (target == null) return false;
            Vector3 targetVelocity = target.Movement != null ? target.Movement.Velocity : Vector3.zero;
            float spreadScale = Mathf.Lerp(1.5f, 0.75f, Mathf.Clamp01(quality));
            float spread = Random.Range(-errorDegrees, errorDegrees) * spreadScale;
            return ReleaseToward(target, targetVelocity, quality, spread);
        }

        internal bool ReleaseForValidation(PlayerController target, Vector3 targetVelocity, float quality,
            float normalizedError)
        {
            float spreadScale = Mathf.Lerp(1.5f, 0.75f, Mathf.Clamp01(quality));
            float spread = Mathf.Clamp(normalizedError, -1f, 1f) * errorDegrees * spreadScale;
            return ReleaseToward(target, targetVelocity, quality, spread);
        }

        private bool ReleaseToward(PlayerController target, Vector3 targetVelocity, float quality, float spread)
        {
            if (target == null || target.Stick == null || target.PassReception == null) return false;
            Vector3 lead = targetVelocity * leadSeconds;
            Vector3 targetPoint = target.Stick.ControlPoint + lead;
            Vector3 passDelta = Vector3.ProjectOnPlane(targetPoint - puck.Body.position, Vector3.up);
            float passDistance = passDelta.magnitude;
            if (passDistance < 0.01f) return false;
            Vector3 direction = passDelta / passDistance;
            direction = Quaternion.Euler(0f, spread, 0f) * direction;
            float launchSpeed = CalculatePassSpeed(passDistance) * Mathf.Lerp(0.88f, 1f, Mathf.Clamp01(quality));
            float receptionEligibilitySeconds = passDistance / Mathf.Max(launchSpeed, 0.01f) + receptionGraceSeconds;
            if (!puck.ReleasePass(player, target, direction, launchSpeed, receptionEligibilitySeconds)) return false;
            nextPassTime = Time.time + cooldown;
            return true;
        }

        internal float CalculatePassSpeed(float distance)
        {
            float safeShortDistance = Mathf.Max(0f, shortPassDistance);
            float safeMediumDistance = Mathf.Max(safeShortDistance + 0.01f, mediumPassDistance);
            float safeLongDistance = Mathf.Max(safeMediumDistance + 0.01f, longPassDistance);
            float safeShortSpeed = Mathf.Max(0f, shortPassSpeed);
            float safeMediumSpeed = Mathf.Max(safeShortSpeed, mediumPassSpeed);
            float safeLongSpeed = Mathf.Max(safeMediumSpeed, longPassSpeed);
            if (distance <= safeShortDistance) return safeShortSpeed;
            if (distance <= safeMediumDistance)
                return Mathf.Lerp(safeShortSpeed, safeMediumSpeed,
                    Mathf.InverseLerp(safeShortDistance, safeMediumDistance, distance));
            return Mathf.Lerp(safeMediumSpeed, safeLongSpeed,
                Mathf.InverseLerp(safeMediumDistance, safeLongDistance, distance));
        }

        private void ShowRecommendation(PlayerController target)
        {
            EnsureFeedback();
            feedbackRoot.SetActive(true);
            Vector3 start = puck.transform.position + Vector3.up * 0.08f;
            Vector3 lead = target.Movement != null ? target.Movement.Velocity * leadSeconds : Vector3.zero;
            Vector3 end = target.Stick.ControlPoint + lead + Vector3.up * 0.08f;
            for (int i = 0; i < pathDots.Length; i++)
            {
                float t = (i + 1f) / (pathDots.Length + 1f);
                pathDots[i].transform.position = Vector3.Lerp(start, end, t);
            }
            targetMarker.transform.position = target.transform.position + Vector3.up * 1.55f;
        }

        private void EnsureFeedback()
        {
            if (feedbackRoot != null) return;
            feedbackRoot = new GameObject($"{name} Recommended Pass Feedback");
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                feedbackMaterial = new Material(shader);
                feedbackMaterial.color = new Color(0.25f, 0.9f, 1f, 0.48f);
            }

            for (int i = 0; i < pathDots.Length; i++)
            {
                GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dot.name = $"Pass Path Dot {i + 1}";
                dot.transform.SetParent(feedbackRoot.transform);
                dot.transform.localScale = Vector3.one * pathDotSize;
                Destroy(dot.GetComponent<Collider>());
                pathDots[i] = dot.GetComponent<Renderer>();
                ApplyFeedbackMaterial(pathDots[i]);
            }

            targetMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            targetMarker.name = "Recommended Pass Target";
            targetMarker.transform.SetParent(feedbackRoot.transform);
            targetMarker.transform.localScale = new Vector3(0.42f, 0.025f, 0.42f);
            Destroy(targetMarker.GetComponent<Collider>());
            ApplyFeedbackMaterial(targetMarker.GetComponent<Renderer>());
        }

        private void ApplyFeedbackMaterial(Renderer renderer)
        {
            if (feedbackMaterial != null) renderer.sharedMaterial = feedbackMaterial;
            else renderer.material.color = new Color(0.25f, 0.9f, 1f, 0.48f);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void HideFeedback()
        {
            if (feedbackRoot != null) feedbackRoot.SetActive(false);
        }

        private void ClearRecommendation()
        {
            recommendation = default;
            nextRecommendationTime = 0f;
            HideFeedback();
        }

        private void OnDestroy()
        {
            if (feedbackRoot != null) Destroy(feedbackRoot);
            if (feedbackMaterial != null) Destroy(feedbackMaterial);
        }
    }
}
