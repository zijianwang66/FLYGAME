# Windows 64 位程序包

当前版本：2026-10-17，Unity 6000.3.8f1。

下载同目录的 `WindowsX64.zip`，完整解压后运行 `WindowsX64/DroneMicroClassSim.exe`。
请保留 EXE 旁的 `DroneMicroClassSim_Data`、`MonoBleedingEdge`、DLL 等文件。

- 包含矩形飞行、8 字飞行、森林和学校训练场景。
- `G`：固定人高观察视角，离地约 1.6 米，自动追踪飞机。
- 超过 18 米后平滑自动变焦，最多 8 倍；飞近时自动缩回。
- `C / V`：循环切换相机视角。

`WindowsX64.sha256` 用于校验下载文件。包内的 `build-manifest.json` 记录构建信息。
此程序包由 2026-10-17 的本地工程构建；构建基础提交与程序集校验值见包内清单。
可编辑工程位于仓库的 `unity/DroneMicroClassSim/`，打开步骤见该目录的 `README.md`。
