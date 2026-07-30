# Drone Flight Tuning Guide

This project keeps the flight feel in `DroneProfile` assets. Tune the profile first; edit controller code only when you need new behavior.

## Where to tune

- Runtime profile assets:
  - `Assets/DroneMicroClass/Profiles/QuadRacer.asset`
  - `Assets/DroneMicroClass/Profiles/DJIInspire.asset`
  - `Assets/DroneMicroClass/Profiles/RedGuard.asset`
- Default generated values:
  - `Assets/DroneMicroClass/Editor/DroneMicroClassSceneBuilder.cs`
- Flight algorithm:
  - `Assets/DroneMicroClass/Scripts/SimpleFlightController.cs`
- Profile field definitions:
  - `Assets/DroneMicroClass/Scripts/DroneProfile.cs`

If you rebuild the demo scene from `Drone MicroClass/Build Demo Scene`, the builder writes its default values back into the profile assets. For long-term defaults, edit `DroneMicroClassSceneBuilder.cs`. For quick Play-mode tuning, edit the `.asset` profile in the Inspector.

## Quick feel recipes

### Make a drone more agile

- Raise `maxTiltAngle`.
- Raise `cyclicSensitivity`.
- Raise `inputResponseSpeed`.
- Raise `attitudeTorqueScale`.
- Raise `pitchTorque` and `rollTorque`.
- Lower `linearDamping`, `angularDamping`, `hoverBrakeAcceleration`, and `activeInputBrakeScale`.
- Raise `maxAngularVelocity`.

Expected result: fast tilt, fast acceleration, more drift after releasing input.

### Make a drone heavier and more stable

- Raise `mass`.
- Lower `maxTiltAngle`.
- Lower `cyclicSensitivity`.
- Lower `inputResponseSpeed`.
- Lower `attitudeTorqueScale`.
- Raise `linearDamping`, `angularDamping`, `hoverBrakeAcceleration`, and `activeInputBrakeScale`.
- Lower `maxAngularVelocity`.

Expected result: delayed response, slower turns, less sideways drift, easier hover.

### Make altitude changes faster

- Raise `maxLiftForce`.
- Raise `altitudeChangeSpeed`.
- Raise `verticalSensitivity`.
- Raise `altitudePid.outputLimit`.
- In manual throttle mode, raise `throttleResponse`.

Keep `maxLiftForce` comfortably above `mass * 9.81`. A lift/weight ratio below about `1.4x` will feel weak.

### Make yaw rotation stronger

- Raise `yawTorque`.
- Raise `yawSensitivity`.
- Raise `maxAngularVelocity`.
- Lower `angularDamping` or `angularRateDamping`.

If the drone drifts while yawing in place, raise `hoverBrakeAcceleration` and keep `activeInputBrakeScale` low enough that pitch/roll still moves freely.

## Key fields

- `mass`: Body weight. Higher values reduce acceleration for the same force.
- `maxLiftForce`: Maximum upward force. Bigger values climb faster and recover altitude better.
- `pitchTorque`, `rollTorque`, `yawTorque`: Manual torque authority and HUD reference values.
- `maxTiltAngle`: Assisted mode target tilt. This strongly affects horizontal acceleration.
- `throttleResponse`: Manual throttle change speed when altitude hold is off.
- `linearDamping`: Air resistance for movement.
- `angularDamping`: Air resistance for rotation.
- `angularRateDamping`: Extra controller damping against angular velocity.
- `hoverBrakeAcceleration`: Horizontal braking when balance control is on.
- `maxHoverBrakeForce`: Cap for hover brake force.
- `cyclicSensitivity`: Pitch/roll input multiplier.
- `yawSensitivity`: Yaw input multiplier.
- `verticalSensitivity`: Ascend/descend input multiplier.
- `inputResponseSpeed`: Stick smoothing. Low values feel heavy; high values feel snappy.
- `attitudeTorqueScale`: Multiplier for assisted pitch/roll correction torque.
- `activeInputBrakeScale`: Brake strength while pitch/roll input is held. Low values drift more.
- `maxAngularVelocity`: Maximum body rotation speed.
- `altitudeChangeSpeed`: Altitude target change speed in height control mode.
- `altitudePid`: Height hold correction.
- `pitchPid` and `rollPid`: Balance correction.
- `rotorVisualSpeed`: Visual-only maximum rotor speed in degrees per second.
- `rotorVisualAxis`: Local-axis used by the rotor mesh. The DJI Training Balanced profile uses `0,1,0`; the DJI Inspire Payload and Red Guard Agile profiles use `0,0,1`.
- `rotorGroundSpinRatio`: Rotor speed while sitting on the launch pad.
- `rotorFlightSpinRatio`: Minimum rotor speed once taking off or airborne.
- `rotorGroundSpinupSpeed`: How quickly rotors spool up before takeoff.
- `rotorFlightSpinupSpeed`: How quickly rotors snap to flight speed during takeoff.
- `rotorStrobeSampleCount`: Number of fixed angles used for the high-speed camera-sampling look. Set this to `0` or `1` for continuous rotation.
- `rotorStrobeStartRatio`: Rotor visual speed ratio where the sampled high-speed look begins.
- `rotorStrobeWobbleDegrees`: Tiny angle variation added to the sampled look so the rotor does not appear frozen.

For a stronger takeoff visual, raise `rotorVisualSpeed`, `rotorFlightSpinRatio`, and `rotorFlightSpinupSpeed`. If you want the high-speed rotor to look like it is flickering through only a few camera-sampled positions, keep `rotorStrobeSampleCount` around `5` to `8`. If a specific model's propellers appear to tumble instead of spinning flat, change `rotorVisualAxis` first.

## Camera and smoothness checks

- `SimpleFlightController` enables Rigidbody interpolation and continuous dynamic collision detection.
- `DroneCameraRig` follows the drone using yaw-only offset, so pitch/roll banking does not jerk the camera forward and backward.
- `FlightRuntimeSettings` sets a 60 FPS target and 60 Hz physics timestep for this micro-class simulator.
- `DroneHud` throttles text refresh to reduce small GC spikes from per-frame string formatting.

If forward flight still looks choppy, first lower `maxTiltAngle`, `cyclicSensitivity`, or `inputResponseSpeed` on the active profile before changing the camera code.

## Current intended profiles

- `DJI Training Balanced` (`QuadRacer.asset`): main DJI aircraft, neutral and forgiving, with moderate tilt authority and stable altitude hold.
- `DJI Inspire Payload`: heavier payload tune, lower tilt/rate limits, higher damping, and stronger altitude PID for smooth lift practice.
- `Red Guard Agile`: smallest airframe, highest tilt authority and input response, reduced yaw spin, low braking, and light damping for agile maneuver training.
