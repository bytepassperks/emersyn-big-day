/**
 * ModelAssets.ts - Asset map for .glb 3D character models
 * Maps character IDs to their bundled .glb asset requires
 * Metro bundles these via the assetExts config in metro.config.js
 */
import { Asset } from 'expo-asset';

// Asset requires for all character models
// These are resolved at build time by Metro bundler
const MODEL_REQUIRES: Record<string, number> = {
  emersyn: require('../assets/models/emersyn.glb') as number,
  ava: require('../assets/models/ava.glb') as number,
  mia: require('../assets/models/mia.glb') as number,
  leo: require('../assets/models/leo.glb') as number,
  shopkeeper: require('../assets/models/shopkeeper.glb') as number,
  teacher: require('../assets/models/teacher.glb') as number,
  pet_cat: require('../assets/models/pet_cat.glb') as number,
  pet_dog: require('../assets/models/pet_dog.glb') as number,
  pet_bunny: require('../assets/models/pet_bunny.glb') as number,
};

/**
 * Get the local URI for a character model asset.
 * Downloads asset if needed and returns the local file URI.
 */
export async function getModelUri(characterId: string): Promise<string | null> {
  const assetModule = MODEL_REQUIRES[characterId];
  if (!assetModule) {
    console.warn(`No model asset found for character: ${characterId}`);
    return null;
  }

  try {
    const asset = Asset.fromModule(assetModule);
    await asset.downloadAsync();
    return asset.localUri || asset.uri;
  } catch (error) {
    console.warn(`Failed to load model asset for ${characterId}:`, error);
    return null;
  }
}

/** List of all available character model IDs */
export const CHARACTER_MODEL_IDS = Object.keys(MODEL_REQUIRES);

/** Animation names that are baked into each humanoid .glb model */
export const HUMANOID_ANIMATIONS = [
  'idle', 'walk', 'run', 'happy', 'dance', 'wave', 'eat', 'sleep', 'jump',
] as const;

/** Animation names for pet .glb models */
export const PET_ANIMATIONS = ['idle'] as const;
