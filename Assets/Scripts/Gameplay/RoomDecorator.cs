using UnityEngine;
using System;
using System.Collections.Generic;

namespace EmersynBigDay.Gameplay
{
    /// <summary>
    /// Enhancement #8: Grid-based room decoration with drag-and-drop placement.
    /// Like Sims FreePlay's build mode and Animal Crossing's room decoration.
    /// </summary>
    public class RoomDecorator : MonoBehaviour
    {
        public static RoomDecorator Instance { get; private set; }

        [Header("Grid Settings")]
        public float GridSize = 0.5f;
        public int GridWidth = 20;
        public int GridDepth = 16;
        public float PlacementHeight = 0f;

        [Header("Decoration Mode")]
        public bool IsDecorating;
        public GameObject SelectedFurniture;

        private Dictionary<Vector2Int, PlacedFurniture> placedItems = new Dictionary<Vector2Int, PlacedFurniture>();
        private GameObject previewObject;
        private Material previewMaterial;
        private Camera mainCamera;
        private int totalFurniturePlaced;

        public event Action<PlacedFurniture> OnFurniturePlaced;
        public event Action<PlacedFurniture> OnFurnitureRemoved;
        public event Action OnDecorationModeEntered;
        public event Action OnDecorationModeExited;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (!IsDecorating || SelectedFurniture == null) return;
            UpdatePreview();
            HandlePlacement();
        }

        public void EnterDecorationMode()
        {
            IsDecorating = true;
            OnDecorationModeEntered?.Invoke();
        }

        public void ExitDecorationMode()
        {
            IsDecorating = false;
            ClearPreview();
            OnDecorationModeExited?.Invoke();
        }

        public void SelectFurniture(GameObject furniturePrefab)
        {
            SelectedFurniture = furniturePrefab;
            CreatePreview();
        }

        private void CreatePreview()
        {
            ClearPreview();
            if (SelectedFurniture == null) return;

            previewObject = Instantiate(SelectedFurniture);
            previewObject.name = "FurniturePreview";

            // Make preview transparent
            var renderers = previewObject.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                foreach (var mat in r.materials)
                {
                    Color c = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
                    c.a = 0.5f;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                    else mat.color = c;

                    // Set transparent mode
                    if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1);
                    mat.SetFloat("_Mode", 3);
                    mat.renderQueue = 3000;
                }
            }

            // Disable colliders on preview
            var colliders = previewObject.GetComponentsInChildren<Collider>();
            foreach (var c in colliders) c.enabled = false;
        }

        private void ClearPreview()
        {
            if (previewObject != null) Destroy(previewObject);
            previewObject = null;
        }

        private void UpdatePreview()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (previewObject == null || mainCamera == null) return;

            // Raycast from touch/mouse to ground
            Vector3 inputPos = Input.touchCount > 0
                ? (Vector3)Input.GetTouch(0).position
                : Input.mousePosition;

            Ray ray = mainCamera.ScreenPointToRay(inputPos);
            Plane groundPlane = new Plane(Vector3.up, Vector3.up * PlacementHeight);

            if (groundPlane.Raycast(ray, out float dist))
            {
                Vector3 hitPoint = ray.GetPoint(dist);
                Vector2Int gridPos = WorldToGrid(hitPoint);
                Vector3 snappedPos = GridToWorld(gridPos);
                previewObject.transform.position = snappedPos;

                // Color based on validity
                bool valid = CanPlace(gridPos);
                var renderers = previewObject.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    foreach (var mat in r.materials)
                    {
                        Color tint = valid ? new Color(0.5f, 1f, 0.5f, 0.5f) : new Color(1f, 0.3f, 0.3f, 0.5f);
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
                        else mat.color = tint;
                    }
                }
            }
        }

        private void HandlePlacement()
        {
            bool tap = (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended) ||
                       Input.GetMouseButtonUp(0);

            if (!tap || previewObject == null) return;

            Vector2Int gridPos = WorldToGrid(previewObject.transform.position);
            if (CanPlace(gridPos))
            {
                PlaceFurniture(gridPos);
            }
        }

        private void PlaceFurniture(Vector2Int gridPos)
        {
            if (SelectedFurniture == null) return;

            Vector3 worldPos = GridToWorld(gridPos);
            var instance = Instantiate(SelectedFurniture, worldPos, Quaternion.identity);
            instance.name = "Furniture_Placed";

            var placed = new PlacedFurniture
            {
                GridPosition = gridPos,
                WorldPosition = worldPos,
                PrefabName = SelectedFurniture.name,
                Instance = instance,
                Rotation = 0f
            };

            placedItems[gridPos] = placed;
            totalFurniturePlaced++;

            // Effects
            if (Visual.ProceduralParticles.Instance != null)
                Visual.ProceduralParticles.Instance.SpawnMagicPoof(worldPos);
            if (Audio.AudioManager.Instance != null)
                Audio.AudioManager.Instance.PlaySFX("tap");

            // Quest progress
            if (QuestSystem.Instance != null)
                QuestSystem.Instance.ReportProgress("place_furniture");

            OnFurniturePlaced?.Invoke(placed);
        }

        public bool RemoveFurniture(Vector2Int gridPos)
        {
            if (!placedItems.ContainsKey(gridPos)) return false;
            var placed = placedItems[gridPos];
            if (placed.Instance != null) Destroy(placed.Instance);
            placedItems.Remove(gridPos);
            OnFurnitureRemoved?.Invoke(placed);
            return true;
        }

        public void RotateFurniture(Vector2Int gridPos)
        {
            if (!placedItems.ContainsKey(gridPos)) return;
            var placed = placedItems[gridPos];
            placed.Rotation = (placed.Rotation + 90f) % 360f;
            if (placed.Instance != null)
                placed.Instance.transform.rotation = Quaternion.Euler(0, placed.Rotation, 0);
        }

        private bool CanPlace(Vector2Int gridPos)
        {
            if (gridPos.x < 0 || gridPos.x >= GridWidth || gridPos.y < 0 || gridPos.y >= GridDepth)
                return false;
            return !placedItems.ContainsKey(gridPos);
        }

        private Vector2Int WorldToGrid(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPos.x / GridSize),
                Mathf.RoundToInt(worldPos.z / GridSize)
            );
        }

        private Vector3 GridToWorld(Vector2Int gridPos)
        {
            return new Vector3(gridPos.x * GridSize, PlacementHeight, gridPos.y * GridSize);
        }

        public int GetPlacedCount() => placedItems.Count;
    }

    [Serializable]
    public class PlacedFurniture
    {
        public Vector2Int GridPosition;
        public Vector3 WorldPosition;
        public string PrefabName;
        public GameObject Instance;
        public float Rotation;
    }
}
