/**
 * RoomBuilder.ts - Creates detailed 3D rooms with interactive furniture
 * Each room has unique furniture, lighting, and interactive objects
 */
import { THREE } from 'expo-three';

export interface InteractableInfo {
  id: string;
  label: string;
  position: THREE.Vector3;
  walkToOffset: THREE.Vector3; // Where character walks to interact
  animOnInteract: string;
  category: string;
}

export interface RoomData {
  interactables: InteractableInfo[];
  ambientColor: number;
  floorColor: number;
  wallColor: number;
}

type RoomType = 'bedroom' | 'kitchen' | 'park' | 'school' | 'arcade' | 'studio' | 'shop' | 'bathroom' | 'home';

// Pastel palette
const PALETTES: Record<RoomType, { floor: number; wall: number; accent: number; sky: number; ambient: number }> = {
  bedroom: { floor: 0xf5e6f0, wall: 0xffe4f0, accent: 0xff6b9d, sky: 0xffd4e8, ambient: 0xffe4f0 },
  kitchen: { floor: 0xfff0d4, wall: 0xfff8e8, accent: 0xff9f43, sky: 0xfff5cc, ambient: 0xfff0d4 },
  park: { floor: 0x7ecf6a, wall: 0x87ceeb, accent: 0x4caf50, sky: 0x87ceeb, ambient: 0xc8e6c9 },
  school: { floor: 0xf0e6ff, wall: 0xf5f0ff, accent: 0x9c5bff, sky: 0xe8d5ff, ambient: 0xf0e6ff },
  arcade: { floor: 0x2d1f4e, wall: 0x3d2a6e, accent: 0xff6b9d, sky: 0x1a1030, ambient: 0x4a3080 },
  studio: { floor: 0xffe0e8, wall: 0xfff0f5, accent: 0xff4081, sky: 0xffd4e0, ambient: 0xffe0e8 },
  shop: { floor: 0xfff8dc, wall: 0xfff5ee, accent: 0xffc107, sky: 0xfff8e1, ambient: 0xfff8dc },
  bathroom: { floor: 0xe0f2f8, wall: 0xf0f8ff, accent: 0x42a5f5, sky: 0xe3f2fd, ambient: 0xe0f2f8 },
  home: { floor: 0xfff0f5, wall: 0xfff5f8, accent: 0xff6b9d, sky: 0xffd4e8, ambient: 0xfff0f5 },
};

function makeRoundedBox(w: number, h: number, d: number, color: number, shininess: number = 40): THREE.Mesh {
  const geom = new THREE.BoxGeometry(w, h, d);
  const mat = new THREE.MeshPhongMaterial({ color, shininess, specular: 0x222222 });
  const mesh = new THREE.Mesh(geom, mat);
  mesh.castShadow = true;
  mesh.receiveShadow = true;
  return mesh;
}

function makeSphere(radius: number, color: number, shininess: number = 40): THREE.Mesh {
  const geom = new THREE.SphereGeometry(radius, 12, 12);
  const mat = new THREE.MeshPhongMaterial({ color, shininess });
  const mesh = new THREE.Mesh(geom, mat);
  mesh.castShadow = true;
  return mesh;
}

function makeCylinder(rTop: number, rBot: number, h: number, color: number): THREE.Mesh {
  const geom = new THREE.CylinderGeometry(rTop, rBot, h, 12);
  const mat = new THREE.MeshPhongMaterial({ color, shininess: 30 });
  const mesh = new THREE.Mesh(geom, mat);
  mesh.castShadow = true;
  return mesh;
}

export class RoomBuilder {
  static build(scene: THREE.Scene, roomType: RoomType, timeOfDay: number = 12): RoomData {
    const palette = PALETTES[roomType] || PALETTES.home;
    const interactables: InteractableInfo[] = [];

    // Lighting
    RoomBuilder.setupLighting(scene, palette, roomType, timeOfDay);

    // Ground / floor
    if (roomType === 'park') {
      RoomBuilder.buildOutdoorGround(scene, palette);
    } else {
      RoomBuilder.buildIndoorRoom(scene, palette);
    }

    // Room-specific furniture
    switch (roomType) {
      case 'bedroom':
        RoomBuilder.buildBedroom(scene, palette, interactables);
        break;
      case 'kitchen':
        RoomBuilder.buildKitchen(scene, palette, interactables);
        break;
      case 'park':
        RoomBuilder.buildPark(scene, palette, interactables);
        break;
      case 'school':
        RoomBuilder.buildSchool(scene, palette, interactables);
        break;
      case 'arcade':
        RoomBuilder.buildArcade(scene, palette, interactables);
        break;
      case 'studio':
        RoomBuilder.buildStudio(scene, palette, interactables);
        break;
      case 'shop':
        RoomBuilder.buildShop(scene, palette, interactables);
        break;
      case 'bathroom':
        RoomBuilder.buildBathroom(scene, palette, interactables);
        break;
      case 'home':
        RoomBuilder.buildHome(scene, palette, interactables);
        break;
    }

    return {
      interactables,
      ambientColor: palette.ambient,
      floorColor: palette.floor,
      wallColor: palette.wall,
    };
  }

  private static setupLighting(scene: THREE.Scene, palette: any, roomType: RoomType, timeOfDay: number) {
    // Ambient light - warm and soft
    const ambientIntensity = roomType === 'arcade' ? 0.3 : 0.5;
    const ambient = new THREE.AmbientLight(palette.ambient, ambientIntensity);
    scene.add(ambient);

    // Main directional light (sun/room light)
    const dirLight = new THREE.DirectionalLight(0xfff5e6, 0.8);
    dirLight.position.set(3, 5, 4);
    dirLight.castShadow = true;
    dirLight.shadow.mapSize.width = 512;
    dirLight.shadow.mapSize.height = 512;
    dirLight.shadow.camera.near = 0.5;
    dirLight.shadow.camera.far = 15;
    dirLight.shadow.camera.left = -5;
    dirLight.shadow.camera.right = 5;
    dirLight.shadow.camera.top = 5;
    dirLight.shadow.camera.bottom = -5;
    scene.add(dirLight);

    // Fill light from opposite side
    const fillLight = new THREE.DirectionalLight(0xe0e8ff, 0.3);
    fillLight.position.set(-2, 3, -2);
    scene.add(fillLight);

    // Point light for warmth
    if (roomType !== 'park') {
      const warmLight = new THREE.PointLight(0xffd4a0, 0.4, 8);
      warmLight.position.set(0, 3, 0);
      scene.add(warmLight);
    }

    // Room-specific lighting
    if (roomType === 'arcade') {
      // Colorful arcade lights
      const neonPink = new THREE.PointLight(0xff6b9d, 0.6, 5);
      neonPink.position.set(-2, 2, -1);
      scene.add(neonPink);
      const neonBlue = new THREE.PointLight(0x42a5f5, 0.6, 5);
      neonBlue.position.set(2, 2, -1);
      scene.add(neonBlue);
    }

    // Sky/background color
    scene.background = new THREE.Color(palette.sky);
  }

  private static buildIndoorRoom(scene: THREE.Scene, palette: any) {
    // Floor
    const floorGeom = new THREE.PlaneGeometry(8, 8);
    const floorMat = new THREE.MeshPhongMaterial({ color: palette.floor, side: THREE.DoubleSide });
    const floor = new THREE.Mesh(floorGeom, floorMat);
    floor.rotation.x = -Math.PI / 2;
    floor.position.y = -0.01;
    floor.receiveShadow = true;
    floor.name = 'floor';
    scene.add(floor);

    // Back wall
    const wallGeom = new THREE.PlaneGeometry(8, 4.5);
    const wallMat = new THREE.MeshPhongMaterial({ color: palette.wall, side: THREE.DoubleSide });
    const backWall = new THREE.Mesh(wallGeom, wallMat);
    backWall.position.set(0, 2.25, -4);
    backWall.receiveShadow = true;
    scene.add(backWall);

    // Left wall
    const leftWall = new THREE.Mesh(wallGeom.clone(), wallMat.clone());
    leftWall.position.set(-4, 2.25, 0);
    leftWall.rotation.y = Math.PI / 2;
    leftWall.receiveShadow = true;
    scene.add(leftWall);

    // Baseboard
    const baseGeom = new THREE.BoxGeometry(8.1, 0.12, 0.05);
    const baseMat = new THREE.MeshPhongMaterial({ color: 0xffffff });
    const baseboard = new THREE.Mesh(baseGeom, baseMat);
    baseboard.position.set(0, 0.06, -3.97);
    scene.add(baseboard);
  }

  private static buildOutdoorGround(scene: THREE.Scene, palette: any) {
    // Grass
    const grassGeom = new THREE.PlaneGeometry(12, 12);
    const grassMat = new THREE.MeshPhongMaterial({ color: 0x7ecf6a, side: THREE.DoubleSide });
    const grass = new THREE.Mesh(grassGeom, grassMat);
    grass.rotation.x = -Math.PI / 2;
    grass.position.y = -0.01;
    grass.receiveShadow = true;
    grass.name = 'floor';
    scene.add(grass);

    // Path
    const pathGeom = new THREE.PlaneGeometry(1.5, 8);
    const pathMat = new THREE.MeshPhongMaterial({ color: 0xdeb887, side: THREE.DoubleSide });
    const path = new THREE.Mesh(pathGeom, pathMat);
    path.rotation.x = -Math.PI / 2;
    path.position.y = 0.01;
    scene.add(path);

    // Sky gradient (hemisphere light)
    const hemiLight = new THREE.HemisphereLight(0x87ceeb, 0x7ecf6a, 0.3);
    scene.add(hemiLight);
  }

  // ===== BEDROOM =====
  private static buildBedroom(scene: THREE.Scene, palette: any, interactables: InteractableInfo[]) {
    // Bed frame
    const bedGroup = new THREE.Group();
    const frame = makeRoundedBox(1.4, 0.35, 1.0, 0xdeb887);
    frame.position.y = 0.175;
    bedGroup.add(frame);
    // Mattress
    const mattress = makeRoundedBox(1.3, 0.12, 0.9, 0xffffff);
    mattress.position.y = 0.41;
    bedGroup.add(mattress);
    // Blanket
    const blanket = makeRoundedBox(1.25, 0.08, 0.65, palette.accent);
    blanket.position.set(0, 0.5, 0.1);
    bedGroup.add(blanket);
    // Pillow
    const pillow = makeSphere(0.14, 0xfff8f0, 20);
    pillow.scale.set(1.5, 0.5, 1);
    pillow.position.set(0, 0.52, -0.32);
    bedGroup.add(pillow);
    // Headboard
    const headboard = makeRoundedBox(1.4, 0.8, 0.08, 0xdeb887);
    headboard.position.set(0, 0.6, -0.5);
    bedGroup.add(headboard);
    // Heart on headboard
    const heart = makeSphere(0.08, 0xff6b9d, 60);
    heart.position.set(0, 0.8, -0.45);
    bedGroup.add(heart);

    bedGroup.position.set(-1.8, 0, -2.5);
    scene.add(bedGroup);
    interactables.push({
      id: 'bed', label: 'Sleep', position: new THREE.Vector3(-1.8, 0.5, -2.5),
      walkToOffset: new THREE.Vector3(-1.8, 0, -1.5), animOnInteract: 'sleep', category: 'rest',
    });

    // Nightstand
    const nightstand = makeRoundedBox(0.45, 0.5, 0.4, 0xc9a96e);
    nightstand.position.set(-0.7, 0.25, -2.8);
    scene.add(nightstand);

    // Lamp on nightstand
    const lampGroup = new THREE.Group();
    const lampBase = makeCylinder(0.06, 0.08, 0.12, palette.accent);
    lampBase.position.y = 0.06;
    lampGroup.add(lampBase);
    const lampShade = makeCylinder(0.04, 0.12, 0.14, 0xfff5cc);
    lampShade.position.y = 0.2;
    const shadeMat = lampShade.material as THREE.MeshPhongMaterial;
    shadeMat.emissive = new THREE.Color(0xffd93d);
    shadeMat.emissiveIntensity = 0.4;
    lampGroup.add(lampShade);
    lampGroup.position.set(-0.7, 0.5, -2.8);
    scene.add(lampGroup);

    // Wardrobe
    const wardrobeGroup = new THREE.Group();
    const wardrobe = makeRoundedBox(0.9, 1.6, 0.5, 0xdeb887);
    wardrobe.position.y = 0.8;
    wardrobeGroup.add(wardrobe);
    // Wardrobe handles
    const handle1 = makeSphere(0.03, 0xc0a060);
    handle1.position.set(-0.12, 0.8, 0.26);
    wardrobeGroup.add(handle1);
    const handle2 = makeSphere(0.03, 0xc0a060);
    handle2.position.set(0.12, 0.8, 0.26);
    wardrobeGroup.add(handle2);
    wardrobeGroup.position.set(1.8, 0, -3.2);
    scene.add(wardrobeGroup);
    interactables.push({
      id: 'wardrobe', label: 'Get Dressed', position: new THREE.Vector3(1.8, 0.8, -3.2),
      walkToOffset: new THREE.Vector3(1.8, 0, -2.2), animOnInteract: 'happy', category: 'dress',
    });

    // Window with curtains
    const windowGroup = new THREE.Group();
    const windowFrame = makeRoundedBox(1.0, 1.0, 0.05, 0xffffff);
    windowGroup.add(windowFrame);
    const windowGlass = makeRoundedBox(0.85, 0.85, 0.03, 0x87ceeb, 80);
    const glassMat = windowGlass.material as THREE.MeshPhongMaterial;
    glassMat.emissive = new THREE.Color(0x87ceeb);
    glassMat.emissiveIntensity = 0.3;
    glassMat.transparent = true;
    glassMat.opacity = 0.7;
    windowGroup.add(windowGlass);
    // Curtains
    const curtainGeom = new THREE.PlaneGeometry(0.25, 1.0);
    const curtainMat = new THREE.MeshPhongMaterial({ color: palette.accent, side: THREE.DoubleSide });
    const leftCurtain = new THREE.Mesh(curtainGeom, curtainMat);
    leftCurtain.position.set(-0.56, 0, 0.03);
    windowGroup.add(leftCurtain);
    const rightCurtain = new THREE.Mesh(curtainGeom, curtainMat);
    rightCurtain.position.set(0.56, 0, 0.03);
    windowGroup.add(rightCurtain);
    windowGroup.position.set(0.3, 2.2, -3.95);
    scene.add(windowGroup);

    // Rug (circular, fluffy)
    const rugGeom = new THREE.CircleGeometry(0.7, 24);
    const rugMat = new THREE.MeshPhongMaterial({ color: 0xb388ff, side: THREE.DoubleSide });
    const rug = new THREE.Mesh(rugGeom, rugMat);
    rug.rotation.x = -Math.PI / 2;
    rug.position.set(0, 0.01, -0.5);
    scene.add(rug);

    // Toy box
    const toyBox = makeRoundedBox(0.5, 0.35, 0.35, 0xff8a65);
    toyBox.position.set(2.5, 0.175, -1.5);
    scene.add(toyBox);
    // Toys poking out
    const teddy = makeSphere(0.1, 0xc9a96e);
    teddy.position.set(2.5, 0.45, -1.5);
    scene.add(teddy);
    interactables.push({
      id: 'toybox', label: 'Play', position: new THREE.Vector3(2.5, 0.3, -1.5),
      walkToOffset: new THREE.Vector3(2.0, 0, -0.8), animOnInteract: 'happy', category: 'fun',
    });

    // Desk
    const deskGroup = new THREE.Group();
    const deskTop = makeRoundedBox(1.0, 0.06, 0.5, 0xdeb887);
    deskTop.position.y = 0.55;
    deskGroup.add(deskTop);
    const deskLeg1 = makeRoundedBox(0.06, 0.55, 0.06, 0xc9a96e);
    deskLeg1.position.set(-0.44, 0.275, -0.2);
    deskGroup.add(deskLeg1);
    const deskLeg2 = makeRoundedBox(0.06, 0.55, 0.06, 0xc9a96e);
    deskLeg2.position.set(0.44, 0.275, -0.2);
    deskGroup.add(deskLeg2);
    deskGroup.position.set(-3.0, 0, -1.5);
    scene.add(deskGroup);
  }

  // ===== KITCHEN =====
  private static buildKitchen(scene: THREE.Scene, palette: any, interactables: InteractableInfo[]) {
    // Counter along back wall
    const counter = makeRoundedBox(4.0, 0.9, 0.6, 0xf5f0e0);
    counter.position.set(0, 0.45, -3.2);
    scene.add(counter);

    // Counter top (marble-like)
    const counterTop = makeRoundedBox(4.1, 0.06, 0.65, 0xf8f8f8, 80);
    counterTop.position.set(0, 0.93, -3.2);
    scene.add(counterTop);

    // Stove
    const stoveGroup = new THREE.Group();
    const stoveBody = makeRoundedBox(0.8, 0.06, 0.5, 0x333333);
    stoveBody.position.y = 0;
    stoveGroup.add(stoveBody);
    // Burners
    for (let i = 0; i < 4; i++) {
      const burner = makeCylinder(0.06, 0.06, 0.02, 0x555555);
      burner.position.set(-0.2 + (i % 2) * 0.4, 0.04, -0.1 + Math.floor(i / 2) * 0.2);
      stoveGroup.add(burner);
    }
    stoveGroup.position.set(-1.0, 0.96, -3.2);
    scene.add(stoveGroup);

    // Pot on stove
    const pot = makeCylinder(0.12, 0.10, 0.14, 0xc0c0c0);
    pot.position.set(-1.0, 1.1, -3.2);
    scene.add(pot);
    interactables.push({
      id: 'stove', label: 'Cook', position: new THREE.Vector3(-1.0, 1.0, -3.2),
      walkToOffset: new THREE.Vector3(-1.0, 0, -2.2), animOnInteract: 'cook', category: 'cook',
    });

    // Fridge
    const fridgeGroup = new THREE.Group();
    const fridgeBody = makeRoundedBox(0.7, 1.6, 0.6, 0xf5f5f5, 60);
    fridgeBody.position.y = 0.8;
    fridgeGroup.add(fridgeBody);
    // Fridge handle
    const fHandle = makeCylinder(0.015, 0.015, 0.4, 0xc0c0c0);
    fHandle.position.set(0.28, 1.0, 0.31);
    fridgeGroup.add(fHandle);
    // Magnets
    const magnet1 = makeSphere(0.03, 0xff6b9d);
    magnet1.position.set(-0.1, 1.3, 0.32);
    fridgeGroup.add(magnet1);
    const magnet2 = makeSphere(0.03, 0x42a5f5);
    magnet2.position.set(0.05, 1.1, 0.32);
    fridgeGroup.add(magnet2);
    fridgeGroup.position.set(2.5, 0, -3.2);
    scene.add(fridgeGroup);
    interactables.push({
      id: 'fridge', label: 'Snack', position: new THREE.Vector3(2.5, 0.8, -3.2),
      walkToOffset: new THREE.Vector3(2.5, 0, -2.2), animOnInteract: 'eat', category: 'eat',
    });

    // Kitchen table
    const tableGroup = new THREE.Group();
    const tableTop = makeRoundedBox(1.4, 0.06, 0.9, 0xdeb887);
    tableTop.position.y = 0.55;
    tableGroup.add(tableTop);
    // Legs
    const legPositions = [[-0.6, -0.35], [0.6, -0.35], [-0.6, 0.35], [0.6, 0.35]];
    legPositions.forEach(([x, z]) => {
      const leg = makeCylinder(0.04, 0.04, 0.55, 0xc9a96e);
      leg.position.set(x, 0.275, z);
      tableGroup.add(leg);
    });
    tableGroup.position.set(0, 0, -0.8);
    scene.add(tableGroup);
    interactables.push({
      id: 'table', label: 'Eat', position: new THREE.Vector3(0, 0.5, -0.8),
      walkToOffset: new THREE.Vector3(0, 0, 0.2), animOnInteract: 'eat', category: 'eat',
    });

    // Plate on table
    const plate = makeCylinder(0.15, 0.15, 0.02, 0xffffff);
    plate.position.set(0, 0.6, -0.8);
    scene.add(plate);

    // Sink
    const sink = makeRoundedBox(0.5, 0.15, 0.4, 0xe0e0e0, 80);
    sink.position.set(1.0, 0.96, -3.2);
    scene.add(sink);
    // Faucet
    const faucet = makeCylinder(0.02, 0.02, 0.25, 0xc0c0c0);
    faucet.position.set(1.0, 1.15, -3.35);
    scene.add(faucet);
    interactables.push({
      id: 'sink', label: 'Wash Dishes', position: new THREE.Vector3(1.0, 1.0, -3.2),
      walkToOffset: new THREE.Vector3(1.0, 0, -2.2), animOnInteract: 'clean', category: 'clean',
    });

    // Hanging pots
    for (let i = 0; i < 3; i++) {
      const hangPot = makeCylinder(0.08, 0.06, 0.1, [0xc0c0c0, 0xb87333, 0x808080][i]);
      hangPot.position.set(-1.8 + i * 0.4, 2.8, -3.5);
      scene.add(hangPot);
    }
  }

  // ===== PARK =====
  private static buildPark(scene: THREE.Scene, palette: any, interactables: InteractableInfo[]) {
    // Trees
    const treePositions = [[-3, -3], [-1.5, -4], [2, -4.5], [4, -3], [-4, 1], [4.5, 0]];
    treePositions.forEach(([x, z], i) => {
      const treeGroup = new THREE.Group();
      // Trunk
      const trunk = makeCylinder(0.08, 0.12, 0.8 + Math.random() * 0.4, 0x8b5e3c);
      trunk.position.y = 0.4;
      treeGroup.add(trunk);
      // Foliage (multiple spheres for fuller look)
      const foliageColors = [0x4caf50, 0x66bb6a, 0x43a047, 0x2e7d32];
      for (let j = 0; j < 3; j++) {
        const foliage = makeSphere(0.3 + Math.random() * 0.15, foliageColors[j % foliageColors.length]);
        foliage.position.set(
          (Math.random() - 0.5) * 0.2,
          0.8 + Math.random() * 0.3,
          (Math.random() - 0.5) * 0.2
        );
        treeGroup.add(foliage);
      }
      treeGroup.position.set(x, 0, z);
      scene.add(treeGroup);
    });

    // Flowers scattered
    const flowerColors = [0xff6b9d, 0xffd93d, 0xb388ff, 0xff9f43, 0x42a5f5, 0xef5350, 0xab47bc];
    for (let i = 0; i < 15; i++) {
      const flowerGroup = new THREE.Group();
      // Stem
      const stem = makeCylinder(0.01, 0.01, 0.15, 0x4caf50);
      stem.position.y = 0.075;
      flowerGroup.add(stem);
      // Petals
      const petal = makeSphere(0.04, flowerColors[i % flowerColors.length]);
      petal.position.y = 0.17;
      flowerGroup.add(petal);
      // Center
      const center = makeSphere(0.02, 0xffd93d);
      center.position.y = 0.19;
      flowerGroup.add(center);
      flowerGroup.position.set(
        -4 + Math.random() * 8,
        0,
        -3 + Math.random() * 6
      );
      scene.add(flowerGroup);
    }

    // Swing set
    const swingGroup = new THREE.Group();
    // Frame
    const poleLeft = makeCylinder(0.04, 0.04, 2.0, 0xc0c0c0);
    poleLeft.position.set(-0.5, 1.0, 0);
    swingGroup.add(poleLeft);
    const poleRight = makeCylinder(0.04, 0.04, 2.0, 0xc0c0c0);
    poleRight.position.set(0.5, 1.0, 0);
    swingGroup.add(poleRight);
    const topBar = makeCylinder(0.03, 0.03, 1.1, 0xc0c0c0);
    topBar.rotation.z = Math.PI / 2;
    topBar.position.y = 2.0;
    swingGroup.add(topBar);
    // Seat
    const seat = makeRoundedBox(0.3, 0.03, 0.15, 0x8b4513);
    seat.position.y = 0.5;
    swingGroup.add(seat);
    // Chains
    const chain1 = makeCylinder(0.008, 0.008, 1.5, 0xaaaaaa);
    chain1.position.set(-0.12, 1.25, 0);
    swingGroup.add(chain1);
    const chain2 = makeCylinder(0.008, 0.008, 1.5, 0xaaaaaa);
    chain2.position.set(0.12, 1.25, 0);
    swingGroup.add(chain2);
    swingGroup.position.set(2.5, 0, -1.5);
    scene.add(swingGroup);
    interactables.push({
      id: 'swing', label: 'Swing', position: new THREE.Vector3(2.5, 0.5, -1.5),
      walkToOffset: new THREE.Vector3(2.5, 0, -0.5), animOnInteract: 'happy', category: 'fun',
    });

    // Slide
    const slideGroup = new THREE.Group();
    const slideLadder = makeCylinder(0.03, 0.03, 1.2, 0xc0c0c0);
    slideLadder.position.set(-0.3, 0.6, 0);
    slideGroup.add(slideLadder);
    const slideBody = makeRoundedBox(0.4, 0.04, 1.5, 0xff6b9d);
    slideBody.rotation.x = Math.PI * 0.15;
    slideBody.position.set(0, 0.4, 0.3);
    slideGroup.add(slideBody);
    slideGroup.position.set(-2.5, 0, -1);
    scene.add(slideGroup);
    interactables.push({
      id: 'slide', label: 'Slide', position: new THREE.Vector3(-2.5, 0.5, -1),
      walkToOffset: new THREE.Vector3(-2.5, 0, 0.5), animOnInteract: 'happy', category: 'fun',
    });

    // Sandbox
    const sandbox = makeRoundedBox(1.2, 0.2, 1.2, 0xdeb887);
    sandbox.position.set(0, 0.1, -2.5);
    scene.add(sandbox);
    const sand = makeRoundedBox(1.1, 0.1, 1.1, 0xf5deb3);
    sand.position.set(0, 0.2, -2.5);
    scene.add(sand);
    // Little sand castle
    const castle = makeCylinder(0.08, 0.12, 0.15, 0xe8d4a0);
    castle.position.set(0.15, 0.3, -2.4);
    scene.add(castle);
    interactables.push({
      id: 'sandbox', label: 'Build Sandcastle', position: new THREE.Vector3(0, 0.2, -2.5),
      walkToOffset: new THREE.Vector3(0, 0, -1.5), animOnInteract: 'happy', category: 'fun',
    });

    // Sun
    const sun = makeSphere(0.5, 0xffd93d);
    const sunMat = sun.material as THREE.MeshPhongMaterial;
    sunMat.emissive = new THREE.Color(0xffd93d);
    sunMat.emissiveIntensity = 0.8;
    sun.position.set(3, 5, -6);
    sun.castShadow = false;
    scene.add(sun);

    // Clouds
    for (let i = 0; i < 4; i++) {
      const cloudGroup = new THREE.Group();
      for (let j = 0; j < 3; j++) {
        const puff = makeSphere(0.2 + Math.random() * 0.15, 0xffffff, 10);
        puff.position.set(j * 0.2 - 0.2, Math.random() * 0.1, 0);
        puff.castShadow = false;
        cloudGroup.add(puff);
      }
      cloudGroup.position.set(-3 + i * 2.5, 4 + Math.random(), -5 - Math.random() * 2);
      scene.add(cloudGroup);
    }

    // Bench
    const benchGroup = new THREE.Group();
    const benchSeat = makeRoundedBox(1.0, 0.06, 0.35, 0x8b5e3c);
    benchSeat.position.y = 0.4;
    benchGroup.add(benchSeat);
    const benchBack = makeRoundedBox(1.0, 0.4, 0.05, 0x8b5e3c);
    benchBack.position.set(0, 0.63, -0.15);
    benchGroup.add(benchBack);
    const benchLeg1 = makeRoundedBox(0.06, 0.4, 0.06, 0x555555);
    benchLeg1.position.set(-0.4, 0.2, 0);
    benchGroup.add(benchLeg1);
    const benchLeg2 = makeRoundedBox(0.06, 0.4, 0.06, 0x555555);
    benchLeg2.position.set(0.4, 0.2, 0);
    benchGroup.add(benchLeg2);
    benchGroup.position.set(-1, 0, 2);
    scene.add(benchGroup);
    interactables.push({
      id: 'bench', label: 'Rest', position: new THREE.Vector3(-1, 0.4, 2),
      walkToOffset: new THREE.Vector3(-1, 0, 2.8), animOnInteract: 'idle', category: 'rest',
    });
  }

  // ===== SCHOOL =====
  private static buildSchool(scene: THREE.Scene, palette: any, interactables: InteractableInfo[]) {
    // Desks in rows
    for (let row = 0; row < 2; row++) {
      for (let col = 0; col < 3; col++) {
        const desk = makeRoundedBox(0.7, 0.05, 0.45, 0xdeb887);
        desk.position.set(-1.5 + col * 1.5, 0.5, -1 + row * 1.5);
        scene.add(desk);
        const deskLeg = makeRoundedBox(0.05, 0.5, 0.05, 0xc9a96e);
        deskLeg.position.set(-1.5 + col * 1.5, 0.25, -1 + row * 1.5);
        scene.add(deskLeg);
      }
    }

    // Teacher's desk at front
    const teacherDesk = makeRoundedBox(1.2, 0.06, 0.6, 0x8b5e3c);
    teacherDesk.position.set(0, 0.6, -3.0);
    scene.add(teacherDesk);

    // Blackboard
    const blackboard = makeRoundedBox(2.5, 1.2, 0.08, 0x2e5e3e);
    blackboard.position.set(0, 2.2, -3.9);
    scene.add(blackboard);
    // Chalk tray
    const chalkTray = makeRoundedBox(2.4, 0.04, 0.08, 0xdeb887);
    chalkTray.position.set(0, 1.55, -3.85);
    scene.add(chalkTray);

    interactables.push({
      id: 'desk', label: 'Study', position: new THREE.Vector3(0, 0.5, -1),
      walkToOffset: new THREE.Vector3(0, 0, 0), animOnInteract: 'study', category: 'study',
    });

    // Globe
    const globe = makeSphere(0.15, 0x42a5f5);
    globe.position.set(-2.5, 1.0, -3.5);
    scene.add(globe);
    // Globe stand
    const globeStand = makeCylinder(0.02, 0.04, 0.3, 0x8b5e3c);
    globeStand.position.set(-2.5, 0.7, -3.5);
    scene.add(globeStand);

    // Bookshelf
    const shelf = makeRoundedBox(1.0, 1.4, 0.3, 0xdeb887);
    shelf.position.set(2.5, 0.7, -3.5);
    scene.add(shelf);
    // Books
    const bookColors = [0xff6b9d, 0x42a5f5, 0x66bb6a, 0xffd93d, 0xab47bc];
    for (let i = 0; i < 5; i++) {
      const book = makeRoundedBox(0.06, 0.2, 0.15, bookColors[i]);
      book.position.set(2.2 + i * 0.12, 1.2, -3.5);
      scene.add(book);
    }

    interactables.push({
      id: 'books', label: 'Read', position: new THREE.Vector3(2.5, 0.8, -3.5),
      walkToOffset: new THREE.Vector3(2.0, 0, -2.5), animOnInteract: 'study', category: 'study',
    });

    // Art corner
    const easel = makeRoundedBox(0.6, 0.8, 0.04, 0xffffff);
    easel.position.set(-3.0, 1.2, -2.0);
    easel.rotation.y = 0.3;
    scene.add(easel);
    interactables.push({
      id: 'art', label: 'Draw', position: new THREE.Vector3(-3.0, 1.0, -2.0),
      walkToOffset: new THREE.Vector3(-2.5, 0, -1.0), animOnInteract: 'happy', category: 'fun',
    });
  }

  // ===== ARCADE =====
  private static buildArcade(scene: THREE.Scene, palette: any, interactables: InteractableInfo[]) {
    // Arcade cabinets
    const cabinetColors = [0xff6b9d, 0x42a5f5, 0x66bb6a, 0xffd93d, 0xab47bc];
    const cabinetNames = ['scooty_dash', 'dance_party', 'karate_star', 'trampoline', 'cooking_game'];
    const cabinetLabels = ['Scooty Dash', 'Dance Party', 'Karate Star', 'Trampoline', 'Cooking'];

    for (let i = 0; i < 5; i++) {
      const cabinetGroup = new THREE.Group();
      // Body
      const body = makeRoundedBox(0.6, 1.3, 0.5, 0x333333);
      body.position.y = 0.65;
      cabinetGroup.add(body);
      // Screen
      const screen = makeRoundedBox(0.45, 0.4, 0.03, cabinetColors[i]);
      const screenMat = screen.material as THREE.MeshPhongMaterial;
      screenMat.emissive = new THREE.Color(cabinetColors[i]);
      screenMat.emissiveIntensity = 0.5;
      screen.position.set(0, 1.0, 0.27);
      cabinetGroup.add(screen);
      // Controls
      const controls = makeRoundedBox(0.35, 0.04, 0.15, 0x555555);
      controls.position.set(0, 0.55, 0.27);
      controls.rotation.x = -0.3;
      cabinetGroup.add(controls);
      // Joystick
      const joystick = makeCylinder(0.02, 0.02, 0.08, 0xff0000);
      joystick.position.set(-0.08, 0.6, 0.3);
      cabinetGroup.add(joystick);
      // Button
      const btn = makeSphere(0.025, cabinetColors[i]);
      btn.position.set(0.08, 0.58, 0.3);
      cabinetGroup.add(btn);

      const x = -2.5 + i * 1.3;
      cabinetGroup.position.set(x, 0, -3.0);
      scene.add(cabinetGroup);

      interactables.push({
        id: cabinetNames[i], label: cabinetLabels[i],
        position: new THREE.Vector3(x, 0.8, -3.0),
        walkToOffset: new THREE.Vector3(x, 0, -2.0),
        animOnInteract: 'happy', category: 'minigame',
      });
    }

    // Neon floor strips
    const stripColors = [0xff6b9d, 0x42a5f5, 0xffd93d];
    stripColors.forEach((color, i) => {
      const strip = makeRoundedBox(8, 0.02, 0.08, color);
      const stripMat = strip.material as THREE.MeshPhongMaterial;
      stripMat.emissive = new THREE.Color(color);
      stripMat.emissiveIntensity = 0.6;
      strip.position.set(0, 0.01, -1 + i * 1.5);
      scene.add(strip);
    });

    // Prize counter
    const prizeCounter = makeRoundedBox(2.0, 0.8, 0.5, 0x8b5e3c);
    prizeCounter.position.set(0, 0.4, 2.5);
    scene.add(prizeCounter);
    // Prizes on counter
    const prizeColors = [0xff6b9d, 0xffd93d, 0x42a5f5, 0x66bb6a];
    for (let i = 0; i < 4; i++) {
      const prize = makeSphere(0.1, prizeColors[i]);
      prize.position.set(-0.6 + i * 0.4, 0.9, 2.5);
      scene.add(prize);
    }
  }

  // ===== STUDIO =====
  private static buildStudio(scene: THREE.Scene, palette: any, interactables: InteractableInfo[]) {
    // Vanity mirror
    const vanityGroup = new THREE.Group();
    const vanityTable = makeRoundedBox(1.2, 0.5, 0.5, 0xffe4f0);
    vanityTable.position.y = 0.25;
    vanityGroup.add(vanityTable);
    const mirror = makeRoundedBox(0.8, 0.8, 0.04, 0xcccccc, 100);
    const mirrorMat = mirror.material as THREE.MeshPhongMaterial;
    mirrorMat.emissive = new THREE.Color(0xffffff);
    mirrorMat.emissiveIntensity = 0.2;
    mirror.position.set(0, 0.9, -0.2);
    vanityGroup.add(mirror);
    // Light bulbs around mirror
    for (let i = 0; i < 6; i++) {
      const bulb = makeSphere(0.03, 0xffd93d);
      const bulbMat = bulb.material as THREE.MeshPhongMaterial;
      bulbMat.emissive = new THREE.Color(0xffd93d);
      bulbMat.emissiveIntensity = 0.8;
      const angle = (i / 6) * Math.PI + Math.PI / 2;
      bulb.position.set(Math.cos(angle) * 0.5, 0.9 + Math.sin(angle) * 0.45, -0.15);
      vanityGroup.add(bulb);
    }
    vanityGroup.position.set(-2.0, 0, -3.0);
    scene.add(vanityGroup);
    interactables.push({
      id: 'makeup', label: 'Makeup', position: new THREE.Vector3(-2.0, 0.5, -3.0),
      walkToOffset: new THREE.Vector3(-2.0, 0, -2.0), animOnInteract: 'happy', category: 'studio',
    });

    // Camera/tripod
    const tripodGroup = new THREE.Group();
    const tripodLeg1 = makeCylinder(0.015, 0.015, 1.0, 0x333333);
    tripodLeg1.position.set(-0.15, 0.5, 0.1);
    tripodLeg1.rotation.z = 0.1;
    tripodGroup.add(tripodLeg1);
    const tripodLeg2 = makeCylinder(0.015, 0.015, 1.0, 0x333333);
    tripodLeg2.position.set(0.15, 0.5, 0.1);
    tripodLeg2.rotation.z = -0.1;
    tripodGroup.add(tripodLeg2);
    const camera = makeRoundedBox(0.2, 0.15, 0.12, 0x333333);
    camera.position.y = 1.05;
    tripodGroup.add(camera);
    const lens = makeSphere(0.04, 0x42a5f5);
    lens.position.set(0, 1.05, 0.08);
    tripodGroup.add(lens);
    tripodGroup.position.set(1.5, 0, -1.0);
    scene.add(tripodGroup);
    interactables.push({
      id: 'camera', label: 'Record Reel', position: new THREE.Vector3(1.5, 1.0, -1.0),
      walkToOffset: new THREE.Vector3(0.5, 0, -0.5), animOnInteract: 'dance', category: 'studio',
    });

    // Stage area (raised platform)
    const stage = makeRoundedBox(2.0, 0.15, 1.5, 0xff4081);
    stage.position.set(0, 0.075, 1.5);
    scene.add(stage);
    // Stage lights
    const stageLight1 = new THREE.PointLight(0xff6b9d, 0.5, 4);
    stageLight1.position.set(-1, 2, 1.5);
    scene.add(stageLight1);
    const stageLight2 = new THREE.PointLight(0x42a5f5, 0.5, 4);
    stageLight2.position.set(1, 2, 1.5);
    scene.add(stageLight2);
    interactables.push({
      id: 'stage', label: 'Dance Show', position: new THREE.Vector3(0, 0.15, 1.5),
      walkToOffset: new THREE.Vector3(0, 0, 0.5), animOnInteract: 'dance', category: 'studio',
    });

    // Clothing rack
    const rackGroup = new THREE.Group();
    const rackBar = makeCylinder(0.02, 0.02, 1.5, 0xc0c0c0);
    rackBar.rotation.z = Math.PI / 2;
    rackBar.position.y = 1.2;
    rackGroup.add(rackBar);
    // Hangers with clothes
    const clothColors = [0xff6b9d, 0x42a5f5, 0xffd93d, 0xab47bc, 0x66bb6a];
    for (let i = 0; i < 5; i++) {
      const cloth = makeRoundedBox(0.2, 0.35, 0.04, clothColors[i]);
      cloth.position.set(-0.6 + i * 0.3, 0.9, 0);
      rackGroup.add(cloth);
    }
    rackGroup.position.set(2.5, 0, -3.0);
    scene.add(rackGroup);
    interactables.push({
      id: 'wardrobe_studio', label: 'Outfits', position: new THREE.Vector3(2.5, 0.8, -3.0),
      walkToOffset: new THREE.Vector3(2.0, 0, -2.0), animOnInteract: 'happy', category: 'dress',
    });
  }

  // ===== SHOP =====
  private static buildShop(scene: THREE.Scene, palette: any, interactables: InteractableInfo[]) {
    // Shop shelves
    for (let row = 0; row < 3; row++) {
      const shelf = makeRoundedBox(2.0, 0.06, 0.4, 0xdeb887);
      shelf.position.set(-1.5, 0.6 + row * 0.7, -3.2);
      scene.add(shelf);

      // Items on shelves
      const itemColors = [0xff6b9d, 0x42a5f5, 0xffd93d, 0xab47bc, 0x66bb6a, 0xff9f43];
      for (let i = 0; i < 5; i++) {
        const item = makeSphere(0.08, itemColors[(row * 5 + i) % itemColors.length]);
        item.position.set(-2.1 + i * 0.5, 0.72 + row * 0.7, -3.2);
        scene.add(item);
      }
    }

    // Counter
    const counter = makeRoundedBox(2.0, 0.9, 0.6, 0xdeb887);
    counter.position.set(1.5, 0.45, -1.0);
    scene.add(counter);
    // Cash register
    const register = makeRoundedBox(0.3, 0.25, 0.25, 0x4caf50);
    register.position.set(1.5, 0.95, -1.0);
    scene.add(register);

    interactables.push({
      id: 'shop_counter', label: 'Buy Items', position: new THREE.Vector3(1.5, 0.6, -1.0),
      walkToOffset: new THREE.Vector3(1.5, 0, 0), animOnInteract: 'happy', category: 'shop',
    });

    // Display racks
    for (let i = 0; i < 3; i++) {
      const rack = makeRoundedBox(0.8, 1.0, 0.4, 0xffe4f0);
      rack.position.set(-2.5 + i * 2.5, 0.5, 1.0);
      scene.add(rack);
      // Items
      for (let j = 0; j < 3; j++) {
        const displayItem = makeSphere(0.06, [0xff6b9d, 0xffd93d, 0x42a5f5][j]);
        displayItem.position.set(-2.5 + i * 2.5, 0.9 + j * 0.15, 1.22);
        scene.add(displayItem);
      }
    }

    interactables.push({
      id: 'clothes_rack', label: 'Try Clothes', position: new THREE.Vector3(0, 0.5, 1.0),
      walkToOffset: new THREE.Vector3(0, 0, 2.0), animOnInteract: 'happy', category: 'shop',
    });
  }

  // ===== BATHROOM =====
  private static buildBathroom(scene: THREE.Scene, palette: any, interactables: InteractableInfo[]) {
    // Bathtub
    const tubGroup = new THREE.Group();
    const tubBody = makeRoundedBox(1.2, 0.45, 0.6, 0xffffff, 80);
    tubBody.position.y = 0.225;
    tubGroup.add(tubBody);
    // Water in tub
    const water = makeRoundedBox(1.1, 0.08, 0.5, 0x87ceeb);
    const waterMat = water.material as THREE.MeshPhongMaterial;
    waterMat.transparent = true;
    waterMat.opacity = 0.6;
    water.position.y = 0.4;
    tubGroup.add(water);
    // Bubbles
    for (let i = 0; i < 6; i++) {
      const bubble = makeSphere(0.04 + Math.random() * 0.03, 0xffffff);
      const bMat = bubble.material as THREE.MeshPhongMaterial;
      bMat.transparent = true;
      bMat.opacity = 0.5;
      bubble.position.set((Math.random() - 0.5) * 0.8, 0.45 + Math.random() * 0.1, (Math.random() - 0.5) * 0.3);
      tubGroup.add(bubble);
    }
    // Faucet
    const faucet = makeCylinder(0.03, 0.03, 0.15, 0xc0c0c0);
    faucet.position.set(0, 0.52, -0.25);
    tubGroup.add(faucet);
    tubGroup.position.set(-1.5, 0, -3.0);
    scene.add(tubGroup);
    interactables.push({
      id: 'bathtub', label: 'Take Bath', position: new THREE.Vector3(-1.5, 0.3, -3.0),
      walkToOffset: new THREE.Vector3(-1.5, 0, -2.0), animOnInteract: 'happy', category: 'clean',
    });

    // Sink with mirror
    const sinkGroup = new THREE.Group();
    const sinkBase = makeRoundedBox(0.5, 0.6, 0.35, 0xffffff);
    sinkBase.position.y = 0.3;
    sinkGroup.add(sinkBase);
    const sinkBowl = makeCylinder(0.15, 0.12, 0.08, 0xffffff);
    sinkBowl.position.y = 0.65;
    sinkGroup.add(sinkBowl);
    // Mirror above sink
    const sinkMirror = makeRoundedBox(0.5, 0.6, 0.04, 0xcccccc, 100);
    const sMirrorMat = sinkMirror.material as THREE.MeshPhongMaterial;
    sMirrorMat.emissive = new THREE.Color(0xffffff);
    sMirrorMat.emissiveIntensity = 0.15;
    sinkMirror.position.set(0, 1.2, -0.15);
    sinkGroup.add(sinkMirror);
    sinkGroup.position.set(1.0, 0, -3.2);
    scene.add(sinkGroup);
    interactables.push({
      id: 'sink_bath', label: 'Wash Hands', position: new THREE.Vector3(1.0, 0.6, -3.2),
      walkToOffset: new THREE.Vector3(1.0, 0, -2.2), animOnInteract: 'clean', category: 'clean',
    });

    // Toilet
    const toiletGroup = new THREE.Group();
    const toiletBase = makeRoundedBox(0.35, 0.35, 0.4, 0xffffff);
    toiletBase.position.y = 0.175;
    toiletGroup.add(toiletBase);
    const toiletSeat = makeCylinder(0.16, 0.16, 0.04, 0xffffff);
    toiletSeat.position.y = 0.38;
    toiletGroup.add(toiletSeat);
    const toiletTank = makeRoundedBox(0.3, 0.35, 0.15, 0xffffff);
    toiletTank.position.set(0, 0.4, -0.22);
    toiletGroup.add(toiletTank);
    toiletGroup.position.set(2.8, 0, -3.0);
    scene.add(toiletGroup);

    // Towel rack
    const rackBar = makeCylinder(0.015, 0.015, 0.6, 0xc0c0c0);
    rackBar.rotation.z = Math.PI / 2;
    rackBar.position.set(-3.2, 1.2, -1.0);
    scene.add(rackBar);
    // Towel
    const towel = makeRoundedBox(0.4, 0.5, 0.03, palette.accent);
    towel.position.set(-3.2, 0.9, -1.0);
    scene.add(towel);

    // Rubber duck
    const duck = makeSphere(0.06, 0xffd93d);
    duck.position.set(-1.3, 0.5, -2.9);
    scene.add(duck);
    // Duck beak
    const beak = makeSphere(0.02, 0xff9f43);
    beak.position.set(-1.3, 0.5, -2.83);
    scene.add(beak);

    // Bath mat
    const mat = makeRoundedBox(0.8, 0.02, 0.5, palette.accent);
    mat.position.set(-1.5, 0.01, -1.8);
    scene.add(mat);

    interactables.push({
      id: 'toothbrush', label: 'Brush Teeth', position: new THREE.Vector3(1.0, 0.8, -3.2),
      walkToOffset: new THREE.Vector3(1.0, 0, -2.2), animOnInteract: 'clean', category: 'clean',
    });
  }

  // ===== HOME (Hub) =====
  private static buildHome(scene: THREE.Scene, palette: any, interactables: InteractableInfo[]) {
    // Couch
    const couchGroup = new THREE.Group();
    const couchSeat = makeRoundedBox(1.5, 0.35, 0.6, palette.accent);
    couchSeat.position.y = 0.25;
    couchGroup.add(couchSeat);
    const couchBack = makeRoundedBox(1.5, 0.5, 0.15, palette.accent);
    couchBack.position.set(0, 0.55, -0.22);
    couchGroup.add(couchBack);
    // Cushions
    const cushion1 = makeSphere(0.12, 0xffd93d);
    cushion1.position.set(-0.45, 0.5, 0);
    cushion1.scale.set(1, 0.8, 0.8);
    couchGroup.add(cushion1);
    const cushion2 = makeSphere(0.12, 0x42a5f5);
    cushion2.position.set(0.45, 0.5, 0);
    cushion2.scale.set(1, 0.8, 0.8);
    couchGroup.add(cushion2);
    couchGroup.position.set(0, 0, -2.5);
    scene.add(couchGroup);

    // TV
    const tvGroup = new THREE.Group();
    const tvScreen = makeRoundedBox(1.2, 0.7, 0.06, 0x222222, 80);
    const tvMat = tvScreen.material as THREE.MeshPhongMaterial;
    tvMat.emissive = new THREE.Color(0x42a5f5);
    tvMat.emissiveIntensity = 0.2;
    tvScreen.position.y = 1.2;
    tvGroup.add(tvScreen);
    const tvStand = makeCylinder(0.04, 0.08, 0.3, 0x333333);
    tvStand.position.y = 0.7;
    tvGroup.add(tvStand);
    const tvBase = makeRoundedBox(0.4, 0.04, 0.2, 0x333333);
    tvBase.position.y = 0.55;
    tvGroup.add(tvBase);
    tvGroup.position.set(0, 0, -3.5);
    scene.add(tvGroup);

    // Shoe rack by door
    const shoeRack = makeRoundedBox(0.8, 0.3, 0.25, 0xdeb887);
    shoeRack.position.set(-3.0, 0.15, 1.5);
    scene.add(shoeRack);
    // Shoes on rack
    for (let i = 0; i < 3; i++) {
      const shoe = makeSphere(0.05, [0xff6b9d, 0x42a5f5, 0x66bb6a][i]);
      shoe.scale.set(1.2, 0.5, 1.5);
      shoe.position.set(-3.15 + i * 0.3, 0.35, 1.5);
      scene.add(shoe);
    }

    // Door
    const door = makeRoundedBox(0.8, 1.8, 0.08, 0xdeb887);
    door.position.set(-3.5, 0.9, 0);
    scene.add(door);
    const doorKnob = makeSphere(0.04, 0xc0a060);
    doorKnob.position.set(-3.22, 0.85, 0.06);
    scene.add(doorKnob);

    // Welcome mat
    const welcomeMat = makeRoundedBox(0.6, 0.02, 0.35, 0x8b5e3c);
    welcomeMat.position.set(-3.0, 0.01, 0.5);
    scene.add(welcomeMat);

    // Family photo frame
    const photoFrame = makeRoundedBox(0.4, 0.3, 0.03, 0xdeb887);
    photoFrame.position.set(1.5, 2.0, -3.95);
    scene.add(photoFrame);
    const photo = makeRoundedBox(0.32, 0.22, 0.02, 0xffe4f0);
    photo.position.set(1.5, 2.0, -3.92);
    scene.add(photo);

    // Clock
    const clock = makeSphere(0.2, 0xffffff);
    clock.position.set(-1.0, 2.5, -3.95);
    scene.add(clock);
    // Clock hands
    const hourHand = makeRoundedBox(0.02, 0.1, 0.01, 0x333333);
    hourHand.position.set(-1.0, 2.55, -3.9);
    hourHand.rotation.z = Math.PI * 0.25;
    scene.add(hourHand);

    // Plant
    const potGroup = new THREE.Group();
    const pot = makeCylinder(0.1, 0.12, 0.18, 0xff8a65);
    pot.position.y = 0.09;
    potGroup.add(pot);
    for (let i = 0; i < 4; i++) {
      const leaf = makeSphere(0.06, 0x4caf50);
      leaf.position.set((Math.random() - 0.5) * 0.1, 0.22 + i * 0.05, (Math.random() - 0.5) * 0.1);
      potGroup.add(leaf);
    }
    potGroup.position.set(2.8, 0, -2.0);
    scene.add(potGroup);

    interactables.push({
      id: 'couch', label: 'Rest', position: new THREE.Vector3(0, 0.4, -2.5),
      walkToOffset: new THREE.Vector3(0, 0, -1.5), animOnInteract: 'idle', category: 'rest',
    });
    interactables.push({
      id: 'door_outside', label: 'Go Outside', position: new THREE.Vector3(-3.5, 0.9, 0),
      walkToOffset: new THREE.Vector3(-3.0, 0, 0.5), animOnInteract: 'walk', category: 'navigate',
    });
  }
}
