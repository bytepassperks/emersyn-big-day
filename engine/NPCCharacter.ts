/**
 * NPCCharacter.ts - NPC friends and pet companions
 * NPCs wander around rooms, have speech bubbles, and interact with player
 */
import { THREE } from 'expo-three';

export type NPCType = 'friend' | 'pet_cat' | 'pet_dog' | 'pet_bunny' | 'shopkeeper' | 'teacher';

interface NPCConfig {
  bodyColor: number;
  hairColor: number;
  outfitColor: number;
  scale: number;
  name: string;
}

const NPC_CONFIGS: Record<string, NPCConfig> = {
  ava: { bodyColor: 0xc68642, hairColor: 0x1a1a1a, outfitColor: 0x42a5f5, scale: 0.85, name: 'Ava' },
  mia: { bodyColor: 0xffdbac, hairColor: 0xff6b00, outfitColor: 0x66bb6a, scale: 0.82, name: 'Mia' },
  leo: { bodyColor: 0xdeb887, hairColor: 0x4a2810, outfitColor: 0xffd93d, scale: 0.88, name: 'Leo' },
  shopkeeper: { bodyColor: 0xffdbac, hairColor: 0x888888, outfitColor: 0xffffff, scale: 1.0, name: 'Mr. Chen' },
  teacher: { bodyColor: 0xc68642, hairColor: 0x1a1a1a, outfitColor: 0x9c5bff, scale: 1.0, name: 'Ms. Priya' },
};

const RANDOM_DIALOGUES: Record<string, string[]> = {
  ava: [
    "Let's play together! 🎮",
    "Your outfit is so cute! 💖",
    "Want to go to the park? 🌳",
    "I found a cool sticker! ⭐",
    "Let's dance! 💃",
    "Do you want to cook? 🍳",
  ],
  mia: [
    "Hi bestie! 🌟",
    "I love your hair bow! 🎀",
    "Let's explore! 🔍",
    "Have you seen the arcade? 🕹️",
    "You're so brave! 💪",
    "Want a snack? 🍪",
  ],
  leo: [
    "Hey! Let's race! 🏃",
    "Check out my karate moves! 🥋",
    "The scooty dash is fun! 🛴",
    "I got a new belt! 🥇",
    "Let's build sandcastles! 🏖️",
    "High five! ✋",
  ],
  shopkeeper: [
    "Welcome! Take a look around! 🛍️",
    "New items in stock today! ✨",
    "Great choice! 👍",
    "Come back anytime! 😊",
  ],
  teacher: [
    "Good morning, class! 📚",
    "Who can solve this? 🤔",
    "Great job, Emersyn! ⭐",
    "Time for art class! 🎨",
    "Let's learn something new! 📖",
  ],
};

export class NPCCharacter {
  group: THREE.Group;
  type: NPCType;
  npcId: string;
  name: string;
  private animTime: number = 0;
  private wanderTarget: THREE.Vector3 | null = null;
  private wanderTimer: number = 0;
  private wanderInterval: number = 3 + Math.random() * 4;
  private moveSpeed: number = 0.8;
  private wanderRadius: number = 2.0;
  private homePosition: THREE.Vector3;
  private isWalking: boolean = false;
  private speechBubble: THREE.Group | null = null;
  currentDialogue: string = '';

  constructor(npcId: string, type: NPCType, position: THREE.Vector3) {
    this.group = new THREE.Group();
    this.type = type;
    this.npcId = npcId;
    this.name = NPC_CONFIGS[npcId]?.name || npcId;
    this.homePosition = position.clone();
    this.group.position.copy(position);

    if (type === 'pet_cat' || type === 'pet_dog' || type === 'pet_bunny') {
      this.buildPet(type);
      this.moveSpeed = 1.2;
    } else {
      this.buildHumanoid(npcId);
    }
  }

  private buildHumanoid(npcId: string) {
    const config = NPC_CONFIGS[npcId] || NPC_CONFIGS.ava;
    const skinMat = new THREE.MeshPhongMaterial({ color: config.bodyColor, shininess: 30 });

    // Body
    const torsoGeom = new THREE.SphereGeometry(0.22, 12, 12);
    torsoGeom.scale(1, 1.1, 0.8);
    const torso = new THREE.Mesh(torsoGeom, skinMat);
    torso.position.y = 0.45;
    this.group.add(torso);

    // Outfit
    const outfitGeom = new THREE.ConeGeometry(0.3, 0.45, 12);
    const outfitMat = new THREE.MeshPhongMaterial({ color: config.outfitColor, shininess: 40 });
    const outfit = new THREE.Mesh(outfitGeom, outfitMat);
    outfit.position.y = 0.22;
    this.group.add(outfit);

    // Head
    const headGeom = new THREE.SphereGeometry(0.22, 14, 14);
    const head = new THREE.Mesh(headGeom, skinMat);
    head.position.y = 0.82;
    this.group.add(head);

    // Hair
    const hairGeom = new THREE.SphereGeometry(0.24, 12, 12);
    hairGeom.scale(1.05, 1.02, 1.02);
    const hairMat = new THREE.MeshPhongMaterial({ color: config.hairColor, shininess: 50 });
    const hair = new THREE.Mesh(hairGeom, hairMat);
    hair.position.y = 0.85;
    this.group.add(hair);

    // Eyes
    const eyeGeom = new THREE.SphereGeometry(0.03, 6, 6);
    const eyeMat = new THREE.MeshBasicMaterial({ color: 0x222222 });
    const leftEye = new THREE.Mesh(eyeGeom, eyeMat);
    leftEye.position.set(-0.07, 0.82, 0.19);
    this.group.add(leftEye);
    const rightEye = new THREE.Mesh(eyeGeom, eyeMat);
    rightEye.position.set(0.07, 0.82, 0.19);
    this.group.add(rightEye);

    // Smile
    const smileGeom = new THREE.TorusGeometry(0.04, 0.01, 6, 12, Math.PI);
    const smileMat = new THREE.MeshPhongMaterial({ color: 0xff6b9d });
    const smile = new THREE.Mesh(smileGeom, smileMat);
    smile.position.set(0, 0.74, 0.2);
    smile.rotation.x = Math.PI;
    smile.rotation.z = Math.PI;
    this.group.add(smile);

    // Legs
    const legGeom = new THREE.CylinderGeometry(0.04, 0.035, 0.22, 6);
    const leftLeg = new THREE.Mesh(legGeom, skinMat);
    leftLeg.position.set(-0.08, -0.05, 0);
    this.group.add(leftLeg);
    const rightLeg = new THREE.Mesh(legGeom, skinMat);
    rightLeg.position.set(0.08, -0.05, 0);
    this.group.add(rightLeg);

    // Shoes
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

    // Body
    const bodyGeom = new THREE.SphereGeometry(0.15, 10, 10);
    bodyGeom.scale(1.3, 0.9, 1);
    const bodyMat = new THREE.MeshPhongMaterial({ color: c.body, shininess: 40 });
    const body = new THREE.Mesh(bodyGeom, bodyMat);
    body.position.y = 0.15;
    this.group.add(body);

    // Head
    const headGeom = new THREE.SphereGeometry(0.12, 10, 10);
    const head = new THREE.Mesh(headGeom, bodyMat);
    head.position.set(0.15, 0.25, 0);
    this.group.add(head);

    // Eyes
    const eyeGeom = new THREE.SphereGeometry(0.025, 6, 6);
    const eyeMat = new THREE.MeshBasicMaterial({ color: 0x222222 });
    const leftEye = new THREE.Mesh(eyeGeom, eyeMat);
    leftEye.position.set(0.2, 0.28, 0.08);
    this.group.add(leftEye);
    const rightEye = new THREE.Mesh(eyeGeom, eyeMat);
    rightEye.position.set(0.2, 0.28, -0.08);
    this.group.add(rightEye);

    // Nose
    const noseGeom = new THREE.SphereGeometry(0.02, 6, 6);
    const noseMat = new THREE.MeshPhongMaterial({ color: 0xff6b9d });
    const nose = new THREE.Mesh(noseGeom, noseMat);
    nose.position.set(0.27, 0.24, 0);
    this.group.add(nose);

    // Ears
    if (type === 'pet_bunny') {
      // Long ears
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
      // Inner ears
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
      // Triangle ears for cat/dog
      const earGeom = new THREE.ConeGeometry(0.04, 0.08, 4);
      const earMat = new THREE.MeshPhongMaterial({ color: c.body });
      const leftEar = new THREE.Mesh(earGeom, earMat);
      leftEar.position.set(0.12, 0.36, 0.06);
      this.group.add(leftEar);
      const rightEar = new THREE.Mesh(earGeom, earMat);
      rightEar.position.set(0.12, 0.36, -0.06);
      this.group.add(rightEar);
    }

    // Tail
    const tailGeom = new THREE.SphereGeometry(0.04, 6, 6);
    if (type === 'pet_bunny') {
      tailGeom.scale(1, 1, 1);
    } else {
      tailGeom.scale(0.5, 0.5, 2);
    }
    const tail = new THREE.Mesh(tailGeom, bodyMat);
    tail.position.set(-0.18, 0.18, 0);
    this.group.add(tail);

    // Legs (4)
    const legGeom = new THREE.CylinderGeometry(0.025, 0.025, 0.1, 6);
    const legPositions = [
      [0.08, 0.05, 0.08],
      [0.08, 0.05, -0.08],
      [-0.08, 0.05, 0.08],
      [-0.08, 0.05, -0.08],
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

  update(dt: number) {
    this.animTime += dt;

    // Idle animation
    const bob = Math.sin(this.animTime * 2) * 0.01;
    this.group.position.y = this.homePosition.y + bob;

    // Wandering behavior
    this.wanderTimer += dt;
    if (this.wanderTimer >= this.wanderInterval && !this.isWalking) {
      this.wanderTimer = 0;
      this.wanderInterval = 3 + Math.random() * 5;

      // Pick random point within wander radius
      const angle = Math.random() * Math.PI * 2;
      const dist = Math.random() * this.wanderRadius;
      this.wanderTarget = new THREE.Vector3(
        this.homePosition.x + Math.cos(angle) * dist,
        this.homePosition.y,
        this.homePosition.z + Math.sin(angle) * dist
      );
      this.isWalking = true;
    }

    // Move toward wander target
    if (this.isWalking && this.wanderTarget) {
      const dir = new THREE.Vector3().subVectors(this.wanderTarget, this.group.position);
      dir.y = 0;
      const distance = dir.length();

      if (distance < 0.1) {
        this.isWalking = false;
        this.wanderTarget = null;
      } else {
        dir.normalize();
        const step = Math.min(this.moveSpeed * dt, distance);
        this.group.position.x += dir.x * step;
        this.group.position.z += dir.z * step;

        // Face direction
        const targetAngle = Math.atan2(dir.x, dir.z);
        this.group.rotation.y += (targetAngle - this.group.rotation.y) * Math.min(1, dt * 5);
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
