using UnityEngine;

namespace EmersynBigDay.MiniGames
{
    /// <summary>
    /// Color Mix: mix primary colors to match a target color.
    /// Drag color blobs together to create new colors. Score based on accuracy.
    /// Satisfies Creativity need.
    /// </summary>
    public class ColorMixGame : MonoBehaviour
    {
        [Header("Settings")]
        public int RoundsTotal = 5;
        public float AccuracyThreshold = 0.85f;
        public float RoundDuration = 20f;

        [Header("Colors")]
        public Color[] PrimaryColors;

        [Header("UI")]
        public UnityEngine.UI.Image TargetColorDisplay;
        public UnityEngine.UI.Image MixedColorDisplay;
        public TMPro.TextMeshProUGUI RoundText;
        public TMPro.TextMeshProUGUI AccuracyText;
        public TMPro.TextMeshProUGUI TimerText;
        public UnityEngine.UI.Button SubmitButton;
        public UnityEngine.UI.Button ResetMixButton;

        private Color targetColor;
        private Color mixedColor = Color.white;
        private int currentRound = 0;
        private int score = 0;
        private float roundTimer;
        private int colorsMixed = 0;
        private bool isActive = false;

        public void StartGame()
        {
            if (PrimaryColors == null || PrimaryColors.Length == 0)
            {
                PrimaryColors = new Color[] { Color.red, Color.blue, Color.yellow, Color.green, Color.white, Color.black };
            }

            currentRound = 0;
            score = 0;
            isActive = true;

            if (SubmitButton != null) SubmitButton.onClick.AddListener(SubmitColor);
            if (ResetMixButton != null) ResetMixButton.onClick.AddListener(ResetMix);

            StartRound();
        }

        private void Update()
        {
            if (!isActive) return;
            roundTimer -= Time.deltaTime;
            if (TimerText != null) TimerText.text = $"{Mathf.CeilToInt(roundTimer)}s";
            if (roundTimer <= 0f) SubmitColor();
        }

        private void StartRound()
        {
            if (currentRound >= RoundsTotal) { EndGame(); return; }

            // Generate target color by mixing 2-3 primaries
            targetColor = GenerateTargetColor();
            mixedColor = Color.white;
            colorsMixed = 0;
            roundTimer = RoundDuration;

            if (TargetColorDisplay != null) TargetColorDisplay.color = targetColor;
            if (MixedColorDisplay != null) MixedColorDisplay.color = mixedColor;
            if (RoundText != null) RoundText.text = $"Round {currentRound + 1}/{RoundsTotal}";
            UpdateAccuracyDisplay();
        }

        private Color GenerateTargetColor()
        {
            // Mix 2-3 random primaries
            int mixCount = UnityEngine.Random.Range(2, 4);
            Color result = Color.black;

            for (int i = 0; i < mixCount; i++)
            {
                Color c = PrimaryColors[UnityEngine.Random.Range(0, PrimaryColors.Length)];
                float weight = UnityEngine.Random.Range(0.2f, 0.8f);
                result = Color.Lerp(result, c, weight);
            }

            return result;
        }

        public void AddColor(int colorIndex)
        {
            if (!isActive || colorIndex < 0 || colorIndex >= PrimaryColors.Length) return;

            Color adding = PrimaryColors[colorIndex];
            colorsMixed++;

            // Subtractive mixing approximation
            if (colorsMixed == 1)
            {
                mixedColor = adding;
            }
            else
            {
                float weight = 1f / colorsMixed;
                mixedColor = Color.Lerp(mixedColor, adding, weight);
            }

            if (MixedColorDisplay != null) MixedColorDisplay.color = mixedColor;
            UpdateAccuracyDisplay();

            if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.PlaySFX("tap");
        }

        public void ResetMix()
        {
            mixedColor = Color.white;
            colorsMixed = 0;
            if (MixedColorDisplay != null) MixedColorDisplay.color = mixedColor;
            UpdateAccuracyDisplay();
        }

        public void SubmitColor()
        {
            float accuracy = CalculateAccuracy();
            int roundScore = Mathf.CeilToInt(accuracy * 100f);
            score += roundScore;

            bool matched = accuracy >= AccuracyThreshold;
            if (matched)
            {
                if (Particles.ParticleManager.Instance != null)
                    Particles.ParticleManager.Instance.SpawnSparkles(Vector3.up * 2f);
                if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.PlaySFX("coin");
            }

            currentRound++;
            StartRound();
        }

        private float CalculateAccuracy()
        {
            float rDiff = Mathf.Abs(targetColor.r - mixedColor.r);
            float gDiff = Mathf.Abs(targetColor.g - mixedColor.g);
            float bDiff = Mathf.Abs(targetColor.b - mixedColor.b);
            float totalDiff = (rDiff + gDiff + bDiff) / 3f;
            return 1f - Mathf.Clamp01(totalDiff);
        }

        private void UpdateAccuracyDisplay()
        {
            float accuracy = CalculateAccuracy();
            if (AccuracyText != null)
            {
                AccuracyText.text = $"Match: {Mathf.CeilToInt(accuracy * 100)}%";
                AccuracyText.color = accuracy >= AccuracyThreshold ? Color.green : Color.yellow;
            }
        }

        private void EndGame()
        {
            isActive = false;

            if (MiniGameManager.Instance != null)
            {
                MiniGameManager.Instance.AddScore(score);
                MiniGameManager.Instance.CompleteGame(score >= RoundsTotal * 50);
            }

            var needSystem = FindFirstObjectByType<Core.NeedSystem>();
            if (needSystem != null) needSystem.SatisfyNeed("Creativity", 25f);
        }
    }
}
