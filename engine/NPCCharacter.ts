/**
 * NPCCharacter.ts - NPC friends and pet companions with GLB models,
 * utility AI personality traits, and autonomous wandering behavior.
 */
import { THREE } from 'expo-three';
import { getModelUri } from './ModelAssets';

export type NPCType = 'friend' | 'pet_cat' | 'pet_dog' | 'pet_bunny' | 'shopkeeper' | 'teacher';

/** Personality traits for utility AI scoring */
export interface Personality {
  friendliness: number;   // 0-1 how likely to approach player
  playfulness: number;    // 0-1 how likely to dance/play
  chattiness: number;     // 0-1 how often speaks
  energy: number;         // 0-1 how fast they move / wander
}

interface NPCConfig {
  bodyColor: number;
  hairColor: number;
  outfitColor: number;
  scale: number;
  name: string;
  characterId: string;
  personality: Personality;
}

const NPC_CONFIGS: Record<string, NPCConfig> = {
  ava: {
    bodyColor: 0xc68642, hairColor: 0x1a1a1a, outfitColor: 0x42a5f5,
    scale: 0.85, name: 'Ava', characterId: 'ava',
    personality: { friendliness: 0.9, playfulness: 0.7, chattiness: 0.8, energy: 0.6 },
  },
  mia: {
    bodyColor: 0xffdbac, hairColor: 0xff6b00, outfitColor: 0x66bb6a,
    scale: 0.82, name: 'Mia', characterId: 'mia',
    personality: { friendliness: 0.7, playfulness: 0.9, chattiness: 0.6, energy: 0.8 },
  },
  leo: {
    bodyColor: 0xdeb887, hairColor: 0x4a2810, outfitColor: 0xffd93d,
    scale: 0.88, name: 'Leo', characterId: 'leo',
    personality: { friendliness: 0.6, playfulness: 0.95, chattiness: 0.5, energy: 0.95 },
  },
  shopkeeper: {
    bodyColor: 0xffdbac, hairColor: 0x888888, outfitColor: 0xffffff,
    scale: 1.0, name: 'Mr. Chen', characterId: 'shopkeeper',
    personality: { friendliness: 0.8, playfulness: 0.2, chattiness: 0.9, energy: 0.3 },
  },
  teacher: {
    bodyColor: 0xc68642, hairColor: 0x1a1a1a, outfitColor: 0x9c5bff,
    scale: 1.0, name: 'Ms. Priya', characterId: 'teacher',
    personality: { friendliness: 0.85, playfulness: 0.4, chattiness: 0.95, energy: 0.4 },
  },
};

const RANDOM_DIALOGUES: Record<string, string[]> = {
  ava: [
    "Let's play together!", "Your outfit is so cute!",
    "Want to go to the park?", "I found a cool sticker!",
    "Let's dance!", "Do you want to cook?",
  ],
  mia: [
    "Hi bestie!", "I love your hair bow!",
    "Let's explore!", "Have you seen the arcade?",
    "You're so brave!", "Want a snack?",
  ],
  leo: [
    "Hey! Let's race!", "Check out my karate moves!",
    "The scooty dash is fun!", "I got a new belt!",
    "Let's build sandcastles!", "High five!",
  ],
  shopkeeper: [
    "Welcome! Take a look around!", "New items in stock today!",
    "Great choice!", "Come back anytime!",
  ],
  teacher: [
    "Good morning, class!", "Who can solve this?",
    "Great job, Emersyn!", "Time for art class!",
    "Let's learn something new!",
  ],
};

/** NPC relationship value with player (-100 to +100) */
export interface NPCRelationship {
  npcId: string;
  value: number; // -100 to +100
  interactionCount: number;
}

export class NPCCharacter {
  group: THREE.Group;
  type: NPCType;
  npcId: string;
  name: string;
  personality: Personality;
  relationship: NPCRelationship;

  private glbScene: THREE.Group | null = null;
  private mixer: THREE.AnimationMixer | null = null;
  private clips: Map<string, THREE.AnimationClip> = new Map();
  private currentAction: THREE.AnimationAction | null = null;
  private glbLoaded: boolean = false;

  private animTime: number = 0;
  private wanderTarget: THREE.Vector3 | null = null;
  private wanderTimer: number = 0;
  private wanderInterval: number = 3 + Math.random() * 4;
  private moveSpeed: number = 0.8;
  private wanderRadius: number = 2.0;
  private homePosition: THREE.Vector3;
  private isWalking: boolean = false;
  currentDialogue: string = '';

  // Idle micro-animations
  private breathPhase: number = Math.random() * Math.PI * 2;
  private blinkTimer: number = 0;
  private blinkInterval: number = 3 + Math.random() * 4;

  // Autonomous behavior
  private autonomousTimer: number = 0;
  private autonomousInterval: number = 5 + Math.random() * 10;
  private currentAutonomousAnim: string = 'idle';

  constructor(npcId: string, type: NPCType, position: THREE.Vector3) {
    this.group = new THREE.Group();
    this.type = type;
    this.npcId = npcId;
    const config = NPC_CONFIGS[npcId];
    this.name = config?.name || npcId;
    this.personality = config?.personality || { friendliness: 0.5, playfulness: 0.5, chattiness: 0.5, energy: 0.5 };
    this.relationship = { npcId, value: 0, interactionCount: 0 };
    this.homePosition = position.clone();
    this.group.position.copy(position);
    this.moveSpeed = 0.5 + this.personality.energy * 1.0;

    if (type === 'pet_cat' || type === 'pet_dog' || type === 'pet_bunny') {
      this.buildPet(type);
      this.moveSpeed = 1.2;
    } else {
      this.buildHumanoid(npcId);
    }
    this.loadGLBModel(npcId, type);
  }

  private async loadGLBModel(npcId: string, type: NPCType): Promise<void> {
    const characterId = NPC_CONFIGS[npcId]?.characterId || type;
    try {
      const uri = await getModelUri(characterId);
      if (!uri) return;
      const { GLTFLoader } = require('three/examples/jsm/loaders/GLTFLoader') as {
        GLTFLoader: new () => {
          load: (
            url: string,
            onLoad: (gltf: { scene: THREE.Group; animations: THREE.AnimationClip[] }) => void,
            onProgress?: (event: ProgressEvent) => void,
            onError?: (event: unknown) => void,
          ) => void;
        };
      };
      const loader = new GLTFLoader();
      const gltf = await new Promise<{ scene: THREE.Group; animations: THREE.AnimationClip[] }>(
        (resolve, reject) => { loader.load(uri, resolve, undefined, reject); },
      );
      this.glbScene = gltf.scene;
      const config = NPC_CONFIGS[npcId];
      this.glbScene.scale.setScalar(config?.scale || 0.85);
      this.glbScene.traverse((child: THREE.Object3D) => {
        if (child instanceof THREE.Mesh) {
          child.castShadow = true;
          child.receiveShadow = true;
        }
      });
      this.mixer = new THREE.AnimationMixer(this.glbScene);
      for (const clip of gltf.animations) {
        this.clips.set(clip.name, clip);
      }
      // Hide procedural mesh children
      const toHide: THREE.Object3D[] = [];
      this.group.children.forEach((child: THREE.Object3D) => { if (child !== this.glbScene) toHide.push(child); });
      toHide.forEach((child: THREE.Object3D) => { child.visible = false; });
      this.group.add(this.glbScene);
      this.playClip('idle');
      this.glbLoaded = true;
    } catch (err) {
      console.warn(`NPC GLB load failed for ${npcId}:`, err);
    }
  }

  private playClip(animName: string, crossFade: number = 0.3): void {
    if (!this.mixer) return;
    const clip = this.clips.get(animName);
    if (!clip) return;
    const newAction = this.mixer.clipAction(clip);
    if (this.currentAction && this.currentAction !== newAction) {
      this.currentAction.fadeOut(crossFade);
    }
    newAction.reset().fadeIn(crossFade).play();
    this.currentAction = newAction;
  }

  private buildHumanoid(npcId: string) {
    const config = NPC_CONFIGS[npcId] || NPC_CONFIGS.ava;
    const skinMat = new THREE.MeshPhongMaterial({ color: config.bodyColor, shininess: 30 });

    const torsoGeom = new THREE.SphereGeometry(0.22, 12, 12);
    torsoGeom.scale(1, 1.1, 0.8);
    const torso = new THREE.Mesh(torsoGeom, skinMat);
    torso.position.y = 0.45;
    this.group.add(torso);

    const outfitGeom = new THREE.ConeGeometry(0.3, 0.45, 12);
    const outfitMat = new THREE.MeshPhongMaterial({ color: config.outfitColor, shininess: 40 });
    const outfit = new THREE.Mesh(outfitGeom, outfitMat);
    outfit.position.y = 0.22;
    this.group.add(outfit);

    const headGeom = new THREE.SphereGeometry(0.22, 14, 14);
    const head = new THREE.Mesh(headGeom, skinMat);
    head.position.y = 0.82;
    this.group.add(head);

    const hairGeom = new THREE.SphereGeometry(0.24, 12, 12);
    hairGeom.scale(1.05, 1.02, 1.02);
    const hairMat = new THREE.MeshPhongMaterial({ color: config.hairColor, shininess: 50 });
    const hair = new THREE.Mesh(hairGeom, hairMat);
    hair.position.y = 0.85;
    this.group.add(hair);

    const eyeGeom = new THREE.SphereGeometry(0.03, 6, 6);
    const eyeMat = new THREE.MeshBasicMaterial({ color: 0x222222 });
    const leftEye = new THREE.Mesh(eyeGeom, eyeMat);
    leftEye.position.set(-0.07, 0.82, 0.19);
    this.group.add(leftEye);
    const rightEye = new THREE.Mesh(eyeGeom, eyeMat);
    rightEye.position.set(0.07, 0.82, 0.19);
    this.group.add(rightEye);

    const smileGeom = new THREE.TorusGeometry(0.04, 0.01, 6, 12, Math.PI);
    const smileMat = new THREE.MeshPhongMaterial({ color: 0xff6b9d });
    const smile = new THREE.Mesh(smileGeom, smileMat);
    smile.position.set(0, 0.74, 0.2);
    smile.rotation.x = Math.PI;
    smile.rotation.z = Math.PI;
    this.group.add(smile);

    const legGeom = new THREE.CylinderGeometry(0.04, 0.035, 0.22, 6);
    const leftLeg = new THREE.Mesh(legGeom, skinMat);
    leftLeg.position.set(-0.08, -0.05, 0);
    this.group.add(leftLeg);
    const rightLeg = new THREE.Mesh(legGeom, skinMat);
    rightLeg.position.set(0.08, -0.05, 0);
    this.group.add(rightLeg);

    const shoeGeom = new THREE.SphereGeometry(0.05, 6, 6);
    shoeGeom.scale(1.1, 0.5, 1.4);
    const shoeMat = new THREE.MeshPhongMaterial({ color: config.outfitColor });
    const leftShoe = new THREE.Mesh(shoeGeom, shoeMat);
    leftShoe.position.set(-0.08, -0.18, 0.02);
    this.group.add(leftShoe);
    const rightShoe = new THREE.Mesh(shoeGeom, shoeMat);
    rightShoe.position.set(0.08, -0.18, 0.02);
    this.group.add(rightShoe);

    this.group.scale.setScalar(config.scale);
  }

  private buildPet(type: NPCType) {
    const colors: Record<string, { body: number; accent: number }> = {
      pet_cat: { body: 0xff9f43, accent: 0xffffff },
      pet_dog: { body: 0xc9a96e, accent: 0xdeb887 },
      pet_bunny: { body: 0xffffff, accent: 0xffb6c1 },
    };
    const c = colors[type] || colors.pet_cat;

    const bodyGeom = new THREE.SphereGeometry(0.15, 10, 10);
    bodyGeom.scale(1.3, 0.9, 1);
    const bodyMat = new THREE.MeshPhongMaterial({ color: c.body, shininess: 40 });
    const body = new THREE.Mesh(bodyGeom, bodyMat);
    body.position.y = 0.15;
    this.group.add(body);

    const headGeom = new THREE.SphereGeometry(0.12, 10, 10);
    const head = new THREE.Mesh(headGeom, bodyMat);
    head.position.set(0.15, 0.25, 0);
    this.group.add(head);

    const eyeGeom = new THREE.SphereGeometry(0.025, 6, 6);
    const eyeMat = new THREE.MeshBasicMaterial({ color: 0x222222 });
    const leftEye = new THREE.Mesh(eyeGeom, eyeMat);
    leftEye.position.set(0.2, 0.28, 0.08);
    this.group.add(leftEye);
    const rightEye = new THREE.Mesh(eyeGeom, eyeMat);
    rightEye.position.set(0.2, 0.28, -0.08);
    this.group.add(rightEye);

    const noseGeom = new THREE.SphereGeometry(0.02, 6, 6);
    const noseMat = new THREE.MeshPhongMaterial({ color: 0xff6b9d });
    const nose = new THREE.Mesh(noseGeom, noseMat);
    nose.position.set(0.27, 0.24, 0);
    this.group.add(nose);

    if (type === 'pet_bunny') {
      const earGeom = new THREE.CylinderGeometry(0.03, 0.02, 0.2, 6);
      const earMat = new THREE.MeshPhongMaterial({ color: c.body });
      const leftEar = new THREE.Mesh(earGeom, earMat);
      leftEar.position.set(0.12, 0.4, 0.05);
      leftEar.rotation.z = 0.2;
      this.group.add(leftEar);
      const rightEar = new THREE.Mesh(earGeom, earMat);
      rightEar.position.set(0.12, 0.4, -0.05);
      rightEar.rotation.z = -0.2;
      this.group.add(rightEar);
      const innerMat = new THREE.MeshPhongMaterial({ color: c.accent });
      const innerGeom = new THREE.CylinderGeometry(0.015, 0.01, 0.15, 6);
      const leftInner = new THREE.Mesh(innerGeom, innerMat);
      leftInner.position.set(0.12, 0.4, 0.05);
      leftInner.rotation.z = 0.2;
      this.group.add(leftInner);
      const rightInner = new THREE.Mesh(innerGeom, innerMat);
      rightInner.position.set(0.12, 0.4, -0.05);
      rightInner.rotation.z = -0.2;
      this.group.add(rightInner);
    } else {
      const earGeom = new THREE.ConeGeometry(0.04, 0.08, 4);
      const earMat = new THREE.MeshPhongMaterial({ color: c.body });
      const leftEar = new THREE.Mesh(earGeom, earMat);
      leftEar.position.set(0.12, 0.36, 0.06);
      this.group.add(leftEar);
      const rightEar = new THREE.Mesh(earGeom, earMat);
      rightEar.position.set(0.12, 0.36, -0.06);
      this.group.add(rightEar);
    }

    const tailGeom = new THREE.SphereGeometry(0.04, 6, 6);
    if (type !== 'pet_bunny') { tailGeom.scale(0.5, 0.5, 2); }
    const tail = new THREE.Mesh(tailGeom, bodyMat);
    tail.position.set(-0.18, 0.18, 0);
    this.group.add(tail);

    const legGeom = new THREE.CylinderGeometry(0.025, 0.025, 0.1, 6);
    const legPositions: [number, number, number][] = [
      [0.08, 0.05, 0.08], [0.08, 0.05, -0.08],
      [-0.08, 0.05, 0.08], [-0.08, 0.05, -0.08],
    ];
    legPositions.forEach(([x, y, z]) => {
      const leg = new THREE.Mesh(legGeom, bodyMat);
      leg.position.set(x, y, z);
      this.group.add(leg);
    });

    this.group.scale.setScalar(0.8);
  }

  getRandomDialogue(): string {
    const dialogues = RANDOM_DIALOGUES[this.npcId] || RANDOM_DIALOGUES.ava;
    return dialogues[Math.floor(Math.random() * dialogues.length)];
  }

  /** Increase relationship on positive interaction */
  onInteraction(): void {
    this.relationship.interactionCount++;
    this.relationship.value = Math.min(100, this.relationship.value + 5);
  }

  update(dt: number) {
    this.animTime += dt;

    // AnimationMixer
    if (this.mixer) {
      this.mixer.update(dt);
    }

    // Idle bob (procedural fallback)
    if (!this.glbLoaded) {
      const bob = Math.sin(this.animTime * 2) * 0.01;
      this.group.position.y = this.homePosition.y + bob;
      // Breathing
      this.breathPhase += dt * 1.5;
    }

    // Wandering behavior
    this.wanderTimer += dt;
    if (this.wanderTimer >= this.wanderInterval && !this.isWalking) {
      this.wanderTimer = 0;
      this.wanderInterval = 3 + Math.random() * 5;
      const angle = Math.random() * Math.PI * 2;
      const dist = Math.random() * this.wanderRadius;
      this.wanderTarget = new THREE.Vector3(
        this.homePosition.x + Math.cos(angle) * dist,
        this.homePosition.y,
        this.homePosition.z + Math.sin(angle) * dist,
      );
      this.isWalking = true;
      if (this.glbLoaded) { this.playClip('walk'); }
    }

    // Move toward wander target
    if (this.isWalking && this.wanderTarget) {
      const dir = new THREE.Vector3().subVectors(this.wanderTarget, this.group.position);
      dir.y = 0;
      const distance = dir.length();
      if (distance < 0.1) {
        this.isWalking = false;
        this.wanderTarget = null;
        if (this.glbLoaded) { this.playClip('idle'); }
      } else {
        dir.normalize();
        const step = Math.min(this.moveSpeed * dt, distance);
        this.group.position.x += dir.x * step;
        this.group.position.z += dir.z * step;
        const targetAngle = Math.atan2(dir.x, dir.z);
        this.group.rotation.y += (targetAngle - this.group.rotation.y) * Math.min(1, dt * 5);
      }
    }

    // Autonomous personality-driven behavior
    this.autonomousTimer += dt;
    if (this.autonomousTimer >= this.autonomousInterval && !this.isWalking) {
      this.autonomousTimer = 0;
      this.autonomousInterval = 5 + Math.random() * 10;
      // Pick autonomous action based on personality
      const r = Math.random();
      if (r < this.personality.playfulness * 0.3) {
        this.currentAutonomousAnim = 'dance';
        if (this.glbLoaded) { this.playClip('dance'); }
        setTimeout(() => {
          this.currentAutonomousAnim = 'idle';
          if (this.glbLoaded) { this.playClip('idle'); }
        }, 3000);
      } else if (r < this.personality.friendliness * 0.2 + 0.3) {
        this.currentAutonomousAnim = 'wave';
        if (this.glbLoaded) { this.playClip('wave'); }
        setTimeout(() => {
          this.currentAutonomousAnim = 'idle';
          if (this.glbLoaded) { this.playClip('idle'); }
        }, 2000);
      }
    }
  }

  setWanderRadius(radius: number) {
    this.wanderRadius = radius;
  }

  setHomePosition(pos: THREE.Vector3) {
    this.homePosition = pos.clone();
  }

  addToScene(scene: THREE.Scene) {
    scene.add(this.group);
  }

  dispose() {
    if (this.mixer) {
      this.mixer.stopAllAction();
      this.mixer = null;
    }
    this.clips.clear();
    this.group.traverse((child: THREE.Object3D) => {
      if (child instanceof THREE.Mesh) {
        child.geometry.dispose();
        if (Array.isArray(child.material)) {
          child.material.forEach((m: THREE.Material) => m.dispose());
        } else {
          child.material.dispose();
        }
      }
    });
  }
}
