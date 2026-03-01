import { create } from 'zustand';
import AsyncStorage from '@react-native-async-storage/async-storage';
import {
  GameState,
  PlayerStats,
  TimeSegment,
  DayType,
  InventoryItem,
  ShopCategory,
  EquippedOutfit,
  StickerAlbum,
  KarateBelt,
  Choreography,
  RoomDecorItem,
  Achievement,
  MiniGameResult,
} from '@/lib/types';
import { initialStickerAlbum } from '@/content/achievements';

const STORAGE_KEY = 'emersyn_game_state';

const WEEKDAY_SEGMENTS: TimeSegment[] = [
  'morning', 'school', 'lunch', 'nap', 'park', 'backHome',
  'homework', 'arcade', 'shopping', 'dinner', 'bedtime',
];

const WEEKEND_SEGMENTS: TimeSegment[] = [
  'morning', 'weekendStudio', 'lunch', 'nap', 'park', 'backHome',
  'arcade', 'shopping', 'dinner', 'bedtime',
];

const DEFAULT_STATS: PlayerStats = {
  hunger: 70,
  energy: 100,
  cleanliness: 80,
  fun: 60,
  popularity: 0,
};

const DEFAULT_OUTFIT: EquippedOutfit = {
  hair: null,
  top: null,
  bottom: null,
  dress: 'dress_default_pink',
  shoes: 'shoes_default_white',
  bag: null,
  accessory: null,
};

const initialState: GameState = {
  playerName: 'Emersyn',
  coins: 50,
  stars: 0,
  xp: 0,
  level: 1,
  stats: { ...DEFAULT_STATS },
  currentDay: 1,
  dayType: 'weekday',
  currentSegment: 'morning',
  segmentsCompleted: [],
  inventory: [],
  equippedOutfit: { ...DEFAULT_OUTFIT },
  stickerAlbum: initialStickerAlbum,
  recipeBook: ['pizza_basic', 'fruit_bowl'],
  roomDecor: [],
  achievements: [],
  totalCoinsEarned: 0,
  totalDaysPlayed: 0,
  totalMinigamesPlayed: 0,
  bugEventsHandled: 0,
  braveLevel: 0,
  karateBelt: 'white',
  karateXP: 0,
  danceStars: 0,
  savedChoreographies: [],
  reelsCreated: 0,
  totalLikes: 0,
  musicEnabled: true,
  sfxEnabled: true,
  breakReminderMinutes: 30,
  lastPlayedAt: null,
};

interface GameActions {
  // Stats
  updateStats: (deltas: Partial<PlayerStats>) => void;
  
  // Currency
  addCoins: (amount: number) => void;
  spendCoins: (amount: number) => boolean;
  addStars: (amount: number) => void;
  addXP: (amount: number) => void;
  
  // Day progression
  advanceSegment: () => void;
  endDay: () => void;
  completeSegment: (segment: TimeSegment) => void;
  
  // Inventory & Shop
  purchaseItem: (itemId: string, category?: ShopCategory, price?: number) => boolean;
  equipItem: (itemId: string, slot: keyof EquippedOutfit) => void;
  unequipItem: (slot: keyof EquippedOutfit) => void;
  ownsItem: (itemId: string) => boolean;
  
  // Recipes
  unlockRecipe: (recipeId: string) => void;
  
  // Stickers
  earnSticker: (stickerId: string) => void;
  
  // Room decor
  placeDecor: (decor: RoomDecorItem) => void;
  
  // Achievements
  unlockAchievement: (achievement: Achievement) => void;
  
  // Mini-games
  recordMiniGameResult: (result: { gameId: string; score: number; coinsEarned: number; playedAt: number }) => void;
  
  // Bug system
  handleBugEvent: (eventId: string, actionId: string, success: boolean) => void;
  
  // Karate
  addKarateXP: (amount: number) => void;
  promoteBelt: () => void;
  
  // Dance
  addDanceStars: (amount: number) => void;
  saveChoreography: (choreo: Choreography) => void;
  
  // Weekend
  createReel: (reelData: { outfitHash: string; likes: number; coins: number; postedAt: number }) => void;
  
  // Settings
  toggleMusic: () => void;
  toggleSFX: () => void;
  setBreakReminder: (minutes: number | null) => void;
  
  // Persistence
  saveGame: () => Promise<void>;
  loadGame: () => Promise<void>;
  resetGame: () => void;
}

const BELT_ORDER: KarateBelt[] = [
  'white', 'yellow', 'orange', 'green', 'blue', 'purple', 'brown', 'black',
];

const BELT_XP_THRESHOLDS: Record<KarateBelt, number> = {
  white: 0,
  yellow: 100,
  orange: 250,
  green: 500,
  blue: 800,
  purple: 1200,
  brown: 1800,
  black: 2500,
};

export const useGameStore = create<GameState & GameActions>((set, get) => ({
  ...initialState,

  updateStats: (deltas) =>
    set((state) => ({
      stats: {
        hunger: Math.min(100, Math.max(0, state.stats.hunger + (deltas.hunger ?? 0))),
        energy: Math.min(100, Math.max(0, state.stats.energy + (deltas.energy ?? 0))),
        cleanliness: Math.min(100, Math.max(0, state.stats.cleanliness + (deltas.cleanliness ?? 0))),
        fun: Math.min(100, Math.max(0, state.stats.fun + (deltas.fun ?? 0))),
        popularity: Math.min(100, Math.max(0, state.stats.popularity + (deltas.popularity ?? 0))),
      },
    })),

  addCoins: (amount) =>
    set((state) => ({
      coins: state.coins + amount,
      totalCoinsEarned: state.totalCoinsEarned + amount,
    })),

  spendCoins: (amount) => {
    const state = get();
    if (state.coins >= amount) {
      set({ coins: state.coins - amount });
      return true;
    }
    return false;
  },

  addStars: (amount) =>
    set((state) => ({ stars: state.stars + amount })),

  addXP: (amount) =>
    set((state) => {
      const newXP = state.xp + amount;
      const newLevel = Math.floor(newXP / 100) + 1;
      return { xp: newXP, level: Math.max(state.level, newLevel) };
    }),

  advanceSegment: () =>
    set((state) => {
      const segments = state.dayType === 'weekday' ? WEEKDAY_SEGMENTS : WEEKEND_SEGMENTS;
      const currentIndex = segments.indexOf(state.currentSegment);
      if (currentIndex < segments.length - 1) {
        return { currentSegment: segments[currentIndex + 1] };
      }
      return {};
    }),

  endDay: () =>
    set((state) => {
      const nextDay = state.currentDay + 1;
      const nextDayType: DayType = nextDay % 7 === 6 || nextDay % 7 === 0 ? 'weekend' : 'weekday';
      return {
        currentDay: nextDay,
        dayType: nextDayType,
        currentSegment: 'morning',
        segmentsCompleted: [],
        totalDaysPlayed: state.totalDaysPlayed + 1,
        stats: {
          ...state.stats,
          energy: 100,
          hunger: Math.max(50, state.stats.hunger - 20),
          cleanliness: Math.max(40, state.stats.cleanliness - 15),
          fun: Math.max(30, state.stats.fun - 10),
        },
      };
    }),

  completeSegment: (segment) =>
    set((state) => ({
      segmentsCompleted: [...state.segmentsCompleted, segment],
    })),

  purchaseItem: (itemId, category, price) => {
    const state = get();
    const shopItem = require('@/content/shopCatalog').getItemById(itemId);
    const itemCategory = category ?? shopItem?.category ?? 'accessories';
    const itemPrice = price ?? shopItem?.price ?? 0;
    if (state.coins >= itemPrice && !state.inventory.find((i: any) => i.id === itemId)) {
      set({
        coins: state.coins - itemPrice,
        inventory: [
          ...state.inventory,
          { id: itemId, category: itemCategory, equipped: false, purchasedAt: new Date().toISOString() },
        ],
      });
      return true;
    }
    return false;
  },

  equipItem: (itemId, slot) =>
    set((state) => ({
      equippedOutfit: { ...state.equippedOutfit, [slot]: itemId },
      inventory: state.inventory.map((item) =>
        item.id === itemId ? { ...item, equipped: true } : item
      ),
    })),

  unequipItem: (slot) =>
    set((state) => {
      const currentItemId = state.equippedOutfit[slot];
      return {
        equippedOutfit: { ...state.equippedOutfit, [slot]: null },
        inventory: state.inventory.map((item) =>
          item.id === currentItemId ? { ...item, equipped: false } : item
        ),
      };
    }),

  ownsItem: (itemId) => {
    return get().inventory.some((i) => i.id === itemId);
  },

  unlockRecipe: (recipeId) =>
    set((state) => ({
      recipeBook: state.recipeBook.includes(recipeId)
        ? state.recipeBook
        : [...state.recipeBook, recipeId],
    })),

  earnSticker: (stickerId) =>
    set((state) => {
      const album = { ...state.stickerAlbum };
      album.pages = album.pages.map((page) => ({
        ...page,
        stickers: page.stickers.map((s) =>
          s.id === stickerId && !s.earned
            ? { ...s, earned: true, earnedAt: new Date().toISOString() }
            : s
        ),
        completed: page.stickers.every((s) =>
          s.id === stickerId ? true : s.earned
        ),
      }));
      return { stickerAlbum: album };
    }),

  placeDecor: (decor) =>
    set((state) => ({
      roomDecor: [
        ...state.roomDecor.filter((d) => d.slot !== decor.slot),
        decor,
      ],
    })),

  unlockAchievement: (achievement) =>
    set((state) => ({
      achievements: state.achievements.some((a) => a.id === achievement.id)
        ? state.achievements
        : [...state.achievements, { ...achievement, earned: true, earnedAt: new Date().toISOString() }],
    })),

  recordMiniGameResult: (result) =>
    set((state) => ({
      totalMinigamesPlayed: state.totalMinigamesPlayed + 1,
    })),

  handleBugEvent: (_eventId, _actionId, _success) =>
    set((state) => ({
      bugEventsHandled: state.bugEventsHandled + 1,
      braveLevel: Math.min(10, Math.floor((state.bugEventsHandled + 1) / 3)),
    })),

  addKarateXP: (amount) =>
    set((state) => ({ karateXP: state.karateXP + amount })),

  promoteBelt: () =>
    set((state) => {
      const currentIndex = BELT_ORDER.indexOf(state.karateBelt);
      if (currentIndex < BELT_ORDER.length - 1) {
        const nextBelt = BELT_ORDER[currentIndex + 1];
        if (state.karateXP >= BELT_XP_THRESHOLDS[nextBelt]) {
          return { karateBelt: nextBelt };
        }
      }
      return {};
    }),

  addDanceStars: (amount) =>
    set((state) => ({ danceStars: state.danceStars + amount })),

  saveChoreography: (choreo) =>
    set((state) => ({
      savedChoreographies: [...state.savedChoreographies, choreo],
    })),

  createReel: (reelData) =>
    set((state) => ({
      reelsCreated: state.reelsCreated + 1,
      totalLikes: state.totalLikes + reelData.likes,
      stats: {
        ...state.stats,
        popularity: Math.min(100, state.stats.popularity + Math.floor(reelData.likes / 5)),
        fun: Math.min(100, state.stats.fun + 10),
      },
    })),

  toggleMusic: () => set((state) => ({ musicEnabled: !state.musicEnabled })),
  toggleSFX: () => set((state) => ({ sfxEnabled: !state.sfxEnabled })),
  setBreakReminder: (minutes) => set({ breakReminderMinutes: minutes }),

  saveGame: async () => {
    try {
      const state = get();
      const saveData: Partial<GameState> = {
        playerName: state.playerName,
        coins: state.coins,
        stars: state.stars,
        xp: state.xp,
        level: state.level,
        stats: state.stats,
        currentDay: state.currentDay,
        dayType: state.dayType,
        currentSegment: state.currentSegment,
        segmentsCompleted: state.segmentsCompleted,
        inventory: state.inventory,
        equippedOutfit: state.equippedOutfit,
        stickerAlbum: state.stickerAlbum,
        recipeBook: state.recipeBook,
        roomDecor: state.roomDecor,
        achievements: state.achievements,
        totalCoinsEarned: state.totalCoinsEarned,
        totalDaysPlayed: state.totalDaysPlayed,
        totalMinigamesPlayed: state.totalMinigamesPlayed,
        bugEventsHandled: state.bugEventsHandled,
        braveLevel: state.braveLevel,
        karateBelt: state.karateBelt,
        karateXP: state.karateXP,
        danceStars: state.danceStars,
        savedChoreographies: state.savedChoreographies,
        reelsCreated: state.reelsCreated,
        totalLikes: state.totalLikes,
        musicEnabled: state.musicEnabled,
        sfxEnabled: state.sfxEnabled,
        breakReminderMinutes: state.breakReminderMinutes,
        lastPlayedAt: new Date().toISOString(),
      };
      await AsyncStorage.setItem(STORAGE_KEY, JSON.stringify(saveData));
    } catch (error) {
      console.error('Failed to save game:', error);
    }
  },

  loadGame: async () => {
    try {
      const data = await AsyncStorage.getItem(STORAGE_KEY);
      if (data) {
        const parsed = JSON.parse(data) as Partial<GameState>;
        set({ ...initialState, ...parsed });
      }
    } catch (error) {
      console.error('Failed to load game:', error);
    }
  },

  resetGame: () => set({ ...initialState }),
}));
