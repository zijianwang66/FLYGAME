# Drone MicroClass Simulator

Unity 6 drone flight simulator prototype for a micro-classroom experience.

## Features

- Three selectable drone profiles: Quad Racer, DJI Inspire, and Red Guard.
- Keyboard/gamepad flight input with assisted balance and altitude hold.
- Per-aircraft mass, lift, torque, damping, PID, and visual rotor settings.
- Follow, wide follow, start tower, nose camera, and fixed human-height observer views.
- HUD for altitude, speed, attitude, current aircraft, camera mode, and aircraft parameters.
- Wild valley training environment with lightweight classroom-friendly assets.

## Controls

- `W/S`: pitch forward/back
- `A/D`: roll left/right
- `Left / Right`: yaw left/right
- `Up / Down`: climb/descend
- `Tab` or `1/2/3`: switch aircraft
- `C / V`: cycle camera views
- `G`: fixed ground observer, 1.6 m above terrain and 8 m behind launch; smooth automatic zoom beyond 18 m (up to 8x)
- `H`: toggle altitude hold
- `T`: toggle balance assist
- `L`: land
- `Backspace`: reset aircraft motion
- Hold `S + D + Down + Left` for 2 seconds: arm/take off
- Hold `S + A + Down + Right` for 2 seconds: lock motors

## Project

- Unity version: `6000.3.8f1`
- Entry scene: `Assets/DroneMicroClass/Scenes/Login.unity`
- Training scenes: `矩形飞行训练`, `8字飞行`, `forest`, and `学校` under `Assets/DroneMicroClass/Scenes/`
- Flight tuning guide: `Assets/DroneMicroClass/Docs/FlightTuningGuide.md`

## Clone and open the editable project

Install Git LFS before cloning so model and Blender source files are downloaded in full:

```sh
git lfs install
git clone https://github.com/zijianwang66/FLYGAME.git
cd FLYGAME
git lfs pull
```

In Unity Hub, add `unity/DroneMicroClassSim` and open it with Unity `6000.3.8f1`.
The project includes `Assets`, `Packages`, `ProjectSettings`, Blender sources, and the local
`Unity-Skills-main/SkillsForUnity` package referenced by `Packages/manifest.json`.
Unity recreates `Library`, `Temp`, and other local caches when the project opens.

## Third-party assets

- Imported drone models are stored under `Assets/Imported/FlyAssets`.
- Additional nature scenery uses CC0 public resources where noted in the imported asset folder.
