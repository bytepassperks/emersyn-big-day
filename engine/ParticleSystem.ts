/**
 * ParticleSystem.ts - Particle effects for visual polish
 * Sparkles, hearts, bubbles, confetti, stars
 */
import { THREE } from 'expo-three';

export type ParticleType = 'sparkle' | 'heart' | 'bubble' | 'confetti' | 'star' | 'coin' | 'dust';

interface Particle {
  mesh: THREE.Mesh;
  velocity: THREE.Vector3;
  lifetime: number;
  maxLifetime: number;
  rotationSpeed: THREE.Vector3;
  type: ParticleType;
  scale: number;
}

const PARTICLE_COLORS: Record<ParticleType, number[]> = {
  sparkle: [0xffd93d, 0xffffff, 0xffe082, 0xffeb3b],
  heart: [0xff6b9d, 0xff4081, 0xf48fb1, 0xec407a],
  bubble: [0x87ceeb, 0xb3e5fc, 0x81d4fa, 0x4fc3f7],
  confetti: [0xff6b9d, 0x42a5f5, 0xffd93d, 0x66bb6a, 0xab47bc, 0xff9f43],
  star: [0xffd93d, 0xffeb3b, 0xfff176, 0xffee58],
  coin: [0xffd93d, 0xffc107, 0xffb300],
  dust: [0xf5deb3, 0xe8d4a0, 0xdeb887],
};

export class ParticleSystem {
  private particles: Particle[] = [];
  private scene: THREE.Scene;
  private maxParticles: number = 200;

  constructor(scene: THREE.Scene) {
    this.scene = scene;
  }

  emit(
    type: ParticleType,
    position: THREE.Vector3,
    count: number = 10,
    options?: {
      spread?: number;
      speed?: number;
      lifetime?: number;
      scale?: number;
      direction?: THREE.Vector3;
    }
  ) {
    const {
      spread = 1.0,
      speed = 2.0,
      lifetime = 1.5,
      scale = 0.05,
      direction,
    } = options || {};

    const colors = PARTICLE_COLORS[type];

    for (let i = 0; i < count; i++) {
      if (this.particles.length >= this.maxParticles) {
        // Remove oldest
        const old = this.particles.shift();
        if (old) {
          this.scene.remove(old.mesh);
          old.mesh.geometry.dispose();
          (old.mesh.material as THREE.Material).dispose();
        }
      }

      const color = colors[Math.floor(Math.random() * colors.length)];
      let geom: THREE.BufferGeometry;

      switch (type) {
        case 'heart':
          geom = new THREE.SphereGeometry(scale, 6, 6);
          break;
        case 'bubble':
          geom = new THREE.SphereGeometry(scale * (0.8 + Math.random() * 0.6), 8, 8);
          break;
        case 'confetti':
          geom = new THREE.PlaneGeometry(scale * 2, scale * 3);
          break;
        case 'star':
          geom = new THREE.OctahedronGeometry(scale, 0);
          break;
        case 'coin':
          geom = new THREE.CylinderGeometry(scale, scale, scale * 0.3, 8);
          break;
        default:
          geom = new THREE.SphereGeometry(scale, 6, 6);
      }

      const mat = new THREE.MeshBasicMaterial({
        color,
        transparent: true,
        opacity: 1.0,
        side: THREE.DoubleSide,
      });

      const mesh = new THREE.Mesh(geom, mat);
      mesh.position.copy(position);
      mesh.position.x += (Math.random() - 0.5) * spread * 0.3;
      mesh.position.y += (Math.random() - 0.5) * spread * 0.3;
      mesh.position.z += (Math.random() - 0.5) * spread * 0.3;

      let vel: THREE.Vector3;
      if (direction) {
        vel = direction.clone().normalize().multiplyScalar(speed);
        vel.x += (Math.random() - 0.5) * spread;
        vel.y += (Math.random() - 0.5) * spread;
        vel.z += (Math.random() - 0.5) * spread;
      } else {
        vel = new THREE.Vector3(
          (Math.random() - 0.5) * spread * speed,
          Math.random() * speed + speed * 0.5,
          (Math.random() - 0.5) * spread * speed
        );
      }

      this.scene.add(mesh);

      this.particles.push({
        mesh,
        velocity: vel,
        lifetime: 0,
        maxLifetime: lifetime + Math.random() * lifetime * 0.5,
        rotationSpeed: new THREE.Vector3(
          (Math.random() - 0.5) * 5,
          (Math.random() - 0.5) * 5,
          (Math.random() - 0.5) * 5
        ),
        type,
        scale,
      });
    }
  }

  emitCoinCollect(position: THREE.Vector3) {
    this.emit('coin', position, 8, { spread: 0.5, speed: 2.5, lifetime: 1.0, scale: 0.04 });
    this.emit('sparkle', position, 5, { spread: 0.8, speed: 1.5, lifetime: 0.8, scale: 0.03 });
  }

  emitHearts(position: THREE.Vector3) {
    this.emit('heart', position, 6, {
      spread: 0.5,
      speed: 1.5,
      lifetime: 2.0,
      scale: 0.04,
      direction: new THREE.Vector3(0, 1, 0),
    });
  }

  emitBubbles(position: THREE.Vector3) {
    this.emit('bubble', position, 8, {
      spread: 0.6,
      speed: 0.8,
      lifetime: 3.0,
      scale: 0.03,
      direction: new THREE.Vector3(0, 1, 0),
    });
  }

  emitConfetti(position: THREE.Vector3) {
    this.emit('confetti', position, 25, {
      spread: 2.0,
      speed: 3.0,
      lifetime: 3.0,
      scale: 0.04,
    });
  }

  emitStars(position: THREE.Vector3) {
    this.emit('star', position, 8, {
      spread: 1.0,
      speed: 2.0,
      lifetime: 1.5,
      scale: 0.05,
      direction: new THREE.Vector3(0, 1, 0),
    });
  }

  emitDust(position: THREE.Vector3) {
    this.emit('dust', position, 5, {
      spread: 0.4,
      speed: 0.5,
      lifetime: 1.0,
      scale: 0.02,
    });
  }

  update(dt: number) {
    const gravity = -3.0;
    const toRemove: number[] = [];

    for (let i = 0; i < this.particles.length; i++) {
      const p = this.particles[i];
      p.lifetime += dt;

      if (p.lifetime >= p.maxLifetime) {
        toRemove.push(i);
        continue;
      }

      // Move
      p.mesh.position.x += p.velocity.x * dt;
      p.mesh.position.y += p.velocity.y * dt;
      p.mesh.position.z += p.velocity.z * dt;

      // Gravity (except bubbles float up)
      if (p.type === 'bubble') {
        p.velocity.y += 0.5 * dt; // Float up
        p.velocity.x += Math.sin(p.lifetime * 3) * 0.3 * dt; // Wobble
      } else if (p.type === 'heart') {
        p.velocity.y += gravity * 0.3 * dt; // Slow fall
        p.velocity.x += Math.sin(p.lifetime * 2) * 0.5 * dt; // Sway
      } else {
        p.velocity.y += gravity * dt;
      }

      // Rotation
      p.mesh.rotation.x += p.rotationSpeed.x * dt;
      p.mesh.rotation.y += p.rotationSpeed.y * dt;
      p.mesh.rotation.z += p.rotationSpeed.z * dt;

      // Fade out
      const lifeRatio = p.lifetime / p.maxLifetime;
      const opacity = lifeRatio > 0.7 ? 1.0 - (lifeRatio - 0.7) / 0.3 : 1.0;
      (p.mesh.material as THREE.MeshBasicMaterial).opacity = Math.max(0, opacity);

      // Scale pulse for sparkles
      if (p.type === 'sparkle' || p.type === 'star') {
        const scalePulse = 1.0 + Math.sin(p.lifetime * 10) * 0.3;
        const scaleDown = lifeRatio > 0.5 ? 1.0 - (lifeRatio - 0.5) * 2 : 1.0;
        p.mesh.scale.setScalar(scalePulse * scaleDown);
      }
    }

    // Remove dead particles (reverse order)
    for (let i = toRemove.length - 1; i >= 0; i--) {
      const idx = toRemove[i];
      const p = this.particles[idx];
      this.scene.remove(p.mesh);
      p.mesh.geometry.dispose();
      (p.mesh.material as THREE.Material).dispose();
      this.particles.splice(idx, 1);
    }
  }

  clear() {
    for (const p of this.particles) {
      this.scene.remove(p.mesh);
      p.mesh.geometry.dispose();
      (p.mesh.material as THREE.Material).dispose();
    }
    this.particles = [];
  }

  get activeCount(): number {
    return this.particles.length;
  }
}
