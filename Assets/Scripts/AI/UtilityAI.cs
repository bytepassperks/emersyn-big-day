using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;
using EmersynBigDay.Core;
using EmersynBigDay.Rooms;

namespace EmersynBigDay.AI
{
    /// <summary>
    /// Sims-style Utility AI system. Characters autonomously choose actions
    /// based on their needs, available objects, and personality weights.
    /// Objects "advertise" their need-satisfying capabilities.
    /// </summary>
    public class UtilityAI : MonoBehaviour
    {
        [Header("AI Settings")]
        public float DecisionInterval = 3f;
        public float ActionTimeout = 30f;
        public bool IsAutonomous = true;

        [Header("Personality Weights (0-2)")]
        public float HungerWeight = 1f;
        public float EnergyWeight = 1f;
        public float HygieneWeight = 1f;
        public float FunWeight = 1.2f;
        public float SocialWeight = 1f;
        public float ComfortWeight = 0.8f;
        public float BladderWeight = 1.5f;
        public float CreativityWeight = 0.9f;

        [Header("State")]
        public AIState CurrentAIState = AIState.Idle;
        public string CurrentActionName = "";
        public float CurrentActionProgress = 0f;

        private NeedSystem needSystem;
        private float decisionTimer;
        private float actionTimer;
        private Rooms.InteractableObject currentTarget;
        private List<Rooms.InteractableObject> nearbyObjects = new List<Rooms.InteractableObject>();

        private void Awake()
        {
            needSystem = GetComponent<NeedSystem>();
        }

        private void Update()
        {
            if (!IsAutonomous) return;

            decisionTimer += Time.deltaTime;

            if (CurrentAIState == AIState.PerformingAction)
            {
                actionTimer += Time.deltaTime;
                if (actionTimer >= ActionTimeout)
                {
                    FinishAction();
                }
                return;
            }

            if (decisionTimer >= DecisionInterval)
            {
                decisionTimer = 0f;
                MakeDecision();
            }
        }

        /// <summary>
        /// Core decision-making: score all available actions and pick the best one.
        /// Uses Sims-style advertisement scoring.
        /// </summary>
        private void MakeDecision()
        {
            if (needSystem == null) return;

            // Find all interactable objects in the room
            RefreshNearbyObjects();

            if (nearbyObjects.Count == 0)
            {
                SetIdleState();
                return;
            }

            // Score each object's advertisements
            float bestScore = 0f;
            Rooms.InteractableObject bestObject = null;
            string bestAction = "";

            foreach (var obj in nearbyObjects)
            {
                if (obj.Advertisements == null) continue;
                foreach (var ad in obj.Advertisements)
                {
                    float score = ScoreAdvertisementRooms(ad);

                    // Add randomness to prevent predictable behavior (±15%)
                    score *= UnityEngine.Random.Range(0.85f, 1.15f);

                    // Distance penalty
                    float dist = Vector3.Distance(transform.position, obj.transform.position);
                    score *= Mathf.Clamp01(1f - dist / 20f);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestObject = obj;
                        bestAction = ad.ActionName;
                    }
                }
            }

            // Minimum score threshold to take action
            if (bestScore > 10f && bestObject != null)
            {
                StartAction(bestObject, bestAction);
            }
            else
            {
                SetIdleState();
            }
        }

        private float ScoreAdvertisementRooms(Rooms.Advertisement ad)
        {
            if (needSystem == null) return 0f;

            // Score based on need satisfaction using the Rooms.Advertisement structure
            float needScore = needSystem.ScoreAdvertisement(ad.NeedAffected, ad.NeedDelta);
            float weight = GetPersonalityWeight(ad.NeedAffected);
            float score = needScore * weight * ad.BaseScore;

            return score;
        }

        private float GetPersonalityWeight(string needName)
        {
            switch (needName)
            {
                case "Hunger": return HungerWeight;
                case "Energy": return EnergyWeight;
                case "Hygiene": return HygieneWeight;
                case "Fun": return FunWeight;
                case "Social": return SocialWeight;
                case "Comfort": return ComfortWeight;
                case "Bladder": return BladderWeight;
                case "Creativity": return CreativityWeight;
                default: return 1f;
            }
        }

        private void StartAction(Rooms.InteractableObject target, string actionName)
        {
            currentTarget = target;
            CurrentActionName = actionName;
            CurrentAIState = AIState.MovingToTarget;
            actionTimer = 0f;

            // Move character toward target using NavMeshAgent
            var agent = GetComponent<NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh && target.InteractionPoint != null)
            {
                agent.SetDestination(target.InteractionPoint.position);
                // BeginPerformAction will be called when we arrive (checked in Update)
                StartCoroutine(WaitForArrival(agent, target.InteractionPoint.position, () =>
                {
                    BeginPerformAction();
                }));
            }
            else
            {
                BeginPerformAction();
            }
        }

        private System.Collections.IEnumerator WaitForArrival(NavMeshAgent agent, Vector3 destination, System.Action onArrived)
        {
            float timeout = 10f;
            float elapsed = 0f;
            while (agent != null && agent.isOnNavMesh && elapsed < timeout)
            {
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
                {
                    onArrived?.Invoke();
                    yield break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            // Timed out, still perform action
            onArrived?.Invoke();
        }

        private void BeginPerformAction()
        {
            CurrentAIState = AIState.PerformingAction;

            if (currentTarget != null)
            {
                currentTarget.StartUse();
            }

            // Apply need effects and schedule completion
            if (currentTarget != null && currentTarget.Advertisements != null)
            {
                foreach (var ad in currentTarget.Advertisements)
                {
                    if (ad.ActionName == CurrentActionName)
                    {
                        if (needSystem != null)
                        {
                            needSystem.SatisfyNeed(ad.NeedAffected, ad.NeedDelta);
                        }

                        // Schedule action completion
                        float duration = ad.Duration > 0f ? ad.Duration : 5f;
                        Invoke(nameof(FinishAction), duration);
                        break;
                    }
                }
            }
        }

        private void FinishAction()
        {
            CancelInvoke(nameof(FinishAction));
            CurrentAIState = AIState.Idle;
            CurrentActionName = "";
            if (currentTarget != null) currentTarget.EndUse();
            currentTarget = null;
            actionTimer = 0f;

            // Small delay before next decision
            decisionTimer = -1f;
        }

        private void SetIdleState()
        {
            CurrentAIState = AIState.Idle;
            CurrentActionName = "Idle";

            // Idle behaviors: look around, fidget, small animations
            var animator = GetComponent<Animator>();
            if (animator != null)
            {
                int idleVariant = UnityEngine.Random.Range(0, 4);
                animator.SetInteger("IdleVariant", idleVariant);
            }
        }

        private void RefreshNearbyObjects()
        {
            nearbyObjects.Clear();
            var allObjects = FindObjectsByType<Rooms.InteractableObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj.IsAvailable && Vector3.Distance(transform.position, obj.transform.position) < 15f)
                {
                    nearbyObjects.Add(obj);
                }
            }
        }

        /// <summary>
        /// Force the AI to perform a specific action (used for player-directed interactions).
        /// </summary>
        public void ForceAction(Rooms.InteractableObject target, string actionName)
        {
            CancelInvoke(nameof(FinishAction));
            StartAction(target, actionName);
        }

        public void SetAutonomous(bool autonomous)
        {
            IsAutonomous = autonomous;
            if (!autonomous)
            {
                FinishAction();
            }
        }
    }

    public enum AIState
    {
        Idle,
        MovingToTarget,
        PerformingAction,
        Socializing,
        Sleeping,
        Eating
    }

    // NOTE: InteractableObject and Advertisement are defined in EmersynBigDay.Rooms namespace.
    // UtilityAI references them via Rooms.InteractableObject and Rooms.Advertisement.
}
