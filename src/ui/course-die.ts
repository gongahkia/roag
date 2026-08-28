import * as THREE from 'three';
import type { DieFace } from '../core/types';

export interface CourseDieScene {
  dispose(): void;
}

const themeColor: Record<DieFace['recipe']['terrain']['theme'], string> = {
  balanced: '#8bbf75',
  speedway: '#e6a341',
  'hazard-run': '#d26f58',
  'ice-rink': '#72bdd3',
  quarry: '#a9916b',
  drift: '#c28763',
  bloom: '#ba78ad',
  pulse: '#7c8ecc',
  carnival: '#e5a050',
  marsh: '#96b76d',
  zephyr: '#70bdc9',
};

const smooth = (value: number) => {
  const bounded = Math.max(0, Math.min(1, value));
  return bounded * bounded * (3 - 2 * bounded);
};

const faceTexture = (face: DieFace, index: number, selected: boolean) => {
  const textureCanvas = document.createElement('canvas');
  textureCanvas.width = 512;
  textureCanvas.height = 352;
  const context = textureCanvas.getContext('2d')!;
  const accent = themeColor[face.recipe.terrain.theme];
  context.fillStyle = '#f9fcf5';
  context.fillRect(0, 0, textureCanvas.width, textureCanvas.height);
  context.fillStyle = `${accent}24`;
  context.fillRect(18, 18, textureCanvas.width - 36, textureCanvas.height - 36);
  context.lineWidth = selected ? 12 : 7;
  context.strokeStyle = selected ? '#d38a18' : accent;
  context.strokeRect(18, 18, textureCanvas.width - 36, textureCanvas.height - 36);
  context.fillStyle = accent;
  context.beginPath();
  context.arc(72, 72, 35, 0, Math.PI * 2);
  context.fill();
  context.fillStyle = '#ffffff';
  context.font = '700 35px Inter, ui-sans-serif, system-ui, sans-serif';
  context.textAlign = 'center';
  context.textBaseline = 'middle';
  context.fillText(String(index + 1), 72, 75);
  context.fillStyle = '#17311b';
  context.font = '700 27px Inter, ui-sans-serif, system-ui, sans-serif';
  context.textAlign = 'left';
  context.textBaseline = 'alphabetic';
  const label = face.label.split(' · ');
  context.fillText(label[0] ?? face.label, 42, 168, 430);
  context.font = '600 21px Inter, ui-sans-serif, system-ui, sans-serif';
  context.fillStyle = '#547057';
  context.fillText(`${face.recipe.terrain.archetype} · ${face.recipe.terrain.sizeProfile}`, 42, 208, 430);
  context.fillStyle = accent;
  context.font = '800 21px Inter, ui-sans-serif, system-ui, sans-serif';
  context.fillText(`${face.weight} weight`, 42, 284);
  const texture = new THREE.CanvasTexture(textureCanvas);
  texture.colorSpace = THREE.SRGBColorSpace;
  texture.anisotropy = 2;
  return texture;
};

/** Renders each course package as a paper panel, then folds the net into a multi-sided rolling die. */
export const createCourseDieScene = (canvas: HTMLCanvasElement, faces: readonly DieFace[], rolling: boolean, selectedId?: string, rollSecondsLeft?: number, reducedMotion = false): CourseDieScene => {
  let renderer: THREE.WebGLRenderer;
  try {
    renderer = new THREE.WebGLRenderer({ canvas, alpha: true, antialias: true, powerPreference: 'low-power' });
  } catch {
    canvas.classList.add('die-scene-fallback');
    return { dispose() {} };
  }
  renderer.setClearColor(0x000000, 0);
  renderer.outputColorSpace = THREE.SRGBColorSpace;
  const scene = new THREE.Scene();
  const camera = new THREE.PerspectiveCamera(34, 1, .1, 100);
  const light = new THREE.HemisphereLight(0xffffff, 0x38523a, 2.4);
  scene.add(light);
  const keyLight = new THREE.DirectionalLight(0xfff7dd, 2.8);
  keyLight.position.set(3, 6, 4);
  scene.add(keyLight);

  const die = new THREE.Group();
  scene.add(die);
  const flatGuides = new THREE.Group();
  die.add(flatGuides);
  const panelGeometry = new THREE.PlaneGeometry(1.45, 1.02);
  const sideCount = Math.max(3, faces.length);
  const columns = Math.ceil(Math.sqrt(sideCount));
  const rows = Math.ceil(sideCount / columns);
  const radius = Math.max(.85, sideCount * .19);
  const panelMeshes: Array<{ mesh: THREE.Mesh<THREE.PlaneGeometry, THREE.MeshStandardMaterial>; flat: THREE.Vector3; folded: THREE.Vector3; angle: number }> = [];
  const materials: THREE.Material[] = [];
  const textures: THREE.Texture[] = [];

  faces.forEach((face, index) => {
    const texture = faceTexture(face, index, face.id === selectedId);
    textures.push(texture);
    const material = new THREE.MeshStandardMaterial({ map: texture, roughness: .68, metalness: .03, side: THREE.FrontSide });
    materials.push(material);
    const mesh = new THREE.Mesh(panelGeometry, material);
    const column = index % columns;
    const row = Math.floor(index / columns);
    const flat = new THREE.Vector3((column - (columns - 1) / 2) * 1.58, 0, (row - (rows - 1) / 2) * 1.17);
    const angle = index / sideCount * Math.PI * 2;
    const folded = new THREE.Vector3(Math.sin(angle) * radius, 0, Math.cos(angle) * radius);
    mesh.position.copy(flat);
    mesh.rotation.x = -Math.PI / 2;
    die.add(mesh);
    panelMeshes.push({ mesh, flat, folded, angle });
  });

  const guideMaterial = new THREE.LineDashedMaterial({ color: 0x789372, dashSize: .1, gapSize: .08, transparent: true, opacity: .55 });
  materials.push(guideMaterial);
  panelMeshes.forEach(({ flat }, index) => {
    const next = panelMeshes[index + 1];
    if (!next || index % columns === columns - 1) return;
    const points = [flat.clone().setY(-.015), next.flat.clone().setY(-.015)];
    const guide = new THREE.Line(new THREE.BufferGeometry().setFromPoints(points), guideMaterial);
    guide.computeLineDistances();
    flatGuides.add(guide);
  });

  const coreMaterial = new THREE.MeshStandardMaterial({ color: 0x315f35, roughness: .45, metalness: .1, flatShading: true });
  materials.push(coreMaterial);
  const core = new THREE.Mesh(new THREE.CylinderGeometry(radius * .92, radius * .92, 1.02, sideCount), coreMaterial);
  core.visible = rolling;
  die.add(core);

  const extent = Math.max(columns * 1.45, rows * 1.1, radius * 3.2);
  camera.position.set(0, Math.max(3.1, extent * .72), Math.max(3.8, extent * .85));
  camera.lookAt(0, 0, 0);
  const resize = () => {
    const { width, height } = canvas.getBoundingClientRect();
    if (!width || !height) return;
    renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
    renderer.setSize(width, height, false);
    camera.aspect = width / height;
    camera.updateProjectionMatrix();
  };
  const resizeObserver = new ResizeObserver(resize);
  resizeObserver.observe(canvas);
  resize();

  let animationFrame = 0;
  let disposed = false;
  const startedAt = performance.now() - (rolling ? Math.max(0, (1.15 - (rollSecondsLeft ?? 1.15)) * 1000) : 0);
  const animate = (now: number) => {
    if (disposed) return;
    const elapsed = now - startedAt;
    const fold = rolling ? reducedMotion ? 1 : smooth(elapsed / 300) : 0;
    panelMeshes.forEach(({ mesh, flat, folded, angle }) => {
      mesh.position.lerpVectors(flat, folded, fold);
      mesh.rotation.set(-Math.PI / 2 * (1 - fold), angle * fold, 0);
    });
    flatGuides.visible = fold < .02;
    core.visible = fold > .02;
    core.scale.setScalar(Math.max(.02, fold));
    if (rolling) {
      const tumble = Math.max(0, elapsed - 250);
      const landing = smooth((tumble - 540) / 310);
      die.rotation.set(
        tumble * .0105 * (1 - landing) + 2.4 * landing,
        tumble * .015 * (1 - landing) + 1.15 * landing,
        tumble * .007 * (1 - landing) + .28 * landing,
      );
      die.position.y = reducedMotion ? 0 : Math.sin(Math.min(tumble, 560) / 560 * Math.PI) * .48 * (1 - landing);
    } else {
      die.rotation.set(0, Math.sin(now / 1400) * .045, 0);
      die.position.y = 0;
    }
    renderer.render(scene, camera);
    animationFrame = window.requestAnimationFrame(animate);
  };
  animationFrame = window.requestAnimationFrame(animate);
  return {
    dispose() {
      disposed = true;
      window.cancelAnimationFrame(animationFrame);
      resizeObserver.disconnect();
      panelGeometry.dispose();
      core.geometry.dispose();
      materials.forEach((material) => material.dispose());
      textures.forEach((texture) => texture.dispose());
      renderer.dispose();
    },
  };
};
