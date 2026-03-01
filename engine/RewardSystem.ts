/**
 * RewardSystem.ts - Daily login rewards, streaks, achievements, and random events.
 * Tracks player progression and provides variable-ratio reinforcement.
 */
import AsyncStorage from '@react-native-async-storage/async-storage';

const STORAGE_KEYS = {
  LAST_LOGIN: 'reward_last_login',
  LOGIN_STREAK: 'reward_login_streak',
  TOTAL_LOGINS: 'reward_total_logins',
  ACHIEVEMENTS: 'reward_achievements',
  LIFETIME_COINS: 'reward_lifetime_coins',
  STICKERS_COLLECTED: 'reward_stickers_collected',
};

/** Daily login reward tiers */
const DAILY_REWARDS: { day: number; coins: number; bonus?: string }[] = [
  { day: 1, coins: 50 },
  { day: 2, coins: 75 },
  { day: 3, coins: 100, bonus: 'random_sticker' },
  { day: 4, coins: 125 },
  { day: 5, coins: 150, bonus: 'rare_outfit' },
  { day: 6, coins: 200 },
  { day: 7, coins: 500, bonus: 'mystery_box' },
];

/** Achievement definitions */
export interface Achievement {
  id: string;
  title: string;
  description: string;
  icon: string;
  requirement: number;
  type: 'coins_earned' | 'activities_done' | 'stickers_collected' | 'friends_made' | 'rooms_visited' | 'login_streak' | 'minigames_played';
}

const ACHIEVEMENTS: Achievement[] = [
  { id: 'first_steps', title: 'First Steps', description: 'Complete your first activity', icon: '👣', requirement: 1, type: 'activities_done' },
  { id: 'social_butterfly', title: 'Social Butterfly', description: 'Make 3 friends', icon: '🦋', requirement: 3, type: 'friends_made' },
  { id: 'coin_collector', title: 'Coin Collector', description: 'Earn 500 coins', icon: '💰', requirement: 500, type: 'coins_earned' },
  { id: 'sticker_fan', title: 'Sticker Fan', description: 'Collect 10 stickers', icon: '⭐', requirement: 10, type: 'stickers_collected' },
  { id: 'explorer', title: 'Explorer', description: 'Visit all 9 rooms', icon: '🗺️', requirement: 9, type: 'rooms_visited' },
  { id: 'dedicated', title: 'Dedicated Player', description: 'Login 7 days in a row', icon: '🔥', requirement: 7, type: 'login_streak' },
  { id: 'rich', title: 'Big Spender', description: 'Earn 5000 coins', icon: '👑', requirement: 5000, type: 'coins_earned' },
  { id: 'gamer', title: 'Arcade Master', description: 'Play 20 mini-games', icon: '🎮', requirement: 20, type: 'minigames_played' },
  { id: 'fashionista', title: 'Fashionista', description: 'Collect 15 outfits', icon: '👗', requirement: 15, type: 'stickers_collected' },
  { id: 'superstar', title: 'Superstar', description: 'Earn 10000 coins total', icon: '🌟', requirement: 10000, type: 'coins_earned' },
  { id: 'loyal', title: 'Loyal Player', description: 'Login 30 days in a row', icon: '💎', requirement: 30, type: 'login_streak' },
  { id: 'completionist', title: 'Completionist', description: 'Complete 100 activities', icon: '🏆', requirement: 100, type: 'activities_done' },
];

/** Random event that can happen during gameplay */
export interface RandomEvent {
  id: string;
  title: string;
  description: string;
  type: 'bonus_coins' | 'surprise_visit' | 'weather_change' | 'special_item' | 'mini_challenge';
  reward?: { coins?: number; sticker?: string; item?: string };
  chance: number; // 0-1 probability per room visit
}

const RANDOM_EVENTS: RandomEvent[] = [
  { id: 'coin_rain', title: 'Coin Rain!', description: 'Coins are falling from the sky!', type: 'bonus_coins', reward: { coins: 100 }, chance: 0.05 },
  { id: 'friend_surprise', title: 'Surprise Visit!', description: 'A friend came to visit!', type: 'surprise_visit', chance: 0.08 },
  { id: 'rainbow', title: 'Rainbow!', description: 'A beautiful rainbow appeared!', type: 'weather_change', reward: { coins: 25 }, chance: 0.06 },
  { id: 'lost_sticker', title: 'Found Sticker!', description: 'You found a rare sticker on the ground!', type: 'special_item', reward: { sticker: 'rare_sparkle' }, chance: 0.04 },
  { id: 'dance_challenge', title: 'Dance Challenge!', description: 'Quick! Show your best dance moves!', type: 'mini_challenge', reward: { coins: 75 }, chance: 0.07 },
  { id: 'cooking_bonus', title: 'Recipe Found!', description: 'You discovered a secret recipe!', type: 'special_item', reward: { item: 'secret_recipe' }, chance: 0.05 },
  { id: 'pet_trick', title: 'Pet Trick!', description: 'Your pet learned a new trick!', type: 'mini_challenge', reward: { coins: 50 }, chance: 0.06 },
  { id: 'star_shower', title: 'Star Shower!', description: 'Stars are twinkling extra bright!', type: 'weather_change', reward: { coins: 30 }, chance: 0.05 },
];

export interface DailyLoginResult {
  isNewDay: boolean;
  streak: number;
  reward: { coins: number; bonus?: string } | null;
  totalLogins: number;
}

export class RewardSystem {
  private unlockedAchievements: Set<string> = new Set();
  private lifetimeCoins: number = 0;
  private loginStreak: number = 0;
  private totalLogins: number = 0;

  async init(): Promise<void> {
    try {
      const [achievementsStr, coinsStr, streakStr, loginsStr] = await Promise.all([
        AsyncStorage.getItem(STORAGE_KEYS.ACHIEVEMENTS),
        AsyncStorage.getItem(STORAGE_KEYS.LIFETIME_COINS),
        AsyncStorage.getItem(STORAGE_KEYS.LOGIN_STREAK),
        AsyncStorage.getItem(STORAGE_KEYS.TOTAL_LOGINS),
      ]);
      if (achievementsStr) {
        const arr = JSON.parse(achievementsStr) as string[];
        arr.forEach((id) => this.unlockedAchievements.add(id));
      }
      this.lifetimeCoins = coinsStr ? parseInt(coinsStr, 10) : 0;
      this.loginStreak = streakStr ? parseInt(streakStr, 10) : 0;
      this.totalLogins = loginsStr ? parseInt(loginsStr, 10) : 0;
    } catch {
      // Fresh start
    }
  }

  /** Check daily login and return reward */
  async checkDailyLogin(): Promise<DailyLoginResult> {
    const now = new Date();
    const todayStr = `${now.getFullYear()}-${now.getMonth()}-${now.getDate()}`;
    try {
      const lastLogin = await AsyncStorage.getItem(STORAGE_KEYS.LAST_LOGIN);
      if (lastLogin === todayStr) {
        return { isNewDay: false, streak: this.loginStreak, reward: null, totalLogins: this.totalLogins };
      }
      // Check if yesterday to continue streak
      const yesterday = new Date(now);
      yesterday.setDate(yesterday.getDate() - 1);
      const yesterdayStr = `${yesterday.getFullYear()}-${yesterday.getMonth()}-${yesterday.getDate()}`;
      if (lastLogin === yesterdayStr) {
        this.loginStreak++;
      } else {
        this.loginStreak = 1;
      }
      this.totalLogins++;
      // Get reward for current streak day (cycles after 7)
      const dayIndex = ((this.loginStreak - 1) % 7);
      const reward = DAILY_REWARDS[dayIndex];
      // Persist
      await Promise.all([
        AsyncStorage.setItem(STORAGE_KEYS.LAST_LOGIN, todayStr),
        AsyncStorage.setItem(STORAGE_KEYS.LOGIN_STREAK, String(this.loginStreak)),
        AsyncStorage.setItem(STORAGE_KEYS.TOTAL_LOGINS, String(this.totalLogins)),
      ]);
      return { isNewDay: true, streak: this.loginStreak, reward, totalLogins: this.totalLogins };
    } catch {
      return { isNewDay: false, streak: 0, reward: null, totalLogins: 0 };
    }
  }

  /** Record coins earned and check achievements */
  async addCoins(amount: number): Promise<Achievement[]> {
    this.lifetimeCoins += amount;
    await AsyncStorage.setItem(STORAGE_KEYS.LIFETIME_COINS, String(this.lifetimeCoins));
    return this.checkAchievements('coins_earned', this.lifetimeCoins);
  }

  /** Check for newly unlocked achievements */
  checkAchievements(type: Achievement['type'], currentValue: number): Achievement[] {
    const newlyUnlocked: Achievement[] = [];
    for (const ach of ACHIEVEMENTS) {
      if (ach.type !== type) continue;
      if (this.unlockedAchievements.has(ach.id)) continue;
      if (currentValue >= ach.requirement) {
        this.unlockedAchievements.add(ach.id);
        newlyUnlocked.push(ach);
      }
    }
    if (newlyUnlocked.length > 0) {
      this.saveAchievements();
    }
    return newlyUnlocked;
  }

  private async saveAchievements(): Promise<void> {
    try {
      await AsyncStorage.setItem(
        STORAGE_KEYS.ACHIEVEMENTS,
        JSON.stringify(Array.from(this.unlockedAchievements)),
      );
    } catch {
      // silently fail
    }
  }

  /** Roll random events for a room visit */
  rollRandomEvents(): RandomEvent[] {
    const triggered: RandomEvent[] = [];
    for (const event of RANDOM_EVENTS) {
      if (Math.random() < event.chance) {
        triggered.push(event);
      }
    }
    return triggered;
  }

  /** Get all achievement definitions */
  getAllAchievements(): (Achievement & { unlocked: boolean })[] {
    return ACHIEVEMENTS.map((a) => ({ ...a, unlocked: this.unlockedAchievements.has(a.id) }));
  }

  getLoginStreak(): number { return this.loginStreak; }
  getLifetimeCoins(): number { return this.lifetimeCoins; }
  getTotalLogins(): number { return this.totalLogins; }
}
