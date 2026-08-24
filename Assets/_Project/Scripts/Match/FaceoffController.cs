/*
 * IceClash Phase 1 faceoff timer.
 * Provides the lightweight reset countdown used at match start and after goals,
 * without attempting advanced faceoff mechanics.
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
    }
}
