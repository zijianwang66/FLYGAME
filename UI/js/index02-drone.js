import * as THREE from "three";
import { GLTFLoader } from "three/addons/loaders/GLTFLoader.js";

const container = document.querySelector("#drone-container");
const stage = document.querySelector(".drone-stage");
const statusText = document.querySelector("#droneStatusText");
const systemText = document.querySelector("#droneSystemText");
const kickerText = document.querySelector("#droneKicker");

let scene;
let camera;
let renderer;
let loader;
let mixer;
let flightRoot;
let droneModel;
let resizeObserver;
let animationFrameId = 0;
let actions = [];
let isLoaded = false;
let isFlying = false;
let isHovering = false;
let takeoffElapsed = 0;
let flightRootBaseScale = 1;

const clock = new THREE.Clock();
const raycaster = new THREE.Raycaster();
const pointer = new THREE.Vector2();

const baseFlightY = 0;
const takeoffHeight = 0.14;
const takeoffDuration = 1.2;
const hoverAmplitude = 0.012;

const STATUS = {
  loading: ["Loading aircraft...", "SYSTEM LOADING", "standby"],
  standby: ["Click aircraft to launch", "SYSTEM STANDBY", "standby"],
  hover: ["Confirm aircraft launch", "READY TO LAUNCH", "hover"],
  active: ["Aircraft active", "FLIGHT LOOP ACTIVE", "active"],
  noAnimation: ["No animation detected in model", "ANIMATION UNAVAILABLE", "error"],
  loadError: ["Aircraft model load failed", "MODEL LOAD ERROR", "error"]
};

function setDroneStatus(message, system, state = "standby") {
  if (statusText) statusText.textContent = message;
  if (systemText) systemText.textContent = system;
  if (kickerText) kickerText.textContent = state === "active" ? "AIRCRAFT ACTIVE" : "AIRCRAFT DISPLAY";
  if (!stage) return;
  stage.classList.toggle("is-active", state === "active");
  stage.classList.toggle("is-hovering", state === "hover");
  stage.classList.toggle("is-error", state === "error");
}

function updatePointerFromEvent(event) {
  const rect = renderer.domElement.getBoundingClientRect();
  pointer.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
  pointer.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
}

function pointerHitsDrone(event) {
  if (!isLoaded || !droneModel || !camera || !renderer) return false;
  updatePointerFromEvent(event);
  raycaster.setFromCamera(pointer, camera);
  return raycaster.intersectObject(droneModel, true).length > 0;
}

function updateHoverState(nextHovering) {
  if (isHovering === nextHovering) return;
  isHovering = nextHovering;
  renderer.domElement.style.cursor = isHovering ? "pointer" : "default";
  if (flightRoot) {
    flightRoot.scale.setScalar(flightRootBaseScale * (isHovering && !isFlying ? 1.01 : 1));
  }

  if (!isFlying && actions.length > 0) {
    setDroneStatus(...(isHovering ? STATUS.hover : STATUS.standby));
  }
}

function frameDroneModel(model) {
  const box = new THREE.Box3().setFromObject(model);
  const size = box.getSize(new THREE.Vector3());
  const center = box.getCenter(new THREE.Vector3());
  const maxDim = Math.max(size.x, size.y, size.z);

  if (!Number.isFinite(maxDim) || maxDim <= 0) {
    console.warn("Drone model has no measurable geometry. The GLB may be empty.");
    return;
  }

  model.position.sub(center);

  const targetSize = 2.35;
  flightRootBaseScale = targetSize / maxDim;
  flightRoot.scale.setScalar(flightRootBaseScale);

  const framedSize = targetSize;
  const fov = THREE.MathUtils.degToRad(camera.fov);
  const distance = framedSize / (2 * Math.tan(fov / 2));
  camera.position.set(distance * 0.18, distance * 0.36, distance * 1.42);
  camera.near = Math.max(distance / 100, 0.01);
  camera.far = distance * 100;
  camera.lookAt(0, 0, 0);
  camera.updateProjectionMatrix();
}

function startFlight() {
  if (!isLoaded || isFlying || !mixer) return;

  if (actions.length === 0) {
    setDroneStatus(...STATUS.noAnimation);
    return;
  }

  isFlying = true;
  takeoffElapsed = 0;
  clock.getDelta();

  actions.forEach((action) => {
    action.reset();
    action.enabled = true;
    action.setEffectiveTimeScale(1);
    action.setEffectiveWeight(1);
    action.setLoop(THREE.LoopRepeat, Infinity);
    action.clampWhenFinished = false;
    action.play();
  });

  setDroneStatus(...STATUS.active);
}

function updateFlightRoot(delta) {
  if (!flightRoot) return;

  if (isFlying) {
    takeoffElapsed += delta;
    const progress = Math.min(takeoffElapsed / takeoffDuration, 1);
    const easedProgress = 1 - Math.pow(1 - progress, 3);
    const hoverOffset = Math.sin(clock.elapsedTime * 2.2) * hoverAmplitude;
    flightRoot.position.y = baseFlightY + takeoffHeight * easedProgress + hoverOffset;
    return;
  }

  flightRoot.position.y = baseFlightY;
}

function resizeRenderer() {
  if (!container || !renderer || !camera) return;

  const width = Math.max(container.clientWidth, 1);
  const height = Math.max(container.clientHeight, 1);

  camera.aspect = width / height;
  camera.updateProjectionMatrix();
  renderer.setSize(width, height, false);
}

function disposeMaterial(material) {
  Object.keys(material).forEach((key) => {
    const value = material[key];
    if (value && value.isTexture) value.dispose();
  });
  material.dispose();
}

function cleanup() {
  cancelAnimationFrame(animationFrameId);
  if (resizeObserver) resizeObserver.disconnect();

  if (renderer?.domElement) {
    renderer.domElement.removeEventListener("pointermove", handlePointerMove);
    renderer.domElement.removeEventListener("pointerdown", handlePointerDown);
    renderer.domElement.removeEventListener("pointerleave", handlePointerLeave);
  }

  if (mixer) mixer.stopAllAction();

  if (flightRoot) {
    flightRoot.traverse((object) => {
      if (object.geometry) object.geometry.dispose();
      if (object.material) {
        if (Array.isArray(object.material)) {
          object.material.forEach(disposeMaterial);
        } else {
          disposeMaterial(object.material);
        }
      }
    });
  }

  if (renderer) {
    renderer.dispose();
    renderer.domElement.remove();
  }
}

function handlePointerMove(event) {
  updateHoverState(pointerHitsDrone(event));
}

function handlePointerDown(event) {
  if (pointerHitsDrone(event)) startFlight();
}

function handlePointerLeave() {
  updateHoverState(false);
}

async function loadDrone() {
  const modelSrc = container.dataset.modelSrc || "/public/model/drone.glb";
  loader = new GLTFLoader();

  try {
    const gltf = await loader.loadAsync(modelSrc);
    droneModel = gltf.scene;
    flightRoot = new THREE.Group();
    flightRoot.name = "DroneFlightRoot";
    flightRoot.add(droneModel);
    scene.add(flightRoot);

    frameDroneModel(droneModel);

    mixer = new THREE.AnimationMixer(gltf.scene);
    actions = gltf.animations.map((clip) => mixer.clipAction(clip));
    actions.forEach((action) => {
      action.stop();
      action.reset();
      action.enabled = false;
    });
    mixer.setTime(0);
    mixer.update(0);

    console.log(
      "Loaded animations:",
      gltf.animations.map((clip) => ({
        name: clip.name,
        duration: clip.duration
      }))
    );

    isLoaded = true;

    if (actions.length === 0) {
      console.warn("No animation detected in model:", modelSrc);
      setDroneStatus(...STATUS.noAnimation);
      return;
    }

    setDroneStatus(...STATUS.standby);
  } catch (error) {
    console.error("Aircraft model load failed:", error);
    setDroneStatus(...STATUS.loadError);
  }
}

function animate() {
  animationFrameId = requestAnimationFrame(animate);
  const delta = clock.getDelta();

  if (isFlying && mixer) {
    mixer.update(delta);
  }

  updateFlightRoot(delta);
  renderer.render(scene, camera);
}

function initDroneStage() {
  if (!container || container.querySelector("canvas")) return;

  scene = new THREE.Scene();
  camera = new THREE.PerspectiveCamera(38, 1, 0.01, 100);
  camera.position.set(0.75, 1.1, 4.2);

  renderer = new THREE.WebGLRenderer({
    antialias: true,
    alpha: true,
    powerPreference: "high-performance"
  });
  renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
  renderer.setClearColor(0x000000, 0);
  renderer.outputColorSpace = THREE.SRGBColorSpace;
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 1.05;
  container.appendChild(renderer.domElement);

  const hemiLight = new THREE.HemisphereLight(0xddeeff, 0x111820, 1.2);
  scene.add(hemiLight);

  const keyLight = new THREE.DirectionalLight(0xf2fbff, 2.4);
  keyLight.position.set(3.5, 4.2, 3.8);
  scene.add(keyLight);

  const rimLight = new THREE.DirectionalLight(0x7fb6c9, 0.9);
  rimLight.position.set(-3.2, 1.8, -2.4);
  scene.add(rimLight);

  resizeObserver = new ResizeObserver(resizeRenderer);
  resizeObserver.observe(container);
  resizeRenderer();

  renderer.domElement.addEventListener("pointermove", handlePointerMove, { passive: true });
  renderer.domElement.addEventListener("pointerdown", handlePointerDown);
  renderer.domElement.addEventListener("pointerleave", handlePointerLeave, { passive: true });

  loadDrone();
  animate();
}

window.addEventListener("beforeunload", cleanup, { once: true });
initDroneStage();
