import * as THREE from "three";
import { GLTFLoader } from "three/addons/loaders/GLTFLoader.js";

const shell = document.querySelector(".free-shell");
const container = document.querySelector("#freeFlightScene");
const spawnOverlay = document.querySelector("#spawnOverlay");
const drawer = document.querySelector("#settingsDrawer");
const drawerMask = document.querySelector("#drawerMask");
const drawerTitle = document.querySelector("#drawerTitle");
const drawerKicker = document.querySelector("#drawerKicker");
const drawerReset = document.querySelector("#drawerReset");
const drawerApply = document.querySelector("#drawerApply");
const drawerCancel = document.querySelector("#drawerCancel");
const drawerClose = document.querySelector("#drawerClose");
const mapInfo = document.querySelector("#mapInfo");
const windArrow = document.querySelector("#windArrow");

const maps = {
  playground: {
    name: "操场训练区",
    code: "PLAYGROUND",
    info: "地形简单，障碍物较少，视线开阔，适合初次自由飞行。",
    ground: 0x385f45,
    accent: 0x9b4f4b,
    spawns: [
      { id: "A", name: "中央草坪", position: new THREE.Vector3(0, 0.08, 0) },
      { id: "B", name: "跑道起点", position: new THREE.Vector3(-2.35, 0.08, 1.15) },
      { id: "C", name: "看台前方", position: new THREE.Vector3(2.05, 0.08, -1.1) }
    ]
  },
  forest: {
    name: "森林飞行区",
    code: "FOREST",
    info: "障碍物较多，可进行低空飞行，适合避障训练与自由探索。",
    ground: 0x244a38,
    accent: 0x7b6b4e,
    spawns: [
      { id: "A", name: "林间空地", position: new THREE.Vector3(-.25, 0.1, .25) },
      { id: "B", name: "木桥入口", position: new THREE.Vector3(1.9, 0.1, 1.1) },
      { id: "C", name: "山坡平台", position: new THREE.Vector3(-2.05, 0.42, -1.25) }
    ]
  }
};

const uavs = [
  { name: "UAV-X01", type: "训练型无人机", speed: "15 M/S", endurance: "32 MIN", level: "基础" },
  { name: "UAV-R17", type: "巡检型无人机", speed: "18 M/S", endurance: "38 MIN", level: "标准" },
  { name: "UAV-M04", type: "机动型无人机", speed: "22 M/S", endurance: "28 MIN", level: "进阶" }
];

const assistModes = [
  { name: "新手辅助", items: ["自动姿态稳定", "高度保持", "障碍提醒", "小地图开启"] },
  { name: "标准控制", items: ["自动姿态稳定", "小地图开启", "关闭操作提示"] },
  { name: "完全手动", items: ["关闭高度保持", "关闭操作提示", "保留基本飞行数据"] }
];

const windDirections = [
  ["北", 0],
  ["东北", 45],
  ["东", 90],
  ["东南", 135],
  ["南", 180],
  ["西南", 225],
  ["西", 270],
  ["西北", 315]
];

const defaults = {
  map: "playground",
  uav: "UAV-X01",
  spawn: "中央草坪",
  initialState: "地面待机",
  speedLimit: 8,
  heightLimit: 50,
  batteryMode: "无限电量",
  collision: "开启",
  assist: "标准控制",
  weather: "sunny",
  windSpeed: 2,
  windDirection: "东北",
  intensity: "",
  visibility: "清晰"
};

const state = { ...defaults };
let draft = { ...state };
let activePanel = "drone";

let scene;
let camera;
let renderer;
let resizeObserver;
let animationFrameId = 0;
let worldRoot;
let mapRoot;
let droneRoot;
let droneModel;
let mixer;
let targetYaw = -.58;
let targetPitch = .58;
let targetDistance = 6.8;
let currentView = "3d";
let isDragging = false;
let lastPointer = { x: 0, y: 0 };

const clock = new THREE.Clock();
const markerAnchors = [];

function setText(selector, value) {
  const node = document.querySelector(selector);
  if (node) node.textContent = value;
}

function weatherLabel(value) {
  return value === "rainy" ? "雨天" : value === "foggy" ? "雾天" : "晴天";
}

function getCurrentMap() {
  return maps[state.map] || maps.playground;
}

function getSpawn() {
  return getCurrentMap().spawns.find((spawn) => spawn.name === state.spawn) || getCurrentMap().spawns[0];
}

function updateSummaries() {
  const currentMap = getCurrentMap();
  const environment = `${weatherLabel(state.weather)} / ${state.windDirection}风 / ${state.windSpeed} M/S`;

  setText("#previewTitle", currentMap.name);
  setText("#summaryMap", currentMap.name);
  setText("#summaryUav", state.uav);
  setText("#summarySpawn", state.spawn);
  setText("#summaryWeather", weatherLabel(state.weather));
  setText("#summaryWind", `${state.windSpeed} M/S`);
  setText("#summaryAssist", state.assist);
  setText("#barMap", currentMap.name);
  setText("#barUav", state.uav);
  setText("#barSpawn", state.spawn);
  setText("#barEnvironment", environment);
  setText("#weatherBadge", weatherLabel(state.weather));
  setText("#spawnBadge", state.spawn);
  setText("#speedValue", `${draft.speedLimit} M/S`);
  setText("#heightValue", `${draft.heightLimit} M`);
  setText("#windValue", `${draft.windSpeed} M/S`);
  if (mapInfo) mapInfo.textContent = currentMap.info;
  if (shell) {
    shell.dataset.map = state.map;
    shell.dataset.weather = state.weather;
  }
  updateWindArrow();
}

function updateWindArrow() {
  if (!windArrow) return;
  const direction = windDirections.find(([label]) => label === state.windDirection);
  const angle = direction ? direction[1] : 45;
  windArrow.style.transform = `rotate(${angle}deg)`;
}

function copyStateToDraft() {
  draft = { ...state };
}

function syncControlStates(source = draft) {
  document.querySelectorAll("[data-setting]").forEach((group) => {
    const key = group.dataset.setting;
    group.querySelectorAll("button").forEach((button) => {
      button.classList.toggle("is-active", source[key] === button.dataset.value);
    });
  });

  document.querySelectorAll("#uavChoices .choice-card").forEach((button) => {
    button.classList.toggle("is-active", source.uav === button.dataset.value);
  });
  document.querySelectorAll("#assistChoices .assist-card").forEach((button) => {
    button.classList.toggle("is-active", source.assist === button.dataset.value);
  });
  document.querySelectorAll("#weatherChoices button").forEach((button) => {
    button.classList.toggle("is-active", source.weather === button.dataset.value);
  });
  document.querySelectorAll("#windDirectionChoices button").forEach((button) => {
    button.classList.toggle("is-active", source.windDirection === button.dataset.value);
  });
  document.querySelectorAll("#weatherIntensityChoices button").forEach((button) => {
    button.classList.toggle("is-active", source.intensity === button.dataset.value);
  });

  const speedLimit = document.querySelector("#speedLimit");
  const heightLimit = document.querySelector("#heightLimit");
  const windSpeed = document.querySelector("#windSpeed");
  if (speedLimit) speedLimit.value = source.speedLimit;
  if (heightLimit) heightLimit.value = source.heightLimit;
  if (windSpeed) windSpeed.value = source.windSpeed;

  updateIntensityOptions(source.weather, source.intensity);
  setText("#speedValue", `${source.speedLimit} M/S`);
  setText("#heightValue", `${source.heightLimit} M`);
  setText("#windValue", `${source.windSpeed} M/S`);
}

function renderUavChoices() {
  const mount = document.querySelector("#uavChoices");
  if (!mount) return;
  mount.innerHTML = uavs.map((uav) => `
    <button class="choice-card" type="button" data-value="${uav.name}">
      <span class="uav-preview" aria-hidden="true"></span>
      <span>
        <em>${uav.name}</em>
        <strong>${uav.type}</strong>
        <small>用户只选择型号，性能参数保持系统预设</small>
        <span class="choice-specs">
          <b>最大速度 ${uav.speed}</b>
          <b>续航时间 ${uav.endurance}</b>
          <b>控制难度 ${uav.level}</b>
        </span>
      </span>
    </button>
  `).join("");

  mount.querySelectorAll(".choice-card").forEach((button) => {
    button.addEventListener("click", () => {
      draft.uav = button.dataset.value;
      syncControlStates();
    });
  });
}

function renderAssistChoices() {
  const mount = document.querySelector("#assistChoices");
  if (!mount) return;
  mount.innerHTML = assistModes.map((mode) => `
    <button class="assist-card" type="button" data-value="${mode.name}">
      <strong>${mode.name}</strong>
      <ul>${mode.items.map((item) => `<li>${item}</li>`).join("")}</ul>
    </button>
  `).join("");

  mount.querySelectorAll(".assist-card").forEach((button) => {
    button.addEventListener("click", () => {
      draft.assist = button.dataset.value;
      syncControlStates();
    });
  });
}

function renderWindDirections() {
  const mount = document.querySelector("#windDirectionChoices");
  if (!mount) return;
  mount.innerHTML = windDirections.map(([label]) => `<button type="button" data-value="${label}">${label}</button>`).join("");
  mount.querySelectorAll("button").forEach((button) => {
    button.addEventListener("click", () => {
      draft.windDirection = button.dataset.value;
      syncControlStates();
    });
  });
}

function updateIntensityOptions(weather, selectedValue) {
  const group = document.querySelector("#intensityGroup");
  const mount = document.querySelector("#weatherIntensityChoices");
  if (!group || !mount) return;

  const options = weather === "rainy" ? ["小雨", "中雨"] : weather === "foggy" ? ["轻雾", "中雾", "浓雾"] : [];
  group.classList.toggle("is-hidden", options.length === 0);
  mount.innerHTML = options.map((option) => `<button type="button" data-value="${option}">${option}</button>`).join("");

  if (options.length > 0 && !options.includes(selectedValue)) {
    draft.intensity = options[0];
  }
  if (options.length === 0) {
    draft.intensity = "";
  }

  mount.querySelectorAll("button").forEach((button) => {
    button.classList.toggle("is-active", draft.intensity === button.dataset.value);
    button.addEventListener("click", () => {
      draft.intensity = button.dataset.value;
      syncControlStates();
    });
  });
}

function buildMaterials() {
  return {
    grass: new THREE.MeshStandardMaterial({ color: 0x3b6d49, roughness: .9 }),
    forestGrass: new THREE.MeshStandardMaterial({ color: 0x254f39, roughness: .95 }),
    track: new THREE.MeshStandardMaterial({ color: 0x914943, roughness: .84 }),
    asphalt: new THREE.MeshStandardMaterial({ color: 0x354248, roughness: .72 }),
    stand: new THREE.MeshStandardMaterial({ color: 0x83969d, metalness: .18, roughness: .66 }),
    platform: new THREE.MeshStandardMaterial({ color: 0x1b4854, metalness: .26, roughness: .45, emissive: 0x12313a }),
    accent: new THREE.MeshStandardMaterial({ color: 0x7deaff, emissive: 0x1a5462, roughness: .35 }),
    trunk: new THREE.MeshStandardMaterial({ color: 0x5a4534, roughness: .86 }),
    leaf: new THREE.MeshStandardMaterial({ color: 0x2f8059, roughness: .88 }),
    rock: new THREE.MeshStandardMaterial({ color: 0x69706e, roughness: .9 }),
    water: new THREE.MeshStandardMaterial({ color: 0x315d70, metalness: .08, roughness: .28, transparent: true, opacity: .74 })
  };
}

function addBox(root, material, size, position, rotation = [0, 0, 0]) {
  const mesh = new THREE.Mesh(new THREE.BoxGeometry(size[0], size[1], size[2]), material);
  mesh.position.set(position[0], position[1], position[2]);
  mesh.rotation.set(rotation[0], rotation[1], rotation[2]);
  root.add(mesh);
  return mesh;
}

function addCylinder(root, material, radiusTop, radiusBottom, height, position, segments = 24) {
  const mesh = new THREE.Mesh(new THREE.CylinderGeometry(radiusTop, radiusBottom, height, segments), material);
  mesh.position.set(position[0], position[1], position[2]);
  root.add(mesh);
  return mesh;
}

function createTree(root, materials, x, z, scale = 1) {
  addCylinder(root, materials.trunk, .045 * scale, .06 * scale, .55 * scale, [x, .31 * scale, z], 8);
  const crown = addCylinder(root, materials.leaf, .05 * scale, .28 * scale, .62 * scale, [x, .86 * scale, z], 7);
  crown.rotation.y = Math.random() * Math.PI;
}

function createLandingPad(root, materials, position) {
  const pad = addCylinder(root, materials.platform, .34, .34, .04, [position.x, position.y, position.z], 48);
  const ring = addCylinder(root, materials.accent, .39, .39, .012, [position.x, position.y + .03, position.z], 48);
  ring.scale.y = .06;
  return pad;
}

function createPlaygroundMap(root, materials) {
  addBox(root, materials.grass, [6.4, .06, 4.2], [0, 0, 0]);
  addBox(root, materials.track, [5.65, .025, 3.1], [0, .06, .1]);
  addBox(root, materials.grass, [3.45, .035, 1.75], [0, .09, .1]);
  addBox(root, materials.asphalt, [1.1, .07, 4.2], [2.92, .1, 0]);
  addBox(root, materials.stand, [.18, .34, 2.4], [2.38, .25, -.75]);
  addBox(root, materials.stand, [.18, .22, 2.0], [2.12, .19, -.75]);
  addBox(root, materials.accent, [.05, .05, .52], [-1.72, .15, -.82]);
  addBox(root, materials.accent, [.52, .05, .05], [-1.72, .42, -.82]);
  addBox(root, materials.accent, [.05, .05, .52], [1.72, .15, .98]);
  addBox(root, materials.accent, [.52, .05, .05], [1.72, .42, .98]);

  [[-2.75, -1.65], [-2.9, 1.45], [2.9, 1.55], [2.85, -1.85]].forEach(([x, z]) => {
    addCylinder(root, materials.stand, .025, .03, .9, [x, .47, z], 8);
    addBox(root, materials.accent, [.24, .035, .08], [x, .93, z]);
  });

  [[-2.85, -1.15], [-2.55, -.5], [-2.75, .72], [2.78, 1.15]].forEach(([x, z]) => createTree(root, materials, x, z, .86));
  maps.playground.spawns.forEach((spawn) => createLandingPad(root, materials, spawn.position));
}

function createForestMap(root, materials) {
  addBox(root, materials.forestGrass, [6.4, .08, 4.2], [0, 0, 0]);
  const path = addBox(root, materials.asphalt, [.58, .035, 5.2], [.55, .09, .05], [0, .36, -.28]);
  path.material = new THREE.MeshStandardMaterial({ color: 0x6c644f, roughness: .92 });
  addBox(root, materials.water, [.52, .025, 4.8], [1.86, .1, .05], [0, .1, -.18]);
  addBox(root, materials.trunk, [.92, .08, .34], [1.82, .17, .84], [0, -.22, 0]);
  addBox(root, materials.trunk, [.92, .08, .34], [1.98, .17, 1.1], [0, -.22, 0]);
  addBox(root, materials.rock, [.72, .36, .5], [-1.7, .24, .78], [0, .25, 0]);
  addBox(root, materials.rock, [.52, .3, .42], [2.38, .22, -1.24], [0, -.5, 0]);
  const slope = addBox(root, materials.forestGrass, [1.65, .32, 1.2], [-2.05, .22, -1.25], [0, 0, -.18]);
  slope.material = new THREE.MeshStandardMaterial({ color: 0x315d3e, roughness: .95 });

  [
    [-2.8, -1.65, 1.12], [-2.35, -.65, .95], [-2.75, 1.35, 1.22],
    [-1.05, -1.72, .9], [-.95, 1.68, 1.05], [.05, -1.5, 1.18],
    [.8, 1.58, .94], [1.25, -1.42, 1.12], [2.55, .28, .88],
    [2.72, 1.58, 1.18], [2.82, -1.72, 1.02], [-.38, .95, .86]
  ].forEach(([x, z, scale]) => createTree(root, materials, x, z, scale));

  maps.forest.spawns.forEach((spawn) => createLandingPad(root, materials, spawn.position));
}

function clearMap() {
  if (!mapRoot) return;
  mapRoot.traverse((object) => {
    if (object.geometry) object.geometry.dispose();
    if (object.material) object.material.dispose();
  });
  worldRoot.remove(mapRoot);
  mapRoot = null;
}

function rebuildMap() {
  if (!worldRoot) return;
  clearMap();
  markerAnchors.length = 0;
  mapRoot = new THREE.Group();
  const materials = buildMaterials();

  if (state.map === "forest") {
    createForestMap(mapRoot, materials);
  } else {
    createPlaygroundMap(mapRoot, materials);
  }

  getCurrentMap().spawns.forEach((spawn) => {
    markerAnchors.push({ id: spawn.id, name: spawn.name, position: spawn.position.clone() });
  });

  worldRoot.add(mapRoot);
  renderSpawnMarkers();
  placeDroneAtSpawn();
}

function renderSpawnMarkers() {
  if (!spawnOverlay) return;
  spawnOverlay.innerHTML = getCurrentMap().spawns.map((spawn) => `
    <button class="spawn-marker" type="button" data-spawn="${spawn.name}">
      <span>${spawn.id}</span>
      <em>SPAWN POINT</em>
    </button>
  `).join("");

  spawnOverlay.querySelectorAll(".spawn-marker").forEach((button) => {
    button.addEventListener("click", () => {
      state.spawn = button.dataset.spawn;
      placeDroneAtSpawn();
      updateSummaries();
      updateSpawnMarkerState();
    });
  });
  updateSpawnMarkerState();
}

function updateSpawnMarkerState() {
  document.querySelectorAll(".spawn-marker").forEach((button) => {
    button.classList.toggle("is-active", button.dataset.spawn === state.spawn);
  });
}

function placeDroneAtSpawn() {
  if (!droneRoot) return;
  const spawn = getSpawn();
  droneRoot.position.set(spawn.position.x, spawn.position.y + .22, spawn.position.z);
}

function frameDroneModel(model) {
  const box = new THREE.Box3().setFromObject(model);
  const size = box.getSize(new THREE.Vector3());
  const center = box.getCenter(new THREE.Vector3());
  const maxDim = Math.max(size.x, size.y, size.z);
  if (!Number.isFinite(maxDim) || maxDim <= 0) return;
  model.position.sub(center);
  droneRoot.scale.setScalar(.72 / maxDim);
}

function applyDroneMaterial(root) {
  root.traverse((object) => {
    if (!object.isMesh) return;
    const original = object.material;
    object.material = new THREE.MeshStandardMaterial({
      color: 0x3d484e,
      metalness: .5,
      roughness: .58,
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

function updateCamera() {
  if (!camera) return;
  if (currentView === "top") {
    camera.position.set(0, 7.8, .05);
    camera.lookAt(0, 0, 0);
    return;
  }
  const x = Math.sin(targetYaw) * targetDistance;
  const z = Math.cos(targetYaw) * targetDistance;
  const y = targetPitch * targetDistance;
  camera.position.set(x, y, z);
  camera.lookAt(0, 0, 0);
}

function projectMarkers() {
  if (!camera || !renderer || !spawnOverlay) return;
  const rect = renderer.domElement.getBoundingClientRect();
  markerAnchors.forEach((anchor) => {
    const button = spawnOverlay.querySelector(`[data-spawn="${anchor.name}"]`);
    if (!button) return;
    const projected = anchor.position.clone();
    projected.y += .55;
    projected.project(camera);
    const visible = projected.z < 1;
    button.style.left = `${(projected.x * .5 + .5) * rect.width}px`;
    button.style.top = `${(-projected.y * .5 + .5) * rect.height}px`;
    button.style.display = visible ? "grid" : "none";
  });
}

function setView(view) {
  currentView = view;
  document.querySelectorAll("[data-view]").forEach((button) => {
    button.classList.toggle("is-active", button.dataset.view === view);
  });
  setText("#previewModeLabel", view === "top" ? "TOP VIEW" : "3D PREVIEW");
}

function resetView() {
  targetYaw = -.58;
  targetPitch = .58;
  targetDistance = 6.8;
  setView("3d");
}

function initScene() {
  if (!container || container.querySelector("canvas")) return;

  scene = new THREE.Scene();
  scene.fog = new THREE.Fog(0x061018, 7, 13);
  camera = new THREE.PerspectiveCamera(38, 1, .01, 100);
  renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true, powerPreference: "high-performance" });
  renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
  renderer.setClearColor(0x000000, 0);
  renderer.outputColorSpace = THREE.SRGBColorSpace;
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 1.04;
  container.appendChild(renderer.domElement);

  scene.add(new THREE.HemisphereLight(0xdff9ff, 0x061018, 1.35));
  const keyLight = new THREE.DirectionalLight(0xeefcff, 2.35);
  keyLight.position.set(4.5, 6.2, 5.4);
  scene.add(keyLight);
  const rimLight = new THREE.DirectionalLight(0x7deaff, 1.2);
  rimLight.position.set(-4, 2, -3);
  scene.add(rimLight);

  worldRoot = new THREE.Group();
  scene.add(worldRoot);
  droneRoot = new THREE.Group();
  worldRoot.add(droneRoot);

  resizeObserver = new ResizeObserver(resizeRenderer);
  resizeObserver.observe(container);
  resizeRenderer();

  const loader = new GLTFLoader();
  loader.load(container.dataset.modelSrc || "/public/model/drone.glb", (gltf) => {
    droneModel = gltf.scene;
    applyDroneMaterial(droneModel);
    droneRoot.add(droneModel);
    frameDroneModel(droneModel);
    mixer = new THREE.AnimationMixer(droneModel);
    gltf.animations.forEach((clip) => mixer.clipAction(clip).setLoop(THREE.LoopRepeat, Infinity).play());
    placeDroneAtSpawn();
  });

  renderer.domElement.addEventListener("pointerdown", (event) => {
    isDragging = true;
    lastPointer = { x: event.clientX, y: event.clientY };
    renderer.domElement.setPointerCapture(event.pointerId);
  });

  renderer.domElement.addEventListener("pointermove", (event) => {
    if (!isDragging || currentView === "top") return;
    const dx = event.clientX - lastPointer.x;
    const dy = event.clientY - lastPointer.y;
    lastPointer = { x: event.clientX, y: event.clientY };
    targetYaw -= dx * .006;
    targetPitch = THREE.MathUtils.clamp(targetPitch + dy * .003, .35, .82);
  });

  renderer.domElement.addEventListener("pointerup", () => {
    isDragging = false;
  });

  renderer.domElement.addEventListener("pointerleave", () => {
    isDragging = false;
  });

  renderer.domElement.addEventListener("wheel", (event) => {
    event.preventDefault();
    targetDistance = THREE.MathUtils.clamp(targetDistance + (event.deltaY > 0 ? .35 : -.35), 4.6, 9.2);
  }, { passive: false });

  rebuildMap();
  animate();
}

function animate() {
  animationFrameId = requestAnimationFrame(animate);
  const delta = clock.getDelta();
  const elapsed = clock.elapsedTime;
  if (mixer) mixer.update(delta * .45);
  if (droneRoot) {
    droneRoot.rotation.y = Math.sin(elapsed * .24) * .08;
    droneRoot.position.y = getSpawn().position.y + .28 + Math.sin(elapsed * 1.6) * .035;
  }
  updateCamera();
  projectMarkers();
  renderer.render(scene, camera);
}

function cleanup() {
  cancelAnimationFrame(animationFrameId);
  if (resizeObserver) resizeObserver.disconnect();
  if (mixer) mixer.stopAllAction();
  if (worldRoot) {
    worldRoot.traverse((object) => {
      if (object.geometry) object.geometry.dispose();
      if (object.material) object.material.dispose();
    });
  }
  if (renderer) {
    renderer.dispose();
    renderer.domElement.remove();
  }
}

function openDrawer(panel) {
  activePanel = panel;
  copyStateToDraft();
  const titles = { drone: "无人机设置", flight: "飞行设置", environment: "环境设置" };
  const kickers = { drone: "AIRCRAFT CONFIG", flight: "FLIGHT CONFIG", environment: "ENVIRONMENT CONFIG" };
  if (drawerTitle) drawerTitle.textContent = titles[panel];
  if (drawerKicker) drawerKicker.textContent = kickers[panel];
  document.querySelectorAll(".drawer-content").forEach((content) => {
    content.classList.toggle("is-active", content.dataset.content === panel);
  });
  syncControlStates();
  if (drawerMask) {
    drawerMask.hidden = false;
    requestAnimationFrame(() => drawerMask.classList.add("is-visible"));
  }
  if (drawer) {
    drawer.classList.add("is-open");
    drawer.setAttribute("aria-hidden", "false");
  }
}

function closeDrawer() {
  if (drawer) {
    drawer.classList.remove("is-open");
    drawer.setAttribute("aria-hidden", "true");
  }
  if (drawerMask) {
    drawerMask.classList.remove("is-visible");
    window.setTimeout(() => {
      if (!drawer?.classList.contains("is-open")) drawerMask.hidden = true;
    }, 220);
  }
}

function applyDraft() {
  Object.assign(state, draft);
  updateSummaries();
  if (droneRoot) droneRoot.rotation.z = 0;
  closeDrawer();
}

function resetDraftForPanel() {
  if (activePanel === "drone") draft.uav = defaults.uav;
  if (activePanel === "flight") {
    draft.initialState = defaults.initialState;
    draft.speedLimit = defaults.speedLimit;
    draft.heightLimit = defaults.heightLimit;
    draft.batteryMode = defaults.batteryMode;
    draft.collision = defaults.collision;
    draft.assist = defaults.assist;
  }
  if (activePanel === "environment") {
    draft.weather = defaults.weather;
    draft.windSpeed = defaults.windSpeed;
    draft.windDirection = defaults.windDirection;
    draft.intensity = defaults.intensity;
    draft.visibility = defaults.visibility;
  }
  syncControlStates();
}

function resetAllDefaults() {
  Object.assign(state, defaults);
  copyStateToDraft();
  rebuildMap();
  updateMapCards();
  updateSummaries();
  syncControlStates(state);
}

function updateMapCards() {
  document.querySelectorAll(".map-card[data-map]").forEach((button) => {
    const active = button.dataset.map === state.map;
    button.classList.toggle("is-active", active);
    button.setAttribute("aria-pressed", String(active));
  });
}

function changeMap(mapKey) {
  if (!maps[mapKey]) return;
  state.map = mapKey;
  state.spawn = maps[mapKey].spawns[0].name;
  updateMapCards();
  rebuildMap();
  updateSummaries();
}

document.querySelectorAll(".map-card[data-map]").forEach((button) => {
  button.addEventListener("click", () => changeMap(button.dataset.map));
});

document.querySelectorAll(".config-entry").forEach((button) => {
  button.addEventListener("click", () => openDrawer(button.dataset.panel));
});

document.querySelectorAll("[data-setting]").forEach((group) => {
  group.addEventListener("click", (event) => {
    const button = event.target.closest("button[data-value]");
    if (!button) return;
    draft[group.dataset.setting] = button.dataset.value;
    syncControlStates();
  });
});

document.querySelector("#weatherChoices")?.addEventListener("click", (event) => {
  const button = event.target.closest("button[data-value]");
  if (!button) return;
  draft.weather = button.dataset.value;
  syncControlStates();
});

document.querySelector("#speedLimit")?.addEventListener("input", (event) => {
  draft.speedLimit = Number(event.target.value);
  setText("#speedValue", `${draft.speedLimit} M/S`);
});

document.querySelector("#heightLimit")?.addEventListener("input", (event) => {
  draft.heightLimit = Number(event.target.value);
  setText("#heightValue", `${draft.heightLimit} M`);
});

document.querySelector("#windSpeed")?.addEventListener("input", (event) => {
  draft.windSpeed = Number(event.target.value);
  setText("#windValue", `${draft.windSpeed} M/S`);
});

document.querySelectorAll("[data-view]").forEach((button) => {
  button.addEventListener("click", () => setView(button.dataset.view));
});

document.querySelector("#resetView")?.addEventListener("click", resetView);
document.querySelector("#resetDefaults")?.addEventListener("click", resetAllDefaults);
document.querySelector("#startFreeFlight")?.addEventListener("click", () => {
  if (!shell) return;
  shell.classList.remove("is-launching");
  void shell.offsetWidth;
  shell.classList.add("is-launching");
  window.setTimeout(() => shell.classList.remove("is-launching"), 900);
});

drawerReset?.addEventListener("click", resetDraftForPanel);
drawerApply?.addEventListener("click", applyDraft);
drawerCancel?.addEventListener("click", closeDrawer);
drawerClose?.addEventListener("click", closeDrawer);
drawerMask?.addEventListener("click", closeDrawer);
window.addEventListener("keydown", (event) => {
  if (event.key === "Escape") closeDrawer();
});
window.addEventListener("beforeunload", cleanup, { once: true });

renderUavChoices();
renderAssistChoices();
renderWindDirections();
syncControlStates();
updateSummaries();
initScene();
