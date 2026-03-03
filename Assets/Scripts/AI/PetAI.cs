using UnityEngine;
using UnityEngine.AI;

namespace EmersynBigDay.AI
{
    /// <summary>
    /// Autonomous pet behavior AI. Pets follow the player, react to touch,
    /// play idle animations, and perform context-aware actions based on room and needs.
    /// Uses a simple state machine with personality traits.
    /// </summary>
    public class PetAI : MonoBehaviour
    {
        [Header("Pet Settings")]
        public string PetName;
        public PetType Type;
        public float FollowDistance = 2f;
        public float WanderRadius = 3f;
        public float IdleTimeRange = 5f;
        public float InteractionCooldown = 3f;

        [Header("Personality")]
        [Range(0, 1)] public float Playfulness = 0.7f;
        [Range(0, 1)] public float Affection = 0.6f;
        [Range(0, 1)] public float Energy = 0.8f;
        [Range(0, 1)] public float Curiosity = 0.5f;

        [Header("References")]
        public NavMeshAgent Agent;
        public Animator PetAnimator;
        public Transform OwnerTransform;

        [Header("Stats")]
        public float Happiness = 80f;
        public float Hunger = 70f;
        public float Tiredness = 30f;

        private PetState currentState = PetState.Idle;
        private float stateTimer;
        private float actionCooldown;
        private Vector3 wanderTarget;
        private Transform interestTarget;
        private float lastPettedTime = -10f;

        public enum PetType { Cat, Dog, Bunny }
        public enum PetState { Idle, Following, Wandering, Playing, Sleeping, Begging, Curious, Reacting }

        private void Start()
        {
            if (Agent == null) Agent = GetComponent<NavMeshAgent>();
            if (PetAnimator == null) PetAnimator = GetComponent<Animator>();

            if (Agent != null)
            {
                Agent.speed = 2.5f;
                Agent.stoppingDistance = FollowDistance;
                Agent.angularSpeed = 360f;
            }

            // Find owner (main character)
            if (OwnerTransform == null)
            {
                var player = FindFirstObjectByType<Characters.CharacterController3D>();
                if (player != null && player.IsMainCharacter)
                    OwnerTransform = player.transform;
            }

            stateTimer = Random.Range(1f, IdleTimeRange);
        }

        private void Update()
        {
            stateTimer -= Time.deltaTime;
            actionCooldown -= Time.deltaTime;

            // Update stats over time
            Happiness -= Time.deltaTime * 0.5f;
            Hunger -= Time.deltaTime * 0.3f;
            Tiredness += Time.deltaTime * 0.2f;

            Happiness = Mathf.Clamp(Happiness, 0f, 100f);
            Hunger = Mathf.Clamp(Hunger, 0f, 100f);
            Tiredness = Mathf.Clamp(Tiredness, 0f, 100f);

            switch (currentState)
            {
                case PetState.Idle: UpdateIdle(); break;
                case PetState.Following: UpdateFollowing(); break;
                case PetState.Wandering: UpdateWandering(); break;
                case PetState.Playing: UpdatePlaying(); break;
                case PetState.Sleeping: UpdateSleeping(); break;
                case PetState.Begging: UpdateBegging(); break;
                case PetState.Curious: UpdateCurious(); break;
                case PetState.Reacting: UpdateReacting(); break;
            }

            UpdateAnimator();
        }

        // --- STATE UPDATES ---

        private void UpdateIdle()
        {
            if (stateTimer <= 0f)
            {
                // Decide next action based on personality and needs
                float decision = Random.value;

                if (Tiredness > 80f)
                {
                    ChangeState(PetState.Sleeping);
                }
                else if (Hunger < 30f && decision < 0.3f)
                {
                    ChangeState(PetState.Begging);
                }
                else if (OwnerTransform != null && DistanceToOwner() > FollowDistance * 2f)
                {
                    ChangeState(PetState.Following);
                }
                else if (decision < Playfulness * 0.5f)
                {
                    ChangeState(PetState.Playing);
                }
                else if (decision < Curiosity * 0.5f + 0.3f)
                {
                    ChangeState(PetState.Wandering);
                }
                else
                {
                    // Stay idle a bit longer
                    stateTimer = Random.Range(2f, IdleTimeRange);
                }
            }

            // Random idle animations
            if (PetAnimator != null && Random.value < 0.01f)
            {
                string[] idleAnims = GetIdleAnimations();
                if (idleAnims.Length > 0)
                    PetAnimator.CrossFadeInFixedTime(idleAnims[Random.Range(0, idleAnims.Length)], 0.3f);
            }
        }

        private void UpdateFollowing()
        {
            if (OwnerTransform == null) { ChangeState(PetState.Idle); return; }

            float dist = DistanceToOwner();
            if (dist > FollowDistance)
            {
                if (Agent != null && Agent.isOnNavMesh)
                {
                    // Follow at an offset to not overlap with player
                    Vector3 offset = (transform.position - OwnerTransform.position).normalized * FollowDistance;
                    if (offset.magnitude < 0.1f) offset = Vector3.right * FollowDistance;
                    Agent.SetDestination(OwnerTransform.position + offset);
                }
            }
            else
            {
                if (Agent != null) Agent.ResetPath();
                if (stateTimer <= 0f) ChangeState(PetState.Idle);
            }

            // Look at owner
            LookAt(OwnerTransform.position);
        }

        private void UpdateWandering()
        {
            if (Agent != null && Agent.isOnNavMesh)
            {
                if (!Agent.hasPath || Agent.remainingDistance < 0.5f)
                {
                    if (stateTimer <= 0f)
                    {
                        ChangeState(PetState.Idle);
                        return;
                    }

                    // Pick new wander point
                    Vector3 center = OwnerTransform != null ? OwnerTransform.position : transform.position;
                    Vector3 randomPoint = center + Random.insideUnitSphere * WanderRadius;
                    randomPoint.y = transform.position.y;

                    if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, WanderRadius, NavMesh.AllAreas))
                    {
                        Agent.SetDestination(hit.position);
                    }
                }
            }
        }

        private void UpdatePlaying()
        {
            if (stateTimer <= 0f) { ChangeState(PetState.Idle); return; }

            // Playful behavior - chase tail, jump around
            if (Random.value < 0.02f)
            {
                string anim = Type switch
                {
                    PetType.Cat => "Pounce",
                    PetType.Dog => "Spin",
                    PetType.Bunny => "Hop",
                    _ => "Jump"
                };
                if (PetAnimator != null) PetAnimator.CrossFadeInFixedTime(anim, 0.2f);
                Happiness += 5f;
            }
        }

        private void UpdateSleeping()
        {
            Tiredness -= Time.deltaTime * 2f; // Recover faster while sleeping
            if (Tiredness <= 20f || stateTimer <= 0f)
            {
                ChangeState(PetState.Idle);
            }
        }

        private void UpdateBegging()
        {
            if (OwnerTransform != null) LookAt(OwnerTransform.position);

            if (stateTimer <= 0f || Hunger > 60f)
            {
                ChangeState(PetState.Idle);
            }
        }

        private void UpdateCurious()
        {
            if (interestTarget == null || stateTimer <= 0f)
            {
                ChangeState(PetState.Idle);
                return;
            }

            float dist = Vector3.Distance(transform.position, interestTarget.position);
            if (dist > 1.5f && Agent != null && Agent.isOnNavMesh)
            {
                Agent.SetDestination(interestTarget.position);
            }
            else
            {
                LookAt(interestTarget.position);
                // Sniff / investigate
                if (PetAnimator != null && Random.value < 0.02f)
                    PetAnimator.CrossFadeInFixedTime("Sniff", 0.3f);
            }
        }

        private void UpdateReacting()
        {
            if (stateTimer <= 0f) ChangeState(PetState.Idle);
        }

        // --- INTERACTIONS ---

        public void OnPetted()
        {
            if (Time.time - lastPettedTime < InteractionCooldown) return;
            lastPettedTime = Time.time;

            Happiness = Mathf.Min(Happiness + 15f, 100f);
            ChangeState(PetState.Reacting);
            stateTimer = 2f;

            if (PetAnimator != null)
            {
                string reaction = Type switch
                {
                    PetType.Cat => "Purr",
                    PetType.Dog => "WagTail",
                    PetType.Bunny => "NuzzleUp",
                    _ => "Happy"
                };
                PetAnimator.CrossFadeInFixedTime(reaction, 0.2f);
            }

            if (Particles.ParticleManager.Instance != null)
                Particles.ParticleManager.Instance.SpawnHearts(transform.position + Vector3.up * 1.5f);
            if (Audio.AudioManager.Instance != null)
                Audio.AudioManager.Instance.PlaySFX("laugh");

            if (Core.RewardSystem.Instance != null)
                Core.RewardSystem.Instance.GrantInteractionReward("pet");
        }

        public void OnFed()
        {
            Hunger = Mathf.Min(Hunger + 30f, 100f);
            Happiness = Mathf.Min(Happiness + 10f, 100f);

            if (PetAnimator != null) PetAnimator.CrossFadeInFixedTime("Eat", 0.2f);
            if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.PlaySFX("eat");

            ChangeState(PetState.Reacting);
            stateTimer = 3f;
        }

        public void OnPoked()
        {
            ChangeState(PetState.Reacting);
            stateTimer = 1.5f;

            if (PetAnimator != null)
            {
                string reaction = Happiness > 50f ? "Surprised" : "Annoyed";
                PetAnimator.CrossFadeInFixedTime(reaction, 0.2f);
            }
        }

        public void OnObjectOfInterest(Transform target)
        {
            if (Curiosity > Random.value)
            {
                interestTarget = target;
                ChangeState(PetState.Curious);
            }
        }

        // --- HELPERS ---

        private void ChangeState(PetState newState)
        {
            currentState = newState;
            stateTimer = newState switch
            {
                PetState.Idle => Random.Range(2f, IdleTimeRange),
                PetState.Following => 10f,
                PetState.Wandering => Random.Range(5f, 10f),
                PetState.Playing => Random.Range(3f, 8f),
                PetState.Sleeping => Random.Range(8f, 15f),
                PetState.Begging => Random.Range(3f, 6f),
                PetState.Curious => Random.Range(3f, 7f),
                PetState.Reacting => 2f,
                _ => 3f
            };
        }

        private float DistanceToOwner()
        {
            if (OwnerTransform == null) return float.MaxValue;
            return Vector3.Distance(transform.position, OwnerTransform.position);
        }

        private void LookAt(Vector3 target)
        {
            Vector3 dir = (target - transform.position);
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion lookRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 5f);
            }
        }

        private void UpdateAnimator()
        {
            if (PetAnimator == null) return;

            float speed = Agent != null ? Agent.velocity.magnitude : 0f;
            PetAnimator.SetFloat("Speed", speed);
            PetAnimator.SetBool("IsSleeping", currentState == PetState.Sleeping);
            PetAnimator.SetFloat("Happiness", Happiness / 100f);
        }

        private string[] GetIdleAnimations()
        {
            return Type switch
            {
                PetType.Cat => new[] { "LickPaw", "StretchIdle", "LookAround", "TailFlick" },
                PetType.Dog => new[] { "Pant", "ScratchEar", "Sniff", "SitIdle" },
                PetType.Bunny => new[] { "TwitchNose", "HopInPlace", "CleanFace", "EarWiggle" },
                _ => new[] { "Idle1", "Idle2" }
            };
        }
    }
}
