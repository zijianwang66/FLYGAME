import * as THREE from "three";
import { GLTFLoader } from "three/addons/loaders/GLTFLoader.js";

const shell = document.querySelector(".control-shell");
const timeNode = document.querySelector("#systemTime");
const modeButtons = [...document.querySelectorAll(".mode-card")];
const selectedMode = document.querySelector("#selectedMode");
const selectedUav = document.querySelector("#selectedUav");
const selectedScene = document.querySelector("#selectedScene");
const selectedDifficulty = document.querySelector("#selectedDifficulty");
const sceneSelect = document.querySelector("#sceneSelect");
const difficultySelect = document.querySelector("#difficultySelect");
const terminalReadout = document.querySelector("#terminalReadout");
const startButton = document.querySelector("#startSimulation");
const container = document.querySelector("#main02Drone");

const uavData = [
  {
    name: "UAV-X01",
    type: "TRAINING TYPE",
    speed: "18 M/S",
    endurance: "32 MIN",
    level: "BASIC",
    payload: "1.2 KG"
  },
  {
    name: "UAV-R17",
    type: "INSPECTION TYPE",
    speed: "24 M/S",
    endurance: "41 MIN",
    level: "NORMAL",
    payload: "1.8 KG"
  },
  {
    name: "UAV-M04",
    type: "MISSION TYPE",
    speed: "21 M/S",
    endurance: "38 MIN",
    level: "ADVANCED",
    payload: "2.4 KG"
  }
];

let currentUav = 0;
let scene;
let camera;
let renderer;
let modelRoot;
let mixer;
let resizeObserver;
let animationFrameId = 0;
let baseScale = 1;
let targetScale = 1;
let targetYaw = -0.35;
let isDragging = false;
let lastPointerX = 0;
let launchBoostUntil = 0;

const clock = new THREE.Clock();

function setText(selector, value) {
  const node = document.querySelector(selector);
  if (node) node.textContent = value;
}

function updateTime() {
  const now = new Date();
  const pad = (value) => String(value).padStart(2, "0");
  if (timeNode) {
    timeNode.textContent = `${now.getFullYear()}.${pad(now.getMonth() + 1)}.${pad(now.getDate())} ${pad(now.getHours())}:${pad(now.getMinutes())}:${pad(now.getSeconds())}`;
  }
}

function setMode(mode) {
  const label = mode === "basic" ? "基础训练" : "任务训练";
  modeButtons.forEach((button) => {
    const active = button.dataset.mode === mode;
    button.classList.toggle("is-active", active);
    button.setAttribute("aria-pressed", String(active));
  });
  if (shell) shell.dataset.mode = mode;
  if (selectedMode) selectedMode.textContent = label;
  if (terminalReadout) terminalReadout.textContent = `${label}已选择，训练参数等待确认。`;
  targetYaw = mode === "basic" ? -0.18 : -0.42;
}

function updateUav(index) {
  currentUav = (index + uavData.length) % uavData.length;
  const uav = uavData[currentUav];
  setText("#uavName", uav.name);
  setText("#uavType", uav.type);
  setText("#uavIndex", `${String(currentUav + 1).padStart(2, "0")} / ${String(uavData.length).padStart(2, "0")}`);
  setText("#specSpeed", uav.speed);
  setText("#specEndurance", uav.endurance);
  setText("#specLevel", uav.level);
  setText("#specPayload", uav.payload);
  if (selectedUav) selectedUav.textContent = uav.name;
  if (terminalReadout) terminalReadout.textContent = `${uav.name} 已载入。拖动模型可旋转，滚轮可缩放。`;
}

function syncConfig() {
  if (selectedScene && sceneSelect) selectedScene.textContent = sceneSelect.value;
  if (selectedDifficulty && difficultySelect) selectedDifficulty.textContent = difficultySelect.value;
}

function triggerLaunch() {
  if (!shell) return;
  shell.classList.remove("is-launching");
  void shell.offsetWidth;
  shell.classList.add("is-launching");
  launchBoostUntil = performance.now() + 1400;
  if (terminalReadout) terminalReadout.textContent = "系统检测中，训练环境正在部署。";
  window.setTimeout(() => {
    shell.classList.remove("is-launching");
    if (terminalReadout) terminalReadout.textContent = "部署完成，正在进入仿真训练。";
  }, 1000);
}

function frameModel(model) {
  const box = new THREE.Box3().setFromObject(model);
  const size = box.getSize(new THREE.Vector3());
  const center = box.getCenter(new THREE.Vector3());
  const maxDim = Math.max(size.x, size.y, size.z);
  if (!Number.isFinite(maxDim) || maxDim <= 0) return;

  model.position.sub(center);
  baseScale = 2.85 / maxDim;
  modelRoot.scale.setScalar(baseScale);
  camera.position.set(0.72, 0.62, 4.9);
  camera.lookAt(0, 0, 0);
  camera.updateProjectionMatrix();
}

function applyDroneMaterial(root) {
  root.traverse((object) => {
    if (!object.isMesh) return;
    const original = object.material;
    object.material = new THREE.MeshStandardMaterial({
      color: 0x3b454b,
      metalness: 0.48,
      roughness: 0.64,
      map: original?.map || null,
      normalMap: original?.normalMap || null
    });
  });
}

function resizeRenderer() {
  if (!container || !renderer || !camera) return;
  const width = Math.max(container.clientWidth, 1);
  const height = Math.max(container.clientHeight, 1);
  camera.aspect = width / height;
  camera.updateProjectionMatrix();
  renderer.setSize(width, height, false);
}

function initDroneScene() {
  if (!container || container.querySelector("canvas")) return;

  scene = new THREE.Scene();
  camera = new THREE.PerspectiveCamera(35, 1, 0.01, 100);
  renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true, powerPreference: "high-performance" });
  renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
  renderer.setClearColor(0x000000, 0);
  renderer.outputColorSpace = THREE.SRGBColorSpace;
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 1.06;
  container.appendChild(renderer.domElement);

  scene.add(new THREE.HemisphereLight(0xdff9ff, 0x061018, 1.35));

  const keyLight = new THREE.DirectionalLight(0xeefcff, 2.25);
  keyLight.position.set(3.4, 3.8, 4.2);
  scene.add(keyLight);

  const rimLight = new THREE.DirectionalLight(0x7deaff, 1.25);
  rimLight.position.set(-3.5, 1.7, -2.4);
  scene.add(rimLight);

  modelRoot = new THREE.Group();
  modelRoot.rotation.set(0.08, targetYaw, 0);
  scene.add(modelRoot);

  resizeObserver = new ResizeObserver(resizeRenderer);
  resizeObserver.observe(container);
  resizeRenderer();

  const loader = new GLTFLoader();
  const modelSrc = container.dataset.modelSrc || "/public/model/drone.glb";
  loader.load(
    modelSrc,
    (gltf) => {
      const model = gltf.scene;
      applyDroneMaterial(model);
      modelRoot.add(model);
      frameModel(model);
      mixer = new THREE.AnimationMixer(model);
      gltf.animations.forEach((clip) => {
        const action = mixer.clipAction(clip);
        action.reset();
        action.setLoop(THREE.LoopRepeat, Infinity);
        action.play();
      });
      if (terminalReadout) terminalReadout.textContent = "无人机模型加载完成。";
    },
    undefined,
    () => {
      if (terminalReadout) terminalReadout.textContent = "无人机模型加载失败，请检查模型路径。";
    }
  );

  renderer.domElement.addEventListener("pointerdown", (event) => {
    isDragging = true;
    lastPointerX = event.clientX;
    renderer.domElement.setPointerCapture(event.pointerId);
  });

  renderer.domElement.addEventListener("pointermove", (event) => {
    if (!isDragging) return;
    const delta = event.clientX - lastPointerX;
    lastPointerX = event.clientX;
    targetYaw += delta * 0.008;
  });

  renderer.domElement.addEventListener("pointerup", () => {
    isDragging = false;
  });

  renderer.domElement.addEventListener("pointerleave", () => {
    isDragging = false;
  });

  renderer.domElement.addEventListener("wheel", (event) => {
    event.preventDefault();
    targetScale = THREE.MathUtils.clamp(targetScale + (event.deltaY > 0 ? -0.04 : 0.04), 0.88, 1.16);
  }, { passive: false });

  renderer.domElement.addEventListener("dblclick", () => {
    targetYaw = -0.35;
    targetScale = 1;
  });

  animate();
}

function animate() {
  animationFrameId = requestAnimationFrame(animate);
  const delta = clock.getDelta();
  const elapsed = clock.elapsedTime;
  const isBoosting = performance.now() < launchBoostUntil;

  if (mixer) mixer.update(delta * (isBoosting ? 2.2 : 0.5));

  if (modelRoot) {
    modelRoot.rotation.y += (targetYaw - modelRoot.rotation.y) * 0.08;
    modelRoot.rotation.z = Math.sin(elapsed * 0.72) * 0.016;
    modelRoot.rotation.x = 0.08 + Math.sin(elapsed * 0.55) * 0.01;
    modelRoot.position.y = Math.sin(elapsed * 0.9) * 0.032 + (isBoosting ? 0.08 : 0);
    modelRoot.scale.setScalar(baseScale * targetScale);
  }

  renderer.render(scene, camera);
}

function cleanup() {
  cancelAnimationFrame(animationFrameId);
  if (resizeObserver) resizeObserver.disconnect();
  if (mixer) mixer.stopAllAction();
  if (modelRoot) {
    modelRoot.traverse((object) => {
      if (object.geometry) object.geometry.dispose();
      if (object.material) object.material.dispose();
    });
  }
  if (renderer) {
    renderer.dispose();
    renderer.domElement.remove();
  }
}

modeButtons.forEach((button) => {
  button.addEventListener("click", () => setMode(button.dataset.mode || "mission"));
});

document.querySelector("#prevUav")?.addEventListener("click", () => updateUav(currentUav - 1));
document.querySelector("#nextUav")?.addEventListener("click", () => updateUav(currentUav + 1));
sceneSelect?.addEventListener("change", syncConfig);
difficultySelect?.addEventListener("change", syncConfig);
startButton?.addEventListener("click", triggerLaunch);

updateTime();
window.setInterval(updateTime, 1000);
syncConfig();
updateUav(0);
initDroneScene();
window.addEventListener("beforeunload", cleanup, { once: true });
