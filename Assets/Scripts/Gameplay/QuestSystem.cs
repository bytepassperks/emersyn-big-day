using UnityEngine;
using System;
using System.Collections.Generic;

namespace EmersynBigDay.Gameplay
{
    /// <summary>
    /// Enhancement #5: Daily Routines & Quest System.
    /// ScriptableObject-style quest system with 3-5 active quests, daily reset.
    /// Like Sims FreePlay's quest chains and Animal Crossing's daily tasks.
    /// </summary>
    public class QuestSystem : MonoBehaviour
    {
        public static QuestSystem Instance { get; private set; }

        [Header("Quest Settings")]
        public int MaxActiveQuests = 5;
        public float QuestRefreshInterval = 300f; // 5 min

        private List<Quest> activeQuests = new List<Quest>();
        private List<Quest> completedQuests = new List<Quest>();
        private List<Quest> allQuestTemplates = new List<Quest>();
        private float refreshTimer;
        private int totalQuestsCompleted;

        public event Action<Quest> OnQuestStarted;
        public event Action<Quest> OnQuestCompleted;
        public event Action<Quest> OnQuestProgress;

        private void OnDestroy()
        {
            OnQuestStarted = null;
            OnQuestCompleted = null;
            OnQuestProgress = null;
        }

        public List<Quest> ActiveQuests => activeQuests;
        public int TotalCompleted => totalQuestsCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeQuestTemplates();
        }

        private void Start()
        {
            RefreshDailyQuests();
        }

        private void Update()
        {
            refreshTimer += Time.deltaTime;
            if (refreshTimer >= QuestRefreshInterval)
            {
                refreshTimer = 0f;
                RefreshDailyQuests();
            }
        }

        private void InitializeQuestTemplates()
        {
            // Daily routine quests
            allQuestTemplates.Add(new Quest("morning_routine", "Morning Routine",
                "Feed Emersyn breakfast and brush teeth", QuestType.Daily,
                new[] { new QuestObjective("feed", "Feed Emersyn", 1), new QuestObjective("clean", "Brush Teeth", 1) },
                15, 3, 0));

            allQuestTemplates.Add(new Quest("playtime", "Playtime!",
                "Play 2 mini-games with friends", QuestType.Daily,
                new[] { new QuestObjective("play_minigame", "Play Mini-Games", 2) },
                20, 5, 0));

            allQuestTemplates.Add(new Quest("social_butterfly", "Social Butterfly",
                "Visit 3 different rooms with a friend", QuestType.Daily,
                new[] { new QuestObjective("visit_room", "Visit Rooms", 3) },
                15, 4, 0));

            allQuestTemplates.Add(new Quest("pet_lover", "Pet Lover",
                "Pet and feed all 3 pets", QuestType.Daily,
                new[] { new QuestObjective("pet_care", "Care for Pets", 3) },
                20, 5, 1));

            allQuestTemplates.Add(new Quest("fashion_star", "Fashion Star",
                "Change Emersyn's outfit", QuestType.Daily,
                new[] { new QuestObjective("change_outfit", "Change Outfit", 1) },
                10, 3, 0));

            allQuestTemplates.Add(new Quest("chef_emersyn", "Chef Emersyn",
                "Cook a meal in the kitchen", QuestType.Daily,
                new[] { new QuestObjective("cook", "Cook a Meal", 1) },
                15, 4, 0));

            allQuestTemplates.Add(new Quest("bedtime_story", "Bedtime Story",
                "Put Emersyn to bed when Energy is low", QuestType.Daily,
                new[] { new QuestObjective("sleep", "Go to Sleep", 1) },
                10, 3, 0));

            allQuestTemplates.Add(new Quest("creative_spark", "Creative Spark",
                "Do 2 creative activities (art, music, dance)", QuestType.Daily,
                new[] { new QuestObjective("create", "Creative Activities", 2) },
                20, 5, 0));

            allQuestTemplates.Add(new Quest("explorer", "Room Explorer",
                "Visit every room in the house", QuestType.Weekly,
                new[] { new QuestObjective("visit_all_rooms", "Visit All Rooms", 9) },
                50, 10, 1));

            allQuestTemplates.Add(new Quest("shopaholic", "Shopaholic",
                "Buy 3 items from the shop", QuestType.Weekly,
                new[] { new QuestObjective("buy_item", "Buy Items", 3) },
                30, 8, 1));

            allQuestTemplates.Add(new Quest("minigame_master", "Mini-Game Master",
                "Win 5 mini-games", QuestType.Weekly,
                new[] { new QuestObjective("win_minigame", "Win Mini-Games", 5) },
                50, 15, 2));

            allQuestTemplates.Add(new Quest("garden_guru", "Garden Guru",
                "Water plants and grow flowers", QuestType.Daily,
                new[] { new QuestObjective("garden", "Garden Activities", 2) },
                15, 4, 0));

            allQuestTemplates.Add(new Quest("dance_party", "Dance Party",
                "Dance in the studio with friends", QuestType.Daily,
                new[] { new QuestObjective("dance", "Dance with Friends", 1) },
                15, 4, 0));

            allQuestTemplates.Add(new Quest("bath_time", "Bath Time",
                "Give Emersyn a bath when Hygiene is low", QuestType.Daily,
                new[] { new QuestObjective("bathe", "Take a Bath", 1) },
                10, 3, 0));

            allQuestTemplates.Add(new Quest("arcade_hero", "Arcade Hero",
                "Score high in arcade games", QuestType.Daily,
                new[] { new QuestObjective("arcade", "Play Arcade Games", 2) },
                20, 5, 0));
        }

        public void RefreshDailyQuests()
        {
            // Remove expired daily quests
            activeQuests.RemoveAll(q => q.Type == QuestType.Daily && q.IsComplete);

            // Fill up to MaxActiveQuests
            var available = allQuestTemplates.FindAll(t =>
                !activeQuests.Exists(a => a.QuestId == t.QuestId) &&
                !completedQuests.Exists(c => c.QuestId == t.QuestId && c.Type == QuestType.Daily));

            while (activeQuests.Count < MaxActiveQuests && available.Count > 0)
            {
                int idx = UnityEngine.Random.Range(0, available.Count);
                var quest = available[idx].Clone();
                activeQuests.Add(quest);
                available.RemoveAt(idx);
                OnQuestStarted?.Invoke(quest);
            }
        }

        public void ReportProgress(string objectiveType, int amount = 1)
        {
            foreach (var quest in activeQuests)
            {
                if (quest.IsComplete) continue;
                foreach (var obj in quest.Objectives)
                {
                    if (obj.ObjectiveType == objectiveType && !obj.IsComplete)
                    {
                        obj.CurrentProgress = Mathf.Min(obj.CurrentProgress + amount, obj.TargetProgress);
                        OnQuestProgress?.Invoke(quest);

                        if (quest.IsComplete)
                        {
                            CompleteQuest(quest);
                        }
                        break;
                    }
                }
            }
        }

        private void CompleteQuest(Quest quest)
        {
            totalQuestsCompleted++;
            completedQuests.Add(quest);

            // Grant rewards
            var gm = Core.GameManager.Instance;
            if (gm != null)
            {
                gm.AddCoins(quest.RewardCoins);
                gm.AddXP(quest.RewardXP);
                gm.Stars += quest.RewardStars;
            }

            // Effects
            if (Visual.ProceduralParticles.Instance != null)
                Visual.ProceduralParticles.Instance.SpawnConfetti(Vector3.up * 3f);
            if (Audio.AudioManager.Instance != null)
                Audio.AudioManager.Instance.PlaySFX("achievement");

            OnQuestCompleted?.Invoke(quest);

            // Achievement tracking
            if (Core.AchievementSystem.Instance != null)
                Core.AchievementSystem.Instance.AddProgress("quest_complete");

            Debug.Log($"[QuestSystem] Quest completed: {quest.DisplayName} (+{quest.RewardCoins} coins, +{quest.RewardXP} XP)");
        }

        public float GetQuestProgress(string questId)
        {
            var quest = activeQuests.Find(q => q.QuestId == questId);
            return quest?.Progress ?? 0f;
        }
    }

    [Serializable]
    public class Quest
    {
        public string QuestId;
        public string DisplayName;
        public string Description;
        public QuestType Type;
        public QuestObjective[] Objectives;
        public int RewardCoins;
        public int RewardXP;
        public int RewardStars;
        public bool IsComplete
        {
            get
            {
                if (Objectives == null) return false;
                foreach (var obj in Objectives)
                    if (!obj.IsComplete) return false;
                return true;
            }
        }
        public float Progress
        {
            get
            {
                if (Objectives == null || Objectives.Length == 0) return 0f;
                float total = 0f;
                foreach (var obj in Objectives)
                    total += obj.NormalizedProgress;
                return total / Objectives.Length;
            }
        }

        public Quest(string id, string name, string desc, QuestType type,
            QuestObjective[] objectives, int coins, int xp, int stars)
        {
            QuestId = id; DisplayName = name; Description = desc; Type = type;
            Objectives = objectives; RewardCoins = coins; RewardXP = xp; RewardStars = stars;
        }

        public Quest Clone()
        {
            var objectives = new QuestObjective[Objectives.Length];
            for (int i = 0; i < Objectives.Length; i++)
                objectives[i] = Objectives[i].Clone();
            return new Quest(QuestId, DisplayName, Description, Type, objectives, RewardCoins, RewardXP, RewardStars);
        }
    }

    [Serializable]
    public class QuestObjective
    {
        public string ObjectiveType;
        public string Description;
        public int TargetProgress;
        public int CurrentProgress;
        public bool IsComplete => CurrentProgress >= TargetProgress;
        public float NormalizedProgress => TargetProgress > 0 ? (float)CurrentProgress / TargetProgress : 0f;

        public QuestObjective(string type, string desc, int target)
        {
            ObjectiveType = type; Description = desc; TargetProgress = target; CurrentProgress = 0;
        }

        public QuestObjective Clone()
        {
            return new QuestObjective(ObjectiveType, Description, TargetProgress) { CurrentProgress = 0 };
        }
    }

    public enum QuestType { Daily, Weekly, Story, Special }
}
