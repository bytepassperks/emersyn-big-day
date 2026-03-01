/**
 * CameraController.ts - Camera with spring follow physics,
 * screen shake system, and dolly zoom for room transitions.
 */
import { THREE } from 'expo-three';

export interface CameraConfig {
  distance: number;
  height: number;
  angle: number;
  lookAtY: number;
  fov: number;
}

const ROOM_CAMERAS: Record<string, CameraConfig> = {
  bedroom: { distance: 5, height: 3.5, angle: Math.PI * 0.15, lookAtY: 0.5, fov: 45 },
  kitchen: { distance: 5.5, height: 3.5, angle: Math.PI * 0.12, lookAtY: 0.5, fov: 45 },
  park: { distance: 7, height: 4.5, angle: Math.PI * 0.15, lookAtY: 0.3, fov: 50 },
  school: { distance: 5.5, height: 3.5, angle: Math.PI * 0.15, lookAtY: 0.5, fov: 45 },
  arcade: { distance: 5, height: 3, angle: Math.PI * 0.12, lookAtY: 0.5, fov: 48 },
  studio: { distance: 5.5, height: 3.5, angle: Math.PI * 0.15, lookAtY: 0.5, fov: 45 },
  shop: { distance: 5.5, height: 3.5, angle: Math.PI * 0.12, lookAtY: 0.5, fov: 45 },
  bathroom: { distance: 4.5, height: 3, angle: Math.PI * 0.15, lookAtY: 0.5, fov: 45 },
  home: { distance: 5.5, height: 3.5, angle: Math.PI * 0.15, lookAtY: 0.5, fov: 45 },
};

// Spring state for camera follow
interface SpringVec3 {
  x: number; y: number; z: number;
  vx: number; vy: number; vz: number;
  tx: number; ty: number; tz: number;
  stiffness: number;
  damping: number;
}

function makeSpringVec3(x: number, y: number, z: number, stiffness: number, damping: number): SpringVec3 {
  return { x, y, z, vx: 0, vy: 0, vz: 0, tx: x, ty: y, tz: z, stiffness, damping };
}

function stepSpringVec3(s: SpringVec3, dt: number): void {
  const fx = -s.stiffness * (s.x - s.tx) - s.damping * s.vx;
  const fy = -s.stiffness * (s.y - s.ty) - s.damping * s.vy;
  const fz = -s.stiffness * (s.z - s.tz) - s.damping * s.vz;
  s.vx += fx * dt; s.vy += fy * dt; s.vz += fz * dt;
  s.x += s.vx * dt; s.y += s.vy * dt; s.z += s.vz * dt;
}

export class CameraController {
  camera: THREE.PerspectiveCamera;
  private targetPosition: THREE.Vector3;
  private targetLookAt: THREE.Vector3;
  private currentLookAt: THREE.Vector3;
  private config: CameraConfig;
  private followTarget: THREE.Object3D | null = null;
  private followOffset: THREE.Vector3;
  private isZoomedIn: boolean = false;
  private zoomTarget: THREE.Vector3 | null = null;

  // Spring physics for smooth follow
  private posSpring: SpringVec3;
  private lookSpring: SpringVec3;

  // Screen shake
  private shakeIntensity: number = 0;
  private shakeDuration: number = 0;
  private shakeElapsed: number = 0;
  private shakeDecay: number = 5;

  // Dolly zoom (room transitions)
  private isDollyZooming: boolean = false;
  private dollyStartFov: number = 45;
  private dollyEndFov: number = 45;
  private dollyProgress: number = 0;
  private dollyDuration: number = 0.8;

  constructor(aspect: number) {
    this.config = ROOM_CAMERAS.home;
    this.camera = new THREE.PerspectiveCamera(this.config.fov, aspect, 0.1, 50);
    this.targetPosition = new THREE.Vector3();
    this.targetLookAt = new THREE.Vector3(0, this.config.lookAtY, 0);
    this.currentLookAt = new THREE.Vector3(0, this.config.lookAtY, 0);
    this.followOffset = new THREE.Vector3(0, 0, 0);
    this.posSpring = makeSpringVec3(0, this.config.height, this.config.distance, 4, 3);
    this.lookSpring = makeSpringVec3(0, this.config.lookAtY, 0, 6, 4);
    this.applyConfig();
  }

  private applyConfig() {
    const { distance, height, angle } = this.config;
    this.targetPosition.set(
      Math.sin(angle) * distance,
      height,
      Math.cos(angle) * distance,
    );
    this.camera.position.copy(this.targetPosition);
    this.camera.lookAt(this.targetLookAt);
    this.camera.fov = this.config.fov;
    this.camera.updateProjectionMatrix();
    // Snap springs to position
    this.posSpring.x = this.targetPosition.x;
    this.posSpring.y = this.targetPosition.y;
    this.posSpring.z = this.targetPosition.z;
    this.posSpring.tx = this.targetPosition.x;
    this.posSpring.ty = this.targetPosition.y;
    this.posSpring.tz = this.targetPosition.z;
    this.lookSpring.x = this.targetLookAt.x;
    this.lookSpring.y = this.targetLookAt.y;
    this.lookSpring.z = this.targetLookAt.z;
    this.lookSpring.tx = this.targetLookAt.x;
    this.lookSpring.ty = this.targetLookAt.y;
    this.lookSpring.tz = this.targetLookAt.z;
  }

  setRoom(roomType: string) {
    const prevFov = this.config.fov;
    this.config = ROOM_CAMERAS[roomType] || ROOM_CAMERAS.home;
    this.isZoomedIn = false;
    this.zoomTarget = null;
    this.targetLookAt.set(0, this.config.lookAtY, 0);
    const { distance, height, angle } = this.config;
    this.targetPosition.set(
      Math.sin(angle) * distance,
      height,
      Math.cos(angle) * distance,
    );
    // Dolly zoom transition
    if (prevFov !== this.config.fov) {
      this.startDollyZoom(prevFov, this.config.fov, 0.8);
    } else {
      this.camera.fov = this.config.fov;
      this.camera.updateProjectionMatrix();
    }
  }

  setFollowTarget(target: THREE.Object3D | null) {
    this.followTarget = target;
  }

  zoomTo(target: THREE.Vector3, zoomFactor: number = 0.6) {
    this.isZoomedIn = true;
    this.zoomTarget = target.clone();
    const dir = new THREE.Vector3().subVectors(this.camera.position, target).normalize();
    this.targetPosition.copy(target).add(dir.multiplyScalar(this.config.distance * zoomFactor));
    this.targetPosition.y = target.y + this.config.height * 0.5;
    this.targetLookAt.copy(target);
    this.targetLookAt.y += 0.5;
  }

  zoomOut() {
    this.isZoomedIn = false;
    this.zoomTarget = null;
    this.setRoom(Object.keys(ROOM_CAMERAS).find(
      (k) => ROOM_CAMERAS[k] === this.config,
    ) || 'home');
  }

  /** Trigger screen shake (e.g. on big impacts, achievements) */
  shake(intensity: number = 0.1, duration: number = 0.3): void {
    this.shakeIntensity = intensity;
    this.shakeDuration = duration;
    this.shakeElapsed = 0;
  }

  /** Start a dolly zoom (fov transition while adjusting distance) */
  private startDollyZoom(fromFov: number, toFov: number, duration: number): void {
    this.isDollyZooming = true;
    this.dollyStartFov = fromFov;
    this.dollyEndFov = toFov;
    this.dollyProgress = 0;
    this.dollyDuration = duration;
  }

  update(dt: number) {
    // Follow target with spring physics
    if (this.followTarget && !this.isZoomedIn) {
      const targetX = this.followTarget.position.x;
      const targetZ = this.followTarget.position.z;
      const { distance, height, angle } = this.config;
      this.posSpring.tx = targetX * 0.3 + Math.sin(angle) * distance;
      this.posSpring.ty = height;
      this.posSpring.tz = targetZ * 0.3 + Math.cos(angle) * distance;
      this.lookSpring.tx = targetX * 0.5;
      this.lookSpring.ty = this.config.lookAtY;
      this.lookSpring.tz = targetZ * 0.5;
    } else {
      this.posSpring.tx = this.targetPosition.x;
      this.posSpring.ty = this.targetPosition.y;
      this.posSpring.tz = this.targetPosition.z;
      this.lookSpring.tx = this.targetLookAt.x;
      this.lookSpring.ty = this.targetLookAt.y;
      this.lookSpring.tz = this.targetLookAt.z;
    }

    // Step springs
    stepSpringVec3(this.posSpring, dt);
    stepSpringVec3(this.lookSpring, dt);

    this.camera.position.set(this.posSpring.x, this.posSpring.y, this.posSpring.z);
    this.currentLookAt.set(this.lookSpring.x, this.lookSpring.y, this.lookSpring.z);

    // Screen shake
    if (this.shakeElapsed < this.shakeDuration) {
      this.shakeElapsed += dt;
      const decay = Math.exp(-this.shakeDecay * this.shakeElapsed);
      const sx = (Math.random() - 0.5) * 2 * this.shakeIntensity * decay;
      const sy = (Math.random() - 0.5) * 2 * this.shakeIntensity * decay;
      this.camera.position.x += sx;
      this.camera.position.y += sy;
    }

    this.camera.lookAt(this.currentLookAt);

    // Dolly zoom
    if (this.isDollyZooming) {
      this.dollyProgress += dt / this.dollyDuration;
      if (this.dollyProgress >= 1) {
        this.dollyProgress = 1;
        this.isDollyZooming = false;
      }
      // Smooth ease-in-out
      const t = this.dollyProgress;
      const ease = t < 0.5 ? 2 * t * t : 1 - Math.pow(-2 * t + 2, 2) / 2;
      this.camera.fov = this.dollyStartFov + (this.dollyEndFov - this.dollyStartFov) * ease;
      this.camera.updateProjectionMatrix();
    }
  }

  // Convert screen tap to world position on floor plane
  screenToWorld(screenX: number, screenY: number, width: number, height: number): THREE.Vector3 | null {
    const ndc = new THREE.Vector2(
      (screenX / width) * 2 - 1,
      -(screenY / height) * 2 + 1,
    );
    const raycaster = new THREE.Raycaster();
    raycaster.setFromCamera(ndc, this.camera);
    const floorPlane = new THREE.Plane(new THREE.Vector3(0, 1, 0), 0);
    const intersection = new THREE.Vector3();
    if (raycaster.ray.intersectPlane(floorPlane, intersection)) {
      return intersection;
    }
    return null;
  }

  raycastObjects(
    screenX: number, screenY: number, width: number, height: number,
    objects: THREE.Object3D[],
  ): THREE.Intersection[] {
    const ndc = new THREE.Vector2(
      (screenX / width) * 2 - 1,
      -(screenY / height) * 2 + 1,
    );
    const raycaster = new THREE.Raycaster();
    raycaster.setFromCamera(ndc, this.camera);
    return raycaster.intersectObjects(objects, true);
  }

  updateAspect(aspect: number) {
    this.camera.aspect = aspect;
    this.camera.updateProjectionMatrix();
  }
}
