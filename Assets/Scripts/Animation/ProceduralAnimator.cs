using UnityEngine;

namespace EmersynBigDay.Animation
{
    /// <summary>
    /// Enhancement #24: Procedural animations - breathing, bobbing, anticipation.
    /// Adds life to characters without needing animation clips.
    /// Like Animal Crossing's subtle idle movements and Talking Tom's breathing.
    /// </summary>
    public class ProceduralAnimator : MonoBehaviour
    {
        [Header("Breathing")]
        public float BreathRate = 0.4f; // Breaths per second
        public float BreathAmplitude = 0.02f;
        public Transform ChestBone;

        [Header("Idle Bob")]
        public float BobSpeed = 0.8f;
        public float BobAmplitude = 0.03f;

        [Header("Anticipation")]
        public float AnticipationSquash = 0.9f;
        public float AnticipationStretch = 1.1f;
        public float AnticipationSpeed = 4f;

        [Header("Head Tilt")]
        public float TiltAmount = 5f;
        public float TiltSpeed = 0.3f;
        public Transform HeadBone;

        [Header("Tail Wag (for pets)")]
        public Transform TailBone;
        public float WagSpeed = 8f;
        public float WagAmplitude = 30f;

        private Vector3 originalPosition;
        private Vector3 originalScale;
        private Vector3 originalChestScale;
        private Quaternion originalHeadRotation;
        private bool isAnticipating;
        private float anticipationTimer;
        private float breathPhase;
        private Coroutine currentBounce;

        private void Start()
        {
            originalPosition = transform.localPosition;
            originalScale = transform.localScale;
            if (ChestBone != null) originalChestScale = ChestBone.localScale;
            if (HeadBone != null) originalHeadRotation = HeadBone.localRotation;
            breathPhase = Random.Range(0f, Mathf.PI * 2f); // Offset so not all characters breathe in sync
        }

        private void LateUpdate()
        {
            AnimateBreathing();
            AnimateIdleBob();
            AnimateHeadTilt();
            AnimateTailWag();
            AnimateAnticipation();
        }

        private void AnimateBreathing()
        {
            if (ChestBone == null) return;
            breathPhase += Time.deltaTime * BreathRate * Mathf.PI * 2f;
            float breathScale = 1f + Mathf.Sin(breathPhase) * BreathAmplitude;
            ChestBone.localScale = new Vector3(
                originalChestScale.x * breathScale,
                originalChestScale.y * (1f + Mathf.Sin(breathPhase) * BreathAmplitude * 1.5f),
                originalChestScale.z * breathScale
            );
        }

        private void AnimateIdleBob()
        {
            float bobOffset = Mathf.Sin(Time.time * BobSpeed * Mathf.PI * 2f) * BobAmplitude;
            transform.localPosition = originalPosition + Vector3.up * bobOffset;
        }

        private void AnimateHeadTilt()
        {
            if (HeadBone == null) return;
            float tilt = Mathf.Sin(Time.time * TiltSpeed) * TiltAmount;
            HeadBone.localRotation = originalHeadRotation * Quaternion.Euler(0, 0, tilt);
        }

        private void AnimateTailWag()
        {
            if (TailBone == null) return;
            float wag = Mathf.Sin(Time.time * WagSpeed) * WagAmplitude;
            TailBone.localRotation = Quaternion.Euler(0, wag, 0);
        }

        /// <summary>
        /// Trigger anticipation squash before a jump or action.
        /// </summary>
        public void TriggerAnticipation()
        {
            isAnticipating = true;
            anticipationTimer = 0f;
        }

        private void AnimateAnticipation()
        {
            if (!isAnticipating) return;
            anticipationTimer += Time.deltaTime * AnticipationSpeed;

            float t = anticipationTimer;
            Vector3 scale;

            if (t < 0.5f) // Squash phase
            {
                float squash = Mathf.Lerp(1f, AnticipationSquash, t * 2f);
                scale = new Vector3(1f / squash, squash, 1f / squash);
            }
            else if (t < 1f) // Stretch phase
            {
                float stretch = Mathf.Lerp(AnticipationSquash, AnticipationStretch, (t - 0.5f) * 2f);
                scale = new Vector3(1f / stretch, stretch, 1f / stretch);
            }
            else if (t < 1.5f) // Return phase
            {
                float ret = Mathf.Lerp(AnticipationStretch, 1f, (t - 1f) * 2f);
                scale = new Vector3(1f / ret, ret, 1f / ret);
            }
            else
            {
                scale = Vector3.one;
                isAnticipating = false;
            }

            transform.localScale = Vector3.Scale(originalScale, scale);
        }

        /// <summary>
        /// Happy bounce animation.
        /// </summary>
        public void TriggerHappyBounce()
        {
            if (currentBounce != null) StopCoroutine(currentBounce);
            currentBounce = StartCoroutine(HappyBounceCoroutine());
        }

        private System.Collections.IEnumerator HappyBounceCoroutine()
        {
            Vector3 startPos = transform.localPosition;
            float duration = 0.5f;
            float height = 0.3f;
            int bounces = 3;

            for (int i = 0; i < bounces; i++)
            {
                float elapsed = 0f;
                while (elapsed < duration / bounces)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / (duration / bounces);
                    float y = Mathf.Sin(t * Mathf.PI) * height * (1f - (float)i / bounces);
                    transform.localPosition = startPos + Vector3.up * y;
                    yield return null;
                }
            }

            transform.localPosition = startPos;
        }

        /// <summary>
        /// Sad droop animation.
        /// </summary>
        public void TriggerSadDroop()
        {
            if (HeadBone != null)
            {
                StartCoroutine(SadDroopCoroutine());
            }
        }

        private System.Collections.IEnumerator SadDroopCoroutine()
        {
            float elapsed = 0f;
            float duration = 0.5f;
            Quaternion startRot = HeadBone.localRotation;
            Quaternion droopRot = startRot * Quaternion.Euler(15f, 0f, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                HeadBone.localRotation = Quaternion.Slerp(startRot, droopRot, elapsed / duration);
                yield return null;
            }
        }
    }
}
