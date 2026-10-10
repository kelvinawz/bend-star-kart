# 弯星卡丁 · Unity 6 阶段 1–5

原创单机卡通卡丁车竞速 Demo。阶段 1 提供车辆物理、漂移、追尾相机、测试赛道和调参面板；阶段 2 提供集气、双氮气、小喷和起步喷；阶段 3 提供三圈比赛、顺序检查点、计时与结算；阶段 4 加入 3 台 AI 对手、实时名次、轮胎印、漂移火花、氮气尾焰、动态速度线和卡通描边；阶段 5 提供 macOS Universal 构建菜单及 DMG 打包脚本。

源码保存在[GitHub 私有仓库](https://github.com/kelvinawz/bend-star-kart)；最新版 macOS 安装包从 [v0.1.1 发行页](https://github.com/kelvinawz/bend-star-kart/releases/tag/v0.1.1)下载。访问需要仓库权限。

## 环境与导入

1. 使用 Unity Hub 新建 **Unity 6 LTS** 的 **Universal 3D（URP）** 空项目。
2. 在 Package Manager 安装 Unity 官方 **Input System** 包；若提示启用新版输入后重启编辑器，按提示操作。建议在 Player Settings → Active Input Handling 选择 **Input System Package (New)**。
3. 将本目录的 **整个 `Assets/`** 复制到新项目的 `Assets/`。`Assets/Shaders/` 与脚本一起导入；`Scenes/` 和 `Settings/` 的实际资源会由菜单生成。`scripts/` 是项目外的 macOS 打包脚本，可直接从本目录运行。
4. 等待脚本编译完成，点击 **Tools/Kart/Generate Race Track**。菜单生成正式场景 `Assets/Scenes/StarloopRace.unity`，并将其加入 Build Profiles 场景列表；`Assets/Settings/KartTuning.asset`、`KartControls.inputactions`、材质和网格资源也会按需生成。阶段 1–2 的测试场景仍可通过 **Tools/Kart/Generate Test Track** 生成。
5. 打开 `StarloopRace`，按 **Play**，点击 Game 视图获取键盘焦点。

重复点击菜单会重建对应场景和程序网格，但保留已保存的 `KartTuning.asset` 与 `KartControls.inputactions` 改绑。操作前若改过生成场景中的手工内容，请自行保存副本。

## 操作

| 键 | 当前行为 |
| --- | --- |
| ↑ | 加速 |
| ↓ | 刹车；停稳后倒车 |
| ← / → | 转向；倒车时反向 |
| 左 Shift + 方向 | 漂移；短按点漂、长按长漂 |
| 漂移时反方向 | 断漂；须松开 Shift 才可再次起漂 |
| R | 随时重置到最近经过的赛道中心检查点；若尚未经过检查点则回到起点 |
| Esc | 暂停或继续 |
| F1 | 调参面板开关 |
| 左 Ctrl / Space | 使用 1 格氮气；macOS 上可用 Space 避开系统的 Ctrl+方向键快捷键 |

正式比赛以 3-2-1-GO 开始。GO 前 0.18 秒至 GO 后 0.2 秒内首次按 ↑ 可触发起步喷；过早按会在 GO 后原地打滑约 1 秒。3 圈完赛后可点“重新比赛”。

## 验收步骤

1. 等待 GO 后按 ↑ 从起点加速，观察速度从起步快速上升，并在接近约 36 m/s 时逐渐变慢；面板速度表约为 200 km/h。
2. 直道按 Shift+方向短点与长按，观察甩尾角逐渐增大；松 Shift 后缓慢回正。漂移中反打应立即进入回正状态，持续按住 Shift 不应反向自动重起漂。
3. 绕环道完整驾驶一圈，特别检查 U 形发夹、S 弯、草地边缘、长直道跳台和护栏。进入草地应降至正常极速的 0.6 倍；跳台高速通过应短暂腾空并落地。
4. 经过检查点后按 R，车辆应回到最近检查点；按 Esc 暂停和继续。按 F1 调整参数，点击保存、读取，退出 Play 后检查 `KartTuning.asset` 是否保留保存的数值。
5. 查看 Console 是否有编译错误、运行时异常；检查是否出现穿墙或持续卡墙。

### 2026-10-10 赛道修复验收

1. 从新起点直线加速：车辆应沿南侧直道行驶，不会朝右侧护栏偏去；经过原来约 `x=10–16` 的位置时，路面保持连续。
2. 在场外或跳台失误后按 R：车辆应回到最近有效检查点的道路中心，车速归零。若越过护栏或跌落超过约 1.5 秒，系统也会自动复位。
3. 重新导入源码的项目，须再点一次 **Tools/Kart/Generate Race Track** 才会把新路线和连续护栏写入场景；直接试玩请使用下方最新版 DMG。

### 阶段 2 验收

1. 漂移时观察集气条：角度、速度越大，增长越快；满条换 1 格氮气，最多 2 格，满格后不再集气。
2. 有氮气时按左 Ctrl 或 Space：持续约 1.2 秒，极速和推力增加，相机 FOV 拉大并出现简易速度线。氮气中漂移应更快结束氮气。
3. 储存 2 格氮气，第一格结束后 0.2 秒内再按一次，HUD 显示“双喷”，推力与最高速度额外增加。
4. 漂移结束且车头回正后，先松开再按 ↑；0.25 秒窗口内应出现持续约 0.4 秒的小喷。反打断漂后也可触发；正面撞墙后的这次漂移不得触发小喷。
5. 重新按 Play：在 GO 前后指定窗口按 ↑ 触发起步喷；提前太多按 ↑ 则 GO 后打滑 1 秒。正面撞护栏应断掉氮气/小喷，侧擦护栏仅小幅损速并沿墙滑行。

### 阶段 3 验收

1. 运行 `StarloopRace`：检查起跑倒计时、正式赛道的拱门/路缘/场景装饰、左上圈数与计时、右上小地图、右下速度表、下方集气条和两格氮气图标。
2. 按路线顺序经过 CP 1–9，再过起终点线；圈数应从 1/3 更新为 2/3。逆向经过检查点、跳过中间检查点直接冲线都不应计圈。
3. 每圈显示本圈时间、累计时间与最佳单圈。完成第 3 圈后出现 3 圈分段、总成绩、历史最佳和“重新比赛”按钮。
4. 完赛后点击“重新比赛”，应重新出现倒计时并从第 1 圈开始。退出 Play 再进入，最佳单圈与最佳总成绩应由 PlayerPrefs 保留。
5. 在中途通过检查点后按 R，应回到最近有效检查点；绕过未通过的检查点后按 R，不应以被绕过的位置为重生点。查看 Console 是否有错误。

### 阶段 4 验收

1. 重新生成并运行 `StarloopRace`：起点后方应有紫、金、珊瑚三台 AI 车；GO 后沿闭合路线行驶，在弯道减速并在跳台经过短跃起弧。小地图出现 3 个 AI 标记和玩家标记。
2. 加速追过 AI 或让 AI 超车：左上实时名次应在 1/4 至 4/4 间变化。AI 完赛时间参与最终名次，玩家完成 3 圈后结算显示最终名次。
3. 漂移时两侧后轮留下短时轮胎印并喷出橙色火花；松开漂移键后停止新印迹和火花。使用氮气时后方出现蓝色尾焰，两侧速度线随时间流动；氮气结束或正面撞墙后尾焰停止。
4. 玩家与 AI 车身、轮胎边缘有深色卡通轮廓；确认材质不呈紫红色错误着色，并检查 Console 无编译或运行异常。

AI 采用沿赛道中心线的运动学路径跟随、弯道限速和车道偏移，以保证单机场景可稳定跑完三圈。AI 目前不使用玩家的漂移或氮气控制器。

### 阶段 5：macOS Universal 构建与 DMG

1. 在 macOS 上通过 Unity Hub 安装适用的 **macOS Build Support** 模块。本文的实物构建使用 Mono 脚本后端；如果改用 IL2CPP，还需安装对应模块和 Xcode。构建前先按上面的步骤生成 `StarloopRace`，并在编辑器中完成编译和三圈驾驶验收。
2. 点击 **Tools/Kart/Build macOS Universal**。脚本把 macOS 架构设为 Intel 64-bit + Apple Silicon，仅打包正式比赛场景，输出到 Unity 项目根目录的 `Builds/macOS/BendStarKart.app`。若场景不存在，先点击 **Tools/Kart/Generate Race Track**。
3. 在终端运行（将路径改为实际位置）：

   ```bash
   bash /path/to/弯星卡丁/scripts/build_dmg.sh \
     /path/to/UnityProject/Builds/macOS/BendStarKart.app
   ```

   默认在 `.app` 同目录生成 `BendStarKart.dmg`；第二个参数可指定其他 `.dmg` 路径。已有同名 DMG 时脚本会停止，先移走旧文件再重新打包。
4. 脚本先用 `lipo` 核对应用主程序及存在时的 `UnityPlayer.dylib` 同时包含 `arm64` 和 `x86_64`。暂存应用后逐个临时签署原生库，再签应用；制作包含应用和 `Applications` 链接的压缩 DMG，执行 `hdiutil verify`，只读挂载再次验签，最后输出 SHA-256。

**验收：** 在 Finder 打开 DMG，确认可将应用拖到 `Applications`；分别在 Apple Silicon 与 Intel Mac 上启动应用，完成一局三圈比赛，并检查输入、画面和退出流程。仅有 `lipo` 双架构与 DMG 校验通过，不能替代两种机器上的实际启动验收。本脚本不执行 Apple Developer ID 签名或公证，对外分发前需另外完成这些步骤。

## 文件职责

- `KartController.cs`：Rigidbody 地面检测、加减速、转向、碰撞与重置。
- `KartDrift.cs`：漂移触发、甩尾角、点漂/长漂/断漂状态。
- `KartBoost.cs`：集气、氮气、双喷、小喷、起步喷与撞墙断喷。
- `KartEffects.cs`：轮胎印、漂移火花和氮气尾焰。
- `KartOutline.cs`：给几何车模添加倒壳描边网格。
- `KartInput.cs`：新版 Input System 输入读取。
- `KartTuning.cs`：可保存的手感数据，包含车辆、漂移与阶段 2 加速参数。
- `KartChaseCamera.cs`：追尾、漂移滞后、落地压缩和撞墙震屏。
- `KartSurface.cs`、`KartCheckpoint.cs`：草地标识和重置点。
- `KartTrackBounds.cs`：根据闭合中心线判断是否越界，供自动复位使用。
- `RaceStartSignal.cs`：正式比赛与起步喷共用的 GO 时间信号。
- `RaceManager.cs`：三圈赛、顺序检查点、计时、完赛和 PlayerPrefs 最佳成绩。
- `KartAiDriver.cs`：沿闭合赛道运行的 3 台运动学 AI 车辆。
- `KartTuningPanel.cs`：F1 实时调参与遥测。
- `KartBoostHud.cs`：集气条、氮气库存、增益状态和简易速度线。
- `RaceHud.cs`：圈数、赛时、速度表、小地图和完赛结算。
- `KartOutline.shader`、`KartFx.shader`：URP 描边和透明特效着色器。
- `KartTestTrackGenerator.cs`：编辑器菜单、测试/正式场景、几何体、资源与 Input Actions 生成。正式赛道是无交叉闭环，起终点位于直道中段，包含东侧 U 弯、北侧宽弯、西侧 S 弯、连续路面与护栏，以及原创场景装饰。
- `KartMacBuild.cs`：编辑器菜单构建 macOS Universal 应用，固定正式赛道场景和输出位置。
- `scripts/build_dmg.sh`：检查应用二进制架构、制作并校验 DMG、输出 SHA-256。

调参面板在 **Unity 编辑器 Play 模式** 中能把运行时副本保存回项目内的 ScriptableObject。独立构建不能直接写入 Unity 项目资源，面板会提示此限制。

## 当前验证边界

2026-10-10 使用本机 **Unity 6000.6.3f1**、URP 17.6.0、Input System 1.19.0 重新编译、生成赛道并构建 macOS Universal。最新版 [BendStarKart.dmg](https://github.com/kelvinawz/bend-star-kart/releases/download/v0.1.1/BendStarKart.dmg) 已上传 GitHub 发行页；本地文件位于 `Builds/macOS/BendStarKart.dmg`，旧映像位于 `Builds/macOS/archive/BendStarKart-20261001.dmg`，构建产物不纳入源码仓库。**安装和试玩请使用最新版 DMG**：主程序和 UnityPlayer 用 `lipo` 核对为 `x86_64 arm64`；DMG 通过 `hdiutil verify`，只读挂载后通过完整代码签名校验，并核对了 `Applications` 链接。最新版 SHA-256 为 `14fe0e0c75d9d3462910ce1af3417b6a239e75952bb9f64f96c7a88cc94e303d`。同步目录会给裸 `.app` 添加扩展属性，使其在该目录中无法通过严格签名校验；DMG 内副本已通过。

本轮针对用户报告的起步冲出地图问题，临时运行时探针以约 35 m/s 从新起点驶过 95.5 米，穿过原缺口路段，车辆未越界；触发复位后位于已通过检查点的道路中心，速度为 0。探针已从最终构建移除。此前的图形版检查曾确认 URP、车辆、AI、HUD、F1、Esc 和 R 可工作；本轮新赛道尚未完成图形界面人工驾驶一整圈，也未在 Intel Mac 上实际启动。当前安装的是 Unity 6.6，原需求指定 Unity 6 LTS；严格按版本条件交付仍需 Unity 6.3 LTS 复测。DMG 使用本机临时签名，未进行 Apple Developer ID 签名或公证。
