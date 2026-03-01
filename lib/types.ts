// ============================================================
// Emersyn's Big Day - Core Type Definitions
// ============================================================

export type DayType = 'weekday' | 'weekend';

export type TimeSegment =
  | 'morning'
  | 'school'
  | 'lunch'
  | 'nap'
  | 'park'
  | 'backHome'
  | 'homework'
  | 'arcade'
  | 'shopping'
  | 'dinner'
  | 'bedtime'
  | 'weekendStudio';

export interface PlayerStats {
  hunger: number;     // 0-100
  energy: number;     // 0-100
  cleanliness: number;// 0-100
  fun: number;        // 0-100
  popularity: number; // 0-100
}

export interface GameState {
  // Player
  playerName: string;
  coins: number;
  stars: number;
  xp: number;
  level: number;
  stats: PlayerStats;

  // Day progression
  currentDay: number;
  dayType: DayType;
  currentSegment: TimeSegment;
  segmentsCompleted: TimeSegment[];

  // Inventory & collections
  inventory: InventoryItem[];
  equippedOutfit: EquippedOutfit;
  stickerAlbum: StickerAlbum;
  recipeBook: string[]; // unlocked recipe IDs
  roomDecor: RoomDecorItem[];

  // Achievements
  achievements: Achievement[];
  totalCoinsEarned: number;
  totalDaysPlayed: number;
  totalMinigamesPlayed: number;

  // Bug system
  bugEventsHandled: number;
  braveLevel: number;

  // Belt progression (karate)
  karateBelt: KarateBelt;
  karateXP: number;

  // Dance
  danceStars: number;
  savedChoreographies: Choreography[];

  // Weekend creator
  reelsCreated: number;
  totalLikes: number;

  // Settings
  musicEnabled: boolean;
  sfxEnabled: boolean;
  breakReminderMinutes: number | null;

  // Timestamps
  lastPlayedAt: string | null;
}

export interface InventoryItem {
  id: string;
  category: ShopCategory;
  equipped: boolean;
  purchasedAt: string;
}

export type ShopCategory =
  | 'bags'
  | 'unicorn'
  | 'clothes_top'
  | 'clothes_bottom'
  | 'clothes_dress'
  | 'shoes'
  | 'accessories'
  | 'hair'
  | 'makeup'
  | 'toys'
  | 'books'
  | 'room_decor'
  | 'food_ingredients'
  | 'bug_safety';

export interface ShopItem {
  id: string;
  name: string;
  category: ShopCategory;
  price: number;
  emoji: string;
  description: string;
  rarity: 'common' | 'rare' | 'epic' | 'legendary';
  unlockLevel: number;
}

export interface EquippedOutfit {
  hair: string | null;
  top: string | null;
  bottom: string | null;
  dress: string | null;
  shoes: string | null;
  bag: string | null;
  accessory: string | null;
}

export interface Recipe {
  id: string;
  name: string;
  emoji: string;
  category: 'breakfast' | 'lunch' | 'snack' | 'dinner' | 'dessert' | 'drink';
  ingredients: string[];
  steps: string[];
  coinReward: number;
  healthBonus: number;
  happinessBonus: number;
  difficulty: 1 | 2 | 3;
  unlockLevel: number;
}

export interface StickerAlbum {
  pages: StickerPage[];
}

export interface StickerPage {
  id: string;
  name: string;
  emoji: string;
  stickers: Sticker[];
  reward: string;
  completed: boolean;
}

export interface Sticker {
  id: string;
  name: string;
  emoji: string;
  earned: boolean;
  earnedAt: string | null;
}

export interface Achievement {
  id: string;
  name: string;
  emoji: string;
  description: string;
  earned: boolean;
  earnedAt: string | null;
}

export interface RoomDecorItem {
  id: string;
  slot: 'bed' | 'wall' | 'floor' | 'lamp' | 'shelf' | 'poster' | 'rug' | 'nightlight';
  itemId: string;
}

export type KarateBelt = 'white' | 'yellow' | 'orange' | 'green' | 'blue' | 'purple' | 'brown' | 'black';

export interface Choreography {
  id: string;
  name: string;
  moves: string[];
  createdAt: string;
}

export interface Activity {
  id: string;
  name: string;
  emoji: string;
  segment: TimeSegment;
  statDeltas: Partial<PlayerStats>;
  coinReward: number;
  starReward: number;
  xpReward: number;
  durationSeconds: number;
  miniGameRoute?: string;
  stickerDrop?: string;
}

export interface MiniGameResult {
  score: number;
  coinsEarned: number;
  xpEarned: number;
  starsEarned: number;
  perfectRun: boolean;
}

export interface BugEvent {
  id: string;
  type: 'mosquito' | 'fly' | 'ant' | 'spider';
  location: 'bedroom' | 'kitchen' | 'window';
  actions: BugAction[];
}

export interface BugAction {
  id: string;
  name: string;
  emoji: string;
  effectiveness: number;
}
