using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections.Generic;

namespace EmersynBigDay.Characters
{
    /// <summary>
    /// Controls character movement, animation, expressions, and touch interactions.
    /// Supports both player-controlled and NPC autonomous behavior.
    /// </summary>
    public class CharacterController3D : MonoBehaviour
    {
        [Header("Character Identity")]
        public string CharacterName = "Emersyn";
        public CharacterType Type = CharacterType.Player;
        public bool IsMainCharacter = false;

        [Header("Movement")]
        public float WalkSpeed = 2f;
        public float RunSpeed = 4.5f;
        public float RotationSpeed = 8f;
        public float StoppingDistance = 0.5f;
        private NavMeshAgent navAgent;
        private Vector3 targetPosition;
        private bool isMoving = false;

        [Header("Animation")]
        public Animator CharacterAnimator;
        private string currentAnimation = "Idle";
        private float idleTimer = 0f;
        private float idleVariationInterval = 5f;

        [Header("Expression System")]
        public SkinnedMeshRenderer FaceRenderer;
        public int HappyBlendShapeIndex = 0;
        public int SadBlendShapeIndex = 1;
        public int AngryBlendShapeIndex = 2;
        public int SurprisedBlendShapeIndex = 3;
        public int SleepyBlendShapeIndex = 4;
        private Expression currentExpression = Expression.Neutral;
        private float expressionBlendSpeed = 3f;

        [Header("Squash & Stretch")]
        public float SquashAmount = 0.15f;
        public float StretchAmount = 0.1f;
        private Vector3 originalScale;
        private float squashTimer = 0f;
        private bool isSquashing = false;

        [Header("Jiggle Physics")]
        public Transform[] JiggleBones;
        public float JiggleStiffness = 15f;
        public float JiggleDamping = 0.8f;
        private Vector3[] jiggleVelocities;
        private Vector3[] jigglePrevPositions;

        [Header("Eye Tracking")]
        public Transform LeftEye;
        public Transform RightEye;
        public float EyeTrackSpeed = 5f;
        public float MaxEyeAngle = 25f;
        private Transform lookTarget;

        [Header("Touch Interaction")]
        public float TouchReactionForce = 2f;
        public float PokeReactionDelay = 0.3f;
        private float lastPokeTime = 0f;
        private int consecutivePokes = 0;

        public event Action<string> OnAnimationChanged;
        public event Action<Expression> OnExpressionChanged;
        public event Action<Vector3> OnMoved;
        public event Action OnTouched;
        public event Action OnPoked;

        public enum CharacterType { Player, Friend, Pet, NPC }
        public enum Expression { Neutral, Happy, Sad, Angry, Surprised, Sleepy, Excited, Shy }

        private void Awake()
        {
            navAgent = GetComponent<NavMeshAgent>();
            originalScale = transform.localScale;

            if (navAgent != null)
            {
                navAgent.speed = WalkSpeed;
                navAgent.angularSpeed = RotationSpeed * 45f;
                navAgent.stoppingDistance = StoppingDistance;
            }

            InitializeJiggleBones();
        }

        private void Update()
        {
            UpdateMovement();
            UpdateIdleVariation();
            UpdateSquashStretch();
            UpdateJigglePhysics();
            UpdateEyeTracking();
            UpdateExpressionBlending();
        }

        public void MoveTo(Vector3 position)
        {
            targetPosition = position;
            isMoving = true;
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.SetDestination(position);
                navAgent.speed = WalkSpeed;
            }
            PlayAnimation("Walk");
            OnMoved?.Invoke(position);
        }

        public void RunTo(Vector3 position)
        {
            targetPosition = position;
            isMoving = true;
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.SetDestination(position);
                navAgent.speed = RunSpeed;
            }
            PlayAnimation("Run");
            OnMoved?.Invoke(position);
        }

        public void StopMoving()
        {
            isMoving = false;
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.ResetPath();
            }
            PlayAnimation("Idle");
        }

        private void UpdateMovement()
        {
            if (!isMoving || navAgent == null) return;
            if (navAgent.remainingDistance <= StoppingDistance && !navAgent.pathPending)
            {
                StopMoving();
            }
        }

        public void PlayAnimation(string animName)
        {
            if (currentAnimation == animName) return;
            currentAnimation = animName;
            if (CharacterAnimator != null)
            {
                CharacterAnimator.CrossFadeInFixedTime(animName, 0.2f);
            }
            OnAnimationChanged?.Invoke(animName);
        }

        private void UpdateIdleVariation()
        {
            if (currentAnimation != "Idle") { idleTimer = 0f; return; }
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleVariationInterval)
            {
                idleTimer = 0f;
                idleVariationInterval = UnityEngine.Random.Range(4f, 8f);
                int variation = UnityEngine.Random.Range(0, 5);
                switch (variation)
                {
                    case 0: PlayAnimation("IdleLookAround"); break;
                    case 1: PlayAnimation("IdleScratchHead"); break;
                    case 2: PlayAnimation("IdleYawn"); break;
                    case 3: PlayAnimation("IdleShift"); break;
                    case 4: PlayAnimation("IdleBlink"); break;
                }
                Invoke(nameof(ReturnToIdle), 2f);
            }
        }

        private void ReturnToIdle() { if (!isMoving) PlayAnimation("Idle"); }

        public void SetExpression(Expression expr, float duration = 0f)
        {
            currentExpression = expr;
            OnExpressionChanged?.Invoke(expr);
            if (duration > 0f) Invoke(nameof(ResetExpression), duration);
        }

        private void ResetExpression() { currentExpression = Expression.Neutral; }

        private void UpdateExpressionBlending()
        {
            if (FaceRenderer == null) return;
            float targetHappy = (currentExpression == Expression.Happy || currentExpression == Expression.Excited) ? 100f : 0f;
            float targetSad = currentExpression == Expression.Sad ? 100f : 0f;
            float targetAngry = currentExpression == Expression.Angry ? 100f : 0f;
            float targetSurprised = (currentExpression == Expression.Surprised || currentExpression == Expression.Shy) ? 100f : 0f;
            float targetSleepy = currentExpression == Expression.Sleepy ? 100f : 0f;
            float speed = expressionBlendSpeed * Time.deltaTime;
            BlendToTarget(HappyBlendShapeIndex, targetHappy, speed);
            BlendToTarget(SadBlendShapeIndex, targetSad, speed);
            BlendToTarget(AngryBlendShapeIndex, targetAngry, speed);
            BlendToTarget(SurprisedBlendShapeIndex, targetSurprised, speed);
            BlendToTarget(SleepyBlendShapeIndex, targetSleepy, speed);
        }

        private void BlendToTarget(int index, float target, float speed)
        {
            if (FaceRenderer == null || index < 0 || index >= FaceRenderer.sharedMesh.blendShapeCount) return;
            float current = FaceRenderer.GetBlendShapeWeight(index);
            FaceRenderer.SetBlendShapeWeight(index, Mathf.Lerp(current, target, speed));
        }

        public void TriggerSquash() { isSquashing = true; squashTimer = 0f; }

        private void UpdateSquashStretch()
        {
            if (!isSquashing) return;
            squashTimer += Time.deltaTime * 8f;
            if (squashTimer < 1f)
            {
                float t = squashTimer;
                float scaleX = 1f + SquashAmount * Mathf.Sin(t * Mathf.PI);
                float scaleY = 1f - SquashAmount * Mathf.Sin(t * Mathf.PI);
                transform.localScale = new Vector3(originalScale.x * scaleX, originalScale.y * scaleY, originalScale.z * scaleX);
            }
            else if (squashTimer < 2f)
            {
                float t = squashTimer - 1f;
                float scaleX = 1f - StretchAmount * 0.5f * Mathf.Sin(t * Mathf.PI);
                float scaleY = 1f + StretchAmount * 0.5f * Mathf.Sin(t * Mathf.PI);
                transform.localScale = new Vector3(originalScale.x * scaleX, originalScale.y * scaleY, originalScale.z * scaleX);
            }
            else
            {
                transform.localScale = originalScale;
                isSquashing = false;
            }
        }

        private void InitializeJiggleBones()
        {
            if (JiggleBones == null || JiggleBones.Length == 0) return;
            jiggleVelocities = new Vector3[JiggleBones.Length];
            jigglePrevPositions = new Vector3[JiggleBones.Length];
            for (int i = 0; i < JiggleBones.Length; i++)
            {
                if (JiggleBones[i] != null) jigglePrevPositions[i] = JiggleBones[i].position;
            }
        }

        private void UpdateJigglePhysics()
        {
            if (JiggleBones == null) return;
            for (int i = 0; i < JiggleBones.Length; i++)
            {
                if (JiggleBones[i] == null) continue;
                Vector3 currentPos = JiggleBones[i].position;
                Vector3 force = (jigglePrevPositions[i] - currentPos) * JiggleStiffness;
                jiggleVelocities[i] = (jiggleVelocities[i] + force * Time.deltaTime) * JiggleDamping;
                JiggleBones[i].localRotation *= Quaternion.Euler(jiggleVelocities[i]);
                jigglePrevPositions[i] = currentPos;
            }
        }

        public void SetLookTarget(Transform target) { lookTarget = target; }

        private void UpdateEyeTracking()
        {
            if (LeftEye == null && RightEye == null) return;
            Vector3 lookDir = lookTarget != null ? (lookTarget.position - transform.position).normalized : transform.forward;
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            float angle = Quaternion.Angle(transform.rotation, targetRot);
            if (angle > MaxEyeAngle) targetRot = Quaternion.RotateTowards(transform.rotation, targetRot, MaxEyeAngle);
            float speed = EyeTrackSpeed * Time.deltaTime;
            if (LeftEye != null) LeftEye.rotation = Quaternion.Slerp(LeftEye.rotation, targetRot, speed);
            if (RightEye != null) RightEye.rotation = Quaternion.Slerp(RightEye.rotation, targetRot, speed);
        }

        public void OnPokeHead()
        {
            float timeSincePoke = Time.time - lastPokeTime;
            lastPokeTime = Time.time;
            consecutivePokes = timeSincePoke < PokeReactionDelay ? consecutivePokes + 1 : 1;

            if (consecutivePokes >= 5) { SetExpression(Expression.Angry, 2f); PlayAnimation("Annoyed"); }
            else if (consecutivePokes >= 3) { SetExpression(Expression.Surprised, 1.5f); PlayAnimation("Dizzy"); }
            else { SetExpression(Expression.Happy, 1f); PlayAnimation("Giggle"); }

            TriggerSquash();
            OnPoked?.Invoke();
        }

        public void OnPetHead() { SetExpression(Expression.Happy, 2f); PlayAnimation("Purr"); OnTouched?.Invoke(); }
        public void OnTickleBelly() { SetExpression(Expression.Excited, 2f); PlayAnimation("Laugh"); TriggerSquash(); OnTouched?.Invoke(); }
        public void OnDragFeet() { SetExpression(Expression.Surprised, 1f); PlayAnimation("Wobble"); OnTouched?.Invoke(); }

        public bool IsIdle => currentAnimation == "Idle" || currentAnimation.StartsWith("Idle");
        public bool IsMovingNow => isMoving;
        public Expression CurrentExpression => currentExpression;
        public string CurrentAnimationName => currentAnimation;
    }
}
