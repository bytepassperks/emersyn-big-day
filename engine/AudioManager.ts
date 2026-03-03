/**
 * AudioManager.ts - Sound effects and background music manager
 * Uses expo-av for audio playback
 */
import { Audio } from 'expo-av';

type SFXType =
  | 'coin_collect'
  | 'tap'
  | 'success'
  | 'levelup'
  | 'footstep'
  | 'splash'
  | 'cooking'
  | 'eating'
  | 'achievement'
  | 'whoosh'
  | 'pop'
  | 'giggle'
  | 'ding';

type BGMType = 'home' | 'bedroom' | 'kitchen' | 'park' | 'school' | 'arcade' | 'studio' | 'shop' | 'bathroom' | 'minigame';

// We generate simple tones programmatically since we don't have audio files
// In a real game, these would be .mp3/.wav files
// For now we use the Audio API silently - the visual feedback is the primary feedback

class AudioManagerClass {
  private initialized: boolean = false;
  private bgmSound: Audio.Sound | null = null;
  private sfxEnabled: boolean = true;
  private bgmEnabled: boolean = true;
  private volume: number = 0.5;

  async init() {
    if (this.initialized) return;
    try {
      await Audio.setAudioModeAsync({
        allowsRecordingIOS: false,
        playsInSilentModeIOS: true,
        staysActiveInBackground: false,
        shouldDuckAndroid: true,
      });
      this.initialized = true;
    } catch (e) {
      // Audio init failed silently - game works without audio
      console.log('Audio init skipped');
    }
  }

  async playSFX(type: SFXType) {
    if (!this.sfxEnabled || !this.initialized) return;
    // In production, load and play actual sound files here
    // For now, this is a placeholder that doesn't crash
    try {
      // Haptic feedback can substitute for audio on mobile
      // We'll add actual audio files in a future update
    } catch (e) {
      // Silent fail
    }
  }

  async playBGM(room: BGMType) {
    if (!this.bgmEnabled || !this.initialized) return;
    try {
      // Stop current BGM
      if (this.bgmSound) {
        await this.bgmSound.stopAsync();
        await this.bgmSound.unloadAsync();
        this.bgmSound = null;
      }
      // In production, load room-specific music here
    } catch (e) {
      // Silent fail
    }
  }

  async stopBGM() {
    try {
      if (this.bgmSound) {
        await this.bgmSound.stopAsync();
        await this.bgmSound.unloadAsync();
        this.bgmSound = null;
      }
    } catch (e) {
      // Silent fail
    }
  }

  setSFXEnabled(enabled: boolean) {
    this.sfxEnabled = enabled;
  }

  setBGMEnabled(enabled: boolean) {
    this.bgmEnabled = enabled;
    if (!enabled) {
      this.stopBGM();
    }
  }

  setVolume(vol: number) {
    this.volume = Math.max(0, Math.min(1, vol));
  }

  get isSFXEnabled() {
    return this.sfxEnabled;
  }

  get isBGMEnabled() {
    return this.bgmEnabled;
  }
}

export const AudioManager = new AudioManagerClass();
