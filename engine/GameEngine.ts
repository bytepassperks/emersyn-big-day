/**
 * GameEngine.ts - Core game engine tying together all subsystems
 * Manages the game loop, scene, character, NPCs, particles, camera
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

export type RoomType = 'bedroom' | 'kitchen' | 'park' | 'school' | 'arcade' | 'studio' | 'shop' | 'bathroom' | 'home';

// Random daily events
const DAILY_EVENTS = [
  { id: 'surprise_party', text: '🎉 Surprise! A friend is visiting!', spawnNPC: 'ava' },
  { id: 'rainy_day', text: '🌧️ It\'s raining! Stay cozy inside!', weather: 'rain' },
  { id: 'pet_arrives', text: '🐱 A cute kitty appeared!', spawnPet: 'pet_cat' },
  { id: 'bonus_coins', text: '💰 Lucky day! Double coins!', coinMultiplier: 2 },
  { id: 'field_trip', text: '🚌 Field trip to the park!', bonusXP: true },
  { id: 'sticker_drop', text: '⭐ You found a rare sticker!', stickerDrop: true },
  { id: 'dance_contest', text: '💃 Dance contest today!', bonusDance: true },
  { id: 'cooking_special', text: '🍰 Special recipe unlocked!', specialRecipe: true },
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
}

export class GameEngine {
  // Core Three.js
  scene: THREE.Scene;
  renderer: THREE.WebGLRenderer | null = null;
  // Subsystems
  character: Character;
  camera: CameraController;
  particles: ParticleSystem;
  npcs: NPCCharacter[] = [];
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

  constructor() {
    this.scene = new THREE.Scene();
    this.character = new Character();
    this.camera = new CameraController(1); // aspect will be updated
    this.particles = new ParticleSystem(this.scene);
  }

  async init(gl: ExpoWebGLRenderingContext, width: number, height: number) {
    // Renderer
    this.renderer = new Renderer({ gl }) as unknown as THREE.WebGLRenderer;
    this.renderer.setSize(width, height);
    this.renderer.shadowMap.enabled = true;
    this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
    this.renderer.toneMappingExposure = 1.1;

    // Camera
    this.camera.updateAspect(width / height);

    // Audio
    await AudioManager.init();

    // Character
    this.character.addToScene(this.scene);
    this.character.setPosition(0, 0, 0);

    // Camera follows character
    this.camera.setFollowTarget(this.character.group);
  }

  loadRoom(roomType: RoomType) {
    // Clear previous room
    this.clearScene();
    this.currentRoom = roomType;
    this.visitSeed = Math.random();

    // Build room
    const roomData = RoomBuilder.build(this.scene, roomType);
    this.interactables = roomData.interactables;

    // Re-add character
    this.character.addToScene(this.scene);
    this.character.setPosition(0, 0, 1.5);
    this.character.setAnimation('idle');

    // Camera
    this.camera.setRoom(roomType);
    this.camera.setFollowTarget(this.character.group);

    // Add glow indicators to interactable objects
    this.addInteractableIndicators();

    // Add random NPCs based on room
    this.addRoomNPCs(roomType);

    // Add animated environment objects
    this.addEnvironmentAnimations(roomType);

    // Random daily event check (10% chance per room visit)
    if (Math.random() < 0.1) {
      this.triggerRandomEvent();
    }

    // Audio
    AudioManager.playBGM(roomType);
  }

  private clearScene() {
    // Remove everything except camera
    const toRemove: THREE.Object3D[] = [];
    this.scene.traverse((child: THREE.Object3D) => {
      if (child !== this.scene) {
        toRemove.push(child);
      }
    });
    // Clear top-level children
    while (this.scene.children.length > 0) {
      this.scene.remove(this.scene.children[0]);
    }
    // Dispose NPCs
    this.npcs.forEach((npc) => npc.dispose());
    this.npcs = [];
    // Clear particles
    this.particles.clear();
    // Clear animated objects
    this.animatedObjects = [];
  }

  private addInteractableIndicators() {
    // Add glowing ring indicators at interactable positions
    this.interactables.forEach((item) => {
      const ringGeom = new THREE.RingGeometry(0.15, 0.2, 16);
      const ringMat = new THREE.MeshBasicMaterial({
        color: 0xffd93d,
        transparent: true,
        opacity: 0.6,
        side: THREE.DoubleSide,
      });
      const ring = new THREE.Mesh(ringGeom, ringMat);
      ring.rotation.x = -Math.PI / 2;
      ring.position.copy(item.walkToOffset);
      ring.position.y = 0.02;
      ring.name = `indicator_${item.id}`;
      ring.userData = { isIndicator: true, interactableId: item.id };
      this.scene.add(ring);

      // Floating label dot above the object
      const dotGeom = new THREE.SphereGeometry(0.06, 8, 8);
      const dotMat = new THREE.MeshBasicMaterial({
        color: 0xffd93d,
        transparent: true,
        opacity: 0.8,
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
        // Friend at park
        const friend = new NPCCharacter('ava', 'friend', new THREE.Vector3(-1, 0, 1));
        friend.setWanderRadius(2.5);
        friend.addToScene(this.scene);
        this.npcs.push(friend);
        // Sometimes a pet
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
        // Pet at home
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
    // Add subtle animated objects based on room
    switch (roomType) {
      case 'park': {
        // Butterflies
        for (let i = 0; i < 3; i++) {
          const bfGroup = new THREE.Group();
          const wingGeom = new THREE.PlaneGeometry(0.08, 0.06);
          const wingColors = [0xff6b9d, 0xffd93d, 0x42a5f5];
          const wingMat = new THREE.MeshBasicMaterial({
            color: wingColors[i],
            transparent: true,
            opacity: 0.8,
            side: THREE.DoubleSide,
          });
          const leftWing = new THREE.Mesh(wingGeom, wingMat);
          leftWing.position.x = -0.04;
          bfGroup.add(leftWing);
          const rightWing = new THREE.Mesh(wingGeom, wingMat);
          rightWing.position.x = 0.04;
          bfGroup.add(rightWing);
          bfGroup.position.set(
            -2 + Math.random() * 4,
            0.8 + Math.random() * 0.5,
            -2 + Math.random() * 3
          );
          this.scene.add(bfGroup);
          this.animatedObjects.push({
            mesh: bfGroup,
            type: 'butterfly',
            speed: 3 + Math.random() * 2,
          });
        }
        break;
      }
      case 'bathroom': {
        // Dripping faucet
        break;
      }
    }
  }

  private triggerRandomEvent() {
    const event = DAILY_EVENTS[Math.floor(Math.random() * DAILY_EVENTS.length)];
    if (event.coinMultiplier) {
      this.coinMultiplier = event.coinMultiplier;
    }
    if (event.spawnPet) {
      const pet = new NPCCharacter(event.spawnPet, event.spawnPet as NPCType, new THREE.Vector3(
        Math.random() * 2 - 1, 0, Math.random() * 2
      ));
      pet.setWanderRadius(2.0);
      pet.addToScene(this.scene);
      this.npcs.push(pet);
    }
    if (event.spawnNPC) {
      const npc = new NPCCharacter(event.spawnNPC, 'friend', new THREE.Vector3(
        Math.random() * 2 - 1, 0, Math.random() * 2
      ));
      npc.setWanderRadius(2.0);
      npc.addToScene(this.scene);
      this.npcs.push(npc);
    }
  }

  setCallbacks(callbacks: GameCallbacks) {
    this.callbacks = callbacks;
  }

  handleTap(screenX: number, screenY: number, viewWidth: number, viewHeight: number) {
    // Don't process taps during active interaction
    if (this.activeInteraction) return;

    // Check if tapped on an interactable indicator
    const worldPos = this.camera.screenToWorld(screenX, screenY, viewWidth, viewHeight);

    // Check proximity to interactables
    if (worldPos) {
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
        // Walk to interactable then interact
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
          // Walk to NPC
          const walkTarget = npc.group.position.clone();
          walkTarget.z += 0.5;
          this.character.walkTo(walkTarget, () => {
            this.character.setAnimation('wave');
            npc.currentDialogue = npc.getRandomDialogue();
            this.callbacks.onNPCTap?.(npc);
            setTimeout(() => {
              this.character.setAnimation('idle');
            }, 2000);
          });
          AudioManager.playSFX('tap');
          return;
        }
      }

      // Tap on floor - walk there
      if (worldPos.y <= 0.1) {
        // Clamp to room bounds
        worldPos.x = Math.max(-3.5, Math.min(3.5, worldPos.x));
        worldPos.z = Math.max(-3.5, Math.min(3.5, worldPos.z));
        worldPos.y = 0;
        this.character.walkTo(worldPos);
        // Dust particles at tap point
        this.particles.emitDust(worldPos);
        AudioManager.playSFX('tap');
        this.callbacks.onFloorTap?.(worldPos);
      }
    }
  }

  private startInteraction(interactable: InteractableInfo) {
    this.activeInteraction = interactable;
    this.interactionTimer = 0;

    // Face the object
    const dir = new THREE.Vector3().subVectors(interactable.position, this.character.group.position);
    const angle = Math.atan2(dir.x, dir.z);
    this.character.setRotation(angle);

    // Play animation
    this.character.setAnimation(interactable.animOnInteract as CharacterAnim);

    // Camera zoom to interaction
    this.camera.zoomTo(interactable.position);

    // Particles based on category
    switch (interactable.category) {
      case 'eat':
        this.particles.emitHearts(interactable.position);
        break;
      case 'clean':
        this.particles.emitBubbles(interactable.position);
        break;
      case 'fun':
        this.particles.emitStars(interactable.position);
        break;
      case 'cook':
        this.particles.emitStars(interactable.position);
        break;
      default:
        this.particles.emitStars(interactable.position);
    }

    AudioManager.playSFX('success');
  }

  private endInteraction() {
    if (!this.activeInteraction) return;

    // Coin particles
    this.particles.emitCoinCollect(this.character.group.position.clone().add(new THREE.Vector3(0, 1, 0)));

    // Callback
    this.callbacks.onInteract?.(this.activeInteraction);
    this.callbacks.onActivityComplete?.(this.activeInteraction.id);

    // Reset
    this.character.setAnimation('happy');
    this.camera.zoomOut();

    // Return to idle after celebration
    setTimeout(() => {
      this.character.setAnimation('idle');
    }, 1500);

    this.activeInteraction = null;
    AudioManager.playSFX('coin_collect');
  }

  getCoinMultiplier(): number {
    return this.coinMultiplier;
  }

  update() {
    const now = Date.now() / 1000;
    if (this.lastTime === 0) {
      this.lastTime = now;
      return;
    }
    const dt = Math.min(now - this.lastTime, 0.05); // Cap at 50ms
    this.lastTime = now;

    // Update character
    this.character.update(dt);

    // Update NPCs
    this.npcs.forEach((npc) => npc.update(dt));

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

    // Render
    if (this.renderer) {
      this.renderer.render(this.scene, this.camera.camera);
    }
  }

  private updateEnvironmentAnimations(dt: number) {
    const time = Date.now() / 1000;
    this.animatedObjects.forEach((obj) => {
      switch (obj.type) {
        case 'butterfly': {
          // Flutter and fly in circles
          obj.mesh.position.x += Math.sin(time * obj.speed) * 0.5 * dt;
          obj.mesh.position.z += Math.cos(time * obj.speed * 0.7) * 0.3 * dt;
          obj.mesh.position.y = 0.8 + Math.sin(time * obj.speed * 1.5) * 0.15;
          obj.mesh.rotation.y = time * obj.speed * 0.5;
          // Wing flap
          if (obj.mesh.children.length >= 2) {
            obj.mesh.children[0].rotation.y = Math.sin(time * 15) * 0.5;
            obj.mesh.children[1].rotation.y = -Math.sin(time * 15) * 0.5;
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
          // Float up and down
          const baseY = child.position.y;
          child.position.y = baseY + Math.sin(time * 3) * 0.02;
          // Pulse opacity
          if (child instanceof THREE.Mesh) {
            const mat = child.material as THREE.MeshBasicMaterial;
            mat.opacity = 0.5 + Math.sin(time * 4) * 0.3;
          }
        } else {
          // Ring pulse
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
