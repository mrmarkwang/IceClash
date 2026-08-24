/*
 * IceClash recommended tap PASS controller.
 * Continuously previews one tactical teammate with pooled dotted-path feedback
 * during human possession, then releases an imperfect non-homing physics pass.
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

        [SerializeField] private float passSpeed = 12.5f;
        [SerializeField] private float cooldown = 0.28f;
        [SerializeField] private float leadSeconds = 0.22f;
        [SerializeField, Range(0f, 12f)] private float errorDegrees = 3.5f;
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
            Vector3 lead = target.Movement != null ? target.Movement.Velocity * leadSeconds : Vector3.zero;
            Vector3 direction = Vector3.ProjectOnPlane(target.Stick.ControlPoint + lead - puck.transform.position, Vector3.up).normalized;
            float spread = Random.Range(-errorDegrees, errorDegrees) * Mathf.Lerp(1.5f, 0.75f, Mathf.Clamp01(quality));
            direction = Quaternion.Euler(0f, spread, 0f) * direction;
            if (!puck.Release(player, direction, passSpeed * Mathf.Lerp(0.88f, 1f, quality))) return false;
            nextPassTime = Time.time + cooldown;
            return true;
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
