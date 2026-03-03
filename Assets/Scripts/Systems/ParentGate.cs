using UnityEngine;
using System;

namespace EmersynBigDay.Systems
{
    /// <summary>
    /// Enhancement #26: Parent gate for age-appropriate content protection.
    /// Simple math puzzle that kids can't solve but parents can.
    /// Required for: settings, in-app purchases, external links, ad settings.
    /// COPPA compliant for ages 4-8 (Emersyn is 6).
    /// </summary>
    public class ParentGate : MonoBehaviour
    {
        public static ParentGate Instance { get; private set; }

        [Header("Settings")]
        public bool IsLocked = true;
        public float LockTimeout = 300f; // Re-lock after 5 min

        private int correctAnswer;
        private float unlockTime;
        private Action onUnlockCallback;

        public event Action OnParentVerified;
        public event Action OnParentLocked;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            // Auto-lock after timeout
            if (!IsLocked && Time.realtimeSinceStartup - unlockTime > LockTimeout)
            {
                Lock();
            }
        }

        /// <summary>
        /// Show parent gate challenge. Returns the question string.
        /// </summary>
        public string GetChallenge()
        {
            int a = UnityEngine.Random.Range(10, 50);
            int b = UnityEngine.Random.Range(10, 50);
            int operation = UnityEngine.Random.Range(0, 3);

            string question;
            switch (operation)
            {
                case 0:
                    correctAnswer = a + b;
                    question = $"What is {a} + {b}?";
                    break;
                case 1:
                    // Ensure positive result
                    if (a < b) { int temp = a; a = b; b = temp; }
                    correctAnswer = a - b;
                    question = $"What is {a} - {b}?";
                    break;
                default:
                    int c = UnityEngine.Random.Range(2, 10);
                    int d = UnityEngine.Random.Range(2, 10);
                    correctAnswer = c * d;
                    question = $"What is {c} x {d}?";
                    break;
            }

            return question;
        }

        /// <summary>
        /// Verify the parent's answer.
        /// </summary>
        public bool VerifyAnswer(int answer)
        {
            if (answer == correctAnswer)
            {
                Unlock();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Request parent verification before performing an action.
        /// </summary>
        public void RequestVerification(Action onVerified)
        {
            if (!IsLocked)
            {
                onVerified?.Invoke();
                return;
            }

            onUnlockCallback = onVerified;
            // The UI layer should show the parent gate popup
            // When the parent answers correctly, call VerifyAnswer
        }

        private void Unlock()
        {
            IsLocked = false;
            unlockTime = Time.realtimeSinceStartup;
            onUnlockCallback?.Invoke();
            onUnlockCallback = null;
            OnParentVerified?.Invoke();
        }

        public void Lock()
        {
            IsLocked = true;
            OnParentLocked?.Invoke();
        }

        /// <summary>
        /// Check if an action requires parent gate.
        /// </summary>
        public bool RequiresParentGate(string action)
        {
            switch (action.ToLower())
            {
                case "settings":
                case "purchase":
                case "external_link":
                case "ad_settings":
                case "delete_save":
                case "social_share":
                case "privacy":
                    return true;
                default:
                    return false;
            }
        }
    }
}
