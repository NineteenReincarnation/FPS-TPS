FPS & TPS 1.0.0 — anno

适用于 RimWorld **1.7**，Mod ID：`anno.fpsandtps`。需要 Harmony 1.7，不需要 DLC。

在右下角原生时间按钮上方显示 FPS 与实际 TPS，支持目标 TPS、达标百分比、帧/tick 耗时。提供独立刷新间隔、精度、布局、字体、自动宽度、颜色阈值、暂停和世界地图选项，以及完整中文/英文设置和预设。

低开销设计：无逐 tick 补丁，复用游戏帧时间、按窗口统计实际 tick 差值、缓存文字和宽度。暂停、变速、读档后重新采样。核心采样与不变文本缓存的分配量检查通过；这不等同于整场游戏零性能开销。

**下载 `FPS-TPS-v1.0.0-RimWorld-1.7.zip` 即为完整可安装 Mod**，已包含编译 DLL、元数据、双语文本、安装说明和许可证，无需自行编译。将里面的 `FPS&TPS` 文件夹放入 `RimWorld/Mods`，启用 Harmony 与本 Mod，重启游戏即可。设置位置：选项 → Mod 设置 → FPS & TPS。

`SHA256SUMS.txt` 提供安装 ZIP 校验值。GitHub 自动生成的 Source code 包是工程源码，普通玩家请下载上述安装 ZIP。

English: Native FPS and actual TPS for RimWorld 1.7, with detailed Chinese/English settings and low-overhead cached sampling. Requires Harmony 1.7. Download the full install ZIP above, extract `FPS&TPS` into `RimWorld/Mods`, enable and restart. The ZIP includes the compiled DLL; no build is required.
