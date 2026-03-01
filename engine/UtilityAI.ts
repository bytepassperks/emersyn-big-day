/**
 * UtilityAI.ts - Sims-style needs system with object advertisements,
 * autonomous behavior scoring, and real-time stat decay.
 */

/** Core character stats that decay over time */
export interface CharacterStats {
  hunger: number;     // 0-100
  energy: number;     // 0-100
  cleanliness: number;// 0-100
  fun: number;        // 0-100
  popularity: number; // 0-100
}

/** Decay rates per second for each stat */
const DECAY_RATES: Record<keyof CharacterStats, number> = {
  hunger: 0.08,       // ~13 minutes to empty from full
  energy: 0.06,       // ~28 minutes
  cleanliness: 0.04,  // ~42 minutes
  fun: 0.1,           // ~17 minutes
  popularity: 0.02,   // ~83 minutes
};

/** What an interactable object advertises it can satisfy */
export interface ObjectAdvertisement {
  objectId: string;
  statBoosts: Partial<Record<keyof CharacterStats, number>>; // how much it restores
  duration: number;   // seconds to complete
  cooldown: number;   // seconds before can use again
  lastUsedAt: number; // timestamp
  position: { x: number; z: number };
}

/** Result of scoring an action */
export interface ScoredAction {
  objectId: string;
  score: number;
  primaryNeed: keyof CharacterStats;
}

/** Personality weights for NPCs (modifies scoring) */
export interface PersonalityWeights {
  hungerWeight: number;
  energyWeight: number;
  cleanlinessWeight: number;
  funWeight: number;
  popularityWeight: number;
}

const DEFAULT_WEIGHTS: PersonalityWeights = {
  hungerWeight: 1.0,
  energyWeight: 1.0,
  cleanlinessWeight: 1.0,
  funWeight: 1.0,
  popularityWeight: 1.0,
};

/** Room object advertisement presets */
export const ROOM_ADVERTISEMENTS: Record<string, ObjectAdvertisement[]> = {
  bedroom: [
    { objectId: 'bed', statBoosts: { energy: 40 }, duration: 5, cooldown: 30, lastUsedAt: 0, position: { x: -1.5, z: -1 } },
    { objectId: 'mirror', statBoosts: { cleanliness: 10, fun: 5 }, duration: 2, cooldown: 10, lastUsedAt: 0, position: { x: 1.5, z: -1 } },
    { objectId: 'closet', statBoosts: { popularity: 15, fun: 10 }, duration: 3, cooldown: 20, lastUsedAt: 0, position: { x: 2, z: 0 } },
    { objectId: 'toy_box', statBoosts: { fun: 25 }, duration: 4, cooldown: 15, lastUsedAt: 0, position: { x: -2, z: 1 } },
  ],
  kitchen: [
    { objectId: 'fridge', statBoosts: { hunger: 30 }, duration: 3, cooldown: 15, lastUsedAt: 0, position: { x: -1, z: -2 } },
    { objectId: 'stove', statBoosts: { hunger: 50, fun: 10 }, duration: 5, cooldown: 20, lastUsedAt: 0, position: { x: 0, z: -2 } },
    { objectId: 'table', statBoosts: { hunger: 20, popularity: 5 }, duration: 3, cooldown: 10, lastUsedAt: 0, position: { x: 0, z: 0 } },
    { objectId: 'sink', statBoosts: { cleanliness: 15 }, duration: 2, cooldown: 8, lastUsedAt: 0, position: { x: 1.5, z: -2 } },
  ],
  bathroom: [
    { objectId: 'bathtub', statBoosts: { cleanliness: 50, fun: 10 }, duration: 5, cooldown: 30, lastUsedAt: 0, position: { x: -1, z: -1 } },
    { objectId: 'toilet', statBoosts: { cleanliness: 5 }, duration: 2, cooldown: 20, lastUsedAt: 0, position: { x: 1, z: -1.5 } },
    { objectId: 'sink_bath', statBoosts: { cleanliness: 20 }, duration: 2, cooldown: 10, lastUsedAt: 0, position: { x: 0, z: -2 } },
  ],
  park: [
    { objectId: 'swing', statBoosts: { fun: 30, energy: -5 }, duration: 4, cooldown: 10, lastUsedAt: 0, position: { x: -2, z: 0 } },
    { objectId: 'sandbox', statBoosts: { fun: 20, popularity: 10 }, duration: 5, cooldown: 15, lastUsedAt: 0, position: { x: 1, z: 1 } },
    { objectId: 'picnic', statBoosts: { hunger: 25, fun: 15, popularity: 10 }, duration: 4, cooldown: 20, lastUsedAt: 0, position: { x: 0, z: 2 } },
    { objectId: 'fountain', statBoosts: { cleanliness: 10, fun: 10 }, duration: 2, cooldown: 8, lastUsedAt: 0, position: { x: 2, z: -1 } },
  ],
  school: [
    { objectId: 'desk', statBoosts: { popularity: 15 }, duration: 5, cooldown: 20, lastUsedAt: 0, position: { x: 0, z: -1 } },
    { objectId: 'art_table', statBoosts: { fun: 25, popularity: 10 }, duration: 4, cooldown: 15, lastUsedAt: 0, position: { x: -2, z: 0 } },
    { objectId: 'playground', statBoosts: { fun: 30, energy: -10 }, duration: 4, cooldown: 10, lastUsedAt: 0, position: { x: 2, z: 1 } },
  ],
  arcade: [
    { objectId: 'dance_machine', statBoosts: { fun: 35, energy: -15 }, duration: 4, cooldown: 10, lastUsedAt: 0, position: { x: -1, z: -1 } },
    { objectId: 'claw_machine', statBoosts: { fun: 20 }, duration: 3, cooldown: 8, lastUsedAt: 0, position: { x: 1, z: -1 } },
    { objectId: 'racing_game', statBoosts: { fun: 30, energy: -10 }, duration: 4, cooldown: 12, lastUsedAt: 0, position: { x: 0, z: 1 } },
  ],
  shop: [
    { objectId: 'clothing_rack', statBoosts: { popularity: 20, fun: 10 }, duration: 3, cooldown: 15, lastUsedAt: 0, position: { x: -1, z: 0 } },
    { objectId: 'snack_bar', statBoosts: { hunger: 15, fun: 5 }, duration: 2, cooldown: 10, lastUsedAt: 0, position: { x: 1, z: 1 } },
  ],
  studio: [
    { objectId: 'easel', statBoosts: { fun: 30, popularity: 15 }, duration: 5, cooldown: 15, lastUsedAt: 0, position: { x: -1, z: -1 } },
    { objectId: 'music_corner', statBoosts: { fun: 25, popularity: 10 }, duration: 4, cooldown: 12, lastUsedAt: 0, position: { x: 1, z: -1 } },
    { objectId: 'craft_table', statBoosts: { fun: 20, popularity: 5 }, duration: 3, cooldown: 10, lastUsedAt: 0, position: { x: 0, z: 1 } },
  ],
  home: [
    { objectId: 'sofa', statBoosts: { energy: 15, fun: 10 }, duration: 3, cooldown: 10, lastUsedAt: 0, position: { x: -1, z: 0 } },
    { objectId: 'tv', statBoosts: { fun: 20 }, duration: 4, cooldown: 12, lastUsedAt: 0, position: { x: 0, z: -2 } },
    { objectId: 'bookshelf', statBoosts: { fun: 10, popularity: 5 }, duration: 3, cooldown: 10, lastUsedAt: 0, position: { x: 2, z: -1 } },
  ],
};

export class UtilityAI {
  stats: CharacterStats;
  private advertisements: ObjectAdvertisement[] = [];
  private personalityWeights: PersonalityWeights;

  constructor(initialStats?: Partial<CharacterStats>, weights?: Partial<PersonalityWeights>) {
    this.stats = {
      hunger: 80,
      energy: 90,
      cleanliness: 85,
      fun: 70,
      popularity: 50,
      ...initialStats,
    };
    this.personalityWeights = { ...DEFAULT_WEIGHTS, ...weights };
  }

  /** Decay all stats by dt seconds */
  decayStats(dt: number): void {
    const keys = Object.keys(DECAY_RATES) as (keyof CharacterStats)[];
    for (const key of keys) {
      this.stats[key] = Math.max(0, this.stats[key] - DECAY_RATES[key] * dt);
    }
  }

  /** Boost a stat (e.g. after completing an activity) */
  boostStat(stat: keyof CharacterStats, amount: number): void {
    this.stats[stat] = Math.max(0, Math.min(100, this.stats[stat] + amount));
  }

  /** Set advertisements for the current room */
  setAdvertisements(roomType: string): void {
    const ads = ROOM_ADVERTISEMENTS[roomType];
    if (ads) {
      this.advertisements = ads.map((a) => ({ ...a, lastUsedAt: 0 }));
    } else {
      this.advertisements = [];
    }
  }

  /** Calculate urgency for a stat (0-1, higher = more urgent) */
  private getUrgency(stat: keyof CharacterStats): number {
    const val = this.stats[stat];
    if (val <= 10) return 1.0;
    if (val <= 30) return 0.8;
    if (val <= 50) return 0.5;
    if (val <= 70) return 0.2;
    return 0.05;
  }

  /** Get the weight for a stat based on personality */
  private getWeight(stat: keyof CharacterStats): number {
    const map: Record<keyof CharacterStats, keyof PersonalityWeights> = {
      hunger: 'hungerWeight',
      energy: 'energyWeight',
      cleanliness: 'cleanlinessWeight',
      fun: 'funWeight',
      popularity: 'popularityWeight',
    };
    return this.personalityWeights[map[stat]];
  }

  /** Score all available actions and return sorted list */
  scoreActions(characterX: number, characterZ: number, now: number): ScoredAction[] {
    const scored: ScoredAction[] = [];
    for (const ad of this.advertisements) {
      // Check cooldown
      if (now - ad.lastUsedAt < ad.cooldown) continue;
      // Distance cost
      const dx = ad.position.x - characterX;
      const dz = ad.position.z - characterZ;
      const dist = Math.sqrt(dx * dx + dz * dz);
      const distCost = dist * 0.1;
      // Score each stat boost
      let totalScore = 0;
      let bestNeed: keyof CharacterStats = 'fun';
      let bestUrgency = 0;
      const boosts = ad.statBoosts;
      const keys = Object.keys(boosts) as (keyof CharacterStats)[];
      for (const stat of keys) {
        const boost = boosts[stat] || 0;
        if (boost === 0) continue;
        const urgency = this.getUrgency(stat);
        const weight = this.getWeight(stat);
        const s = urgency * (boost / 50) * weight;
        totalScore += s;
        if (urgency > bestUrgency) {
          bestUrgency = urgency;
          bestNeed = stat;
        }
      }
      totalScore -= distCost;
      if (totalScore > 0) {
        scored.push({ objectId: ad.objectId, score: totalScore, primaryNeed: bestNeed });
      }
    }
    scored.sort((a, b) => b.score - a.score);
    return scored;
  }

  /** Pick the best action (or null if nothing is urgent) */
  pickBestAction(characterX: number, characterZ: number, now: number): ScoredAction | null {
    const actions = this.scoreActions(characterX, characterZ, now);
    return actions.length > 0 ? actions[0] : null;
  }

  /** Mark an object as used (set cooldown timestamp) */
  markUsed(objectId: string, now: number): void {
    const ad = this.advertisements.find((a) => a.objectId === objectId);
    if (ad) { ad.lastUsedAt = now; }
  }

  /** Apply stat boosts from an object */
  applyAdvertisement(objectId: string): void {
    const ad = this.advertisements.find((a) => a.objectId === objectId);
    if (!ad) return;
    const keys = Object.keys(ad.statBoosts) as (keyof CharacterStats)[];
    for (const stat of keys) {
      const boost = ad.statBoosts[stat] || 0;
      this.boostStat(stat, boost);
    }
  }

  /** Get the most critical need */
  getMostCriticalNeed(): keyof CharacterStats {
    let worst: keyof CharacterStats = 'hunger';
    let worstVal = this.stats.hunger;
    const keys = Object.keys(this.stats) as (keyof CharacterStats)[];
    for (const key of keys) {
      if (this.stats[key] < worstVal) {
        worstVal = this.stats[key];
        worst = key;
      }
    }
    return worst;
  }

  /** Get emoji indicator for current mood */
  getMoodEmoji(): string {
    const critical = this.getMostCriticalNeed();
    const val = this.stats[critical];
    if (val < 15) {
      switch (critical) {
        case 'hunger': return 'starving';
        case 'energy': return 'exhausted';
        case 'cleanliness': return 'dirty';
        case 'fun': return 'bored';
        case 'popularity': return 'lonely';
      }
    }
    if (val < 40) {
      switch (critical) {
        case 'hunger': return 'hungry';
        case 'energy': return 'tired';
        case 'cleanliness': return 'messy';
        case 'fun': return 'restless';
        case 'popularity': return 'shy';
      }
    }
    // All stats are OK
    const avg = (this.stats.hunger + this.stats.energy + this.stats.cleanliness + this.stats.fun + this.stats.popularity) / 5;
    if (avg > 80) return 'thriving';
    if (avg > 60) return 'happy';
    return 'okay';
  }
}
