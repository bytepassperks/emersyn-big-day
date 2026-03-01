using UnityEngine;
using System;
using System.Collections.Generic;

namespace EmersynBigDay.MiniGames
{
    /// <summary>
    /// Manages all 15+ mini-games: launching, scoring, rewards, and transitions.
    /// Each mini-game is a self-contained component that reports results back here.
    /// </summary>
    public class MiniGameManager : MonoBehaviour
    {
        public static MiniGameManager Instance { get; private set; }

        [Header("Mini-Game Registry")]
        public MiniGameData[] AvailableGames;

        [Header("State")]
        public bool IsPlaying = false;
        public MiniGameData CurrentGame;
        public int CurrentScore = 0;
        public int HighScore = 0;
        public float GameTimer = 0f;

        [Header("Reward Settings")]
        public int BaseCoinsPerWin = 10;
        public int BaseXPPerGame = 5;
        public int BonusCoinsPerStar = 5;

        public event Action<MiniGameData> OnGameStarted;
        public event Action<MiniGameResult> OnGameCompleted;
        public event Action OnGameCancelled;

        private Dictionary<string, int> highScores = new Dictionary<string, int>();
        private Dictionary<string, int> playCounts = new Dictionary<string, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void StartGame(string gameName)
        {
            if (IsPlaying || AvailableGames == null) return;

            MiniGameData game = null;
            foreach (var g in AvailableGames)
            {
                if (g.GameName == gameName) { game = g; break; }
            }

            if (game == null) { Debug.LogWarning($"Mini-game '{gameName}' not found"); return; }

            CurrentGame = game;
            CurrentScore = 0;
            GameTimer = game.TimeLimit;
            IsPlaying = true;

            // Track play count
            if (!playCounts.ContainsKey(gameName)) playCounts[gameName] = 0;
            playCounts[gameName]++;

            // Set game state
            if (Core.GameManager.Instance != null)
                Core.GameManager.Instance.CurrentState = Core.GameState.MiniGame;

            OnGameStarted?.Invoke(game);
        }

        public void AddScore(int points)
        {
            if (!IsPlaying) return;
            CurrentScore += points;
        }

        public void CompleteGame(bool won)
        {
            if (!IsPlaying) return;
            IsPlaying = false;

            // Calculate stars (1-3 based on score thresholds)
            int stars = CalculateStars();

            // Update high score
            string gameName = CurrentGame.GameName;
            if (!highScores.ContainsKey(gameName)) highScores[gameName] = 0;
            if (CurrentScore > highScores[gameName]) highScores[gameName] = CurrentScore;

            // Calculate rewards
            int coins = won ? BaseCoinsPerWin + (stars * BonusCoinsPerStar) : BaseCoinsPerWin / 2;
            int xp = BaseXPPerGame + (stars * 2);

            // Apply rewards
            var gm = Core.GameManager.Instance;
            if (gm != null)
            {
                gm.AddCoins(coins);
                gm.AddXP(xp);
                if (stars >= 3) gm.Stars++;
                gm.CurrentState = Core.GameState.Playing;
            }

            // Satisfy Fun need
            var needSystem = FindFirstObjectByType<Core.NeedSystem>();
            if (needSystem != null)
            {
                needSystem.SatisfyNeed("Fun", 15f + stars * 5f);
            }

            var result = new MiniGameResult
            {
                GameName = gameName,
                Score = CurrentScore,
                HighScore = highScores[gameName],
                Stars = stars,
                Won = won,
                CoinsEarned = coins,
                XPEarned = xp
            };

            OnGameCompleted?.Invoke(result);

            // Show reward popup
            if (UI.UIManager.Instance != null)
            {
                UI.UIManager.Instance.ShowRewardPopup(
                    won ? "You Won!" : "Good Try!",
                    coins, stars >= 3 ? 1 : 0, xp
                );
            }

            // Particles
            if (Particles.ParticleManager.Instance != null)
            {
                if (won) Particles.ParticleManager.Instance.SpawnConfetti(Vector3.up * 3f);
                Particles.ParticleManager.Instance.SpawnStarBurst(Vector3.up * 2f);
            }

            // Audio
            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySFX(won ? "win" : "lose");
            }

            CurrentGame = null;
        }

        public void CancelGame()
        {
            IsPlaying = false;
            CurrentGame = null;
            if (Core.GameManager.Instance != null)
                Core.GameManager.Instance.CurrentState = Core.GameState.Playing;
            OnGameCancelled?.Invoke();
        }

        private int CalculateStars()
        {
            if (CurrentGame == null) return 0;
            float ratio = CurrentGame.MaxScore > 0 ? (float)CurrentScore / CurrentGame.MaxScore : 0f;
            if (ratio >= 0.9f) return 3;
            if (ratio >= 0.6f) return 2;
            if (ratio >= 0.3f) return 1;
            return 0;
        }

        private void Update()
        {
            if (!IsPlaying) return;

            if (CurrentGame != null && CurrentGame.HasTimeLimit)
            {
                GameTimer -= Time.deltaTime;
                if (GameTimer <= 0f)
                {
                    GameTimer = 0f;
                    CompleteGame(CurrentScore >= CurrentGame.WinThreshold);
                }
            }
        }

        public int GetHighScore(string gameName)
        {
            return highScores.ContainsKey(gameName) ? highScores[gameName] : 0;
        }

        public int GetPlayCount(string gameName)
        {
            return playCounts.ContainsKey(gameName) ? playCounts[gameName] : 0;
        }

        /// <summary>
        /// Get a random mini-game that hasn't been played recently.
        /// </summary>
        public MiniGameData GetRandomGame()
        {
            if (AvailableGames == null || AvailableGames.Length == 0) return null;
            return AvailableGames[UnityEngine.Random.Range(0, AvailableGames.Length)];
        }
    }

    [Serializable]
    public class MiniGameData
    {
        public string GameName;
        public string DisplayName;
        public string Description;
        public Sprite Icon;
        public GameObject GamePrefab;
        public MiniGameType Type;
        public int MaxScore = 100;
        public int WinThreshold = 50;
        public bool HasTimeLimit = true;
        public float TimeLimit = 30f;
        public int UnlockLevel = 1;
        public string[] RequiredNeeds; // Needs this game satisfies
    }

    [Serializable]
    public class MiniGameResult
    {
        public string GameName;
        public int Score;
        public int HighScore;
        public int Stars;
        public bool Won;
        public int CoinsEarned;
        public int XPEarned;
    }

    public enum MiniGameType
    {
        CookingChallenge,   // Kitchen: cook meals by following recipes
        DanceParty,         // Studio: rhythm game, tap to beat
        ArtStudio,          // Studio: draw/color pictures
        MemoryMatch,        // School: flip cards to find pairs
        BubblePop,          // Bathroom: pop bubbles in order
        GardenGrow,         // Garden: plant and grow flowers
        FashionShow,        // Bedroom: dress up and pose
        PetCare,            // Park: feed and play with pets
        MusicMaker,         // Studio: create simple melodies
        PuzzleSolve,        // School: jigsaw puzzles
        ShoppingSpree,      // Shop: buy items within budget
        RacingRun,          // Park: simple running game
        HideAndSeek,        // Any room: find hidden objects
        StarCatcher,        // Arcade: catch falling stars
        ColorMix            // Art: mix colors to match target
    }
}
