/*
 * IceClash Phase 1 faceoff timer.
 * Provides the lightweight reset countdown used at match start, after goals, and
 * after offside stoppages, without attempting advanced faceoff mechanics.
 */

using UnityEngine;

namespace IceClash.Match
{
    public sealed class FaceoffController : MonoBehaviour
    {
        [SerializeField] private float faceoffDelay = 1.25f;
        private float completesAt;
        public bool IsRunning { get; private set; }
        public void Begin() { IsRunning = true; completesAt = Time.time + faceoffDelay; }
        public bool TickComplete()
        {
            if (!IsRunning || Time.time < completesAt) return false;
            IsRunning = false;
            return true;
        }

#if UNITY_EDITOR
        internal void CompleteDelayForValidation() => completesAt = Time.time;
#endif
    }
}
