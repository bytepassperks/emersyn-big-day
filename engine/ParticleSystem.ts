/**
 * ParticleSystem.ts - Enhanced particle effects with 10+ types
 * Includes sparkles, hearts, bubbles, confetti, stars, coins, dust,
 * steam wisps, zzz letters, musical notes, and more.
 */
import { THREE } from 'expo-three';

export type ParticleType =
  | 'sparkle' | 'heart' | 'bubble' | 'confetti' | 'star'
  | 'coin' | 'dust' | 'steam' | 'zzz' | 'note'
  | 'splash' | 'fire' | 'snow' | 'leaf' | 'petal';

interface Particle {
  mesh: THREE.Mesh;
  velocity: THREE.Vector3;
  lifetime: number;
  maxLifetime: number;
  rotationSpeed: THREE.Vector3;
  type: ParticleType;
  scale: number;
  wobblePhase: number;
}

const PARTICLE_COLORS: Record<ParticleType, number[]> = {
  sparkle: [0xffd93d, 0xffffff, 0xffe082, 0xffeb3b],
  heart: [0xff6b9d, 0xff4081, 0xf48fb1, 0xec407a],
  bubble: [0x87ceeb, 0xb3e5fc, 0x81d4fa, 0x4fc3f7],
  confetti: [0xff6b9d, 0x42a5f5, 0xffd93d, 0x66bb6a, 0xab47bc, 0xff9f43],
  star: [0xffd93d, 0xffeb3b, 0xfff176, 0xffee58],
  coin: [0xffd93d, 0xffc107, 0xffb300],
  dust: [0xf5deb3, 0xe8d4a0, 0xdeb887],
  steam: [0xeeeeee, 0xdddddd, 0xcccccc, 0xffffff],
  zzz: [0x9c5bff, 0xb388ff, 0x7c4dff, 0xd1c4e9],
  note: [0xff6b9d, 0x42a5f5, 0xffd93d, 0x66bb6a],
  splash: [0x4fc3f7, 0x81d4fa, 0xb3e5fc, 0x03a9f4],
  fire: [0xff6b00, 0xff9800, 0xffcc00, 0xff5722],
  snow: [0xffffff, 0xeceff1, 0xe3f2fd, 0xf5f5f5],
  leaf: [0x4caf50, 0x66bb6a, 0x81c784, 0xa5d6a7],
  petal: [0xf48fb1, 0xf8bbd0, 0xfce4ec, 0xff80ab],
};

export class ParticleSystem {
  private particles: Particle[] = [];
  private scene: THREE.Scene;
  private maxParticles: number = 300;

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
        case 'note':
          geom = new THREE.OctahedronGeometry(scale, 0);
          break;
        case 'coin':
          geom = new THREE.CylinderGeometry(scale, scale, scale * 0.3, 8);
          break;
        case 'steam':
          geom = new THREE.SphereGeometry(scale * (1 + Math.random() * 0.5), 6, 6);
          break;
        case 'zzz':
          geom = new THREE.PlaneGeometry(scale * 2.5, scale * 2.5);
          break;
        case 'splash':
          geom = new THREE.SphereGeometry(scale * 0.6, 4, 4);
          break;
        case 'fire':
          geom = new THREE.ConeGeometry(scale * 0.8, scale * 2, 5);
          break;
        case 'snow':
          geom = new THREE.SphereGeometry(scale * 0.5, 4, 4);
          break;
        case 'leaf':
        case 'petal':
          geom = new THREE.PlaneGeometry(scale * 1.5, scale * 2);
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
          (Math.random() - 0.5) * spread * speed,
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
          (Math.random() - 0.5) * 5,
        ),
        type,
        scale,
        wobblePhase: Math.random() * Math.PI * 2,
      });
    }
  }

  // Convenience methods

  emitCoinCollect(position: THREE.Vector3) {
    this.emit('coin', position, 8, { spread: 0.5, speed: 2.5, lifetime: 1.0, scale: 0.04 });
    this.emit('sparkle', position, 5, { spread: 0.8, speed: 1.5, lifetime: 0.8, scale: 0.03 });
  }

  emitHearts(position: THREE.Vector3) {
    this.emit('heart', position, 6, {
      spread: 0.5, speed: 1.5, lifetime: 2.0, scale: 0.04,
      direction: new THREE.Vector3(0, 1, 0),
    });
  }

  emitBubbles(position: THREE.Vector3) {
    this.emit('bubble', position, 8, {
      spread: 0.6, speed: 0.8, lifetime: 3.0, scale: 0.03,
      direction: new THREE.Vector3(0, 1, 0),
    });
  }

  emitConfetti(position: THREE.Vector3) {
    this.emit('confetti', position, 25, { spread: 2.0, speed: 3.0, lifetime: 3.0, scale: 0.04 });
  }

  emitStars(position: THREE.Vector3) {
    this.emit('star', position, 8, {
      spread: 1.0, speed: 2.0, lifetime: 1.5, scale: 0.05,
      direction: new THREE.Vector3(0, 1, 0),
    });
  }

  emitDust(position: THREE.Vector3) {
    this.emit('dust', position, 5, { spread: 0.4, speed: 0.5, lifetime: 1.0, scale: 0.02 });
  }

  emitSteam(position: THREE.Vector3) {
    this.emit('steam', position, 6, {
      spread: 0.3, speed: 0.4, lifetime: 2.5, scale: 0.04,
      direction: new THREE.Vector3(0, 1, 0),
    });
  }

  emitZzz(position: THREE.Vector3) {
    this.emit('zzz', position, 3, {
      spread: 0.3, speed: 0.3, lifetime: 3.0, scale: 0.06,
      direction: new THREE.Vector3(0.2, 1, 0),
    });
  }

  emitMusicalNotes(position: THREE.Vector3) {
    this.emit('note', position, 5, {
      spread: 0.6, speed: 1.2, lifetime: 2.0, scale: 0.04,
      direction: new THREE.Vector3(0, 1, 0),
    });
  }

  emitSplash(position: THREE.Vector3) {
    this.emit('splash', position, 12, { spread: 1.2, speed: 3.0, lifetime: 0.8, scale: 0.03 });
  }

  emitFire(position: THREE.Vector3) {
    this.emit('fire', position, 8, {
      spread: 0.3, speed: 1.5, lifetime: 1.0, scale: 0.04,
      direction: new THREE.Vector3(0, 1, 0),
    });
  }

  emitSnow(position: THREE.Vector3, area: number = 4) {
    for (let i = 0; i < 5; i++) {
      const p = position.clone();
      p.x += (Math.random() - 0.5) * area;
      p.z += (Math.random() - 0.5) * area;
      p.y += 3 + Math.random();
      this.emit('snow', p, 1, {
        spread: 0.1, speed: 0.3, lifetime: 5.0, scale: 0.02,
        direction: new THREE.Vector3(0, -1, 0),
      });
    }
  }

  emitLeaves(position: THREE.Vector3) {
    this.emit('leaf', position, 4, {
      spread: 1.5, speed: 0.5, lifetime: 4.0, scale: 0.03,
      direction: new THREE.Vector3(0.5, -0.3, 0.2),
    });
  }

  emitPetals(position: THREE.Vector3) {
    this.emit('petal', position, 6, {
      spread: 1.0, speed: 0.4, lifetime: 4.0, scale: 0.03,
      direction: new THREE.Vector3(0.3, -0.2, 0.3),
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

      // Type-specific physics
      switch (p.type) {
        case 'bubble':
          p.velocity.y += 0.5 * dt;
          p.velocity.x += Math.sin(p.lifetime * 3 + p.wobblePhase) * 0.3 * dt;
          break;
        case 'heart':
          p.velocity.y += gravity * 0.3 * dt;
          p.velocity.x += Math.sin(p.lifetime * 2 + p.wobblePhase) * 0.5 * dt;
          break;
        case 'steam':
          p.velocity.y += 0.2 * dt;
          p.velocity.x += Math.sin(p.lifetime * 1.5 + p.wobblePhase) * 0.4 * dt;
          p.mesh.scale.setScalar(1 + p.lifetime * 0.5);
          break;
        case 'zzz':
          p.velocity.y += 0.1 * dt;
          p.velocity.x += Math.sin(p.lifetime * 0.8 + p.wobblePhase) * 0.3 * dt;
          p.mesh.scale.setScalar(0.5 + p.lifetime * 0.3);
          break;
        case 'note':
          p.velocity.y += gravity * 0.15 * dt;
          p.velocity.x += Math.sin(p.lifetime * 4 + p.wobblePhase) * 0.6 * dt;
          break;
        case 'snow':
          p.velocity.x += Math.sin(p.lifetime * 0.5 + p.wobblePhase) * 0.2 * dt;
          p.velocity.z += Math.cos(p.lifetime * 0.3 + p.wobblePhase) * 0.2 * dt;
          break;
        case 'leaf':
        case 'petal':
          p.velocity.y += gravity * 0.1 * dt;
          p.velocity.x += Math.sin(p.lifetime * 1.2 + p.wobblePhase) * 0.4 * dt;
          p.velocity.z += Math.cos(p.lifetime * 0.8 + p.wobblePhase) * 0.3 * dt;
          break;
        case 'fire':
          p.velocity.y += 0.8 * dt;
          p.velocity.x += Math.sin(p.lifetime * 5 + p.wobblePhase) * 0.5 * dt;
          break;
        default:
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

      // Scale pulse for sparkles/stars
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
