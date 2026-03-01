using UnityEngine;
using System.Collections.Generic;

namespace EmersynBigDay.MiniGames
{
    /// <summary>
    /// Art Studio: draw and color pictures using touch input.
    /// Multiple brush sizes, colors, stamps, and stickers.
    /// Satisfies Creativity need.
    /// </summary>
    public class ArtStudioGame : MonoBehaviour
    {
        [Header("Settings")]
        public int CanvasWidth = 512;
        public int CanvasHeight = 512;
        public float BrushSize = 5f;
        public Color BrushColor = Color.red;

        [Header("Tools")]
        public Color[] ColorPalette;
        public float[] BrushSizes;
        public Sprite[] Stamps;

        [Header("UI")]
        public UnityEngine.UI.RawImage CanvasDisplay;
        public UnityEngine.UI.Button ClearButton;
        public UnityEngine.UI.Button SaveButton;

        private Texture2D canvas;
        private Vector2 lastDrawPos;
        private bool isDrawing = false;
        private bool isActive = false;
        private int strokeCount = 0;
        private int score = 0;

        public void StartGame()
        {
            canvas = new Texture2D(CanvasWidth, CanvasHeight);
            Color[] clearPixels = new Color[CanvasWidth * CanvasHeight];
            for (int i = 0; i < clearPixels.Length; i++) clearPixels[i] = Color.white;
            canvas.SetPixels(clearPixels);
            canvas.Apply();

            if (CanvasDisplay != null) CanvasDisplay.texture = canvas;
            if (ClearButton != null) ClearButton.onClick.AddListener(ClearCanvas);
            if (SaveButton != null) SaveButton.onClick.AddListener(FinishArt);

            strokeCount = 0;
            score = 0;
            isActive = true;
        }

        private void Update()
        {
            if (!isActive) return;

#if UNITY_EDITOR || UNITY_STANDALONE
            if (UnityEngine.Input.GetMouseButtonDown(0)) { isDrawing = true; lastDrawPos = GetCanvasPosition(UnityEngine.Input.mousePosition); }
            if (UnityEngine.Input.GetMouseButtonUp(0)) { isDrawing = false; strokeCount++; }
            if (isDrawing) DrawAtPosition(GetCanvasPosition(UnityEngine.Input.mousePosition));
#else
            if (UnityEngine.Input.touchCount > 0)
            {
                Touch touch = UnityEngine.Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began) { isDrawing = true; lastDrawPos = GetCanvasPosition(touch.position); }
                if (touch.phase == TouchPhase.Ended) { isDrawing = false; strokeCount++; }
                if (isDrawing) DrawAtPosition(GetCanvasPosition(touch.position));
            }
#endif
        }

        private Vector2 GetCanvasPosition(Vector2 screenPos)
        {
            if (CanvasDisplay == null) return Vector2.zero;
            RectTransform rt = CanvasDisplay.rectTransform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPos, null, out Vector2 localPoint))
            {
                float x = (localPoint.x / rt.rect.width + 0.5f) * CanvasWidth;
                float y = (localPoint.y / rt.rect.height + 0.5f) * CanvasHeight;
                return new Vector2(x, y);
            }
            return Vector2.zero;
        }

        private void DrawAtPosition(Vector2 pos)
        {
            // Draw line from last position to current
            float distance = Vector2.Distance(lastDrawPos, pos);
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / (BrushSize * 0.5f)));

            for (int s = 0; s <= steps; s++)
            {
                float t = steps > 0 ? (float)s / steps : 0f;
                Vector2 p = Vector2.Lerp(lastDrawPos, pos, t);

                int radius = Mathf.CeilToInt(BrushSize);
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (dx * dx + dy * dy <= radius * radius)
                        {
                            int px = Mathf.Clamp((int)p.x + dx, 0, CanvasWidth - 1);
                            int py = Mathf.Clamp((int)p.y + dy, 0, CanvasHeight - 1);
                            canvas.SetPixel(px, py, BrushColor);
                        }
                    }
                }
            }

            canvas.Apply();
            lastDrawPos = pos;
        }

        public void SetColor(int colorIndex)
        {
            if (ColorPalette != null && colorIndex >= 0 && colorIndex < ColorPalette.Length)
                BrushColor = ColorPalette[colorIndex];
        }

        public void SetBrushSize(int sizeIndex)
        {
            if (BrushSizes != null && sizeIndex >= 0 && sizeIndex < BrushSizes.Length)
                BrushSize = BrushSizes[sizeIndex];
        }

        public void ClearCanvas()
        {
            if (canvas == null) return;
            Color[] clearPixels = new Color[CanvasWidth * CanvasHeight];
            for (int i = 0; i < clearPixels.Length; i++) clearPixels[i] = Color.white;
            canvas.SetPixels(clearPixels);
            canvas.Apply();
            strokeCount = 0;
        }

        public void PlaceStamp(int stampIndex, Vector2 position)
        {
            if (Stamps == null || stampIndex < 0 || stampIndex >= Stamps.Length) return;
            // Blit stamp sprite onto canvas
            Sprite stamp = Stamps[stampIndex];
            if (stamp == null || stamp.texture == null) return;

            Texture2D stampTex = stamp.texture;
            int sx = (int)position.x - stampTex.width / 2;
            int sy = (int)position.y - stampTex.height / 2;

            for (int x = 0; x < stampTex.width; x++)
            {
                for (int y = 0; y < stampTex.height; y++)
                {
                    Color sc = stampTex.GetPixel(x, y);
                    if (sc.a < 0.1f) continue;
                    int px = Mathf.Clamp(sx + x, 0, CanvasWidth - 1);
                    int py = Mathf.Clamp(sy + y, 0, CanvasHeight - 1);
                    canvas.SetPixel(px, py, sc);
                }
            }
            canvas.Apply();
            strokeCount++;
        }

        public void FinishArt()
        {
            isActive = false;

            // Score based on effort (strokes) and canvas coverage
            int coloredPixels = 0;
            Color[] pixels = canvas.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i] != Color.white) coloredPixels++;
            }

            float coverage = (float)coloredPixels / pixels.Length;
            score = Mathf.CeilToInt(coverage * 50f) + Mathf.Min(strokeCount * 2, 50);

            if (MiniGameManager.Instance != null)
            {
                MiniGameManager.Instance.AddScore(score);
                MiniGameManager.Instance.CompleteGame(score >= 30);
            }

            if (Particles.ParticleManager.Instance != null)
                Particles.ParticleManager.Instance.SpawnConfetti(Vector3.up * 2f);

            var needSystem = FindFirstObjectByType<Core.NeedSystem>();
            if (needSystem != null) needSystem.SatisfyNeed("Creativity", 30f);
        }

        private void OnDestroy()
        {
            if (canvas != null) Destroy(canvas);
        }
    }
}
