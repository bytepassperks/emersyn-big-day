using UnityEngine;

namespace EmersynBigDay.Visual
{
    /// <summary>
    /// Enhancement #6: Characters' eyes follow the camera/player touch.
    /// Like Talking Tom's eye tracking and Animal Crossing's character awareness.
    /// </summary>
    public class EyeTracker : MonoBehaviour
    {
        [Header("Eye References")]
        public Transform LeftEye;
        public Transform RightEye;

        [Header("Settings")]
        public float TrackSpeed = 5f;
        public float MaxAngle = 20f;
        public float BlinkInterval = 4f;
        public float BlinkDuration = 0.15f;

        private Transform lookTarget;
        private Camera mainCamera;
        private float blinkTimer;
        private bool isBlinking;
        private Vector3 leftEyeOrigScale;
        private Vector3 rightEyeOrigScale;
        private Vector3 lastTouchWorldPos;
        private bool trackTouch;

        private void Start()
        {
            mainCamera = Camera.main;
            blinkTimer = Random.Range(2f, BlinkInterval);
            if (LeftEye != null) leftEyeOrigScale = LeftEye.localScale;
            if (RightEye != null) rightEyeOrigScale = RightEye.localScale;
        }

        private void Update()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null && lookTarget == null && !trackTouch) return;
            }
            UpdateEyeTracking();
            UpdateBlinking();
        }

        private void UpdateEyeTracking()
        {
            Vector3 targetPos;

            if (trackTouch)
            {
                targetPos = lastTouchWorldPos;
            }
            else if (lookTarget != null)
            {
                targetPos = lookTarget.position;
            }
            else if (mainCamera != null)
            {
                targetPos = mainCamera.transform.position;
            }
            else return;

            Vector3 dirToTarget = (targetPos - transform.position).normalized;

            // Clamp look direction to max angle
            Vector3 forward = transform.forward;
            float angle = Vector3.Angle(forward, dirToTarget);
            if (angle > MaxAngle)
            {
                dirToTarget = Vector3.RotateTowards(forward, dirToTarget, MaxAngle * Mathf.Deg2Rad, 0f);
            }

            // Calculate eye offset (small local position shift for eye tracking)
            Vector3 localDir = transform.InverseTransformDirection(dirToTarget);
            float eyeOffsetX = Mathf.Clamp(localDir.x * 0.05f, -0.03f, 0.03f);
            float eyeOffsetY = Mathf.Clamp(localDir.y * 0.03f, -0.02f, 0.02f);

            float speed = TrackSpeed * Time.deltaTime;

            if (LeftEye != null)
            {
                Vector3 targetLocalPos = LeftEye.localPosition;
                targetLocalPos.z = Mathf.Lerp(targetLocalPos.z, -0.4f + eyeOffsetX, speed);
                LeftEye.localPosition = Vector3.Lerp(LeftEye.localPosition, targetLocalPos, speed);
            }

            if (RightEye != null)
            {
                Vector3 targetLocalPos = RightEye.localPosition;
                targetLocalPos.z = Mathf.Lerp(targetLocalPos.z, -0.4f + eyeOffsetX, speed);
                RightEye.localPosition = Vector3.Lerp(RightEye.localPosition, targetLocalPos, speed);
            }
        }

        private void UpdateBlinking()
        {
            blinkTimer -= Time.deltaTime;

            if (blinkTimer <= 0f && !isBlinking)
            {
                isBlinking = true;
                blinkTimer = BlinkDuration;
                // Squash eyes vertically for blink
                if (LeftEye != null)
                    LeftEye.localScale = new Vector3(leftEyeOrigScale.x, leftEyeOrigScale.y * 0.1f, leftEyeOrigScale.z);
                if (RightEye != null)
                    RightEye.localScale = new Vector3(rightEyeOrigScale.x, rightEyeOrigScale.y * 0.1f, rightEyeOrigScale.z);
            }
            else if (isBlinking && blinkTimer <= 0f)
            {
                isBlinking = false;
                blinkTimer = Random.Range(2f, BlinkInterval);
                // Restore eye scale
                if (LeftEye != null) LeftEye.localScale = leftEyeOrigScale;
                if (RightEye != null) RightEye.localScale = rightEyeOrigScale;
            }
        }

        public void SetLookTarget(Transform target) { lookTarget = target; trackTouch = false; }
        public void LookAtTouch(Vector3 worldPos) { lastTouchWorldPos = worldPos; trackTouch = true; }
        public void ClearLookTarget() { lookTarget = null; trackTouch = false; }
    }
}
