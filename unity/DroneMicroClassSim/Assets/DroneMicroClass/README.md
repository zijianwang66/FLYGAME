# Drone MicroClass Simulator

这是无人机微课堂仿真项目的第一版 MVP。

## 打开方式

使用 Unity 6000.3.8f1 打开：

`K:/FileK/unityprojects/DroneMicroClassSim`

主场景：

`Assets/DroneMicroClass/Scenes/DroneFigureEightTrainingUnity.unity`

## 操作

- `W/S` 或方向键上下：俯仰，前进/后退
- `A/D` 或方向键左右：横滚，左/右移动
- `Space`：升高
- `Left Shift`：降低
- `Q/E`：左/右偏航
- 单独按 `Q/E` 时会启用悬停刹车辅助，尽量保持原地自旋
- `H`：切换高度保持
- `T`：切换姿态稳定
- `Backspace`：重置无人机

## 已实现

- Unity 6 URP 项目
- 最小无人机 prefab
- 已导入旧 Fly 项目的无人机模型、贴图、材质、TerrainData、Skybox 和 URP Flares 资源
- `DroneProfile` 参数资产
- `DroneController` 键盘输入
- `SimpleFlightController` 升力、扭矩、混控和 PID
- HUD 显示高度、速度、姿态、模式、油门
- 一个带起降区、训练门、导入地形、Skybox 和模型展示台的演示场景

## 下一步建议

1. 调整 `TrainingQuad.asset` 中的质量、最大升力、PID 参数，找到舒服的课堂手感。
2. 添加第二个 `DroneProfile`，做“竞速机”和“重载机”的手感对比。
3. 给训练门加触发器和评分逻辑，形成第一节课的飞行任务。
