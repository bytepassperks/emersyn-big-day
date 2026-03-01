import React, { useRef, useEffect, useCallback } from 'react';
import { View, StyleSheet, Platform } from 'react-native';
import { ExpoWebGLRenderingContext, GLView } from 'expo-gl';
import { Renderer, THREE } from 'expo-three';

interface Scene3DProps {
  width?: number;
  height?: number;
  sceneType: 'bedroom' | 'kitchen' | 'park' | 'school' | 'arcade' | 'studio' | 'shop' | 'bathroom' | 'home';
  characterVisible?: boolean;
  characterAnimation?: 'idle' | 'happy' | 'eating' | 'sleeping' | 'dancing' | 'karate';
  style?: any;
}

// Pastel color palette for 3D scenes
const SCENE_COLORS: Record<string, { floor: number; wall: number; accent: number; sky: number }> = {
  bedroom: { floor: 0xd4eeff, wall: 0xffe4f0, accent: 0xff6b9d, sky: 0xffe4f0 },
  kitchen: { floor: 0xffe0b2, wall: 0xfff5cc, accent: 0xff9f43, sky: 0xfff5cc },
  park: { floor: 0x7ecf6a, wall: 0xd4f5d4, accent: 0x6bcb77, sky: 0x87ceeb },
  school: { floor: 0xe8d5ff, wall: 0xf0e6ff, accent: 0xb06aff, sky: 0xe8d5ff },
  arcade: { floor: 0xe8d5ff, wall: 0xffd4d4, accent: 0xff6b9d, sky: 0xe8d5ff },
  studio: { floor: 0xffd4d4, wall: 0xffe4f0, accent: 0xff6b6b, sky: 0xffd4d4 },
  shop: { floor: 0xfff5cc, wall: 0xffe4f0, accent: 0xffd93d, sky: 0xfff5cc },
  bathroom: { floor: 0xd4eeff, wall: 0xe8f5ff, accent: 0x6cbfff, sky: 0xd4eeff },
  home: { floor: 0xffe4f0, wall: 0xfff0f5, accent: 0xff6b9d, sky: 0xffe4f0 },
};

const createCuteCharacter = (scene: THREE.Scene, animation: string) => {
  const group = new THREE.Group();

  // Body - rounded cute proportions
  const bodyGeom = new THREE.SphereGeometry(0.35, 16, 16);
  bodyGeom.scale(1, 1.2, 0.8);
  const bodyMat = new THREE.MeshPhongMaterial({ color: 0xffd4b8, shininess: 60 });
  const body = new THREE.Mesh(bodyGeom, bodyMat);
  body.position.y = 0.5;
  group.add(body);

  // Head - big cute head
  const headGeom = new THREE.SphereGeometry(0.3, 16, 16);
  const headMat = new THREE.MeshPhongMaterial({ color: 0xffd4b8, shininess: 60 });
  const head = new THREE.Mesh(headGeom, headMat);
  head.position.y = 1.1;
  group.add(head);

  // Hair - pink cute hair
  const hairGeom = new THREE.SphereGeometry(0.32, 16, 16);
  hairGeom.scale(1.1, 1, 1.1);
  const hairMat = new THREE.MeshPhongMaterial({ color: 0x8b4513, shininess: 40 });
  const hair = new THREE.Mesh(hairGeom, hairMat);
  hair.position.y = 1.2;
  group.add(hair);

  // Hair bow - pink ribbon
  const bowGeom = new THREE.SphereGeometry(0.08, 8, 8);
  bowGeom.scale(2, 1, 1);
  const bowMat = new THREE.MeshPhongMaterial({ color: 0xff6b9d, shininess: 80 });
  const bow = new THREE.Mesh(bowGeom, bowMat);
  bow.position.set(0.25, 1.35, 0.1);
  group.add(bow);

  // Eyes - big cute eyes
  const eyeGeom = new THREE.SphereGeometry(0.05, 8, 8);
  const eyeMat = new THREE.MeshPhongMaterial({ color: 0x2d1f2d });
  const leftEye = new THREE.Mesh(eyeGeom, eyeMat);
  leftEye.position.set(-0.1, 1.12, 0.27);
  group.add(leftEye);
  const rightEye = new THREE.Mesh(eyeGeom, eyeMat);
  rightEye.position.set(0.1, 1.12, 0.27);
  group.add(rightEye);

  // Eye highlights
  const highlightGeom = new THREE.SphereGeometry(0.02, 6, 6);
  const highlightMat = new THREE.MeshBasicMaterial({ color: 0xffffff });
  const leftHighlight = new THREE.Mesh(highlightGeom, highlightMat);
  leftHighlight.position.set(-0.08, 1.14, 0.3);
  group.add(leftHighlight);
  const rightHighlight = new THREE.Mesh(highlightGeom, highlightMat);
  rightHighlight.position.set(0.12, 1.14, 0.3);
  group.add(rightHighlight);

  // Smile
  const smileGeom = new THREE.TorusGeometry(0.06, 0.015, 8, 16, Math.PI);
  const smileMat = new THREE.MeshPhongMaterial({ color: 0xff6b9d });
  const smile = new THREE.Mesh(smileGeom, smileMat);
  smile.position.set(0, 1.02, 0.27);
  smile.rotation.x = Math.PI;
  group.add(smile);

  // Dress - pink cute dress
  const dressGeom = new THREE.ConeGeometry(0.4, 0.6, 16);
  const dressMat = new THREE.MeshPhongMaterial({ color: 0xff6b9d, shininess: 60 });
  const dress = new THREE.Mesh(dressGeom, dressMat);
  dress.position.y = 0.2;
  group.add(dress);

  // Legs
  const legGeom = new THREE.CylinderGeometry(0.06, 0.06, 0.3, 8);
  const legMat = new THREE.MeshPhongMaterial({ color: 0xffd4b8 });
  const leftLeg = new THREE.Mesh(legGeom, legMat);
  leftLeg.position.set(-0.12, -0.1, 0);
  group.add(leftLeg);
  const rightLeg = new THREE.Mesh(legGeom, legMat);
  rightLeg.position.set(0.12, -0.1, 0);
  group.add(rightLeg);

  // Shoes - cute pink shoes
  const shoeGeom = new THREE.SphereGeometry(0.08, 8, 8);
  shoeGeom.scale(1.3, 0.6, 1.5);
  const shoeMat = new THREE.MeshPhongMaterial({ color: 0xff6b9d, shininess: 80 });
  const leftShoe = new THREE.Mesh(shoeGeom, shoeMat);
  leftShoe.position.set(-0.12, -0.28, 0.03);
  group.add(leftShoe);
  const rightShoe = new THREE.Mesh(shoeGeom, shoeMat);
  rightShoe.position.set(0.12, -0.28, 0.03);
  group.add(rightShoe);

  // Animation state
  group.userData = { animation, time: 0 };

  scene.add(group);
  return group;
};

const createRoomFurniture = (scene: THREE.Scene, sceneType: string, colors: typeof SCENE_COLORS['bedroom']) => {
  // Floor
  const floorGeom = new THREE.PlaneGeometry(6, 6);
  const floorMat = new THREE.MeshPhongMaterial({ color: colors.floor, side: THREE.DoubleSide });
  const floor = new THREE.Mesh(floorGeom, floorMat);
  floor.rotation.x = -Math.PI / 2;
  floor.position.y = -0.3;
  scene.add(floor);

  // Back wall
  const wallGeom = new THREE.PlaneGeometry(6, 4);
  const wallMat = new THREE.MeshPhongMaterial({ color: colors.wall, side: THREE.DoubleSide });
  const wall = new THREE.Mesh(wallGeom, wallMat);
  wall.position.set(0, 1.7, -3);
  scene.add(wall);

  switch (sceneType) {
    case 'bedroom':
      addBedroomFurniture(scene, colors);
      break;
    case 'kitchen':
      addKitchenFurniture(scene, colors);
      break;
    case 'park':
      addParkElements(scene, colors);
      break;
    case 'school':
      addSchoolFurniture(scene, colors);
      break;
    case 'arcade':
      addArcadeFurniture(scene, colors);
      break;
    default:
      addDefaultFurniture(scene, colors);
      break;
  }
};

const addBedroomFurniture = (scene: THREE.Scene, colors: typeof SCENE_COLORS['bedroom']) => {
  // Bed
  const bedBaseGeom = new THREE.BoxGeometry(1.2, 0.3, 0.8);
  const bedMat = new THREE.MeshPhongMaterial({ color: 0xffe4f0 });
  const bed = new THREE.Mesh(bedBaseGeom, bedMat);
  bed.position.set(-1.2, 0, -1.5);
  scene.add(bed);

  // Blanket
  const blanketGeom = new THREE.BoxGeometry(1.1, 0.08, 0.6);
  const blanketMat = new THREE.MeshPhongMaterial({ color: 0xff6b9d });
  const blanket = new THREE.Mesh(blanketGeom, blanketMat);
  blanket.position.set(-1.2, 0.18, -1.45);
  scene.add(blanket);

  // Pillow
  const pillowGeom = new THREE.SphereGeometry(0.15, 8, 8);
  pillowGeom.scale(1.5, 0.5, 1);
  const pillowMat = new THREE.MeshPhongMaterial({ color: 0xffffff });
  const pillow = new THREE.Mesh(pillowGeom, pillowMat);
  pillow.position.set(-1.2, 0.22, -1.8);
  scene.add(pillow);

  // Nightstand
  const standGeom = new THREE.BoxGeometry(0.4, 0.5, 0.4);
  const standMat = new THREE.MeshPhongMaterial({ color: 0xdeb887 });
  const stand = new THREE.Mesh(standGeom, standMat);
  stand.position.set(-0.3, 0, -1.5);
  scene.add(stand);

  // Lamp on nightstand
  const lampBaseGeom = new THREE.CylinderGeometry(0.06, 0.08, 0.15, 8);
  const lampBaseMat = new THREE.MeshPhongMaterial({ color: colors.accent });
  const lampBase = new THREE.Mesh(lampBaseGeom, lampBaseMat);
  lampBase.position.set(-0.3, 0.33, -1.5);
  scene.add(lampBase);

  // Lamp shade
  const shadeGeom = new THREE.ConeGeometry(0.12, 0.15, 8);
  const shadeMat = new THREE.MeshPhongMaterial({ color: 0xfff5cc, emissive: 0xffd93d, emissiveIntensity: 0.3 });
  const shade = new THREE.Mesh(shadeGeom, shadeMat);
  shade.position.set(-0.3, 0.45, -1.5);
  shade.rotation.x = Math.PI;
  scene.add(shade);

  // Window
  const windowGeom = new THREE.PlaneGeometry(0.8, 0.8);
  const windowMat = new THREE.MeshPhongMaterial({ color: 0x87ceeb, emissive: 0x87ceeb, emissiveIntensity: 0.2 });
  const window_ = new THREE.Mesh(windowGeom, windowMat);
  window_.position.set(1.2, 1.5, -2.98);
  scene.add(window_);

  // Rug
  const rugGeom = new THREE.CircleGeometry(0.6, 16);
  const rugMat = new THREE.MeshPhongMaterial({ color: 0xb06aff, side: THREE.DoubleSide });
  const rug = new THREE.Mesh(rugGeom, rugMat);
  rug.rotation.x = -Math.PI / 2;
  rug.position.set(0, -0.28, 0);
  scene.add(rug);
};

const addKitchenFurniture = (scene: THREE.Scene, colors: typeof SCENE_COLORS['bedroom']) => {
  // Counter
  const counterGeom = new THREE.BoxGeometry(2.5, 0.8, 0.6);
  const counterMat = new THREE.MeshPhongMaterial({ color: 0xf5f5dc });
  const counter = new THREE.Mesh(counterGeom, counterMat);
  counter.position.set(0, 0.1, -2.2);
  scene.add(counter);

  // Stove
  const stoveGeom = new THREE.BoxGeometry(0.8, 0.1, 0.5);
  const stoveMat = new THREE.MeshPhongMaterial({ color: 0x333333 });
  const stove = new THREE.Mesh(stoveGeom, stoveMat);
  stove.position.set(-0.5, 0.55, -2.2);
  scene.add(stove);

  // Pot on stove
  const potGeom = new THREE.CylinderGeometry(0.12, 0.1, 0.15, 8);
  const potMat = new THREE.MeshPhongMaterial({ color: 0xc0c0c0, shininess: 80 });
  const pot = new THREE.Mesh(potGeom, potMat);
  pot.position.set(-0.5, 0.67, -2.2);
  scene.add(pot);

  // Fridge
  const fridgeGeom = new THREE.BoxGeometry(0.6, 1.4, 0.5);
  const fridgeMat = new THREE.MeshPhongMaterial({ color: 0xf0f0f0, shininess: 60 });
  const fridge = new THREE.Mesh(fridgeGeom, fridgeMat);
  fridge.position.set(1.8, 0.4, -2.2);
  scene.add(fridge);

  // Kitchen table
  const tableTopGeom = new THREE.BoxGeometry(1.2, 0.06, 0.8);
  const tableMat = new THREE.MeshPhongMaterial({ color: 0xdeb887 });
  const tableTop = new THREE.Mesh(tableTopGeom, tableMat);
  tableTop.position.set(0, 0.3, -0.5);
  scene.add(tableTop);
};

const addParkElements = (scene: THREE.Scene, colors: typeof SCENE_COLORS['bedroom']) => {
  // Remove back wall for outdoor scene
  // Trees
  for (let i = 0; i < 5; i++) {
    const trunkGeom = new THREE.CylinderGeometry(0.08, 0.1, 0.8, 8);
    const trunkMat = new THREE.MeshPhongMaterial({ color: 0x8b4513 });
    const trunk = new THREE.Mesh(trunkGeom, trunkMat);
    trunk.position.set(-2.5 + i * 1.2, 0.1, -2.5 + Math.random() * 0.5);
    scene.add(trunk);

    const foliageGeom = new THREE.SphereGeometry(0.4, 8, 8);
    const foliageMat = new THREE.MeshPhongMaterial({ color: 0x2e8b57 + Math.floor(Math.random() * 0x002000) });
    const foliage = new THREE.Mesh(foliageGeom, foliageMat);
    foliage.position.set(-2.5 + i * 1.2, 0.7, -2.5 + Math.random() * 0.5);
    scene.add(foliage);
  }

  // Flowers
  for (let i = 0; i < 8; i++) {
    const flowerGeom = new THREE.SphereGeometry(0.06, 6, 6);
    const flowerColors = [0xff6b9d, 0xffd93d, 0xb06aff, 0xff9f43, 0x6cbfff];
    const flowerMat = new THREE.MeshPhongMaterial({ color: flowerColors[i % flowerColors.length] });
    const flower = new THREE.Mesh(flowerGeom, flowerMat);
    flower.position.set(-2 + Math.random() * 4, -0.2, -1 + Math.random() * 2);
    scene.add(flower);
  }

  // Swing
  const swingPoleGeom = new THREE.CylinderGeometry(0.04, 0.04, 1.5, 6);
  const poleMat = new THREE.MeshPhongMaterial({ color: 0xc0c0c0 });
  const leftPole = new THREE.Mesh(swingPoleGeom, poleMat);
  leftPole.position.set(1.5, 0.45, -1);
  scene.add(leftPole);
  const rightPole = new THREE.Mesh(swingPoleGeom, poleMat);
  rightPole.position.set(2.1, 0.45, -1);
  scene.add(rightPole);

  // Sun
  const sunGeom = new THREE.SphereGeometry(0.3, 12, 12);
  const sunMat = new THREE.MeshBasicMaterial({ color: 0xffd93d });
  const sun = new THREE.Mesh(sunGeom, sunMat);
  sun.position.set(2, 3, -4);
  scene.add(sun);

  // Clouds
  for (let i = 0; i < 3; i++) {
    const cloudGeom = new THREE.SphereGeometry(0.25, 8, 8);
    cloudGeom.scale(2, 0.6, 1);
    const cloudMat = new THREE.MeshPhongMaterial({ color: 0xffffff, opacity: 0.8, transparent: true });
    const cloud = new THREE.Mesh(cloudGeom, cloudMat);
    cloud.position.set(-2 + i * 2, 2.5 + Math.random() * 0.5, -3);
    scene.add(cloud);
  }
};

const addSchoolFurniture = (scene: THREE.Scene, _colors: typeof SCENE_COLORS['bedroom']) => {
  // Desk
  const deskGeom = new THREE.BoxGeometry(1.2, 0.06, 0.6);
  const deskMat = new THREE.MeshPhongMaterial({ color: 0xdeb887 });
  const desk = new THREE.Mesh(deskGeom, deskMat);
  desk.position.set(0, 0.35, -1);
  scene.add(desk);

  // Chair
  const seatGeom = new THREE.BoxGeometry(0.4, 0.06, 0.35);
  const chairMat = new THREE.MeshPhongMaterial({ color: 0xb06aff });
  const seat = new THREE.Mesh(seatGeom, chairMat);
  seat.position.set(0, 0.2, -0.3);
  scene.add(seat);

  // Blackboard
  const boardGeom = new THREE.PlaneGeometry(2, 1);
  const boardMat = new THREE.MeshPhongMaterial({ color: 0x2e4e2e });
  const board = new THREE.Mesh(boardGeom, boardMat);
  board.position.set(0, 1.5, -2.95);
  scene.add(board);

  // Books on desk
  for (let i = 0; i < 3; i++) {
    const bookGeom = new THREE.BoxGeometry(0.15, 0.04, 0.2);
    const bookColors = [0xff6b9d, 0x6cbfff, 0xffd93d];
    const bookMat = new THREE.MeshPhongMaterial({ color: bookColors[i] });
    const book = new THREE.Mesh(bookGeom, bookMat);
    book.position.set(-0.3 + i * 0.2, 0.4, -1);
    scene.add(book);
  }
};

const addArcadeFurniture = (scene: THREE.Scene, _colors: typeof SCENE_COLORS['bedroom']) => {
  // Arcade machines
  for (let i = 0; i < 3; i++) {
    const machineGeom = new THREE.BoxGeometry(0.6, 1.2, 0.4);
    const machineColors = [0xff6b9d, 0xb06aff, 0x6cbfff];
    const machineMat = new THREE.MeshPhongMaterial({ color: machineColors[i] });
    const machine = new THREE.Mesh(machineGeom, machineMat);
    machine.position.set(-1.5 + i * 1.5, 0.3, -2);
    scene.add(machine);

    // Screen glow
    const screenGeom = new THREE.PlaneGeometry(0.4, 0.3);
    const screenMat = new THREE.MeshBasicMaterial({ color: 0x00ff88, emissive: 0x00ff88 });
    const screen = new THREE.Mesh(screenGeom, screenMat);
    screen.position.set(-1.5 + i * 1.5, 0.6, -1.79);
    scene.add(screen);
  }

  // Disco ball
  const ballGeom = new THREE.SphereGeometry(0.15, 12, 12);
  const ballMat = new THREE.MeshPhongMaterial({ color: 0xc0c0c0, shininess: 100, specular: 0xffffff });
  const ball = new THREE.Mesh(ballGeom, ballMat);
  ball.position.set(0, 3, -1.5);
  scene.add(ball);
};

const addDefaultFurniture = (scene: THREE.Scene, colors: typeof SCENE_COLORS['bedroom']) => {
  // Simple cozy room with a rug and some furniture
  const rugGeom = new THREE.CircleGeometry(0.8, 16);
  const rugMat = new THREE.MeshPhongMaterial({ color: colors.accent, side: THREE.DoubleSide });
  const rug = new THREE.Mesh(rugGeom, rugMat);
  rug.rotation.x = -Math.PI / 2;
  rug.position.set(0, -0.28, 0);
  scene.add(rug);
};

const animateCharacter = (character: THREE.Group, time: number) => {
  const anim = character.userData.animation || 'idle';

  switch (anim) {
    case 'idle':
      character.position.y = Math.sin(time * 2) * 0.03;
      character.rotation.y = Math.sin(time * 0.5) * 0.1;
      break;
    case 'happy':
      character.position.y = Math.abs(Math.sin(time * 4)) * 0.15;
      character.rotation.y = Math.sin(time * 2) * 0.3;
      break;
    case 'dancing':
      character.position.y = Math.abs(Math.sin(time * 6)) * 0.2;
      character.rotation.y = time * 2;
      break;
    case 'eating':
      character.position.y = Math.sin(time * 3) * 0.02;
      character.rotation.x = Math.sin(time * 4) * 0.05;
      break;
    case 'sleeping':
      character.position.y = Math.sin(time * 1) * 0.02;
      character.rotation.z = 0.3;
      break;
    case 'karate':
      character.position.y = Math.abs(Math.sin(time * 5)) * 0.2;
      character.rotation.y = Math.sin(time * 3) * 0.5;
      break;
  }
};

export const Scene3D: React.FC<Scene3DProps> = ({
  width,
  height = 280,
  sceneType = 'home',
  characterVisible = true,
  characterAnimation = 'idle',
  style,
}) => {
  const onContextCreate = useCallback(async (gl: ExpoWebGLRenderingContext) => {
    const renderer = new Renderer({ gl }) as any;
    renderer.setSize(gl.drawingBufferWidth, gl.drawingBufferHeight);
    renderer.setClearColor(SCENE_COLORS[sceneType]?.sky ?? 0xffe4f0);

    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(45, gl.drawingBufferWidth / gl.drawingBufferHeight, 0.1, 100);
    camera.position.set(0, 1.5, 3.5);
    camera.lookAt(0, 0.5, 0);

    // Lighting - warm and soft
    const ambientLight = new THREE.AmbientLight(0xfff0e6, 0.6);
    scene.add(ambientLight);

    const mainLight = new THREE.DirectionalLight(0xffffff, 0.8);
    mainLight.position.set(3, 5, 3);
    mainLight.castShadow = true;
    scene.add(mainLight);

    const fillLight = new THREE.DirectionalLight(0xffe4f0, 0.3);
    fillLight.position.set(-2, 2, -1);
    scene.add(fillLight);

    // Build scene
    const colors = SCENE_COLORS[sceneType] ?? SCENE_COLORS.home;
    createRoomFurniture(scene, sceneType, colors);

    let character: THREE.Group | null = null;
    if (characterVisible) {
      character = createCuteCharacter(scene, characterAnimation);
    }

    // Animation loop
    let animationTime = 0;
    const animate = () => {
      requestAnimationFrame(animate);
      animationTime += 0.016;

      if (character) {
        animateCharacter(character, animationTime);
      }

      renderer.render(scene, camera);
      gl.endFrameEXP();
    };

    animate();
  }, [sceneType, characterVisible, characterAnimation]);

  return (
    <View style={[styles.container, { height }, style]}>
      <GLView
        style={styles.glView}
        onContextCreate={onContextCreate}
      />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    borderRadius: 20,
    overflow: 'hidden',
    marginHorizontal: 16,
    marginVertical: 8,
    backgroundColor: '#FFE4F0',
    shadowColor: '#2D1F2D',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.15,
    shadowRadius: 8,
    elevation: 6,
  },
  glView: {
    flex: 1,
  },
});

export default Scene3D;
