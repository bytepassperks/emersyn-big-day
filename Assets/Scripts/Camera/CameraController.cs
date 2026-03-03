using UnityEngine;

namespace EmersynBigDay.CameraSystem
{
    /// <summary>
    /// Spring-follow camera with smooth transitions, screen shake, zoom,
    /// and room-based camera presets. Implements Sims-style orbital camera
    /// with Talking Tom-style follow behavior.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        [Header("Target")]
        public Transform Target;
        public Vector3 Offset = new Vector3(0f, 3f, -5f);
        public float FollowSpeed = 5f;
        public float RotationSpeed = 3f;

        [Header("Spring Settings")]
        // Round 25 (Claude 4.5 Bedrock): Very high stiffness+damping to snap to position instantly
        public float SpringStiffness = 200f;
        public float SpringDamping = 40f;
        private Vector3 springVelocity = Vector3.zero;
        private bool initialPositionSet = false;
        private int frameCount = 0;

        [Header("Zoom")]
        public float MinZoom = 3f;
        public float MaxZoom = 25f; // Round 28: Reduced from 40 to prevent extreme zoom-out
        public float ZoomSpeed = 2f;
        public float CurrentZoom = 18f; // Round 28: Default for phones, overridden by SceneBuilder

        [Header("Orbital Control")]
        public float OrbitSpeed = 120f;
        public float MinPitch = 10f;
        public float MaxPitch = 80f;
        // Round 26: 50° pitch for true Sims 4 dollhouse angle — looking DOWN at floor
        public float DefaultPitch = 50f;
        private float currentYaw = 0f;
        private float currentPitch = 50f;

        [Header("Screen Shake")]
        public float ShakeDecay = 5f;
        private float shakeIntensity = 0f;
        private float shakeTimer = 0f;

        [Header("Boundaries")]
        public Vector3 MinBounds = new Vector3(-50, 0, -50);
        public Vector3 MaxBounds = new Vector3(50, 30, 50);

        [Header("Transition")]
        public float TransitionSpeed = 3f;
        private Vector3 transitionTarget;
        private bool isTransitioning = false;

        private Camera cam;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            cam = GetComponent<Camera>();
            currentPitch = DefaultPitch;
            CurrentZoom = Mathf.Clamp(CurrentZoom, MinZoom, MaxZoom);
            // Round 25 (Bedrock): Force camera to exact position immediately on Awake
            initialPositionSet = false;
            frameCount = 0;
        }

        private void LateUpdate()
        {
            if (Target == null) return;
            frameCount++;

            // Round 25 (Bedrock): Force exact position for first 10 frames to prevent spring oscillation
            if (frameCount <= 10)
            {
                ForceExactPosition();
                return;
            }

            if (isTransitioning)
            {
                UpdateTransition();
            }
            else
            {
                UpdateSpringFollow();
            }

            UpdateScreenShake();
            ClampPosition();
        }

        // Round 25 (Bedrock): Snap camera to exact calculated position (no spring)
        private void ForceExactPosition()
        {
            Vector3 desiredPosition = CalculateDesiredPosition();
            transform.position = desiredPosition;
            springVelocity = Vector3.zero;
            // Round 26: Look at floor center (y=1.0) not room center (y=2.5) to tilt camera down
            Vector3 lookTarget = Target.position + Vector3.up * 1.0f;
            transform.LookAt(lookTarget);
            initialPositionSet = true;
        }

        // --- SPRING FOLLOW ---
        private void UpdateSpringFollow()
        {
            Vector3 desiredPosition = CalculateDesiredPosition();
            Vector3 displacement = transform.position - desiredPosition;
            Vector3 springForce = -SpringStiffness * displacement - SpringDamping * springVelocity;
            springVelocity += springForce * Time.deltaTime;
            transform.position += springVelocity * Time.deltaTime;

            // Round 26: Look at floor center (y=1.0) to keep camera looking down
            Vector3 lookTarget = Target.position + Vector3.up * 1.0f;
            Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
        }

        private Vector3 CalculateDesiredPosition()
        {
            float yawRad = currentYaw * Mathf.Deg2Rad;
            float pitchRad = currentPitch * Mathf.Deg2Rad;

            Vector3 direction = new Vector3(
                Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
                Mathf.Sin(pitchRad),
                -Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
            );

            return Target.position + direction * CurrentZoom;
        }

        // --- ORBITAL CAMERA ---
        public void OrbitHorizontal(float delta)
        {
            currentYaw += delta * OrbitSpeed * Time.deltaTime;
        }

        public void OrbitVertical(float delta)
        {
            currentPitch = Mathf.Clamp(currentPitch + delta * OrbitSpeed * Time.deltaTime, MinPitch, MaxPitch);
        }

        // --- ZOOM ---
        public void Zoom(float delta)
        {
            CurrentZoom = Mathf.Clamp(CurrentZoom - delta * ZoomSpeed, MinZoom, MaxZoom);
        }

        public void SetZoom(float zoom)
        {
            CurrentZoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);
        }

        // --- SCREEN SHAKE ---
        public void Shake(float intensity, float duration)
        {
            shakeIntensity = intensity;
            shakeTimer = duration;
        }

        public void ShakeSmall() { Shake(0.1f, 0.2f); }
        public void ShakeMedium() { Shake(0.3f, 0.3f); }
        public void ShakeLarge() { Shake(0.6f, 0.5f); }

        private void UpdateScreenShake()
        {
            if (shakeTimer <= 0f) return;

            shakeTimer -= Time.deltaTime;
            float currentIntensity = shakeIntensity * (shakeTimer > 0 ? 1f : 0f);

            Vector3 shakeOffset = new Vector3(
                UnityEngine.Random.Range(-1f, 1f) * currentIntensity,
                UnityEngine.Random.Range(-1f, 1f) * currentIntensity,
                0f
            );

            transform.position += shakeOffset;
            shakeIntensity *= (1f - ShakeDecay * Time.deltaTime);
        }

        // --- TRANSITIONS ---
        public void TransitionTo(Vector3 position, float zoom, float pitch = -1f, float yaw = -1f)
        {
            transitionTarget = position;
            if (zoom > 0) CurrentZoom = zoom;
            if (pitch >= 0) currentPitch = pitch;
            if (yaw >= 0) currentYaw = yaw;
            isTransitioning = true;
        }

        public void TransitionToRoom(Rooms.RoomData room)
        {
            if (room == null) return;
            Offset = room.CameraOffset;
            if (cam != null) cam.fieldOfView = room.CameraFOV;
        }

        private void UpdateTransition()
        {
            transform.position = Vector3.Lerp(transform.position, transitionTarget, TransitionSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, transitionTarget) < 0.1f)
            {
                isTransitioning = false;
            }
        }

        // --- FOCUS ---
        public void FocusOn(Transform target, float zoomLevel = 4f)
        {
            Target = target;
            SetZoom(zoomLevel);
        }

        public void ResetToDefault()
        {
            currentYaw = 0f;
            currentPitch = DefaultPitch;
            // Round 29: 3-tier adaptive zoom on reset based on screen aspect
            float resetAspect = (float)Screen.width / Screen.height;
            if (resetAspect < 0.5f) CurrentZoom = 15f;
            else if (resetAspect < 0.6f) CurrentZoom = 18f;
            else CurrentZoom = 16f; // Round 29: Tablets closer
            frameCount = 0; // Round 25: Reset to force exact position again
            springVelocity = Vector3.zero;
            isTransitioning = false;
        }

        // --- BOUNDARIES ---
        private void ClampPosition()
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, MinBounds.x, MaxBounds.x);
            pos.y = Mathf.Clamp(pos.y, MinBounds.y, MaxBounds.y);
            pos.z = Mathf.Clamp(pos.z, MinBounds.z, MaxBounds.z);
            transform.position = pos;
        }

        // --- PROPERTIES ---
        public float Yaw => currentYaw;
        public float Pitch => currentPitch;
        public bool IsTransitioning => isTransitioning;
    }
}
