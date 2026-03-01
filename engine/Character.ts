/**
 * Character.ts - 3D Chibi Character with skeletal-like animation
 * Creates Emersyn as a cute animated character from Three.js primitives
 */
import { THREE } from 'expo-three';

export type CharacterAnim =
  | 'idle'
  | 'walk'
  | 'run'
  | 'happy'
  | 'eat'
  | 'sleep'
  | 'dance'
  | 'karate'
  | 'wave'
  | 'jump'
  | 'cook'
  | 'clean'
  | 'study'
  | 'scared';

export type EmotionFace = 'happy' | 'neutral' | 'sleepy' | 'excited' | 'hungry' | 'sad';

interface CharacterOptions {
  skinColor?: number;
  hairColor?: number;
  dressColor?: number;
  shoeColor?: number;
  scale?: number;
}

const DEFAULT_OPTIONS: CharacterOptions = {
  skinColor: 0xffdbac,
  hairColor: 0x4a2810,
  dressColor: 0xff6b9d,
  shoeColor: 0xff4081,
  scale: 1.0,
};

export class Character {
  group: THREE.Group;
  // Body part groups for animation
  head: THREE.Group;
  body: THREE.Group;
  leftArm: THREE.Group;
  rightArm: THREE.Group;
  leftLeg: THREE.Group;
  rightLeg: THREE.Group;
  hairBow: THREE.Mesh;
  leftEye: THREE.Group;
  rightEye: THREE.Group;
  mouth: THREE.Mesh;
  dress: THREE.Mesh;
  // State
  animState: CharacterAnim = 'idle';
  emotion: EmotionFace = 'happy';
  animTime: number = 0;
  targetPosition: THREE.Vector3 | null = null;
  moveSpeed: number = 2.0;
  isMoving: boolean = false;
  private onArriveCallback: (() => void) | null = null;
  private options: CharacterOptions;

  constructor(opts?: Partial<CharacterOptions>) {
    this.options = { ...DEFAULT_OPTIONS, ...opts };
    this.group = new THREE.Group();
    this.head = new THREE.Group();
    this.body = new THREE.Group();
    this.leftArm = new THREE.Group();
    this.rightArm = new THREE.Group();
    this.leftLeg = new THREE.Group();
    this.rightLeg = new THREE.Group();
    this.leftEye = new THREE.Group();
    this.rightEye = new THREE.Group();
    this.hairBow = new THREE.Mesh();
    this.mouth = new THREE.Mesh();
    this.dress = new THREE.Mesh();
    this.build();
  }

  private build() {
    const { skinColor, hairColor, dressColor, shoeColor, scale } = this.options;
    const skinMat = new THREE.MeshPhongMaterial({
      color: skinColor,
      shininess: 30,
      specular: 0x443322,
    });

    // === BODY (torso) ===
    const torsoGeom = new THREE.SphereGeometry(0.28, 16, 16);
    torsoGeom.scale(1, 1.15, 0.85);
    const torso = new THREE.Mesh(torsoGeom, skinMat);
    torso.position.y = 0.55;
    this.body.add(torso);

    // === DRESS ===
    const dressGeom = new THREE.ConeGeometry(0.38, 0.55, 16);
    const dressMat = new THREE.MeshPhongMaterial({
      color: dressColor,
      shininess: 50,
      specular: 0x442222,
    });
    this.dress = new THREE.Mesh(dressGeom, dressMat);
    this.dress.position.y = 0.28;
    this.body.add(this.dress);

    // Dress ruffle at bottom
    const ruffleGeom = new THREE.TorusGeometry(0.37, 0.03, 8, 24);
    const ruffleMat = new THREE.MeshPhongMaterial({ color: dressColor! - 0x111111 });
    const ruffle = new THREE.Mesh(ruffleGeom, ruffleMat);
    ruffle.position.y = 0.03;
    ruffle.rotation.x = Math.PI / 2;
    this.body.add(ruffle);

    // Collar / neckline detail
    const collarGeom = new THREE.TorusGeometry(0.12, 0.02, 8, 16);
    const collarMat = new THREE.MeshPhongMaterial({ color: 0xffffff });
    const collar = new THREE.Mesh(collarGeom, collarMat);
    collar.position.y = 0.7;
    collar.rotation.x = Math.PI / 2;
    this.body.add(collar);

    // === HEAD ===
    const headGeom = new THREE.SphereGeometry(0.28, 20, 20);
    const headMesh = new THREE.Mesh(headGeom, skinMat);
    headMesh.position.y = 1.0;
    this.head.add(headMesh);

    // Cheeks (blush)
    const cheekGeom = new THREE.SphereGeometry(0.06, 8, 8);
    const cheekMat = new THREE.MeshPhongMaterial({ color: 0xffaaaa, transparent: true, opacity: 0.6 });
    const leftCheek = new THREE.Mesh(cheekGeom, cheekMat);
    leftCheek.position.set(-0.18, 0.94, 0.2);
    this.head.add(leftCheek);
    const rightCheek = new THREE.Mesh(cheekGeom, cheekMat);
    rightCheek.position.set(0.18, 0.94, 0.2);
    this.head.add(rightCheek);

    // === HAIR ===
    // Main hair (back)
    const hairBackGeom = new THREE.SphereGeometry(0.30, 16, 16);
    hairBackGeom.scale(1.08, 1.05, 1.05);
    const hairMat = new THREE.MeshPhongMaterial({
      color: hairColor,
      shininess: 60,
      specular: 0x333333,
    });
    const hairBack = new THREE.Mesh(hairBackGeom, hairMat);
    hairBack.position.y = 1.03;
    this.head.add(hairBack);

    // Bangs
    const bangGeom = new THREE.SphereGeometry(0.29, 12, 8, 0, Math.PI * 2, 0, Math.PI * 0.35);
    const bangs = new THREE.Mesh(bangGeom, hairMat);
    bangs.position.set(0, 1.06, 0.02);
    this.head.add(bangs);

    // Side hair strands (pigtails)
    const pigtailGeom = new THREE.SphereGeometry(0.12, 10, 10);
    pigtailGeom.scale(0.8, 1.4, 0.8);
    const leftPigtail = new THREE.Mesh(pigtailGeom, hairMat);
    leftPigtail.position.set(-0.3, 0.85, -0.05);
    this.head.add(leftPigtail);
    const rightPigtail = new THREE.Mesh(pigtailGeom, hairMat);
    rightPigtail.position.set(0.3, 0.85, -0.05);
    this.head.add(rightPigtail);

    // Hair bow
    const bowGeom = new THREE.SphereGeometry(0.07, 8, 8);
    bowGeom.scale(1.8, 1, 0.8);
    const bowMat = new THREE.MeshPhongMaterial({ color: 0xff4081, shininess: 80 });
    this.hairBow = new THREE.Mesh(bowGeom, bowMat);
    this.hairBow.position.set(0.2, 1.28, 0.1);
    this.head.add(this.hairBow);
    // Second bow lobe
    const bow2 = new THREE.Mesh(bowGeom, bowMat);
    bow2.position.set(0.2, 1.28, -0.05);
    bow2.rotation.y = Math.PI * 0.3;
    this.head.add(bow2);

    // === EYES ===
    this.buildEyes();

    // === MOUTH ===
    this.buildMouth('happy');

    // === ARMS ===
    const armGeom = new THREE.CylinderGeometry(0.05, 0.045, 0.32, 8);
    const leftArmMesh = new THREE.Mesh(armGeom, skinMat);
    leftArmMesh.position.set(0, -0.12, 0);
    this.leftArm.add(leftArmMesh);
    this.leftArm.position.set(-0.35, 0.7, 0);

    // Hand
    const handGeom = new THREE.SphereGeometry(0.05, 8, 8);
    const leftHand = new THREE.Mesh(handGeom, skinMat);
    leftHand.position.set(0, -0.3, 0);
    this.leftArm.add(leftHand);

    const rightArmMesh = new THREE.Mesh(armGeom, skinMat);
    rightArmMesh.position.set(0, -0.12, 0);
    this.rightArm.add(rightArmMesh);
    this.rightArm.position.set(0.35, 0.7, 0);
    const rightHand = new THREE.Mesh(handGeom, skinMat);
    rightHand.position.set(0, -0.3, 0);
    this.rightArm.add(rightHand);

    // === LEGS ===
    const legGeom = new THREE.CylinderGeometry(0.055, 0.05, 0.28, 8);
    const leftLegMesh = new THREE.Mesh(legGeom, skinMat);
    leftLegMesh.position.set(0, -0.1, 0);
    this.leftLeg.add(leftLegMesh);
    this.leftLeg.position.set(-0.12, 0.05, 0);

    const rightLegMesh = new THREE.Mesh(legGeom, skinMat);
    rightLegMesh.position.set(0, -0.1, 0);
    this.rightLeg.add(rightLegMesh);
    this.rightLeg.position.set(0.12, 0.05, 0);

    // === SHOES ===
    const shoeGeom = new THREE.SphereGeometry(0.065, 8, 8);
    shoeGeom.scale(1.2, 0.6, 1.6);
    const shoeMat = new THREE.MeshPhongMaterial({ color: shoeColor, shininess: 80 });
    const leftShoe = new THREE.Mesh(shoeGeom, shoeMat);
    leftShoe.position.set(0, -0.26, 0.02);
    this.leftLeg.add(leftShoe);
    const rightShoe = new THREE.Mesh(shoeGeom, shoeMat);
    rightShoe.position.set(0, -0.26, 0.02);
    this.rightLeg.add(rightShoe);

    // Assemble
    this.group.add(this.body);
    this.group.add(this.head);
    this.group.add(this.leftArm);
    this.group.add(this.rightArm);
    this.group.add(this.leftLeg);
    this.group.add(this.rightLeg);

    if (scale && scale !== 1.0) {
      this.group.scale.setScalar(scale);
    }

    // Shadow
    this.group.traverse((child: THREE.Object3D) => {
      if (child instanceof THREE.Mesh) {
        child.castShadow = true;
        child.receiveShadow = true;
      }
    });
  }

  private buildEyes() {
    // Clear old eyes
    while (this.leftEye.children.length) this.leftEye.remove(this.leftEye.children[0]);
    while (this.rightEye.children.length) this.rightEye.remove(this.rightEye.children[0]);

    // Eye whites
    const whiteGeom = new THREE.SphereGeometry(0.055, 10, 10);
    const whiteMat = new THREE.MeshPhongMaterial({ color: 0xffffff });
    const leftWhite = new THREE.Mesh(whiteGeom, whiteMat);
    this.leftEye.add(leftWhite);
    const rightWhite = new THREE.Mesh(whiteGeom, whiteMat);
    this.rightEye.add(rightWhite);

    // Iris
    const irisGeom = new THREE.SphereGeometry(0.035, 8, 8);
    const irisMat = new THREE.MeshPhongMaterial({ color: 0x3d2314 });
    const leftIris = new THREE.Mesh(irisGeom, irisMat);
    leftIris.position.z = 0.03;
    this.leftEye.add(leftIris);
    const rightIris = new THREE.Mesh(irisGeom, irisMat);
    rightIris.position.z = 0.03;
    this.rightEye.add(rightIris);

    // Pupil
    const pupilGeom = new THREE.SphereGeometry(0.018, 6, 6);
    const pupilMat = new THREE.MeshBasicMaterial({ color: 0x000000 });
    const leftPupil = new THREE.Mesh(pupilGeom, pupilMat);
    leftPupil.position.z = 0.05;
    this.leftEye.add(leftPupil);
    const rightPupil = new THREE.Mesh(pupilGeom, pupilMat);
    rightPupil.position.z = 0.05;
    this.rightEye.add(rightPupil);

    // Eye shine/highlight
    const shineGeom = new THREE.SphereGeometry(0.012, 6, 6);
    const shineMat = new THREE.MeshBasicMaterial({ color: 0xffffff });
    const leftShine = new THREE.Mesh(shineGeom, shineMat);
    leftShine.position.set(0.015, 0.015, 0.055);
    this.leftEye.add(leftShine);
    const rightShine = new THREE.Mesh(shineGeom, shineMat);
    rightShine.position.set(0.015, 0.015, 0.055);
    this.rightEye.add(rightShine);

    // Position eyes on head
    this.leftEye.position.set(-0.1, 1.0, 0.22);
    this.rightEye.position.set(0.1, 1.0, 0.22);

    this.head.add(this.leftEye);
    this.head.add(this.rightEye);
  }

  private buildMouth(emotion: EmotionFace) {
    if (this.mouth.parent) {
      this.mouth.parent.remove(this.mouth);
    }
    let geom: THREE.BufferGeometry;
    let mat: THREE.Material;

    switch (emotion) {
      case 'happy':
      case 'excited':
        geom = new THREE.TorusGeometry(0.05, 0.012, 8, 16, Math.PI);
        mat = new THREE.MeshPhongMaterial({ color: 0xff6b9d });
        this.mouth = new THREE.Mesh(geom, mat);
        this.mouth.rotation.x = Math.PI;
        this.mouth.rotation.z = Math.PI;
        break;
      case 'sad':
      case 'hungry':
        geom = new THREE.TorusGeometry(0.04, 0.01, 8, 12, Math.PI);
        mat = new THREE.MeshPhongMaterial({ color: 0xff6b9d });
        this.mouth = new THREE.Mesh(geom, mat);
        break;
      case 'sleepy':
        geom = new THREE.PlaneGeometry(0.06, 0.01);
        mat = new THREE.MeshPhongMaterial({ color: 0xff6b9d, side: THREE.DoubleSide });
        this.mouth = new THREE.Mesh(geom, mat);
        break;
      default:
        geom = new THREE.TorusGeometry(0.04, 0.01, 8, 12, Math.PI);
        mat = new THREE.MeshPhongMaterial({ color: 0xff6b9d });
        this.mouth = new THREE.Mesh(geom, mat);
        this.mouth.rotation.x = Math.PI;
        this.mouth.rotation.z = Math.PI;
    }
    this.mouth.position.set(0, 0.9, 0.26);
    this.head.add(this.mouth);
  }

  setEmotion(emotion: EmotionFace) {
    if (this.emotion === emotion) return;
    this.emotion = emotion;
    this.buildMouth(emotion);
  }

  setAnimation(anim: CharacterAnim) {
    if (this.animState === anim) return;
    this.animState = anim;
    this.animTime = 0;

    // Reset pose
    this.leftArm.rotation.set(0, 0, 0);
    this.rightArm.rotation.set(0, 0, 0);
    this.leftLeg.rotation.set(0, 0, 0);
    this.rightLeg.rotation.set(0, 0, 0);
    this.head.rotation.set(0, 0, 0);
    this.body.rotation.set(0, 0, 0);

    // Set emotion based on animation
    switch (anim) {
      case 'happy':
      case 'dance':
        this.setEmotion('excited');
        break;
      case 'sleep':
        this.setEmotion('sleepy');
        break;
      case 'eat':
      case 'cook':
        this.setEmotion('happy');
        break;
      case 'scared':
        this.setEmotion('sad');
        break;
      default:
        this.setEmotion('happy');
    }
  }

  walkTo(target: THREE.Vector3, onArrive?: () => void) {
    this.targetPosition = target.clone();
    this.targetPosition.y = this.group.position.y;
    this.isMoving = true;
    this.setAnimation('walk');
    this.onArriveCallback = onArrive || null;
  }

  stopMoving() {
    this.targetPosition = null;
    this.isMoving = false;
    this.setAnimation('idle');
  }

  update(dt: number) {
    this.animTime += dt;

    // Handle movement
    if (this.isMoving && this.targetPosition) {
      const dir = new THREE.Vector3().subVectors(this.targetPosition, this.group.position);
      dir.y = 0;
      const dist = dir.length();

      if (dist < 0.1) {
        this.group.position.x = this.targetPosition.x;
        this.group.position.z = this.targetPosition.z;
        this.isMoving = false;
        this.targetPosition = null;
        this.setAnimation('idle');
        if (this.onArriveCallback) {
          const cb = this.onArriveCallback;
          this.onArriveCallback = null;
          cb();
        }
      } else {
        dir.normalize();
        const step = Math.min(this.moveSpeed * dt, dist);
        this.group.position.x += dir.x * step;
        this.group.position.z += dir.z * step;

        // Face movement direction
        const angle = Math.atan2(dir.x, dir.z);
        const currentAngle = this.group.rotation.y;
        let deltaAngle = angle - currentAngle;
        while (deltaAngle > Math.PI) deltaAngle -= Math.PI * 2;
        while (deltaAngle < -Math.PI) deltaAngle += Math.PI * 2;
        this.group.rotation.y += deltaAngle * Math.min(1, dt * 8);
      }
    }

    // Animate
    this.animate(dt);
  }

  private animate(dt: number) {
    const t = this.animTime;
    const speed = 1.0;

    switch (this.animState) {
      case 'idle': {
        // Gentle breathing/bobbing
        const bob = Math.sin(t * 2) * 0.02;
        this.body.position.y = bob;
        this.head.position.y = bob;
        this.leftArm.position.y = 0.7 + bob;
        this.rightArm.position.y = 0.7 + bob;
        // Subtle arm sway
        this.leftArm.rotation.z = Math.sin(t * 1.5) * 0.05 + 0.05;
        this.rightArm.rotation.z = -Math.sin(t * 1.5) * 0.05 - 0.05;
        // Slight head tilt
        this.head.rotation.z = Math.sin(t * 0.8) * 0.03;
        break;
      }

      case 'walk':
      case 'run': {
        const walkSpeed = this.animState === 'run' ? 10 : 6;
        const walkAmplitude = this.animState === 'run' ? 0.5 : 0.35;
        // Legs swing
        this.leftLeg.rotation.x = Math.sin(t * walkSpeed) * walkAmplitude;
        this.rightLeg.rotation.x = -Math.sin(t * walkSpeed) * walkAmplitude;
        // Arms counter-swing
        this.leftArm.rotation.x = -Math.sin(t * walkSpeed) * walkAmplitude * 0.6;
        this.rightArm.rotation.x = Math.sin(t * walkSpeed) * walkAmplitude * 0.6;
        // Body bounce
        const bounce = Math.abs(Math.sin(t * walkSpeed)) * 0.04;
        this.body.position.y = bounce;
        this.head.position.y = bounce;
        this.leftArm.position.y = 0.7 + bounce;
        this.rightArm.position.y = 0.7 + bounce;
        // Head bob
        this.head.rotation.z = Math.sin(t * walkSpeed) * 0.04;
        break;
      }

      case 'happy': {
        // Bouncy jump animation
        const jumpHeight = Math.abs(Math.sin(t * 5)) * 0.15;
        this.group.position.y = jumpHeight;
        // Arms up celebration
        this.leftArm.rotation.z = Math.sin(t * 8) * 0.3 + 0.8;
        this.rightArm.rotation.z = -Math.sin(t * 8) * 0.3 - 0.8;
        this.leftArm.rotation.x = Math.sin(t * 6) * 0.2;
        this.rightArm.rotation.x = Math.sin(t * 6) * 0.2;
        // Spin slightly
        this.head.rotation.z = Math.sin(t * 4) * 0.1;
        break;
      }

      case 'jump': {
        const jumpPhase = (t * 3) % (Math.PI * 2);
        const jumpH = Math.max(0, Math.sin(jumpPhase)) * 0.5;
        this.group.position.y = jumpH;
        if (jumpH > 0.1) {
          this.leftArm.rotation.z = 1.2;
          this.rightArm.rotation.z = -1.2;
        }
        break;
      }

      case 'eat': {
        // Right arm moves to mouth
        this.rightArm.rotation.x = -Math.sin(t * 3) * 0.4 - 0.8;
        this.rightArm.rotation.z = -0.3;
        // Head tilts slightly with each bite
        this.head.rotation.x = Math.sin(t * 3) * 0.05;
        // Body stays still
        const eatBob = Math.sin(t * 2) * 0.01;
        this.body.position.y = eatBob;
        this.head.position.y = eatBob;
        break;
      }

      case 'cook': {
        // Stir motion with right arm
        this.rightArm.rotation.x = -0.6;
        this.rightArm.rotation.z = Math.sin(t * 4) * 0.3;
        // Left arm holds pot
        this.leftArm.rotation.x = -0.4;
        this.leftArm.rotation.z = 0.2;
        // Body sway
        this.body.rotation.y = Math.sin(t * 2) * 0.05;
        this.head.rotation.y = Math.sin(t * 2) * 0.05;
        break;
      }

      case 'sleep': {
        // Lie down (rotate character)
        const sleepT = Math.min(t * 2, 1);
        this.body.rotation.x = sleepT * 0.3;
        this.head.rotation.x = sleepT * 0.1;
        // Breathing
        const breathe = Math.sin(t * 1.5) * 0.02;
        this.body.position.y = breathe;
        this.head.position.y = breathe;
        // Arms at sides
        this.leftArm.rotation.z = 0.3 * sleepT;
        this.rightArm.rotation.z = -0.3 * sleepT;
        break;
      }

      case 'dance': {
        // Dancing animation - rhythmic bounce and spin
        const danceBounce = Math.abs(Math.sin(t * 6)) * 0.1;
        this.group.position.y = danceBounce;
        // Body sway
        this.body.rotation.z = Math.sin(t * 4) * 0.15;
        this.head.rotation.z = Math.sin(t * 4) * 0.1;
        // Arms wave
        this.leftArm.rotation.z = Math.sin(t * 6) * 0.5 + 0.5;
        this.rightArm.rotation.z = -Math.sin(t * 6 + 1) * 0.5 - 0.5;
        this.leftArm.rotation.x = Math.cos(t * 4) * 0.3;
        this.rightArm.rotation.x = -Math.cos(t * 4) * 0.3;
        // Legs dance
        this.leftLeg.rotation.x = Math.sin(t * 6) * 0.2;
        this.rightLeg.rotation.x = -Math.sin(t * 6) * 0.2;
        // Slow spin
        this.group.rotation.y += dt * 1.5;
        break;
      }

      case 'karate': {
        // Karate kicks and punches
        const phase = Math.floor(t * 2) % 4;
        switch (phase) {
          case 0: // Punch right
            this.rightArm.rotation.x = -1.2;
            this.rightArm.rotation.z = -0.1;
            this.leftArm.rotation.x = -0.3;
            this.leftArm.rotation.z = 0.3;
            break;
          case 1: // Kick left
            this.leftLeg.rotation.x = -0.8;
            this.rightArm.rotation.x = -0.3;
            break;
          case 2: // Punch left
            this.leftArm.rotation.x = -1.2;
            this.leftArm.rotation.z = 0.1;
            this.rightArm.rotation.x = -0.3;
            this.rightArm.rotation.z = -0.3;
            break;
          case 3: // Kick right
            this.rightLeg.rotation.x = -0.8;
            this.leftArm.rotation.x = -0.3;
            break;
        }
        // Ready stance bob
        const karateBob = Math.sin(t * 4) * 0.03;
        this.body.position.y = karateBob;
        break;
      }

      case 'wave': {
        // Wave hello
        this.rightArm.rotation.z = -1.2;
        this.rightArm.rotation.x = Math.sin(t * 6) * 0.3;
        // Body still with gentle bob
        const waveBob = Math.sin(t * 2) * 0.02;
        this.body.position.y = waveBob;
        this.head.position.y = waveBob;
        // Head tilt
        this.head.rotation.z = Math.sin(t * 3) * 0.08;
        break;
      }

      case 'clean': {
        // Scrubbing motion
        this.rightArm.rotation.x = -0.5 + Math.sin(t * 8) * 0.3;
        this.rightArm.rotation.z = Math.cos(t * 8) * 0.2;
        this.body.rotation.y = Math.sin(t * 4) * 0.1;
        const cleanBob = Math.sin(t * 2) * 0.02;
        this.body.position.y = cleanBob;
        this.head.position.y = cleanBob;
        break;
      }

      case 'study': {
        // Looking down at book
        this.head.rotation.x = 0.2;
        this.body.rotation.x = 0.1;
        // Page flip motion
        this.rightArm.rotation.x = -0.3 + Math.sin(t * 1.5) * 0.15;
        this.leftArm.rotation.x = -0.3;
        const studyBob = Math.sin(t * 1) * 0.01;
        this.body.position.y = studyBob;
        break;
      }

      case 'scared': {
        // Shaking/trembling
        const shake = Math.sin(t * 20) * 0.02;
        this.group.position.x += shake;
        this.leftArm.rotation.z = 0.3 + Math.sin(t * 15) * 0.1;
        this.rightArm.rotation.z = -0.3 - Math.sin(t * 15) * 0.1;
        this.head.rotation.z = Math.sin(t * 12) * 0.05;
        break;
      }
    }
  }

  addToScene(scene: THREE.Scene) {
    scene.add(this.group);
  }

  setPosition(x: number, y: number, z: number) {
    this.group.position.set(x, y, z);
  }

  setRotation(y: number) {
    this.group.rotation.y = y;
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
