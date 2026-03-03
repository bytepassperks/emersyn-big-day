/**
 * Character.ts - 3D Chibi Character with GLTFLoader, AnimationMixer,
 * squash & stretch physics, eye tracking, jiggle bones, touch zones,
 * and idle micro-animations (breathing, blinking, looking around).
 */
import { THREE } from 'expo-three';
import { getModelUri } from './ModelAssets';

// Types
export type CharacterAnim =
  | 'idle' | 'walk' | 'run' | 'happy' | 'eat' | 'sleep'
  | 'dance' | 'karate' | 'wave' | 'jump' | 'cook' | 'clean'
  | 'study' | 'scared';

export type EmotionFace = 'happy' | 'neutral' | 'sleepy' | 'excited' | 'hungry' | 'sad';
export type TouchZone = 'head' | 'belly' | 'leftArm' | 'rightArm' | 'leftLeg' | 'rightLeg' | 'none';

// Spring physics helper
interface SpringState {
  value: number;
  velocity: number;
  target: number;
  stiffness: number;
  damping: number;
}

function springStep(s: SpringState, dt: number): void {
  const force = -s.stiffness * (s.value - s.target) - s.damping * s.velocity;
  s.velocity += force * dt;
  s.value += s.velocity * dt;
}

function makeSpring(target: number, stiffness: number, damping: number): SpringState {
  return { value: target, velocity: 0, target, stiffness, damping };
}

// Jiggle bone for secondary motion (pigtails, dress, ears, tail)
interface JiggleBone {
  object: THREE.Object3D;
  restRotX: number;
  restRotZ: number;
  springX: SpringState;
  springZ: SpringState;
}

function makeJiggleBone(obj: THREE.Object3D, stiffness: number, damping: number): JiggleBone {
  return {
    object: obj,
    restRotX: obj.rotation.x,
    restRotZ: obj.rotation.z,
    springX: makeSpring(0, stiffness, damping),
    springZ: makeSpring(0, stiffness, damping),
  };
}

// Map our animation names to Blender clip names
const ANIM_NAME_MAP: Record<string, string> = {
  idle: 'idle', walk: 'walk', run: 'run', happy: 'happy', eat: 'eat',
  sleep: 'sleep', dance: 'dance', karate: 'happy', wave: 'wave',
  jump: 'jump', cook: 'eat', clean: 'wave', study: 'idle', scared: 'idle',
};

interface CharacterOptions {
  skinColor?: number;
  hairColor?: number;
  dressColor?: number;
  shoeColor?: number;
  scale?: number;
  characterId?: string;
}

const DEFAULT_OPTIONS: CharacterOptions = {
  skinColor: 0xffdbac,
  hairColor: 0x4a2810,
  dressColor: 0xff6b9d,
  shoeColor: 0xff4081,
  scale: 1.0,
  characterId: 'emersyn',
};

export class Character {
  group: THREE.Group;
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

  // GLB model state
  private glbScene: THREE.Group | null = null;
  private mixer: THREE.AnimationMixer | null = null;
  private clips: Map<string, THREE.AnimationClip> = new Map();
  private currentAction: THREE.AnimationAction | null = null;
  private glbLoaded: boolean = false;

  // State
  animState: CharacterAnim = 'idle';
  emotion: EmotionFace = 'happy';
  animTime: number = 0;
  targetPosition: THREE.Vector3 | null = null;
  moveSpeed: number = 2.0;
  isMoving: boolean = false;
  private onArriveCallback: (() => void) | null = null;
  private options: CharacterOptions;

  // Squash & Stretch Springs
  private squashY: SpringState;
  private squashXZ: SpringState;
  private isSquashing: boolean = false;

  // Jiggle bones
  private jiggleBones: JiggleBone[] = [];
  private prevGroupPos: THREE.Vector3;

  // Eye / Head tracking
  private eyeTarget: THREE.Vector3 = new THREE.Vector3(0, 1, 5);
  private gazeTimer: number = 0;
  private gazeInterval: number = 3 + Math.random() * 3;
  private lastTouchWorld: THREE.Vector3 | null = null;

  // Blink
  private blinkTimer: number = 0;
  private blinkInterval: number = 3 + Math.random() * 4;
  private isBlinking: boolean = false;
  private blinkDuration: number = 0.15;
  private blinkElapsed: number = 0;

  // Idle micro-animations
  private breathPhase: number = Math.random() * Math.PI * 2;
  private weightShiftPhase: number = Math.random() * Math.PI * 2;
  private fidgetTimer: number = 0;
  private fidgetInterval: number = 8 + Math.random() * 6;
  private isFidgeting: boolean = false;
  private fidgetElapsed: number = 0;

  // Touch zone response
  private touchResponseTimer: number = 0;
  private touchResponseZone: TouchZone = 'none';

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
    this.squashY = makeSpring(1, 12, 4);
    this.squashXZ = makeSpring(1, 12, 4);
    this.prevGroupPos = new THREE.Vector3();
    this.build();
    this.loadGLBModel();
  }

  // =========================================================================
  // GLB Loading
  // =========================================================================

  private async loadGLBModel(): Promise<void> {
    const characterId = this.options.characterId || 'emersyn';
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
      this.glbScene.scale.setScalar(this.options.scale || 1.0);
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
      // Hide procedural, show GLB
      this.head.visible = false;
      this.body.visible = false;
      this.leftArm.visible = false;
      this.rightArm.visible = false;
      this.leftLeg.visible = false;
      this.rightLeg.visible = false;
      this.group.add(this.glbScene);
      this.setupJiggleBones();
      this.playClip('idle');
      this.glbLoaded = true;
    } catch (err) {
      console.warn('GLB load failed, using procedural mesh:', err);
    }
  }

  private playClip(animName: string, crossFade: number = 0.3): void {
    if (!this.mixer) return;
    const clipName = ANIM_NAME_MAP[animName] || animName;
    const clip = this.clips.get(clipName);
    if (!clip) return;
    const newAction = this.mixer.clipAction(clip);
    if (this.currentAction && this.currentAction !== newAction) {
      this.currentAction.fadeOut(crossFade);
    }
    newAction.reset().fadeIn(crossFade).play();
    this.currentAction = newAction;
  }

  private setupJiggleBones(): void {
    if (!this.glbScene) return;
    const jiggleNames = [
      'pigtail_l', 'pigtail_r', 'dress_front', 'dress_back',
      'dress_l', 'dress_r', 'ear_l', 'ear_r', 'tail', 'hair_front', 'bow',
    ];
    this.glbScene.traverse((child: THREE.Object3D) => {
      const lower = child.name.toLowerCase();
      for (const jn of jiggleNames) {
        if (lower.includes(jn)) {
          this.jiggleBones.push(makeJiggleBone(child, 8, 3));
          break;
        }
      }
    });
  }

  // =========================================================================
  // Procedural mesh build (fallback)
  // =========================================================================

  private build() {
    const { skinColor, hairColor, dressColor, shoeColor, scale } = this.options;
    const skinMat = new THREE.MeshPhongMaterial({ color: skinColor, shininess: 30, specular: 0x443322 });

    // Torso
    const torsoGeom = new THREE.SphereGeometry(0.28, 16, 16);
    torsoGeom.scale(1, 1.15, 0.85);
    const torso = new THREE.Mesh(torsoGeom, skinMat);
    torso.position.y = 0.55;
    this.body.add(torso);

    // Dress
    const dressGeom = new THREE.ConeGeometry(0.38, 0.55, 16);
    const dressMat = new THREE.MeshPhongMaterial({ color: dressColor, shininess: 50, specular: 0x442222 });
    this.dress = new THREE.Mesh(dressGeom, dressMat);
    this.dress.position.y = 0.28;
    this.body.add(this.dress);

    const ruffleGeom = new THREE.TorusGeometry(0.37, 0.03, 8, 24);
    const ruffleMat = new THREE.MeshPhongMaterial({ color: (dressColor || 0xff6b9d) - 0x111111 });
    const ruffle = new THREE.Mesh(ruffleGeom, ruffleMat);
    ruffle.position.y = 0.03;
    ruffle.rotation.x = Math.PI / 2;
    this.body.add(ruffle);

    const collarGeom = new THREE.TorusGeometry(0.12, 0.02, 8, 16);
    const collarMat = new THREE.MeshPhongMaterial({ color: 0xffffff });
    const collar = new THREE.Mesh(collarGeom, collarMat);
    collar.position.y = 0.7;
    collar.rotation.x = Math.PI / 2;
    this.body.add(collar);

    // Head
    const headGeom = new THREE.SphereGeometry(0.28, 20, 20);
    const headMesh = new THREE.Mesh(headGeom, skinMat);
    headMesh.position.y = 1.0;
    this.head.add(headMesh);

    const cheekGeom = new THREE.SphereGeometry(0.06, 8, 8);
    const cheekMat = new THREE.MeshPhongMaterial({ color: 0xffaaaa, transparent: true, opacity: 0.6 });
    const lCheek = new THREE.Mesh(cheekGeom, cheekMat);
    lCheek.position.set(-0.18, 0.94, 0.2);
    this.head.add(lCheek);
    const rCheek = new THREE.Mesh(cheekGeom, cheekMat);
    rCheek.position.set(0.18, 0.94, 0.2);
    this.head.add(rCheek);

    // Hair
    const hairBackGeom = new THREE.SphereGeometry(0.30, 16, 16);
    hairBackGeom.scale(1.08, 1.05, 1.05);
    const hairMat = new THREE.MeshPhongMaterial({ color: hairColor, shininess: 60, specular: 0x333333 });
    const hairBack = new THREE.Mesh(hairBackGeom, hairMat);
    hairBack.position.y = 1.03;
    this.head.add(hairBack);

    const bangGeom = new THREE.SphereGeometry(0.29, 12, 8, 0, Math.PI * 2, 0, Math.PI * 0.35);
    const bangs = new THREE.Mesh(bangGeom, hairMat);
    bangs.position.set(0, 1.06, 0.02);
    this.head.add(bangs);

    const ptGeom = new THREE.SphereGeometry(0.12, 10, 10);
    ptGeom.scale(0.8, 1.4, 0.8);
    const lPigtail = new THREE.Mesh(ptGeom, hairMat);
    lPigtail.position.set(-0.3, 0.85, -0.05);
    lPigtail.name = 'pigtail_l';
    this.head.add(lPigtail);
    const rPigtail = new THREE.Mesh(ptGeom, hairMat);
    rPigtail.position.set(0.3, 0.85, -0.05);
    rPigtail.name = 'pigtail_r';
    this.head.add(rPigtail);

    const bowGeom = new THREE.SphereGeometry(0.07, 8, 8);
    bowGeom.scale(1.8, 1, 0.8);
    const bowMat = new THREE.MeshPhongMaterial({ color: 0xff4081, shininess: 80 });
    this.hairBow = new THREE.Mesh(bowGeom, bowMat);
    this.hairBow.position.set(0.2, 1.28, 0.1);
    this.head.add(this.hairBow);
    const bow2 = new THREE.Mesh(bowGeom, bowMat);
    bow2.position.set(0.2, 1.28, -0.05);
    bow2.rotation.y = Math.PI * 0.3;
    this.head.add(bow2);

    this.buildEyes();
    this.buildMouth('happy');

    // Arms
    const armGeom = new THREE.CylinderGeometry(0.05, 0.045, 0.32, 8);
    const handGeom = new THREE.SphereGeometry(0.05, 8, 8);
    const lArmM = new THREE.Mesh(armGeom, skinMat);
    lArmM.position.set(0, -0.12, 0);
    this.leftArm.add(lArmM);
    this.leftArm.position.set(-0.35, 0.7, 0);
    const lHand = new THREE.Mesh(handGeom, skinMat);
    lHand.position.set(0, -0.3, 0);
    this.leftArm.add(lHand);

    const rArmM = new THREE.Mesh(armGeom, skinMat);
    rArmM.position.set(0, -0.12, 0);
    this.rightArm.add(rArmM);
    this.rightArm.position.set(0.35, 0.7, 0);
    const rHand = new THREE.Mesh(handGeom, skinMat);
    rHand.position.set(0, -0.3, 0);
    this.rightArm.add(rHand);

    // Legs
    const legGeom = new THREE.CylinderGeometry(0.055, 0.05, 0.28, 8);
    const lLegM = new THREE.Mesh(legGeom, skinMat);
    lLegM.position.set(0, -0.1, 0);
    this.leftLeg.add(lLegM);
    this.leftLeg.position.set(-0.12, 0.05, 0);
    const rLegM = new THREE.Mesh(legGeom, skinMat);
    rLegM.position.set(0, -0.1, 0);
    this.rightLeg.add(rLegM);
    this.rightLeg.position.set(0.12, 0.05, 0);

    // Shoes
    const shoeGeom = new THREE.SphereGeometry(0.065, 8, 8);
    shoeGeom.scale(1.2, 0.6, 1.6);
    const shoeMat = new THREE.MeshPhongMaterial({ color: shoeColor, shininess: 80 });
    const lShoe = new THREE.Mesh(shoeGeom, shoeMat);
    lShoe.position.set(0, -0.26, 0.02);
    this.leftLeg.add(lShoe);
    const rShoe = new THREE.Mesh(shoeGeom, shoeMat);
    rShoe.position.set(0, -0.26, 0.02);
    this.rightLeg.add(rShoe);

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
    this.group.traverse((child: THREE.Object3D) => {
      if (child instanceof THREE.Mesh) {
        child.castShadow = true;
        child.receiveShadow = true;
      }
    });
    // Jiggle on procedural pigtails
    this.jiggleBones.push(makeJiggleBone(lPigtail, 6, 2.5));
    this.jiggleBones.push(makeJiggleBone(rPigtail, 6, 2.5));
  }

  private buildEyes() {
    while (this.leftEye.children.length) this.leftEye.remove(this.leftEye.children[0]);
    while (this.rightEye.children.length) this.rightEye.remove(this.rightEye.children[0]);

    const whiteGeom = new THREE.SphereGeometry(0.055, 10, 10);
    const whiteMat = new THREE.MeshPhongMaterial({ color: 0xffffff });
    this.leftEye.add(new THREE.Mesh(whiteGeom, whiteMat));
    this.rightEye.add(new THREE.Mesh(whiteGeom, whiteMat));

    const irisGeom = new THREE.SphereGeometry(0.035, 8, 8);
    const irisMat = new THREE.MeshPhongMaterial({ color: 0x3d2314 });
    const lIris = new THREE.Mesh(irisGeom, irisMat);
    lIris.position.z = 0.03;
    lIris.name = 'iris';
    this.leftEye.add(lIris);
    const rIris = new THREE.Mesh(irisGeom, irisMat);
    rIris.position.z = 0.03;
    rIris.name = 'iris';
    this.rightEye.add(rIris);

    const pupilGeom = new THREE.SphereGeometry(0.018, 6, 6);
    const pupilMat = new THREE.MeshBasicMaterial({ color: 0x000000 });
    const lPupil = new THREE.Mesh(pupilGeom, pupilMat);
    lPupil.position.z = 0.05;
    this.leftEye.add(lPupil);
    const rPupil = new THREE.Mesh(pupilGeom, pupilMat);
    rPupil.position.z = 0.05;
    this.rightEye.add(rPupil);

    const shineGeom = new THREE.SphereGeometry(0.012, 6, 6);
    const shineMat = new THREE.MeshBasicMaterial({ color: 0xffffff });
    const lShine = new THREE.Mesh(shineGeom, shineMat);
    lShine.position.set(0.015, 0.015, 0.055);
    this.leftEye.add(lShine);
    const rShine = new THREE.Mesh(shineGeom, shineMat);
    rShine.position.set(0.015, 0.015, 0.055);
    this.rightEye.add(rShine);

    // Eyelids for blinking
    const lidGeom = new THREE.SphereGeometry(0.057, 10, 10, 0, Math.PI * 2, 0, Math.PI * 0.5);
    const lidMat = new THREE.MeshPhongMaterial({ color: this.options.skinColor || 0xffdbac });
    const lLid = new THREE.Mesh(lidGeom, lidMat);
    lLid.name = 'eyelid';
    lLid.rotation.x = Math.PI;
    lLid.scale.y = 0;
    this.leftEye.add(lLid);
    const rLid = new THREE.Mesh(lidGeom, lidMat);
    rLid.name = 'eyelid';
    rLid.rotation.x = Math.PI;
    rLid.scale.y = 0;
    this.rightEye.add(rLid);

    this.leftEye.position.set(-0.1, 1.0, 0.22);
    this.rightEye.position.set(0.1, 1.0, 0.22);
    this.head.add(this.leftEye);
    this.head.add(this.rightEye);
  }

  private buildMouth(emotion: EmotionFace) {
    if (this.mouth.parent) {
      // Dispose old mouth resources before replacing
      if (this.mouth instanceof THREE.Mesh) {
        this.mouth.geometry.dispose();
        if (Array.isArray(this.mouth.material)) {
          this.mouth.material.forEach((m: THREE.Material) => { m.dispose(); });
        } else {
          this.mouth.material.dispose();
        }
      }
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

  // =========================================================================
  // Public API
  // =========================================================================

  setEmotion(emotion: EmotionFace) {
    if (this.emotion === emotion) return;
    this.emotion = emotion;
    this.buildMouth(emotion);
  }

  setAnimation(anim: CharacterAnim) {
    if (this.animState === anim) return;
    this.animState = anim;
    this.animTime = 0;
    this.leftArm.rotation.set(0, 0, 0);
    this.rightArm.rotation.set(0, 0, 0);
    this.leftLeg.rotation.set(0, 0, 0);
    this.rightLeg.rotation.set(0, 0, 0);
    this.head.rotation.set(0, 0, 0);
    this.body.rotation.set(0, 0, 0);
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
    if (this.glbLoaded) {
      this.playClip(anim);
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

  /** Called when player taps on the character */
  onTap(zone: TouchZone = 'belly'): void {
    this.touchResponseZone = zone;
    this.touchResponseTimer = 0;
    this.isSquashing = true;
    switch (zone) {
      case 'head':
        this.squashY.velocity = -4;
        this.squashXZ.velocity = 2;
        break;
      case 'belly':
        this.squashY.velocity = -6;
        this.squashXZ.velocity = 3;
        break;
      case 'leftArm':
      case 'rightArm':
        this.squashY.velocity = 2;
        this.squashXZ.velocity = -1;
        break;
      case 'leftLeg':
      case 'rightLeg':
        this.squashY.velocity = 3;
        this.squashXZ.velocity = -2;
        break;
      default:
        this.squashY.velocity = -3;
        this.squashXZ.velocity = 2;
    }
  }

  /** Detect which touch zone was hit based on local y position */
  getTouchZone(localY: number): TouchZone {
    if (localY > 0.85) return 'head';
    if (localY > 0.35) return 'belly';
    return 'leftLeg';
  }

  /** Set where the character's eyes should look */
  setLookTarget(worldPos: THREE.Vector3): void {
    this.lastTouchWorld = worldPos.clone();
    this.gazeTimer = 0;
  }

  // =========================================================================
  // Update (main loop)
  // =========================================================================

  update(dt: number) {
    this.animTime += dt;

    // Movement
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
        // Landing squash
        this.squashY.velocity = -3;
        this.squashXZ.velocity = 2;
        this.isSquashing = true;
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
        const angle = Math.atan2(dir.x, dir.z);
        let deltaAngle = angle - this.group.rotation.y;
        while (deltaAngle > Math.PI) deltaAngle -= Math.PI * 2;
        while (deltaAngle < -Math.PI) deltaAngle += Math.PI * 2;
        this.group.rotation.y += deltaAngle * Math.min(1, dt * 8);
      }
    }

    // AnimationMixer
    if (this.mixer) {
      this.mixer.update(dt);
    }

    // Procedural animation fallback
    if (!this.glbLoaded) {
      this.animate(dt);
    }

    // Physics & micro-animations
    this.updateSquashStretch(dt);
    this.updateJiggleBones(dt);
    this.updateEyeTracking(dt);
    this.updateBlink(dt);
    this.updateIdleMicro(dt);
    this.updateTouchResponse(dt);
    this.prevGroupPos.copy(this.group.position);
  }

  // =========================================================================
  // Squash & Stretch
  // =========================================================================

  private updateSquashStretch(dt: number): void {
    if (!this.isSquashing && Math.abs(this.squashY.value - 1) < 0.005 && Math.abs(this.squashY.velocity) < 0.01) {
      return;
    }
    springStep(this.squashY, dt);
    springStep(this.squashXZ, dt);
    // Volume preservation: sxz = 1/sqrt(sy)
    const sy = this.squashY.value;
    const sxz = 1 / Math.sqrt(Math.max(0.5, sy));
    if (this.glbLoaded && this.glbScene) {
      const bs = this.options.scale || 1;
      this.glbScene.scale.set(bs * sxz, bs * sy, bs * sxz);
    } else {
      this.body.scale.set(sxz, sy, sxz);
      this.head.scale.set(sxz, sy, sxz);
    }
    if (Math.abs(this.squashY.value - 1) < 0.005 && Math.abs(this.squashY.velocity) < 0.01) {
      this.squashY.value = 1;
      this.squashY.velocity = 0;
      this.squashXZ.value = 1;
      this.squashXZ.velocity = 0;
      this.isSquashing = false;
      if (this.glbLoaded && this.glbScene) {
        this.glbScene.scale.setScalar(this.options.scale || 1);
      } else {
        this.body.scale.set(1, 1, 1);
        this.head.scale.set(1, 1, 1);
      }
    }
  }

  // =========================================================================
  // Jiggle Bones (secondary motion)
  // =========================================================================

  private updateJiggleBones(dt: number): void {
    if (this.jiggleBones.length === 0) return;
    const dx = this.group.position.x - this.prevGroupPos.x;
    const dz = this.group.position.z - this.prevGroupPos.z;
    const moveMag = Math.sqrt(dx * dx + dz * dz);
    for (const jb of this.jiggleBones) {
      if (moveMag > 0.001) {
        jb.springX.velocity -= dx * 15;
        jb.springZ.velocity -= dz * 15;
      }
      if (this.isSquashing) {
        jb.springX.velocity += (Math.random() - 0.5) * 2;
        jb.springZ.velocity += (Math.random() - 0.5) * 2;
      }
      springStep(jb.springX, dt);
      springStep(jb.springZ, dt);
      jb.object.rotation.x = jb.restRotX + jb.springX.value * 0.15;
      jb.object.rotation.z = jb.restRotZ + jb.springZ.value * 0.15;
    }
  }

  // =========================================================================
  // Eye / Head Tracking
  // =========================================================================

  private updateEyeTracking(dt: number): void {
    if (this.glbLoaded) return;
    let target = this.eyeTarget;
    if (this.lastTouchWorld) {
      target = this.lastTouchWorld;
      this.gazeTimer += dt;
      if (this.gazeTimer > 3) {
        this.lastTouchWorld = null;
        this.gazeTimer = 0;
      }
    } else {
      this.gazeTimer += dt;
      if (this.gazeTimer >= this.gazeInterval) {
        this.gazeTimer = 0;
        this.gazeInterval = 2 + Math.random() * 4;
        this.eyeTarget = new THREE.Vector3(
          (Math.random() - 0.5) * 4,
          0.5 + Math.random() * 1.5,
          2 + Math.random() * 3,
        );
      }
      target = this.eyeTarget;
    }
    const localTarget = this.head.worldToLocal(target.clone());
    const maxH = 0.52;
    const maxV = 0.35;
    const eyeOffsetX = Math.max(-maxH, Math.min(maxH, Math.atan2(localTarget.x, localTarget.z) * 0.3));
    const eyeOffsetY = Math.max(-maxV, Math.min(maxV, Math.atan2(localTarget.y - 1, localTarget.z) * 0.3));
    const lf = 1 - Math.exp(-5 * dt);
    for (const eye of [this.leftEye, this.rightEye]) {
      for (const child of eye.children) {
        if (child.name === 'iris') {
          child.position.x += (eyeOffsetX * 0.02 - child.position.x) * lf;
          child.position.y += (eyeOffsetY * 0.02 - child.position.y) * lf;
        }
      }
    }
    // Head follows gaze in idle
    if (this.animState === 'idle') {
      const hl = 1 - Math.exp(-2 * dt);
      const hty = Math.max(-0.25, Math.min(0.25, eyeOffsetX * 0.5));
      const htx = Math.max(-0.15, Math.min(0.15, -eyeOffsetY * 0.3));
      this.head.rotation.y += (hty - this.head.rotation.y) * hl;
      this.head.rotation.x += (htx - this.head.rotation.x) * hl;
    }
  }

  // =========================================================================
  // Blinking
  // =========================================================================

  private updateBlink(dt: number): void {
    if (this.glbLoaded) return;
    if (this.isBlinking) {
      this.blinkElapsed += dt;
      const progress = this.blinkElapsed / this.blinkDuration;
      const lidScale = progress < 0.5 ? progress * 2 : 2 - progress * 2;
      for (const eye of [this.leftEye, this.rightEye]) {
        for (const child of eye.children) {
          if (child.name === 'eyelid') {
            child.scale.y = lidScale;
          }
        }
      }
      if (this.blinkElapsed >= this.blinkDuration) {
        this.isBlinking = false;
        this.blinkElapsed = 0;
        for (const eye of [this.leftEye, this.rightEye]) {
          for (const child of eye.children) {
            if (child.name === 'eyelid') {
              child.scale.y = 0;
            }
          }
        }
      }
    } else {
      this.blinkTimer += dt;
      if (this.blinkTimer >= this.blinkInterval) {
        this.blinkTimer = 0;
        this.blinkInterval = 2 + Math.random() * 5;
        this.isBlinking = true;
        this.blinkElapsed = 0;
      }
    }
  }

  // =========================================================================
  // Idle micro-animations (breathing, weight shift, fidgets)
  // =========================================================================

  private updateIdleMicro(dt: number): void {
    if (this.glbLoaded) return;
    if (this.animState !== 'idle') return;

    // Breathing (1.8 Hz)
    this.breathPhase += dt * 1.8;
    const bs = 1 + Math.sin(this.breathPhase) * 0.015;
    this.body.scale.x = bs;
    this.body.scale.z = bs;

    // Weight shift (0.6 Hz)
    this.weightShiftPhase += dt * 0.6;
    const sway = Math.sin(this.weightShiftPhase) * 0.02;
    this.body.rotation.z = sway;
    this.head.rotation.z = sway * 0.5;

    // Fidgets (every 8-14s)
    this.fidgetTimer += dt;
    if (this.isFidgeting) {
      this.fidgetElapsed += dt;
      const ft = this.fidgetElapsed;
      this.leftArm.rotation.z = Math.sin(ft * 3) * 0.5 + 0.5;
      this.rightArm.rotation.z = -Math.sin(ft * 3 + 0.5) * 0.3;
      this.head.rotation.x = Math.sin(ft * 2) * 0.1;
      if (this.fidgetElapsed > 1.5) {
        this.isFidgeting = false;
        this.fidgetElapsed = 0;
        this.fidgetTimer = 0;
        this.fidgetInterval = 6 + Math.random() * 8;
        this.leftArm.rotation.set(0, 0, 0);
        this.rightArm.rotation.set(0, 0, 0);
      }
    } else if (this.fidgetTimer >= this.fidgetInterval) {
      this.isFidgeting = true;
      this.fidgetElapsed = 0;
    }
  }

  // =========================================================================
  // Touch response overlay
  // =========================================================================

  private updateTouchResponse(dt: number): void {
    if (this.touchResponseZone === 'none') return;
    this.touchResponseTimer += dt;
    const t = this.touchResponseTimer;
    if (t > 0.8) {
      this.touchResponseZone = 'none';
      this.touchResponseTimer = 0;
      return;
    }
    if (this.glbLoaded) return;
    const decay = Math.exp(-t * 5);
    switch (this.touchResponseZone) {
      case 'head':
        this.head.rotation.x = Math.sin(t * 15) * 0.15 * decay;
        break;
      case 'belly':
        this.body.rotation.x = Math.sin(t * 12) * 0.1 * decay;
        this.leftArm.rotation.z = Math.sin(t * 10) * 0.3 * decay + 0.1;
        this.rightArm.rotation.z = -Math.sin(t * 10) * 0.3 * decay - 0.1;
        break;
      case 'leftArm':
        this.leftArm.rotation.z = Math.sin(t * 12) * 0.4 * decay + 0.3;
        break;
      case 'rightArm':
        this.rightArm.rotation.z = -Math.sin(t * 12) * 0.4 * decay - 0.3;
        break;
      case 'leftLeg':
        this.leftLeg.rotation.x = -Math.sin(t * 10) * 0.5 * decay;
        break;
      case 'rightLeg':
        this.rightLeg.rotation.x = -Math.sin(t * 10) * 0.5 * decay;
        break;
    }
  }

  // =========================================================================
  // Procedural animation (fallback when GLB not loaded)
  // =========================================================================

  private animate(dt: number) {
    const t = this.animTime;
    switch (this.animState) {
      case 'idle': {
        const bob = Math.sin(t * 2) * 0.02;
        this.body.position.y = bob;
        this.head.position.y = bob;
        this.leftArm.position.y = 0.7 + bob;
        this.rightArm.position.y = 0.7 + bob;
        this.leftArm.rotation.z = Math.sin(t * 1.5) * 0.05 + 0.05;
        this.rightArm.rotation.z = -Math.sin(t * 1.5) * 0.05 - 0.05;
        this.head.rotation.z = Math.sin(t * 0.8) * 0.03;
        break;
      }
      case 'walk':
      case 'run': {
        const ws = this.animState === 'run' ? 10 : 6;
        const wa = this.animState === 'run' ? 0.5 : 0.35;
        this.leftLeg.rotation.x = Math.sin(t * ws) * wa;
        this.rightLeg.rotation.x = -Math.sin(t * ws) * wa;
        this.leftArm.rotation.x = -Math.sin(t * ws) * wa * 0.6;
        this.rightArm.rotation.x = Math.sin(t * ws) * wa * 0.6;
        const bounce = Math.abs(Math.sin(t * ws)) * 0.04;
        this.body.position.y = bounce;
        this.head.position.y = bounce;
        this.leftArm.position.y = 0.7 + bounce;
        this.rightArm.position.y = 0.7 + bounce;
        this.head.rotation.z = Math.sin(t * ws) * 0.04;
        break;
      }
      case 'happy': {
        const jh = Math.abs(Math.sin(t * 5)) * 0.15;
        this.body.position.y = jh;
        this.leftArm.rotation.z = Math.sin(t * 8) * 0.3 + 0.8;
        this.rightArm.rotation.z = -Math.sin(t * 8) * 0.3 - 0.8;
        this.leftArm.rotation.x = Math.sin(t * 6) * 0.2;
        this.rightArm.rotation.x = Math.sin(t * 6) * 0.2;
        this.head.rotation.z = Math.sin(t * 4) * 0.1;
        break;
      }
      case 'jump': {
        const jp = (t * 3) % (Math.PI * 2);
        const jH = Math.max(0, Math.sin(jp)) * 0.5;
        this.body.position.y = jH;
        if (jH > 0.1) {
          this.leftArm.rotation.z = 1.2;
          this.rightArm.rotation.z = -1.2;
        }
        break;
      }
      case 'eat': {
        this.rightArm.rotation.x = -Math.sin(t * 3) * 0.4 - 0.8;
        this.rightArm.rotation.z = -0.3;
        this.head.rotation.x = Math.sin(t * 3) * 0.05;
        const eb = Math.sin(t * 2) * 0.01;
        this.body.position.y = eb;
        this.head.position.y = eb;
        break;
      }
      case 'cook': {
        this.rightArm.rotation.x = -0.6;
        this.rightArm.rotation.z = Math.sin(t * 4) * 0.3;
        this.leftArm.rotation.x = -0.4;
        this.leftArm.rotation.z = 0.2;
        this.body.rotation.y = Math.sin(t * 2) * 0.05;
        this.head.rotation.y = Math.sin(t * 2) * 0.05;
        break;
      }
      case 'sleep': {
        const st = Math.min(t * 2, 1);
        this.body.rotation.x = st * 0.3;
        this.head.rotation.x = st * 0.1;
        const br = Math.sin(t * 1.5) * 0.02;
        this.body.position.y = br;
        this.head.position.y = br;
        this.leftArm.rotation.z = 0.3 * st;
        this.rightArm.rotation.z = -0.3 * st;
        break;
      }
      case 'dance': {
        const db = Math.abs(Math.sin(t * 6)) * 0.1;
        this.body.position.y = db;
        this.body.rotation.z = Math.sin(t * 4) * 0.15;
        this.head.rotation.z = Math.sin(t * 4) * 0.1;
        this.leftArm.rotation.z = Math.sin(t * 6) * 0.5 + 0.5;
        this.rightArm.rotation.z = -Math.sin(t * 6 + 1) * 0.5 - 0.5;
        this.leftArm.rotation.x = Math.cos(t * 4) * 0.3;
        this.rightArm.rotation.x = -Math.cos(t * 4) * 0.3;
        this.leftLeg.rotation.x = Math.sin(t * 6) * 0.2;
        this.rightLeg.rotation.x = -Math.sin(t * 6) * 0.2;
        this.group.rotation.y += dt * 1.5;
        break;
      }
      case 'karate': {
        const phase = Math.floor(t * 2) % 4;
        switch (phase) {
          case 0:
            this.rightArm.rotation.x = -1.2;
            this.rightArm.rotation.z = -0.1;
            this.leftArm.rotation.x = -0.3;
            this.leftArm.rotation.z = 0.3;
            break;
          case 1:
            this.leftLeg.rotation.x = -0.8;
            this.rightArm.rotation.x = -0.3;
            break;
          case 2:
            this.leftArm.rotation.x = -1.2;
            this.leftArm.rotation.z = 0.1;
            this.rightArm.rotation.x = -0.3;
            this.rightArm.rotation.z = -0.3;
            break;
          case 3:
            this.rightLeg.rotation.x = -0.8;
            this.leftArm.rotation.x = -0.3;
            break;
        }
        this.body.position.y = Math.sin(t * 4) * 0.03;
        break;
      }
      case 'wave': {
        this.rightArm.rotation.z = -1.2;
        this.rightArm.rotation.x = Math.sin(t * 6) * 0.3;
        const wb = Math.sin(t * 2) * 0.02;
        this.body.position.y = wb;
        this.head.position.y = wb;
        this.head.rotation.z = Math.sin(t * 3) * 0.08;
        break;
      }
      case 'clean': {
        this.rightArm.rotation.x = -0.5 + Math.sin(t * 8) * 0.3;
        this.rightArm.rotation.z = Math.cos(t * 8) * 0.2;
        this.body.rotation.y = Math.sin(t * 4) * 0.1;
        const cb = Math.sin(t * 2) * 0.02;
        this.body.position.y = cb;
        this.head.position.y = cb;
        break;
      }
      case 'study': {
        this.head.rotation.x = 0.2;
        this.body.rotation.x = 0.1;
        this.rightArm.rotation.x = -0.3 + Math.sin(t * 1.5) * 0.15;
        this.leftArm.rotation.x = -0.3;
        this.body.position.y = Math.sin(t) * 0.01;
        break;
      }
      case 'scared': {
        const sh = Math.sin(t * 20) * 0.02;
        this.body.position.x = sh;
        this.leftArm.rotation.z = 0.3 + Math.sin(t * 15) * 0.1;
        this.rightArm.rotation.z = -0.3 - Math.sin(t * 15) * 0.1;
        this.head.rotation.z = Math.sin(t * 12) * 0.05;
        break;
      }
    }
  }

  // =========================================================================
  // Scene management
  // =========================================================================

  addToScene(scene: THREE.Scene) {
    scene.add(this.group);
  }

  setPosition(x: number, y: number, z: number) {
    this.group.position.set(x, y, z);
    this.prevGroupPos.set(x, y, z);
  }

  setRotation(y: number) {
    this.group.rotation.y = y;
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
          child.material.forEach((m: THREE.Material) => { m.dispose(); });
        } else {
          child.material.dispose();
        }
      }
    });
  }
}
