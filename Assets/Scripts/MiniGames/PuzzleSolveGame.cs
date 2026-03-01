using UnityEngine;
using System.Collections.Generic;

namespace EmersynBigDay.MiniGames
{
    /// <summary>
    /// Puzzle Solve: drag jigsaw pieces to correct positions on a grid.
    /// Multiple difficulty levels with more pieces. Timer-based scoring.
    /// Satisfies Creativity need.
    /// </summary>
    public class PuzzleSolveGame : MonoBehaviour
    {
        [Header("Settings")]
        public int GridSize = 3; // 3x3 = 9 pieces
        public float GameDuration = 60f;
        public float SnapDistance = 0.5f;

        [Header("Visuals")]
        public Sprite PuzzleImage;
        public GameObject PiecePrefab;
        public Transform PuzzleBoard;
        public Transform PieceSpawnArea;

        private List<PuzzlePiece> pieces = new List<PuzzlePiece>();
        private PuzzlePiece draggedPiece;
        private int piecesPlaced = 0;
        private int totalPieces;
        private float gameTimer;
        private int score = 0;
        private bool isActive = false;

        public void StartGame()
        {
            totalPieces = GridSize * GridSize;
            piecesPlaced = 0;
            gameTimer = GameDuration;
            score = 0;
            isActive = true;
            CreatePuzzle();
        }

        private void Update()
        {
            if (!isActive) return;
            gameTimer -= Time.deltaTime;
            if (gameTimer <= 0f) { EndGame(false); return; }
        }

        private void CreatePuzzle()
        {
            if (PiecePrefab == null || PuzzleBoard == null) return;

            float pieceSize = 1f / GridSize;

            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    Vector3 correctPos = PuzzleBoard.position + new Vector3(
                        (col - GridSize / 2f + 0.5f) * pieceSize * 3f,
                        (row - GridSize / 2f + 0.5f) * pieceSize * 3f,
                        0f
                    );

                    // Spawn piece at random position in spawn area
                    Vector3 spawnPos = PieceSpawnArea != null
                        ? PieceSpawnArea.position + new Vector3(
                            UnityEngine.Random.Range(-2f, 2f),
                            UnityEngine.Random.Range(-2f, 2f), 0f)
                        : correctPos + new Vector3(
                            UnityEngine.Random.Range(-3f, 3f),
                            UnityEngine.Random.Range(-3f, 3f), 0f);

                    GameObject pieceObj = Instantiate(PiecePrefab, spawnPos, Quaternion.identity);
                    pieceObj.transform.localScale = Vector3.one * pieceSize * 3f;

                    pieces.Add(new PuzzlePiece
                    {
                        PieceObject = pieceObj,
                        CorrectPosition = correctPos,
                        IsPlaced = false,
                        Row = row,
                        Col = col
                    });
                }
            }
        }

        public void OnPieceDragStart(GameObject pieceObj)
        {
            if (!isActive) return;
            foreach (var piece in pieces)
            {
                if (piece.PieceObject == pieceObj && !piece.IsPlaced)
                {
                    draggedPiece = piece;
                    break;
                }
            }
        }

        public void OnPieceDrag(Vector3 worldPosition)
        {
            if (!isActive || draggedPiece == null) return;
            draggedPiece.PieceObject.transform.position = worldPosition;
        }

        public void OnPieceDragEnd()
        {
            if (!isActive || draggedPiece == null) return;

            float dist = Vector3.Distance(draggedPiece.PieceObject.transform.position, draggedPiece.CorrectPosition);
            if (dist < SnapDistance)
            {
                // Snap to correct position
                draggedPiece.PieceObject.transform.position = draggedPiece.CorrectPosition;
                draggedPiece.IsPlaced = true;
                piecesPlaced++;
                score += Mathf.CeilToInt(20 * (gameTimer / GameDuration));

                if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.PlaySFX("coin");
                if (Particles.ParticleManager.Instance != null)
                    Particles.ParticleManager.Instance.SpawnSparkles(draggedPiece.CorrectPosition);

                if (piecesPlaced >= totalPieces) EndGame(true);
            }

            draggedPiece = null;
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
            if (needSystem != null) needSystem.SatisfyNeed("Creativity", 25f);
        }

        public class PuzzlePiece
        {
            public GameObject PieceObject;
            public Vector3 CorrectPosition;
            public bool IsPlaced;
            public int Row;
            public int Col;
        }
    }
}
