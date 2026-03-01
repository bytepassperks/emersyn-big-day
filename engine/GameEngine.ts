/**
 * GameEngine.ts - Core game engine with Utility AI integration,
 * stat decay, object advertisements, environment animations,
 * touch zones, drag-drop, and camera shake.
 */
import { THREE } from 'expo-three';
import { Renderer } from 'expo-three';
import { ExpoWebGLRenderingContext } from 'expo-gl';
import { Character, CharacterAnim } from './Character';
import { RoomBuilder, InteractableInfo } from './RoomBuilder';
import { ParticleSystem } from './ParticleSystem';
import { CameraController } from './CameraController';
import { NPCCharacter, NPCType } from './NPCCharacter';
import { AudioManager } from './AudioManager';
import { UtilityAI } from './UtilityAI';
import { RewardSystem } from './RewardSystem';

export type RoomType = 'bedroom' | 'kitchen' | 'park' | 'school' | 'arcade' | 'studio' | 'shop' | 'bathroom' | 'home';

// Random daily events
const DAILY_EVENTS = [
  { id: 'surprise_party', text: 'Surprise! A friend is visiting!', spawnNPC: 'ava' },
  { id: 'rainy_day', text: 'It\'s raining! Stay cozy inside!', weather: 'rain' },
  { id: 'pet_arrives', text: 'A cute kitty appeared!', spawnPet: 'pet_cat' },
  { id: 'bonus_coins', text: 'Lucky day! Double coins!', coinMultiplier: 2 },
  { id: 'field_trip', text: 'Field trip to the park!', bonusXP: true },
  { id: 'sticker_drop', text: 'You found a rare sticker!', stickerDrop: true },
  { id: 'dance_contest', text: 'Dance contest today!', bonusDance: true },
  { id: 'cooking_special', text: 'Special recipe unlocked!', specialRecipe: true },
];

// Random environment details per room visit
const RANDOM_DETAILS: Record<string, string[][]> = {
  park: [
    ['butterfly', 'ladybug', 'dragonfly'],
    ['sunny', 'partly_cloudy', 'breezy'],
    ['roses', 'daisies', 'tulips', 'sunflowers'],
  ],
  bedroom: [
    ['teddy_on_bed', 'books_on_desk', 'drawing_on_wall'],
    ['morning_light', 'afternoon_glow', 'starry_night'],
  ],
  kitchen: [
    ['fruit_bowl', 'cookie_jar', 'juice_box'],
    ['baking_aroma', 'fresh_herbs', 'sizzling_pan'],
  ],
};

export interface GameCallbacks {
  onInteract?: (interactable: InteractableInfo) => void;
  onNPCTap?: (npc: NPCCharacter) => void;
  onFloorTap?: (position: THREE.Vector3) => void;
  onActivityComplete?: (activityId: string) => void;
  onStatUpdate?: (stats: { hunger: number; energy: number; cleanliness: number; fun: number; popularity: number }) => void;
  onAchievement?: (achievement: { title: string; description: string; icon: string }) => void;
  onMoodChange?: (mood: string) => void;
  onRandomEvent?: (event: { title: string; description: string }) => void;
}

export class GameEngine {
  // Core Three.js
  scene: THREE.Scene;
  renderer: THREE.WebGLRenderer | null = null;
  private gl: ExpoWebGLRenderingContext | null = null;
  // Subsystems
  character: Character;
  camera: CameraController;
  particles: ParticleSystem;
  npcs: NPCCharacter[] = [];
  // AI & Rewards
  utilityAI: UtilityAI;
  rewardSystem: RewardSystem;
  // Room data
  currentRoom: RoomType = 'home';
  interactables: InteractableInfo[] = [];
  // State
  private lastTime: number = 0;
  private animationId: number | null = null;
  private callbacks: GameCallbacks = {};
  private coinMultiplier: number = 1;
  private activeInteraction: InteractableInfo | null = null;
  private interactionTimer: number = 0;
  private interactionDuration: number = 2.0;
  // Environment animation objects
  private animatedObjects: { mesh: THREE.Object3D; type: string; speed: number }[] = [];
  // Random details for this visit
  private visitSeed: number = Math.random();
  // Stat update timer (don't callback every frame)
  private statUpdateTimer: number = 0;
  private lastMood: string = 'happy';
  // Autonomous AI timer
  private aiDecisionTimer: number = 0;
  private aiDecisionInterval: number = 10; // seconds between autonomous decisions
  // Drag state
  private isDragging: boolean = false;
  private dragObject: string | null = null;

  constructor() {
    this.scene = new THREE.Scene();
    this.character = new Character();
    this.camera = new CameraController(1);
    this.particles = new ParticleSystem(this.scene);
    this.utilityAI = new UtilityAI();
    this.rewardSystem = new RewardSystem();
  }

  async init(gl: ExpoWebGLRenderingContext, width: number, height: number) {
    this.gl = gl;

    this.renderer = new Renderer({ gl }) as unknown as THREE.WebGLRenderer;
    this.renderer.setSize(width, height);
    this.renderer.setClearColor(0xffd4e8, 1);
    this.renderer.shadowMap.enabled = true;
    this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
    this.renderer.toneMappingExposure = 1.1;

    this.camera.updateAspect(width / height);

    await AudioManager.init();
    await this.rewardSystem.init();

    this.character.addToScene(this.scene);
    this.character.setPosition(0, 0, 0);

    this.camera.setFollowTarget(this.character.group);

    // Check daily login
    const loginResult = await this.rewardSystem.checkDailyLogin();
    if (loginResult.isNewDay && loginResult.reward) {
      // Emit coins and confetti for daily reward
      this.particles.emitConfetti(new THREE.Vector3(0, 1.5, 0));
      this.particles.emitCoinCollect(new THREE.Vector3(0, 1, 0));
    }
  }

  loadRoom(roomType: RoomType) {
    this.clearScene();
    this.currentRoom = roomType;
    this.visitSeed = Math.random();

    const roomData = RoomBuilder.build(this.scene, roomType);
    this.interactables = roomData.interactables;

    this.character.addToScene(this.scene);
    this.character.setPosition(0, 0, 1.5);
    this.character.setAnimation('idle');

    this.camera.setRoom(roomType);
    this.camera.setFollowTarget(this.character.group);

    this.addInteractableIndicators();
    this.addRoomNPCs(roomType);
    this.addEnvironmentAnimations(roomType);

    // Set utility AI advertisements for this room
    this.utilityAI.setAdvertisements(roomType);

    // Random daily event check (10% chance per room visit)
    if (Math.random() < 0.1) {
      this.triggerRandomEvent();
    }

    // Random events from reward system
    const events = this.rewardSystem.rollRandomEvents();
    for (const event of events) {
      this.callbacks.onRandomEvent?.({ title: event.title, description: event.description });
      if (event.reward?.coins) {
        this.particles.emitCoinCollect(this.character.group.position.clone().add(new THREE.Vector3(0, 1, 0)));
      }
    }

    AudioManager.playBGM(roomType);
  }

  private clearScene() {
    // Dispose geometries and materials to prevent GPU memory leaks
    const toRemove = [...this.scene.children];
    for (const child of toRemove) {
      child.traverse((node: THREE.Object3D) => {
        if (node instanceof THREE.Mesh) {
          node.geometry.dispose();
          if (Array.isArray(node.material)) {
            node.material.forEach((m: THREE.Material) => { m.dispose(); });
          } else {
            node.material.dispose();
          }
        }
      });
      this.scene.remove(child);
    }
    this.npcs.forEach((npc) => { npc.dispose(); });
    this.npcs = [];
    this.particles.clear();
    this.animatedObjects = [];
  }

  private addInteractableIndicators() {
    this.interactables.forEach((item) => {
      const ringGeom = new THREE.RingGeometry(0.15, 0.2, 16);
      const ringMat = new THREE.MeshBasicMaterial({
        color: 0xffd93d, transparent: true, opacity: 0.6, side: THREE.DoubleSide,
      });
      const ring = new THREE.Mesh(ringGeom, ringMat);
      ring.rotation.x = -Math.PI / 2;
      ring.position.copy(item.walkToOffset);
      ring.position.y = 0.02;
      ring.name = `indicator_${item.id}`;
      ring.userData = { isIndicator: true, interactableId: item.id };
      this.scene.add(ring);

      const dotGeom = new THREE.SphereGeometry(0.06, 8, 8);
      const dotMat = new THREE.MeshBasicMaterial({
        color: 0xffd93d, transparent: true, opacity: 0.8,
      });
      const dot = new THREE.Mesh(dotGeom, dotMat);
      dot.position.copy(item.position);
      dot.position.y += 0.4;
      dot.name = `dot_${item.id}`;
      dot.userData = { isIndicator: true, interactableId: item.id, isDot: true };
      this.scene.add(dot);
    });
  }

  private addRoomNPCs(roomType: RoomType) {
    switch (roomType) {
      case 'park': {
        const friend = new NPCCharacter('ava', 'friend', new THREE.Vector3(-1, 0, 1));
        friend.setWanderRadius(2.5);
        friend.addToScene(this.scene);
        this.npcs.push(friend);
        if (this.visitSeed > 0.5) {
          const pet = new NPCCharacter('pet_cat', 'pet_cat', new THREE.Vector3(1, 0, 0.5));
          pet.setWanderRadius(3.0);
          pet.addToScene(this.scene);
          this.npcs.push(pet);
        }
        break;
      }
      case 'school': {
        const teacher = new NPCCharacter('teacher', 'teacher', new THREE.Vector3(0, 0, -2.5));
        teacher.setWanderRadius(1.0);
        teacher.addToScene(this.scene);
        this.npcs.push(teacher);
        const friend = new NPCCharacter('mia', 'friend', new THREE.Vector3(1.5, 0, -1));
        friend.setWanderRadius(1.5);
        friend.addToScene(this.scene);
        this.npcs.push(friend);
        break;
      }
      case 'shop': {
        const shopkeeper = new NPCCharacter('shopkeeper', 'shopkeeper', new THREE.Vector3(1.5, 0, -0.5));
        shopkeeper.setWanderRadius(0.5);
        shopkeeper.addToScene(this.scene);
        this.npcs.push(shopkeeper);
        break;
      }
      case 'arcade': {
        const friend = new NPCCharacter('leo', 'friend', new THREE.Vector3(-1, 0, -1));
        friend.setWanderRadius(2.0);
        friend.addToScene(this.scene);
        this.npcs.push(friend);
        break;
      }
      case 'home': {
        if (this.visitSeed > 0.3) {
          const pet = new NPCCharacter('pet_bunny', 'pet_bunny', new THREE.Vector3(1, 0, -1));
          pet.setWanderRadius(2.0);
          pet.addToScene(this.scene);
          this.npcs.push(pet);
        }
        break;
      }
    }
  }

  private addEnvironmentAnimations(roomType: RoomType) {
    switch (roomType) {
      case 'park': {
        // Butterflies
        for (let i = 0; i < 3; i++) {
          const bfGroup = new THREE.Group();
          const wingGeom = new THREE.PlaneGeometry(0.08, 0.06);
          const wingColors = [0xff6b9d, 0xffd93d, 0x42a5f5];
          const wingMat = new THREE.MeshBasicMaterial({
            color: wingColors[i], transparent: true, opacity: 0.8, side: THREE.DoubleSide,
          });
          const leftWing = new THREE.Mesh(wingGeom, wingMat);
          leftWing.position.x = -0.04;
          bfGroup.add(leftWing);
          const rightWing = new THREE.Mesh(wingGeom, wingMat);
          rightWing.position.x = 0.04;
          bfGroup.add(rightWing);
          bfGroup.position.set(-2 + Math.random() * 4, 0.8 + Math.random() * 0.5, -2 + Math.random() * 3);
          this.scene.add(bfGroup);
          this.animatedObjects.push({ mesh: bfGroup, type: 'butterfly', speed: 3 + Math.random() * 2 });
        }
        // Floating clouds
        for (let i = 0; i < 2; i++) {
          const cloudGroup = new THREE.Group();
          const cloudMat = new THREE.MeshBasicMaterial({ color: 0xffffff, transparent: true, opacity: 0.7 });
          for (let j = 0; j < 3; j++) {
            const puff = new THREE.Mesh(new THREE.SphereGeometry(0.2 + Math.random() * 0.15, 8, 8), cloudMat);
            puff.position.set((j - 1) * 0.25, Math.random() * 0.1, 0);
            cloudGroup.add(puff);
          }
          cloudGroup.position.set(-3 + i * 6, 4 + Math.random(), -3);
          this.scene.add(cloudGroup);
          this.animatedObjects.push({ mesh: cloudGroup, type: 'cloud', speed: 0.1 + Math.random() * 0.1 });
        }
        break;
      }
      case 'bedroom': {
        // Floating dust motes
        for (let i = 0; i < 5; i++) {
          const mote = new THREE.Mesh(
            new THREE.SphereGeometry(0.01, 4, 4),
            new THREE.MeshBasicMaterial({ color: 0xffd93d, transparent: true, opacity: 0.4 }),
          );
          mote.position.set(-2 + Math.random() * 4, 0.5 + Math.random() * 2, -2 + Math.random() * 3);
          this.scene.add(mote);
          this.animatedObjects.push({ mesh: mote, type: 'dust_mote', speed: 0.5 + Math.random() * 0.5 });
        }
        break;
      }
      case 'kitchen': {
        // Steam from stove area
        this.animatedObjects.push({ mesh: new THREE.Group(), type: 'steam_emitter', speed: 1 });
        break;
      }
      case 'bathroom': {
        // Dripping water
        this.animatedObjects.push({ mesh: new THREE.Group(), type: 'drip_emitter', speed: 2 });
        break;
      }
    }
    // Add swaying plants to indoor rooms
    if (['bedroom', 'kitchen', 'home', 'studio'].includes(roomType)) {
      const plantGroup = new THREE.Group();
      const stemGeom = new THREE.CylinderGeometry(0.02, 0.025, 0.3, 6);
      const stemMat = new THREE.MeshPhongMaterial({ color: 0x4caf50 });
      const stem = new THREE.Mesh(stemGeom, stemMat);
      stem.position.y = 0.15;
      plantGroup.add(stem);
      const leafGeom = new THREE.SphereGeometry(0.1, 8, 8);
      leafGeom.scale(1.2, 0.8, 1);
      const leafMat = new THREE.MeshPhongMaterial({ color: 0x66bb6a });
      const leaves = new THREE.Mesh(leafGeom, leafMat);
      leaves.position.y = 0.35;
      plantGroup.add(leaves);
      plantGroup.position.set(2.5, 0, 2);
      this.scene.add(plantGroup);
      this.animatedObjects.push({ mesh: plantGroup, type: 'plant_sway', speed: 1 });
    }
  }

  private triggerRandomEvent() {
    const event = DAILY_EVENTS[Math.floor(Math.random() * DAILY_EVENTS.length)];
    if (event.coinMultiplier) {
      this.coinMultiplier = event.coinMultiplier;
    }
    if (event.spawnPet) {
      const pet = new NPCCharacter(event.spawnPet, event.spawnPet as NPCType, new THREE.Vector3(
        Math.random() * 2 - 1, 0, Math.random() * 2,
      ));
      pet.setWanderRadius(2.0);
      pet.addToScene(this.scene);
      this.npcs.push(pet);
    }
    if (event.spawnNPC) {
      const npc = new NPCCharacter(event.spawnNPC, 'friend', new THREE.Vector3(
        Math.random() * 2 - 1, 0, Math.random() * 2,
      ));
      npc.setWanderRadius(2.0);
      npc.addToScene(this.scene);
      this.npcs.push(npc);
    }
    this.callbacks.onRandomEvent?.({ title: event.id, description: event.text });
  }

  setCallbacks(callbacks: GameCallbacks) {
    this.callbacks = callbacks;
  }

  handleTap(screenX: number, screenY: number, viewWidth: number, viewHeight: number) {
    if (this.activeInteraction) return;

    const worldPos = this.camera.screenToWorld(screenX, screenY, viewWidth, viewHeight);

    if (worldPos) {
      // Check if tapped on character (touch zone detection)
      const charPos = this.character.group.position;
      const distToChar = new THREE.Vector2(worldPos.x - charPos.x, worldPos.z - charPos.z).length();
      if (distToChar < 0.5) {
        // Character tapped - determine zone
        const localY = worldPos.y || 0.5;
        const zone = this.character.getTouchZone(localY);
        this.character.onTap(zone);
        // Particles based on zone
        const charTop = charPos.clone().add(new THREE.Vector3(0, 1, 0));
        this.particles.emitHearts(charTop);
        this.particles.emitStars(charTop);
        // Camera shake on belly tap
        if (zone === 'belly') {
          this.camera.shake(0.05, 0.2);
        }
        // Boost fun stat
        this.utilityAI.boostStat('fun', 5);
        AudioManager.playSFX('tap');
        return;
      }

      // Check proximity to interactables
      let closestInteractable: InteractableInfo | null = null;
      let closestDist = Infinity;
      for (const item of this.interactables) {
        const dist = worldPos.distanceTo(item.walkToOffset);
        if (dist < 1.0 && dist < closestDist) {
          closestDist = dist;
          closestInteractable = item;
        }
      }
      if (closestInteractable) {
        this.character.walkTo(closestInteractable.walkToOffset, () => {
          this.startInteraction(closestInteractable!);
        });
        AudioManager.playSFX('tap');
        return;
      }

      // Check NPC tap
      for (const npc of this.npcs) {
        const npcDist = worldPos.distanceTo(npc.group.position);
        if (npcDist < 0.8) {
          const walkTarget = npc.group.position.clone();
          walkTarget.z += 0.5;
          this.character.walkTo(walkTarget, () => {
            this.character.setAnimation('wave');
            npc.onInteraction();
            this.utilityAI.boostStat('popularity', 5);
            this.utilityAI.boostStat('fun', 3);
            this.callbacks.onNPCTap?.(npc);
            setTimeout(() => { this.character.setAnimation('idle'); }, 2000);
          });
          AudioManager.playSFX('tap');
          return;
        }
      }

      // Tap on floor - walk there
      if (worldPos.y <= 0.1) {
        worldPos.x = Math.max(-3.5, Math.min(3.5, worldPos.x));
        worldPos.z = Math.max(-3.5, Math.min(3.5, worldPos.z));
        worldPos.y = 0;
        this.character.walkTo(worldPos);
        this.particles.emitDust(worldPos);
        AudioManager.playSFX('tap');
        this.callbacks.onFloorTap?.(worldPos);
      }
    }
  }

  /** Start drag from inventory (food, clothes, etc.) */
  startDrag(objectType: string): void {
    this.isDragging = true;
    this.dragObject = objectType;
  }

  /** End drag at a world position */
  endDrag(screenX: number, screenY: number, viewWidth: number, viewHeight: number): void {
    if (!this.isDragging || !this.dragObject) return;
    this.isDragging = false;
    const worldPos = this.camera.screenToWorld(screenX, screenY, viewWidth, viewHeight);
    if (!worldPos) { this.dragObject = null; return; }

    // Check if dropped on character
    const charPos = this.character.group.position;
    const dist = new THREE.Vector2(worldPos.x - charPos.x, worldPos.z - charPos.z).length();
    if (dist < 1.0) {
      // Apply drag object effect
      switch (this.dragObject) {
        case 'food':
          this.character.setAnimation('eat');
          this.utilityAI.boostStat('hunger', 20);
          this.particles.emitHearts(charPos.clone().add(new THREE.Vector3(0, 1, 0)));
          setTimeout(() => { this.character.setAnimation('happy'); }, 2000);
          setTimeout(() => { this.character.setAnimation('idle'); }, 3500);
          break;
        case 'clothes':
          this.character.setAnimation('happy');
          this.utilityAI.boostStat('popularity', 15);
          this.particles.emitConfetti(charPos.clone().add(new THREE.Vector3(0, 1.5, 0)));
          setTimeout(() => { this.character.setAnimation('idle'); }, 2000);
          break;
        case 'soap':
          this.character.setAnimation('clean');
          this.utilityAI.boostStat('cleanliness', 25);
          this.particles.emitBubbles(charPos.clone().add(new THREE.Vector3(0, 0.5, 0)));
          setTimeout(() => { this.character.setAnimation('idle'); }, 2500);
          break;
        case 'toy':
          this.character.setAnimation('happy');
          this.utilityAI.boostStat('fun', 20);
          this.particles.emitStars(charPos.clone().add(new THREE.Vector3(0, 1, 0)));
          setTimeout(() => { this.character.setAnimation('idle'); }, 2000);
          break;
      }
      this.camera.shake(0.03, 0.15);
      AudioManager.playSFX('success');
    }
    this.dragObject = null;
  }

  private startInteraction(interactable: InteractableInfo) {
    this.activeInteraction = interactable;
    this.interactionTimer = 0;

    const dir = new THREE.Vector3().subVectors(interactable.position, this.character.group.position);
    const angle = Math.atan2(dir.x, dir.z);
    this.character.setRotation(angle);
    this.character.setAnimation(interactable.animOnInteract as CharacterAnim);

    this.camera.zoomTo(interactable.position);

    // Particles and stat boosts based on category
    switch (interactable.category) {
      case 'eat':
        this.particles.emitHearts(interactable.position);
        this.utilityAI.boostStat('hunger', 15);
        break;
      case 'clean':
        this.particles.emitBubbles(interactable.position);
        this.utilityAI.boostStat('cleanliness', 15);
        break;
      case 'fun':
        this.particles.emitStars(interactable.position);
        this.utilityAI.boostStat('fun', 15);
        break;
      case 'cook':
        this.particles.emitSteam(interactable.position);
        this.particles.emitStars(interactable.position);
        this.utilityAI.boostStat('hunger', 10);
        this.utilityAI.boostStat('fun', 10);
        break;
      default:
        this.particles.emitStars(interactable.position);
        this.utilityAI.boostStat('fun', 10);
    }

    // Mark the advertisement as used
    this.utilityAI.markUsed(interactable.id, Date.now() / 1000);

    AudioManager.playSFX('success');
  }

  private endInteraction() {
    if (!this.activeInteraction) return;

    this.particles.emitCoinCollect(this.character.group.position.clone().add(new THREE.Vector3(0, 1, 0)));

    this.callbacks.onInteract?.(this.activeInteraction);
    this.callbacks.onActivityComplete?.(this.activeInteraction.id);

    this.character.setAnimation('happy');
    this.camera.zoomOut();
    this.camera.shake(0.02, 0.1);

    setTimeout(() => { this.character.setAnimation('idle'); }, 1500);

    this.activeInteraction = null;
    AudioManager.playSFX('coin_collect');
  }

  getCoinMultiplier(): number {
    return this.coinMultiplier;
  }

  getStats() {
    return { ...this.utilityAI.stats };
  }

  getMood(): string {
    return this.utilityAI.getMoodEmoji();
  }

  update() {
    const now = Date.now() / 1000;
    if (this.lastTime === 0) { this.lastTime = now; return; }
    const dt = Math.min(now - this.lastTime, 0.05);
    this.lastTime = now;

    // Decay stats
    this.utilityAI.decayStats(dt);

    // Update character
    this.character.update(dt);

    // Update NPCs
    this.npcs.forEach((npc) => { npc.update(dt); });

    // Update particles
    this.particles.update(dt);

    // Update camera
    this.camera.update(dt);

    // Update interaction timer
    if (this.activeInteraction) {
      this.interactionTimer += dt;
      if (this.interactionTimer >= this.interactionDuration) {
        this.endInteraction();
      }
    }

    // Animate environment objects
    this.updateEnvironmentAnimations(dt);

    // Animate interactable indicators
    this.updateIndicators(dt);

    // Periodic stat update callback
    this.statUpdateTimer += dt;
    if (this.statUpdateTimer >= 1.0) {
      this.statUpdateTimer = 0;
      this.callbacks.onStatUpdate?.(this.utilityAI.stats);
      const mood = this.utilityAI.getMoodEmoji();
      if (mood !== this.lastMood) {
        this.lastMood = mood;
        this.callbacks.onMoodChange?.(mood);
        // Change character emotion based on mood
        if (mood === 'starving' || mood === 'exhausted' || mood === 'dirty') {
          this.character.setEmotion('sad');
        } else if (mood === 'hungry' || mood === 'tired' || mood === 'bored') {
          this.character.setEmotion('hungry');
        } else if (mood === 'thriving') {
          this.character.setEmotion('excited');
        } else {
          this.character.setEmotion('happy');
        }
      }
      // Sleep particles when energy is very low
      if (this.utilityAI.stats.energy < 20 && !this.activeInteraction) {
        this.particles.emitZzz(this.character.group.position.clone().add(new THREE.Vector3(0.3, 1.3, 0)));
      }
    }

    // Autonomous AI decisions (if idle for a while)
    this.aiDecisionTimer += dt;
    if (this.aiDecisionTimer >= this.aiDecisionInterval && !this.activeInteraction && !this.character.isMoving) {
      this.aiDecisionTimer = 0;
      this.aiDecisionInterval = 8 + Math.random() * 8;
      // Use utility AI to pick an autonomous action
      const action = this.utilityAI.pickBestAction(
        this.character.group.position.x,
        this.character.group.position.z,
        now,
      );
      if (action && action.score > 0.3) {
        // Find the interactable matching this action
        const target = this.interactables.find((i) => i.id === action.objectId);
        if (target) {
          this.character.walkTo(target.walkToOffset, () => {
            this.startInteraction(target);
          });
        }
      }
    }

    // Render
    if (this.renderer) {
      this.renderer.render(this.scene, this.camera.camera);
      if (this.gl) {
        this.gl.endFrameEXP();
      }
    }
  }

  private updateEnvironmentAnimations(dt: number) {
    const time = Date.now() / 1000;
    this.animatedObjects.forEach((obj) => {
      switch (obj.type) {
        case 'butterfly': {
          obj.mesh.position.x += Math.sin(time * obj.speed) * 0.5 * dt;
          obj.mesh.position.z += Math.cos(time * obj.speed * 0.7) * 0.3 * dt;
          obj.mesh.position.y = 0.8 + Math.sin(time * obj.speed * 1.5) * 0.15;
          obj.mesh.rotation.y = time * obj.speed * 0.5;
          if (obj.mesh.children.length >= 2) {
            obj.mesh.children[0].rotation.y = Math.sin(time * 15) * 0.5;
            obj.mesh.children[1].rotation.y = -Math.sin(time * 15) * 0.5;
          }
          break;
        }
        case 'cloud': {
          obj.mesh.position.x += obj.speed * dt;
          if (obj.mesh.position.x > 5) { obj.mesh.position.x = -5; }
          break;
        }
        case 'dust_mote': {
          obj.mesh.position.y += Math.sin(time * obj.speed + obj.mesh.position.x) * 0.1 * dt;
          obj.mesh.position.x += Math.sin(time * 0.3 + obj.mesh.position.z) * 0.05 * dt;
          break;
        }
        case 'plant_sway': {
          obj.mesh.rotation.z = Math.sin(time * 0.8) * 0.03;
          obj.mesh.rotation.x = Math.sin(time * 0.5 + 1) * 0.02;
          break;
        }
        case 'steam_emitter': {
          // Periodically emit steam particles
          if (Math.random() < 0.02) {
            this.particles.emitSteam(new THREE.Vector3(0, 0.8, -2));
          }
          break;
        }
        case 'drip_emitter': {
          if (Math.random() < 0.01) {
            this.particles.emitSplash(new THREE.Vector3(1, 0.1, -1.5));
          }
          break;
        }
      }
    });
  }

  private updateIndicators(dt: number) {
    const time = Date.now() / 1000;
    this.scene.traverse((child: THREE.Object3D) => {
      if (child.userData?.isIndicator) {
        if (child.userData.isDot) {
          if (child.userData.baseY === undefined) {
            child.userData.baseY = child.position.y;
          }
          child.position.y = child.userData.baseY + Math.sin(time * 3) * 0.02;
          if (child instanceof THREE.Mesh) {
            const mat = child.material as THREE.MeshBasicMaterial;
            mat.opacity = 0.5 + Math.sin(time * 4) * 0.3;
          }
        } else {
          const scale = 1 + Math.sin(time * 3) * 0.15;
          child.scale.set(scale, scale, 1);
          if (child instanceof THREE.Mesh) {
            const mat = child.material as THREE.MeshBasicMaterial;
            mat.opacity = 0.3 + Math.sin(time * 2) * 0.2;
          }
        }
      }
    });
  }

  startLoop() {
    // Guard against multiple concurrent RAF loops
    if (this.animationId !== null) {
      cancelAnimationFrame(this.animationId);
      this.animationId = null;
    }
    this.lastTime = 0;
    const loop = () => {
      this.update();
      this.animationId = requestAnimationFrame(loop);
    };
    loop();
  }

  stopLoop() {
    if (this.animationId !== null) {
      cancelAnimationFrame(this.animationId);
      this.animationId = null;
    }
  }

  dispose() {
    this.stopLoop();
    this.clearScene();
    this.character.dispose();
    if (this.renderer) {
      this.renderer.dispose();
    }
    AudioManager.stopBGM();
  }
}
