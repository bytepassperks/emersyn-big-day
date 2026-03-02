using UnityEngine;
using System.Collections;

namespace EmersynBigDay.Visual
{
    /// <summary>
    /// Enhancement #4: Dynamic lighting per room with day/night cycle.
    /// Warm indoor lights, cool moonlight at night, colored accent lights per room.
    /// Like Sims FreePlay's time-of-day lighting and Animal Crossing's golden hour.
    /// </summary>
    public class DynamicLighting : MonoBehaviour
    {
        public static DynamicLighting Instance { get; private set; }

        [Header("Day/Night Cycle")]
        public float DayDuration = 300f; // 5 min = 1 game day
        public float CurrentTimeOfDay = 0.3f; // 0=midnight, 0.25=sunrise, 0.5=noon, 0.75=sunset
        public bool EnableDayNightCycle = true;

        [Header("Sun Colors")]
        public Color SunriseColor = new Color(1f, 0.7f, 0.4f);
        public Color NoonColor = new Color(1f, 0.98f, 0.92f);
        public Color SunsetColor = new Color(1f, 0.5f, 0.3f);
        public Color NightColor = new Color(0.3f, 0.35f, 0.6f);

        [Header("Ambient Colors")]
        public Color DayAmbient = new Color(0.9f, 0.92f, 0.95f);
        public Color NightAmbient = new Color(0.15f, 0.18f, 0.3f);
        public Color IndoorAmbient = new Color(0.85f, 0.82f, 0.75f);

        [Header("Room Accent Lights")]
        public Color BedroomAccent = new Color(1f, 0.85f, 0.95f);
        public Color KitchenAccent = new Color(1f, 0.95f, 0.8f);
        public Color BathroomAccent = new Color(0.8f, 0.95f, 1f);
        public Color ArcadeAccent = new Color(0.6f, 0.3f, 1f);

        private Light mainLight;
        private Light fillLight;
        private Light roomAccentLight;
        private bool isIndoor = true;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            SetupLights();
        }

        private void Update()
        {
            if (EnableDayNightCycle)
            {
                CurrentTimeOfDay += Time.deltaTime / DayDuration;
                if (CurrentTimeOfDay >= 1f) CurrentTimeOfDay -= 1f;
                UpdateLighting();
            }
        }

        private void SetupLights()
        {
            // Find existing main directional light
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional && mainLight == null)
                    mainLight = l;
                else if (l.type == LightType.Directional && fillLight == null)
                    fillLight = l;
            }

            // Create room accent light
            var accentObj = new GameObject("RoomAccentLight");
            accentObj.transform.SetParent(transform);
            accentObj.transform.position = new Vector3(0, 4f, 0);
            roomAccentLight = accentObj.AddComponent<Light>();
            roomAccentLight.type = LightType.Point;
            roomAccentLight.range = 15f;
            roomAccentLight.intensity = 0.4f;
            roomAccentLight.color = BedroomAccent;
            roomAccentLight.shadows = LightShadows.Soft;
        }

        private void UpdateLighting()
        {
            if (mainLight == null) return;

            // Calculate sun color based on time of day
            Color sunColor;
            float sunIntensity;

            if (CurrentTimeOfDay < 0.25f) // Night to sunrise
            {
                float t = CurrentTimeOfDay / 0.25f;
                sunColor = Color.Lerp(NightColor, SunriseColor, t);
                sunIntensity = Mathf.Lerp(0.2f, 0.8f, t);
            }
            else if (CurrentTimeOfDay < 0.5f) // Sunrise to noon
            {
                float t = (CurrentTimeOfDay - 0.25f) / 0.25f;
                sunColor = Color.Lerp(SunriseColor, NoonColor, t);
                sunIntensity = Mathf.Lerp(0.8f, 1.2f, t);
            }
            else if (CurrentTimeOfDay < 0.75f) // Noon to sunset
            {
                float t = (CurrentTimeOfDay - 0.5f) / 0.25f;
                sunColor = Color.Lerp(NoonColor, SunsetColor, t);
                sunIntensity = Mathf.Lerp(1.2f, 0.6f, t);
            }
            else // Sunset to night
            {
                float t = (CurrentTimeOfDay - 0.75f) / 0.25f;
                sunColor = Color.Lerp(SunsetColor, NightColor, t);
                sunIntensity = Mathf.Lerp(0.6f, 0.2f, t);
            }

            mainLight.color = sunColor;
            mainLight.intensity = sunIntensity;

            // Sun rotation
            float sunAngle = CurrentTimeOfDay * 360f - 90f;
            mainLight.transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);

            // Ambient
            Color ambient = isIndoor
                ? Color.Lerp(IndoorAmbient, IndoorAmbient * 0.6f, IsNight() ? 1f : 0f)
                : Color.Lerp(DayAmbient, NightAmbient, IsNight() ? 1f : 0f);
            RenderSettings.ambientLight = ambient;

            // Camera background for outdoors
            if (!isIndoor)
            {
                Camera.main.backgroundColor = Color.Lerp(
                    new Color(0.55f, 0.80f, 0.95f),
                    new Color(0.05f, 0.08f, 0.2f),
                    IsNight() ? 1f : 0f
                );
            }
        }

        public void SetRoomLighting(int roomIndex, bool outdoor)
        {
            isIndoor = !outdoor;

            Color accent;
            switch (roomIndex)
            {
                case 0: accent = BedroomAccent; break;
                case 1: accent = KitchenAccent; break;
                case 2: accent = BathroomAccent; break;
                case 5: accent = ArcadeAccent; break;
                default: accent = new Color(0.9f, 0.9f, 0.85f); break;
            }

            if (roomAccentLight != null)
            {
                roomAccentLight.color = accent;
                roomAccentLight.intensity = outdoor ? 0f : 0.5f;
            }
        }

        public bool IsNight()
        {
            return CurrentTimeOfDay < 0.2f || CurrentTimeOfDay > 0.8f;
        }

        public bool IsSunrise()
        {
            return CurrentTimeOfDay >= 0.2f && CurrentTimeOfDay < 0.3f;
        }

        public bool IsSunset()
        {
            return CurrentTimeOfDay >= 0.7f && CurrentTimeOfDay < 0.8f;
        }

        public string GetTimeOfDayString()
        {
            if (CurrentTimeOfDay < 0.25f) return "Night";
            if (CurrentTimeOfDay < 0.5f) return "Morning";
            if (CurrentTimeOfDay < 0.75f) return "Afternoon";
            return "Evening";
        }
    }
}
