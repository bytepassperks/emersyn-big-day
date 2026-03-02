using UnityEngine;
using System;
using System.Collections.Generic;

namespace EmersynBigDay.Gameplay
{
    /// <summary>
    /// Enhancement #4: Mini-game collection launcher. Connects existing 14+ mini-games
    /// into a unified system with scoring, rewards, difficulty scaling, and unlock progression.
    /// Like My Talking Angela 2's game collection and Toca Life's activity system.
    /// </summary>
    public class MiniGameLauncher : MonoBehaviour
    {
        public static MiniGameLauncher Instance { get; private set; }

        private List<MiniGameEntry> allGames = new List<MiniGameEntry>();
        private MiniGameEntry currentGame;
        private int gamesPlayedToday;
        private int totalGamesPlayed;
        private int totalGamesWon;

        public event Action<MiniGameEntry> OnGameStarted;
        public event Action<MiniGameEntry, int, bool> OnGameCompleted; // game, score, won
        public event Action OnGameExited;

        public bool IsPlaying => currentGame != null;
        public int GamesPlayedToday => gamesPlayedToday;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeGames();
        }

        private void InitializeGames()
        {
            // Register all 14+ mini-games with their properties
            allGames.Add(new MiniGameEntry("art_studio", "Art Studio", "Draw and color pictures!",
                MiniGameCategory.Creative, "Fun", 25f, "Creativity", 20f, 0));

            allGames.Add(new MiniGameEntry("bubble_pop", "Bubble Pop", "Pop colorful bubbles!",
                MiniGameCategory.Action, "Fun", 30f, "", 0f, 0));

            allGames.Add(new MiniGameEntry("color_mix", "Color Mix", "Mix colors to match!",
                MiniGameCategory.Puzzle, "Creativity", 20f, "Fun", 15f, 2));

            allGames.Add(new MiniGameEntry("cooking", "Cooking Time", "Cook yummy meals!",
                MiniGameCategory.Creative, "Hunger", 35f, "Fun", 15f, 0));

            allGames.Add(new MiniGameEntry("dance_party", "Dance Party", "Dance to the beat!",
                MiniGameCategory.Action, "Fun", 30f, "Social", 20f, 0));

            allGames.Add(new MiniGameEntry("fashion_show", "Fashion Show", "Style the perfect outfit!",
                MiniGameCategory.Creative, "Fun", 20f, "Social", 15f, 3));

            allGames.Add(new MiniGameEntry("garden_grow", "Garden Grow", "Plant and grow flowers!",
                MiniGameCategory.Relaxing, "Comfort", 20f, "Creativity", 15f, 2));

            allGames.Add(new MiniGameEntry("hide_seek", "Hide & Seek", "Find hidden friends!",
                MiniGameCategory.Puzzle, "Fun", 25f, "Social", 25f, 0));

            allGames.Add(new MiniGameEntry("memory_match", "Memory Match", "Match the cards!",
                MiniGameCategory.Puzzle, "Fun", 20f, "", 0f, 0));

            allGames.Add(new MiniGameEntry("music_maker", "Music Maker", "Create melodies!",
                MiniGameCategory.Creative, "Creativity", 30f, "Fun", 20f, 4));

            allGames.Add(new MiniGameEntry("pet_care", "Pet Care", "Take care of your pets!",
                MiniGameCategory.Relaxing, "Social", 25f, "Comfort", 15f, 0));

            allGames.Add(new MiniGameEntry("puzzle_solve", "Puzzle Solve", "Complete the jigsaw!",
                MiniGameCategory.Puzzle, "Fun", 20f, "Creativity", 15f, 3));

            allGames.Add(new MiniGameEntry("racing_run", "Racing Run", "Race to the finish!",
                MiniGameCategory.Action, "Fun", 30f, "Energy", -10f, 5));

            allGames.Add(new MiniGameEntry("shopping_spree", "Shopping Spree", "Shop for the best deals!",
                MiniGameCategory.Relaxing, "Fun", 20f, "Social", 10f, 0));

            allGames.Add(new MiniGameEntry("star_catcher", "Star Catcher", "Catch falling stars!",
                MiniGameCategory.Action, "Fun", 25f, "", 0f, 2));

            // New games from enhancement
            allGames.Add(new MiniGameEntry("karate_chop", "Karate Chop", "Break boards like Emersyn!",
                MiniGameCategory.Action, "Fun", 30f, "Energy", -15f, 3));

            allGames.Add(new MiniGameEntry("story_time", "Story Time", "Create stories together!",
                MiniGameCategory.Creative, "Creativity", 30f, "Social", 20f, 5));

            allGames.Add(new MiniGameEntry("treasure_hunt", "Treasure Hunt", "Find hidden treasures!",
                MiniGameCategory.Puzzle, "Fun", 25f, "Creativity", 10f, 4));
        }

        public void LaunchGame(string gameId)
        {
            var game = allGames.Find(g => g.GameId == gameId);
            if (game == null) return;

            var gm = Core.GameManager.Instance;
            if (gm != null && gm.Level < game.UnlockLevel) return;

            currentGame = game;
            gamesPlayedToday++;
            totalGamesPlayed++;

            OnGameStarted?.Invoke(game);

            // Notify quest system
            if (QuestSystem.Instance != null)
                QuestSystem.Instance.ReportProgress("play_minigame");

            if (Audio.AudioManager.Instance != null)
                Audio.AudioManager.Instance.PlaySFX("popup_open");

            Debug.Log($"[MiniGameLauncher] Starting: {game.DisplayName}");
        }

        public void CompleteGame(int score, int stars, bool won)
        {
            if (currentGame == null) return;
            var game = currentGame;

            if (won) totalGamesWon++;

            // Apply need effects
            var needSystem = FindObjectOfType<Core.NeedSystem>();
            if (needSystem != null)
            {
                if (!string.IsNullOrEmpty(game.PrimaryNeed))
                    needSystem.SatisfyNeed(game.PrimaryNeed, game.PrimaryNeedDelta);
                if (!string.IsNullOrEmpty(game.SecondaryNeed))
                    needSystem.SatisfyNeed(game.SecondaryNeed, game.SecondaryNeedDelta);
            }

            // Grant rewards
            if (Core.RewardSystem.Instance != null)
                Core.RewardSystem.Instance.GrantMiniGameReward(game.GameId, score, stars, won);

            // Effects
            if (won)
            {
                if (Visual.ProceduralParticles.Instance != null)
                    Visual.ProceduralParticles.Instance.SpawnConfetti(Vector3.up * 3f);
            }

            // Quest progress
            if (QuestSystem.Instance != null && won)
                QuestSystem.Instance.ReportProgress("win_minigame");

            // Achievement tracking
            if (Core.AchievementSystem.Instance != null)
            {
                Core.AchievementSystem.Instance.AddProgress("minigame_played");
                if (won) Core.AchievementSystem.Instance.AddProgress("minigame_won");
                if (stars >= 3) Core.AchievementSystem.Instance.AddProgress("minigame_perfect");
            }

            OnGameCompleted?.Invoke(game, score, won);
            currentGame = null;
        }

        public void ExitGame()
        {
            currentGame = null;
            OnGameExited?.Invoke();
        }

        public List<MiniGameEntry> GetGamesByCategory(MiniGameCategory category)
        {
            return allGames.FindAll(g => g.Category == category);
        }

        public List<MiniGameEntry> GetUnlockedGames()
        {
            var gm = Core.GameManager.Instance;
            int level = gm != null ? gm.Level : 1;
            return allGames.FindAll(g => level >= g.UnlockLevel);
        }

        public MiniGameEntry GetRandomGame()
        {
            var unlocked = GetUnlockedGames();
            return unlocked.Count > 0 ? unlocked[UnityEngine.Random.Range(0, unlocked.Count)] : null;
        }
    }

    [Serializable]
    public class MiniGameEntry
    {
        public string GameId;
        public string DisplayName;
        public string Description;
        public MiniGameCategory Category;
        public string PrimaryNeed;
        public float PrimaryNeedDelta;
        public string SecondaryNeed;
        public float SecondaryNeedDelta;
        public int UnlockLevel;
        public int HighScore;
        public int TimesPlayed;

        public MiniGameEntry(string id, string name, string desc, MiniGameCategory cat,
            string primaryNeed, float primaryDelta, string secondaryNeed, float secondaryDelta, int unlockLevel)
        {
            GameId = id; DisplayName = name; Description = desc; Category = cat;
            PrimaryNeed = primaryNeed; PrimaryNeedDelta = primaryDelta;
            SecondaryNeed = secondaryNeed; SecondaryNeedDelta = secondaryDelta;
            UnlockLevel = unlockLevel;
        }
    }

    public enum MiniGameCategory { Action, Puzzle, Creative, Relaxing }
}
