using UnityEngine;
using System.Collections.Generic;

namespace EmersynBigDay.MiniGames
{
    /// <summary>
    /// Memory Match: flip cards to find matching pairs. Grid sizes scale with difficulty.
    /// Satisfies Creativity need on completion.
    /// </summary>
    public class MemoryMatchGame : MonoBehaviour
    {
        [Header("Settings")]
        public int GridWidth = 4;
        public int GridHeight = 3;
        public float CardFlipTime = 0.3f;
        public float ShowTime = 1f;
        public float MaxTime = 60f;

        [Header("Visuals")]
        public GameObject CardPrefab;
        public Transform GridContainer;
        public Sprite[] CardFaces;
        public Sprite CardBack;

        private MemoryCard[] cards;
        private MemoryCard firstFlipped;
        private MemoryCard secondFlipped;
        private int matchesFound = 0;
        private int totalPairs;
        private int score = 0;
        private int moves = 0;
        private float gameTimer;
        private bool isActive = false;
        private bool isChecking = false;

        public void StartGame()
        {
            totalPairs = (GridWidth * GridHeight) / 2;
            matchesFound = 0;
            score = 0;
            moves = 0;
            gameTimer = MaxTime;
            isActive = true;

            SetupGrid();
        }

        private void Update()
        {
            if (!isActive) return;
            gameTimer -= Time.deltaTime;
            if (gameTimer <= 0f)
            {
                gameTimer = 0f;
                EndGame(false);
            }
        }

        private void SetupGrid()
        {
            if (CardPrefab == null || GridContainer == null || CardFaces == null) return;

            int totalCards = GridWidth * GridHeight;
            if (totalCards % 2 != 0) totalCards--;

            // Create pair list
            List<int> pairIds = new List<int>();
            for (int i = 0; i < totalCards / 2; i++)
            {
                int faceIdx = i % CardFaces.Length;
                pairIds.Add(faceIdx);
                pairIds.Add(faceIdx);
            }

            // Shuffle
            for (int i = pairIds.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                int temp = pairIds[i];
                pairIds[i] = pairIds[j];
                pairIds[j] = temp;
            }

            cards = new MemoryCard[totalCards];
            for (int i = 0; i < totalCards; i++)
            {
                int row = i / GridWidth;
                int col = i % GridWidth;
                Vector3 pos = new Vector3(col * 1.2f - (GridWidth * 0.6f), 0, row * 1.5f - (GridHeight * 0.75f));

                GameObject cardObj = Instantiate(CardPrefab, GridContainer);
                cardObj.transform.localPosition = pos;

                var card = new MemoryCard
                {
                    CardObject = cardObj,
                    PairId = pairIds[i],
                    FaceSprite = CardFaces[pairIds[i]],
                    IsFlipped = false,
                    IsMatched = false,
                    Index = i
                };
                cards[i] = card;
            }
        }

        public void OnCardTapped(int cardIndex)
        {
            if (!isActive || isChecking) return;
            if (cardIndex < 0 || cardIndex >= cards.Length) return;

            var card = cards[cardIndex];
            if (card.IsFlipped || card.IsMatched) return;

            FlipCard(card, true);
            moves++;

            if (firstFlipped == null)
            {
                firstFlipped = card;
            }
            else
            {
                secondFlipped = card;
                isChecking = true;
                StartCoroutine(CheckMatch());
            }
        }

        private System.Collections.IEnumerator CheckMatch()
        {
            yield return new WaitForSeconds(ShowTime);

            if (firstFlipped.PairId == secondFlipped.PairId)
            {
                // Match found!
                firstFlipped.IsMatched = true;
                secondFlipped.IsMatched = true;
                matchesFound++;
                score += Mathf.CeilToInt(100 * (gameTimer / MaxTime));

                if (Audio.AudioManager.Instance != null)
                    Audio.AudioManager.Instance.PlaySFX("coin");
                if (Particles.ParticleManager.Instance != null)
                    Particles.ParticleManager.Instance.SpawnSparkles(firstFlipped.CardObject.transform.position);

                if (matchesFound >= totalPairs)
                {
                    EndGame(true);
                }
            }
            else
            {
                // No match - flip back
                FlipCard(firstFlipped, false);
                FlipCard(secondFlipped, false);
            }

            firstFlipped = null;
            secondFlipped = null;
            isChecking = false;
        }

        private void FlipCard(MemoryCard card, bool faceUp)
        {
            card.IsFlipped = faceUp;
            // Visual flip animation would be handled by the card's own component
            var renderer = card.CardObject.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sprite = faceUp ? card.FaceSprite : CardBack;
            }
        }

        private void EndGame(bool won)
        {
            isActive = false;
            if (MiniGameManager.Instance != null)
            {
                MiniGameManager.Instance.AddScore(score);
                MiniGameManager.Instance.CompleteGame(won);
            }

            var needSystem = FindFirstObjectByType<Core.NeedSystem>();
            if (needSystem != null) needSystem.SatisfyNeed("Creativity", 20f);
        }
    }

    public class MemoryCard
    {
        public GameObject CardObject;
        public int PairId;
        public Sprite FaceSprite;
        public bool IsFlipped;
        public bool IsMatched;
        public int Index;
    }
}
