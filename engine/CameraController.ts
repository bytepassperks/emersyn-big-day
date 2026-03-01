/**
 * CameraController.ts - Camera positioning and smooth transitions
 * Isometric/3/4 view with follow, zoom, and pan
 */
import { THREE } from 'expo-three';

export interface CameraConfig {
  distance: number;
  height: number;
  angle: number; // Y rotation in radians
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

export class CameraController {
  camera: THREE.PerspectiveCamera;
  private targetPosition: THREE.Vector3;
  private targetLookAt: THREE.Vector3;
  private currentLookAt: THREE.Vector3;
  private config: CameraConfig;
  private followTarget: THREE.Object3D | null = null;
  private followOffset: THREE.Vector3;
  private lerpSpeed: number = 3.0;
  private isZoomedIn: boolean = false;
  private zoomTarget: THREE.Vector3 | null = null;
  private zoomProgress: number = 0;

  constructor(aspect: number) {
    this.config = ROOM_CAMERAS.home;
    this.camera = new THREE.PerspectiveCamera(this.config.fov, aspect, 0.1, 50);
    this.targetPosition = new THREE.Vector3();
    this.targetLookAt = new THREE.Vector3(0, this.config.lookAtY, 0);
    this.currentLookAt = new THREE.Vector3(0, this.config.lookAtY, 0);
    this.followOffset = new THREE.Vector3(0, 0, 0);
    this.applyConfig();
  }

  private applyConfig() {
    const { distance, height, angle } = this.config;
    this.targetPosition.set(
      Math.sin(angle) * distance,
      height,
      Math.cos(angle) * distance
    );
    this.camera.position.copy(this.targetPosition);
    this.camera.lookAt(this.targetLookAt);
    this.camera.fov = this.config.fov;
    this.camera.updateProjectionMatrix();
  }

  setRoom(roomType: string) {
    this.config = ROOM_CAMERAS[roomType] || ROOM_CAMERAS.home;
    this.isZoomedIn = false;
    this.zoomTarget = null;
    this.targetLookAt.set(0, this.config.lookAtY, 0);
    const { distance, height, angle } = this.config;
    this.targetPosition.set(
      Math.sin(angle) * distance,
      height,
      Math.cos(angle) * distance
    );
    this.camera.fov = this.config.fov;
    this.camera.updateProjectionMatrix();
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
      k => ROOM_CAMERAS[k] === this.config
    ) || 'home');
  }

  update(dt: number) {
    // Smooth follow character
    if (this.followTarget && !this.isZoomedIn) {
      const targetX = this.followTarget.position.x;
      const targetZ = this.followTarget.position.z;

      // Offset camera based on character position (subtle follow)
      const { distance, height, angle } = this.config;
      this.targetPosition.set(
        targetX * 0.3 + Math.sin(angle) * distance,
        height,
        targetZ * 0.3 + Math.cos(angle) * distance
      );
      this.targetLookAt.set(
        targetX * 0.5,
        this.config.lookAtY,
        targetZ * 0.5
      );
    }

    // Smooth lerp camera position
    const lerpFactor = 1 - Math.exp(-this.lerpSpeed * dt);
    this.camera.position.lerp(this.targetPosition, lerpFactor);
    this.currentLookAt.lerp(this.targetLookAt, lerpFactor);
    this.camera.lookAt(this.currentLookAt);
  }

  // Convert screen tap to world position on floor plane
  screenToWorld(screenX: number, screenY: number, width: number, height: number): THREE.Vector3 | null {
    const ndc = new THREE.Vector2(
      (screenX / width) * 2 - 1,
      -(screenY / height) * 2 + 1
    );

    const raycaster = new THREE.Raycaster();
    raycaster.setFromCamera(ndc, this.camera);

    // Intersect with floor plane (y=0)
    const floorPlane = new THREE.Plane(new THREE.Vector3(0, 1, 0), 0);
    const intersection = new THREE.Vector3();
    const ray = raycaster.ray;

    if (ray.intersectPlane(floorPlane, intersection)) {
      return intersection;
    }
    return null;
  }

  // Raycast to find clicked 3D objects
  raycastObjects(
    screenX: number,
    screenY: number,
    width: number,
    height: number,
    objects: THREE.Object3D[]
  ): THREE.Intersection[] {
    const ndc = new THREE.Vector2(
      (screenX / width) * 2 - 1,
      -(screenY / height) * 2 + 1
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
