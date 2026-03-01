using UnityEngine;
using System;
using System.Collections.Generic;

namespace EmersynBigDay.Audio
{
    /// <summary>
    /// Manages all game audio: ambient room sounds, SFX, music, footsteps, and UI sounds.
    /// Implements layered audio like Talking Tom (ambient + SFX + music + UI).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        public AudioSource MusicSource;
        public AudioSource AmbientSource;
        public AudioSource SFXSource;
        public AudioSource UISource;
        public AudioSource FootstepSource;

        [Header("Music")]
        public AudioClip MainMenuMusic;
        public AudioClip[] GameplayMusic;
        private int currentMusicIndex = 0;

        [Header("Ambient Sounds")]
        public AudioClip[] BedroomAmbient;
        public AudioClip[] KitchenAmbient;
        public AudioClip[] BathroomAmbient;
        public AudioClip[] ParkAmbient;
        public AudioClip[] SchoolAmbient;
        public AudioClip[] ArcadeAmbient;

        [Header("SFX Library")]
        public AudioClip TapSFX;
        public AudioClip CoinCollectSFX;
        public AudioClip StarCollectSFX;
        public AudioClip LevelUpSFX;
        public AudioClip AchievementSFX;
        public AudioClip ButtonClickSFX;
        public AudioClip PopupOpenSFX;
        public AudioClip PopupCloseSFX;
        public AudioClip EatSFX;
        public AudioClip DrinkSFX;
        public AudioClip SleepSFX;
        public AudioClip ShowerSFX;
        public AudioClip LaughSFX;
        public AudioClip GiggleSFX;
        public AudioClip SadSFX;
        public AudioClip WinSFX;
        public AudioClip LoseSFX;

        [Header("Footstep Sounds")]
        public AudioClip[] CarpetFootsteps;
        public AudioClip[] TileFootsteps;
        public AudioClip[] GrassFootsteps;
        public AudioClip[] WoodFootsteps;

        [Header("Settings")]
        public float MasterVolume = 1f;
        public float MusicVolume = 0.5f;
        public float SFXVolume = 0.8f;
        public float AmbientVolume = 0.3f;
        public bool IsMuted = false;

        private Dictionary<string, AudioClip> sfxCache = new Dictionary<string, AudioClip>();
        private float footstepTimer = 0f;
        private float footstepInterval = 0.4f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildSFXCache();
        }

        private void Start()
        {
            ApplyVolumes();
        }

        private void BuildSFXCache()
        {
            AddToCache("tap", TapSFX);
            AddToCache("coin", CoinCollectSFX);
            AddToCache("star", StarCollectSFX);
            AddToCache("levelup", LevelUpSFX);
            AddToCache("achievement", AchievementSFX);
            AddToCache("button", ButtonClickSFX);
            AddToCache("popup_open", PopupOpenSFX);
            AddToCache("popup_close", PopupCloseSFX);
            AddToCache("eat", EatSFX);
            AddToCache("drink", DrinkSFX);
            AddToCache("sleep", SleepSFX);
            AddToCache("shower", ShowerSFX);
            AddToCache("laugh", LaughSFX);
            AddToCache("giggle", GiggleSFX);
            AddToCache("sad", SadSFX);
            AddToCache("win", WinSFX);
            AddToCache("lose", LoseSFX);
        }

        private void AddToCache(string key, AudioClip clip)
        {
            if (clip != null) sfxCache[key] = clip;
        }

        // --- MUSIC ---
        public void PlayMusic(AudioClip clip)
        {
            if (MusicSource == null || clip == null) return;
            MusicSource.clip = clip;
            MusicSource.loop = true;
            MusicSource.Play();
        }

        public void PlayMainMenuMusic() { PlayMusic(MainMenuMusic); }

        public void PlayGameplayMusic()
        {
            if (GameplayMusic == null || GameplayMusic.Length == 0) return;
            currentMusicIndex = UnityEngine.Random.Range(0, GameplayMusic.Length);
            PlayMusic(GameplayMusic[currentMusicIndex]);
        }

        public void NextTrack()
        {
            if (GameplayMusic == null || GameplayMusic.Length == 0) return;
            currentMusicIndex = (currentMusicIndex + 1) % GameplayMusic.Length;
            PlayMusic(GameplayMusic[currentMusicIndex]);
        }

        public void StopMusic()
        {
            if (MusicSource != null) MusicSource.Stop();
        }

        public void FadeMusic(float targetVolume, float duration)
        {
            StartCoroutine(FadeMusicCoroutine(targetVolume, duration));
        }

        private System.Collections.IEnumerator FadeMusicCoroutine(float targetVolume, float duration)
        {
            if (MusicSource == null) yield break;
            float startVol = MusicSource.volume;
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                MusicSource.volume = Mathf.Lerp(startVol, targetVolume * MusicVolume * MasterVolume, timer / duration);
                yield return null;
            }
        }

        // --- AMBIENT ---
        public void SetAmbient(Rooms.RoomType roomType)
        {
            AudioClip[] clips = GetAmbientClips(roomType);
            if (clips == null || clips.Length == 0 || AmbientSource == null) return;

            AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Length)];
            AmbientSource.clip = clip;
            AmbientSource.loop = true;
            AmbientSource.Play();
        }

        private AudioClip[] GetAmbientClips(Rooms.RoomType roomType)
        {
            switch (roomType)
            {
                case Rooms.RoomType.Bedroom: return BedroomAmbient;
                case Rooms.RoomType.Kitchen: return KitchenAmbient;
                case Rooms.RoomType.Bathroom: return BathroomAmbient;
                case Rooms.RoomType.Park: return ParkAmbient;
                case Rooms.RoomType.School: return SchoolAmbient;
                case Rooms.RoomType.Arcade: return ArcadeAmbient;
                default: return BedroomAmbient;
            }
        }

        public void StopAmbient()
        {
            if (AmbientSource != null) AmbientSource.Stop();
        }

        // --- SFX ---
        public void PlaySFX(string sfxName)
        {
            if (IsMuted || SFXSource == null) return;
            if (sfxCache.TryGetValue(sfxName, out AudioClip clip))
            {
                SFXSource.PlayOneShot(clip, SFXVolume * MasterVolume);
            }
        }

        public void PlaySFX(AudioClip clip)
        {
            if (IsMuted || SFXSource == null || clip == null) return;
            SFXSource.PlayOneShot(clip, SFXVolume * MasterVolume);
        }

        public void PlayUISound(string sfxName)
        {
            if (IsMuted || UISource == null) return;
            if (sfxCache.TryGetValue(sfxName, out AudioClip clip))
            {
                UISource.PlayOneShot(clip, SFXVolume * MasterVolume * 0.7f);
            }
        }

        // --- FOOTSTEPS ---
        public void PlayFootstep(string surfaceType)
        {
            footstepTimer += Time.deltaTime;
            if (footstepTimer < footstepInterval) return;
            footstepTimer = 0f;

            AudioClip[] clips = GetFootstepClips(surfaceType);
            if (clips == null || clips.Length == 0 || FootstepSource == null) return;

            AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Length)];
            FootstepSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
            FootstepSource.PlayOneShot(clip, SFXVolume * MasterVolume * 0.5f);
        }

        private AudioClip[] GetFootstepClips(string surfaceType)
        {
            switch (surfaceType.ToLower())
            {
                case "carpet": return CarpetFootsteps;
                case "tile": return TileFootsteps;
                case "grass": return GrassFootsteps;
                case "wood": return WoodFootsteps;
                default: return WoodFootsteps;
            }
        }

        // --- VOLUME CONTROL ---
        public void SetMasterVolume(float vol)
        {
            MasterVolume = Mathf.Clamp01(vol);
            ApplyVolumes();
        }

        public void SetMusicVolume(float vol)
        {
            MusicVolume = Mathf.Clamp01(vol);
            ApplyVolumes();
        }

        public void SetSFXVolume(float vol)
        {
            SFXVolume = Mathf.Clamp01(vol);
            ApplyVolumes();
        }

        public void ToggleMute()
        {
            IsMuted = !IsMuted;
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            float effectiveMaster = IsMuted ? 0f : MasterVolume;
            if (MusicSource != null) MusicSource.volume = MusicVolume * effectiveMaster;
            if (AmbientSource != null) AmbientSource.volume = AmbientVolume * effectiveMaster;
            if (SFXSource != null) SFXSource.volume = SFXVolume * effectiveMaster;
            if (UISource != null) UISource.volume = SFXVolume * effectiveMaster * 0.7f;
            if (FootstepSource != null) FootstepSource.volume = SFXVolume * effectiveMaster * 0.5f;
        }
    }
}
