using UnityEngine;
using System;

namespace EmersynBigDay.InputSystem
{
    /// <summary>
    /// Handles all touch input: tap, double-tap, long press, drag, pinch-to-zoom,
    /// swipe, and multi-touch gestures. Translates raw input into game actions.
    /// Works with both touch (mobile) and mouse (editor testing).
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [Header("Settings")]
        public float TapThreshold = 0.3f;
        public float DoubleTapThreshold = 0.4f;
        public float LongPressThreshold = 0.8f;
        public float DragThreshold = 10f;
        public float SwipeThreshold = 50f;
        public float PinchSensitivity = 0.01f;

        [Header("Raycasting")]
        public Camera MainCamera;
        public LayerMask InteractableLayer;
        public float RaycastDistance = 100f;

        // Touch state
        private Vector2 touchStartPos;
        private float touchStartTime;
        private float lastTapTime;
        private bool isDragging;
        private bool isLongPressing;
        private Vector2 lastDragPos;

        // Pinch state
        private float initialPinchDistance;
        private float currentPinchScale;

        // Events
        public event Action<Vector3> OnTap;
        public event Action<Vector3> OnDoubleTap;
        public event Action<Vector3> OnLongPress;
        public event Action<Vector2> OnDragStart;
        public event Action<Vector2, Vector2> OnDrag;
        public event Action<Vector2> OnDragEnd;
        public event Action<SwipeDirection> OnSwipe;
        public event Action<float> OnPinchZoom;
        public event Action<GameObject> OnObjectTapped;
        public event Action<GameObject, Vector3> OnObjectDragged;

        public enum SwipeDirection { Up, Down, Left, Right }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (MainCamera == null) MainCamera = Camera.main;
        }

        private void Update()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            HandleMouseInput();
#else
            HandleTouchInput();
#endif
        }

        // --- MOUSE INPUT (Editor/Desktop) ---
        private void HandleMouseInput()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                touchStartPos = UnityEngine.Input.mousePosition;
                touchStartTime = Time.time;
                isDragging = false;
                isLongPressing = false;
                lastDragPos = touchStartPos;
            }
            else if (UnityEngine.Input.GetMouseButton(0))
            {
                Vector2 currentPos = UnityEngine.Input.mousePosition;
                float distance = Vector2.Distance(touchStartPos, currentPos);
                float holdTime = Time.time - touchStartTime;

                if (!isDragging && distance > DragThreshold)
                {
                    isDragging = true;
                    OnDragStart?.Invoke(touchStartPos);
                }

                if (isDragging)
                {
                    Vector2 delta = currentPos - lastDragPos;
                    OnDrag?.Invoke(currentPos, delta);
                    HandleObjectDrag(currentPos);
                    lastDragPos = currentPos;
                }
                else if (!isLongPressing && holdTime >= LongPressThreshold)
                {
                    isLongPressing = true;
                    Vector3 worldPos = ScreenToWorld(touchStartPos);
                    OnLongPress?.Invoke(worldPos);
                }
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                if (isDragging)
                {
                    Vector2 endPos = UnityEngine.Input.mousePosition;
                    OnDragEnd?.Invoke(endPos);
                    DetectSwipe(touchStartPos, endPos);
                }
                else if (!isLongPressing)
                {
                    float holdTime = Time.time - touchStartTime;
                    if (holdTime < TapThreshold)
                    {
                        Vector3 worldPos = ScreenToWorld(touchStartPos);
                        float timeSinceLastTap = Time.time - lastTapTime;

                        if (timeSinceLastTap < DoubleTapThreshold)
                        {
                            OnDoubleTap?.Invoke(worldPos);
                        }
                        else
                        {
                            OnTap?.Invoke(worldPos);
                            RaycastForObject(touchStartPos);
                        }

                        lastTapTime = Time.time;
                    }
                }
            }

            // Scroll wheel for zoom
            float scroll = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                OnPinchZoom?.Invoke(scroll * 10f);
            }
        }

        // --- TOUCH INPUT (Mobile) ---
        private void HandleTouchInput()
        {
            int touchCount = UnityEngine.Input.touchCount;

            if (touchCount == 1)
            {
                Touch touch = UnityEngine.Input.GetTouch(0);
                HandleSingleTouch(touch);
            }
            else if (touchCount == 2)
            {
                HandlePinchZoom();
            }
        }

        private void HandleSingleTouch(Touch touch)
        {
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPos = touch.position;
                    touchStartTime = Time.time;
                    isDragging = false;
                    isLongPressing = false;
                    lastDragPos = touch.position;
                    break;

                case TouchPhase.Moved:
                    float dist = Vector2.Distance(touchStartPos, touch.position);
                    if (!isDragging && dist > DragThreshold)
                    {
                        isDragging = true;
                        OnDragStart?.Invoke(touchStartPos);
                    }
                    if (isDragging)
                    {
                        Vector2 delta = touch.position - lastDragPos;
                        OnDrag?.Invoke(touch.position, delta);
                        HandleObjectDrag(touch.position);
                        lastDragPos = touch.position;
                    }
                    break;

                case TouchPhase.Stationary:
                    float holdTime = Time.time - touchStartTime;
                    if (!isLongPressing && !isDragging && holdTime >= LongPressThreshold)
                    {
                        isLongPressing = true;
                        Vector3 worldPos = ScreenToWorld(touchStartPos);
                        OnLongPress?.Invoke(worldPos);
                    }
                    break;

                case TouchPhase.Ended:
                    if (isDragging)
                    {
                        OnDragEnd?.Invoke(touch.position);
                        DetectSwipe(touchStartPos, touch.position);
                    }
                    else if (!isLongPressing)
                    {
                        float tapTime = Time.time - touchStartTime;
                        if (tapTime < TapThreshold)
                        {
                            Vector3 wp = ScreenToWorld(touch.position);
                            float timeSinceLastTap = Time.time - lastTapTime;
                            if (timeSinceLastTap < DoubleTapThreshold)
                                OnDoubleTap?.Invoke(wp);
                            else
                            {
                                OnTap?.Invoke(wp);
                                RaycastForObject(touch.position);
                            }
                            lastTapTime = Time.time;
                        }
                    }
                    break;
            }
        }

        private void HandlePinchZoom()
        {
            Touch t0 = UnityEngine.Input.GetTouch(0);
            Touch t1 = UnityEngine.Input.GetTouch(1);

            if (t1.phase == TouchPhase.Began)
            {
                initialPinchDistance = Vector2.Distance(t0.position, t1.position);
                return;
            }

            float currentDistance = Vector2.Distance(t0.position, t1.position);
            float delta = (currentDistance - initialPinchDistance) * PinchSensitivity;
            OnPinchZoom?.Invoke(delta);
            initialPinchDistance = currentDistance;
        }

        // --- SWIPE DETECTION ---
        private void DetectSwipe(Vector2 startPos, Vector2 endPos)
        {
            Vector2 diff = endPos - startPos;
            if (diff.magnitude < SwipeThreshold) return;

            if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
            {
                OnSwipe?.Invoke(diff.x > 0 ? SwipeDirection.Right : SwipeDirection.Left);
            }
            else
            {
                OnSwipe?.Invoke(diff.y > 0 ? SwipeDirection.Up : SwipeDirection.Down);
            }
        }

        // --- RAYCASTING ---
        private void RaycastForObject(Vector2 screenPos)
        {
            if (MainCamera == null) return;
            Ray ray = MainCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, RaycastDistance, InteractableLayer))
            {
                OnObjectTapped?.Invoke(hit.collider.gameObject);
            }
        }

        private void HandleObjectDrag(Vector2 screenPos)
        {
            if (MainCamera == null) return;
            Ray ray = MainCamera.ScreenPointToRay(screenPos);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPoint = ray.GetPoint(distance);
                // Check if we're dragging an object
                if (Physics.Raycast(ray, out RaycastHit hit, RaycastDistance, InteractableLayer))
                {
                    OnObjectDragged?.Invoke(hit.collider.gameObject, worldPoint);
                }
            }
        }

        // --- HELPERS ---
        private Vector3 ScreenToWorld(Vector2 screenPos)
        {
            if (MainCamera == null) return Vector3.zero;
            Ray ray = MainCamera.ScreenPointToRay(screenPos);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }
            return MainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
        }
    }
}
