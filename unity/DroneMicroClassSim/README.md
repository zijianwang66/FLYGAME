# Drone MicroClass Simulator

Unity 6 drone flight simulator prototype for a micro-classroom experience.

## Features

- Three selectable drone profiles: Quad Racer, DJI Inspire, and Red Guard.
- Keyboard/gamepad flight input with assisted balance and altitude hold.
- Per-aircraft mass, lift, torque, damping, PID, and visual rotor settings.
- Follow, fixed observer, overhead, and nose-camera views.
- HUD for altitude, speed, attitude, current aircraft, camera mode, and aircraft parameters.
- Wild valley training environment with lightweight classroom-friendly assets.

## Controls

- `W/S`: pitch forward/back
- `A/D`: roll left/right
- `Q/E`: yaw left/right
- `Space/Ctrl`: climb/descend
- `Tab` or `1/2/3`: switch aircraft
- `V`: switch camera
- `H`: toggle altitude hold
- `B`: toggle balance assist
- `R`: reset aircraft motion

## Project

- Unity version: `6000.3.8f1`
- Main scene: `Assets/DroneMicroClass/Scenes/MicroClassFlightDemo.unity`
- Flight tuning guide: `Assets/DroneMicroClass/Docs/FlightTuningGuide.md`

## Third-party assets

- Imported drone models are stored under `Assets/Imported/FlyAssets`.
- Additional nature scenery uses CC0 public resources where noted in the imported asset folder.
