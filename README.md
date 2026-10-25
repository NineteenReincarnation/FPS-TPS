# FPS & TPS

RimWorld **1.7** 右下角原生 FPS / 实际 TPS 计数器。作者 **anno**，Mod ID **`anno.fpsandtps`**。

## 安装

从 [Releases](https://github.com/NineteenReincarnation/FPS-TPS/releases) 下载 `FPS-TPS-v1.0.0-RimWorld-1.7.zip`，将其中的 `FPS&TPS` 文件夹解压到 `RimWorld/Mods`。安装并启用 [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) 的 1.7 版本，启用本 Mod 后重启。建议顺序：Harmony → Core / DLC → FPS & TPS。不需要 DLC 或开发者模式。

打开 **选项 → Mod 设置 → FPS & TPS**，可使用简洁/详细预设或逐项调整。设置即时生效，关闭 Mod 设置时保存。不向存档写入数据，可随时添加或移除。

## 功能

- 原生时间控制按钮上方显示指标，先为原生日期、天气、信件等布局预留高度。
- FPS、实际 TPS、当前目标 TPS、TPS 达标百分比、帧时间、tick 执行耗时分别可选。
- FPS 和 TPS 独立刷新间隔，范围 0.1–5 秒；0–2 位小数。
- 地图/世界地图显示、暂停隐藏、原版调试计数器去重、悬停说明。
- 单行/双行、FPS/TPS 顺序、原生字体、自动宽度、固定宽度、行高、右侧留白、间距、背景不透明度。
- 三种配色、FPS 正常/警告阈值、TPS 正常/警告达成率。
- 简体中文/英文界面，恢复默认、简洁和详细预设，以及完整统计方式与兼容说明。

暂停时 TPS 显示“已暂停”；恢复、变速、切换存档或长时间未绘制后，显示 `—` 等待完整的新窗口。默认先 FPS 后 TPS，每行 26 UI 单位，保持原生外观。TPS 括号为当前目标，不是历史峰值。

## 统计与性能

FPS 读取 `Root.AverageFrameTime`；实际 TPS 为 `TicksGame` 差值除以高精度真实经过秒数，**不是**根据 tick 耗时反推的理论 TPS。目标读取当前游戏倍率，因此兼容修改该 getter 的速度 Mod；目标的强制降速/空闲提速变化在 TPS 间隔刷新。帧时间包括限帧等待，tick 耗时使用游戏内置平均统计。

只有三个 UI Harmony 补丁：共享时间控制栏后置补丁，以及两个原版调试计数器前置补丁。无逐 tick 补丁、后台线程、轮询任务、逐 Pawn 扫描或存档组件。采样在重绘时每帧最多一次；文本只在显示数值变化时生成，文字宽度只在内容或字体变化时测量。关闭指标即停止对应采集。目标倍率仅在采样间隔或状态变化时读取，避免游戏 getter 内部的空闲状态检查变为每帧工作。悬停说明延迟生成，透明背景不增加背景绘制。

`Tests` 链接实际采样/文本缓存源文件，包含暂停、变速、tick 回退、长间隔、格式化和分配量回归检查。微基准仅覆盖核心计算，不包含游戏或原生 UI 绘制；本项目不承诺零开销。详见 [验证记录](docs/VALIDATION.md)。

## 构建

需要 .NET 10 SDK、本地 RimWorld 1.7 和 Harmony 1.7。不通过 NuGet 拉取游戏程序集，也不分发游戏/Harmony 程序集。

```powershell
./Build.ps1 -RimWorldPath 'D:\Steam\steamapps\common\RimWorld' `
  -DeployPath 'D:\Steam\steamapps\common\RimWorld\Mods\FPS&TPS'
```

非标准 Harmony 路径可传 `-HarmonyPath '...\1.7\Assemblies\0Harmony.dll'`。脚本依次运行核心测试、编译、检查 XML/双语键/作者 ID、生成完整安装 ZIP 与 SHA-256，可选部署。发布包只含 `About`、`LoadFolders.xml`、`1.7/Assemblies/Anno.FpsTps.dll`、语言、安装说明和许可证；不会打包 `bin`、`obj`、测试数据或游戏文件。项目源码和可直接安装的 `Mod/` 都在仓库中。

`FPS-TPS.sln` 可直接用 Visual Studio 打开。部署后可运行 `./Tests/Run-RuntimeSmoke.ps1`，使用隔离配置和游戏自带无界面模式复现原生 API 集成检查；详细范围见验证记录。GitHub Actions 自动运行不依赖游戏程序集的核心检查。

## 兼容范围

仅支持 RimWorld 1.7 的 .NET 10 运行时。完全替换原生右下角控制栏的 Mod 可能需要兼容补丁。原版调试计数器去重只跳过当前可见的同类指标，不修改用户的调试设置。其他 Mod 的计数器不受影响，出现重复时请关闭相应显示。短窗口/追赶 tick 可以短暂超过 100%。

## 致谢与许可

显示位置与功能概念参考 [FPS And TPS](https://steamcommunity.com/sharedfiles/filedetails/?id=3264612179)。本项目为独立实现，未复制原项目的代码、程序集、美术或翻译。原创部分使用 [MIT](LICENSE) 许可证。

## English

Native bottom-right FPS and actual TPS counters for RimWorld 1.7, by **anno** (`anno.fpsandtps`). Requires Harmony 1.7; no DLC or developer mode needed. Download the complete ZIP from Releases and extract its `FPS&TPS` folder into `RimWorld/Mods`. Enable Harmony and this mod, restart, then configure in **Options → Mod settings → FPS & TPS**. Safe to add/remove from existing saves.

Features include actual/target TPS, percentage, frame/tick timing, independent 0.1–5 s intervals, precision, horizontal/vertical layout, ordering, native fonts, adaptive width, spacing, optional background, color palettes/thresholds, world view, pause behavior, vanilla debug-counter deduplication, lazy tooltips and two presets. Chinese and English are included.

FPS reuses native smoothed timing; TPS samples actual tick progress against wall time. Only UI methods are patched, never the simulation tick loop. Text and width are cached. Core tests check behavior and zero steady-state allocations; they are not an in-game overhead measurement. Build with .NET 10 and `./Build.ps1 -RimWorldPath 'your game path' [-HarmonyPath '0Harmony.dll'] [-DeployPath 'your mod folder']`. Game and Harmony binaries are never redistributed.
