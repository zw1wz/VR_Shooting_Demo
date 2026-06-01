# VR Shooting Demo 开发计划

## 1. 项目目标

目标是把当前的射击 Demo 发展成一个拟真的 VR 快速射击训练模拟器。

最终体验是：用户佩戴 VR 眼镜，手持自制枪型手柄，像操作真实手枪一样完成快速反应射击训练。VR 头显负责视野和空间感，枪型手柄负责姿态、扳机、拉枪机、更换弹匣、空仓挂机和真实后坐力反馈。

最终能力：

- VR 头显控制用户视角。
- 枪型手柄实时控制游戏内手枪姿态。
- 扣动扳机触发射击。
- 拉动枪机完成上膛、退弹或空仓操作。
- 插入和拔出弹匣会同时反映到硬件和游戏状态。
- 弹匣打空后进入空仓挂机状态。
- 自制枪型手柄能产生可感知的后坐力。
- 手枪模型、枪口火焰、抛壳、枪声、命中特效、弹道和靶子反应都尽量拟真。
- 训练系统记录反应时间、命中率、换弹速度、误操作和稳定性。

## 2. 设计原则

- 先做模拟核心，再接硬件。
- 在最终阶段之前，始终保留鼠标键盘调试模式。
- 不要把游戏逻辑直接绑定到某一种输入设备。
- 枪械机制、输入、弹道、特效和训练规则要分层。
- 自制枪型手柄只负责输入和反馈，不负责游戏规则。
- VR 头显和空间追踪优先使用成熟的 Unity XR / OpenXR 方案。
- 自制硬件重点放在拟真操作上：扳机、枪机、弹匣、空仓挂机、后坐力。

## 3. 总体架构

```text
输入层
  MouseKeyboardWeaponInput
  VRControllerWeaponInput
  CustomGunHandleInput
        |
        v
武器输入抽象层
  扳机
  枪机
  弹匣
  姿态
        |
        v
手枪状态机
  膛内状态
  弹匣状态
  空仓挂机
  扳机逻辑
  换弹逻辑
        |
        v
射击系统
  射速限制
  弹道
  命中检测
  射击记录
        |
        v
反馈系统
  枪口火焰
  抛壳
  音效
  后坐力
  震动
  命中特效
        |
        v
训练系统
  训练流程
  靶子
  计分
  历史记录
  结果 UI
```

## 4. 当前项目状态

已经实现：

- Unity/Tuanjie 射击场景。
- 鼠标控制枪口瞄准。
- 鼠标左键射击。
- 随机生成靶子。
- 听到 beep 后快速射击的训练流程。
- 分数、命中数、命中率和每枪记录。
- 枪声音效、金属命中音效、枪口火焰和命中特效。
- 基础 UI 面板。
- 运行时 UI 主题美化。
- 手枪射速限制。
- 射速冷却期间的短输入缓冲，避免第二枪被吞。
- 第一版通用武器配置 `WeaponProfile`，为后续多枪械扩展做准备。
- 第一版手枪状态机，包含弹匣、膛内弹、拉枪机、空仓挂机和扳机复位逻辑。
- 第一版输入抽象层 `IWeaponInput`，并接入键鼠调试输入 `MouseKeyboardWeaponInput`。
- 独立弹药 HUD，显示弹匣、膛内弹和枪机状态。
- 手枪操作和异常原因提示，包含插拔弹匣、枪机操作、空击和空仓挂机。
- 无外部素材时自动生成轻微空击占位声。
- 训练界面、武器 HUD 和操作提示使用中文，并使用项目内置的 Noto Sans CJK SC 开源字体。
- Git 本地仓库和 GitHub 远程仓库。

当前限制：

- 还没有真正的 VR 输入。
- 手枪状态机还是逻辑层，暂时还没有对应的模型动画和完整操作音效素材。
- 还没有接入自制枪型手柄。
- 弹道目前还是简单 Raycast。
- 场景美术仍然是原型级别。
- 还没有训练历史保存。
- 还没有真实后坐力反馈。

## 5. 开发路线图

### 阶段 1：稳定当前桌面原型

目标：让当前非 VR 版本变得稳定、清晰、可测试。

任务：

- 确认 UI 在常见分辨率下清晰可读。
- 确认射速和输入缓冲手感可靠。
- 增加一个简单的调试状态显示，或在 Inspector 中暴露关键字段。
- 明确开始、等待、射击、结果四个状态。
- 完善重新开始流程。
- 保留键盘快捷键，方便测试。
- 提交一个稳定版本。

交付物：

- 一个可反复游玩的鼠标版快速反应射击原型。

验收标准：

- 玩家可以开始一轮、等待 beep、完成 5 发射击并看到结果。
- 快速连点不会产生不真实的超高射速。
- 第一枪后第二枪稍早点按不会被吞掉。
- UI 不遮挡靶子，信息清楚。

### 阶段 2：手枪状态机

目标：在接入 VR 和硬件前，先把真实半自动手枪的核心逻辑模拟出来。

新增脚本建议：

```text
Assets/Scripts/Weapons/PistolStateMachine.cs
Assets/Scripts/Weapons/PistolMagazine.cs
Assets/Scripts/Weapons/PistolConfig.cs
PistolTriggerResult enum
```

状态字段：

- `magazineInserted`：是否插入弹匣
- `magazineAmmo`：弹匣剩余子弹
- `magazineCapacity`：弹匣容量
- `roundInChamber`：膛内是否有弹
- `slideLocked`：是否空仓挂机
- `triggerHeld`：扳机是否被按住
- `lastShotTime`：上次开火时间
- `canFire`：当前是否可以开火

操作：

- 插入弹匣
- 拔出弹匣
- 拉枪机
- 释放枪机
- 扣动扳机
- 松开扳机
- 击发膛内子弹
- 射击后自动循环
- 上下一发
- 弹匣空后空仓挂机
- 无膛内弹时空击

建议初始配置：

```text
手枪类型：G17 类半自动手枪
弹匣容量：17 发
射速限制：300 RPM
单发冷却：0.2 秒
输入缓冲：0.15 秒
```

验收标准：

- 没有膛内弹时不能正常射击。
- 只插入弹匣但未上膛时不能正常射击。
- 插入弹匣后，拉动并释放枪机可以上膛一发。
- 每次射击消耗膛内子弹。
- 弹匣还有子弹时，射击后自动上下一发。
- 弹匣打空后进入空仓挂机。
- 空仓挂机状态下插入新弹匣并释放枪机后可以继续射击。

### 阶段 3：输入抽象层

目标：让同一套手枪逻辑可以同时支持鼠标、VR 控制器和自制枪型手柄。

接口建议：

```text
IWeaponInput
  bool TriggerPressedThisFrame
  bool TriggerReleasedThisFrame
  bool SlidePulledThisFrame
  bool SlideReleasedThisFrame
  bool MagazineInsertedThisFrame
  bool MagazineRemovedThisFrame
  bool SlideReleasePressedThisFrame
  Pose WeaponPose
```

输入实现：

```text
MouseKeyboardWeaponInput
VRControllerWeaponInput
CustomGunHandleInput
```

调试按键建议：

```text
鼠标左键：扳机
R：插入满弹匣
T：拔出弹匣
F：拉枪机
G：释放枪机
V：释放空仓挂机
```

验收标准：

- `GunShooter` 不再直接读取鼠标输入。
- 鼠标键盘可以驱动手枪状态机。
- 后续新增 VR 输入和自制硬件输入时，不需要重写射击逻辑。

### 阶段 4：弹药、换弹和操作反馈

目标：让玩家清楚知道当前手枪为什么能打或不能打。

功能：

- 弹药 HUD：
  - 弹匣剩余子弹
  - 膛内是否有弹
  - 是否空仓挂机
- 空击声音
- 拉枪机声音
- 释放枪机声音
- 插入弹匣声音
- 拔出弹匣声音
- 空仓挂机声音
- 可选的新手提示

当前进度：

- 已实现独立弹药 HUD，显示弹匣剩余数量、膛内状态和枪机状态。
- 已实现操作提示和无法射击原因提示。
- 已实现自动生成的空击占位声。
- 尚未加入插拔弹匣、枪机和空仓挂机的独立音效素材。
- 尚未记录空击次数、换弹时间等训练指标。

训练指标：

- 误射次数
- 空击次数
- 换弹时间
- 空仓挂机处理时间
- 射击次数
- 命中次数
- 命中率

验收标准：

- 玩家能理解手枪没有开火的原因。
- 空枪、无弹匣、空仓挂机都有不同反馈。

### 阶段 5：VR 基础接入

目标：从桌面摄像机迁移到 VR 头显，同时保留桌面调试模式。

系统：

- OpenXR Plugin
- XR Interaction Toolkit
- Input System
- XR Origin
- World Space UI

场景改动：

- 添加 XR Origin。
- 使用 XR Camera 作为玩家视角。
- 保留桌面调试相机，必要时可禁用。
- UI 改成放在玩家前方的世界空间 Canvas。
- 支持 VR 中开始训练和查看结果。

验收标准：

- 项目可以在 VR 中运行。
- 玩家可以在射击场中自然观察。
- UI 在 VR 中可读。
- 当前快速射击训练仍可完成。
- 桌面调试模式仍然可用。

### 阶段 6：手枪姿态追踪

目标：让游戏内手枪模型跟随实体枪型手柄实时运动。

推荐追踪方案：

- 使用现成 VR 控制器或 Tracker 固定在枪型手柄上。
- 由 VR 系统提供 6DoF 位置和旋转。
- 自制电路只负责扳机、枪机、弹匣、空仓挂机、后坐力等机械输入和反馈。

原因：

- 纯自制 IMU 姿态很容易漂移。
- 现成 VR 追踪系统更稳定。
- 自制硬件可以专注做真实操作和后坐力。

姿态链路：

```text
VR 控制器或 Tracker 姿态
        |
        v
GunPoseDriver
        |
        v
游戏内手枪 Transform
```

校准功能：

- 握把偏移
- 枪口前向偏移
- 瞄具对齐偏移
- 惯用手设置
- 重新居中

验收标准：

- 游戏内手枪能平滑跟随实体枪型手柄。
- 枪口方向和用户实际瞄准方向一致。
- 可以通过校准修正安装误差。

### 阶段 7：自制枪型手柄硬件

目标：制作一个可以上报真实手枪操作，并接收 Unity 后坐力命令的枪型控制器。

推荐硬件架构：

```text
VR Tracker 或 VR 控制器
  负责姿态追踪

微控制器
  读取扳机传感器
  读取枪机传感器
  读取弹匣传感器
  读取空仓挂机传感器
  可选：电池状态
  控制后坐力执行器

Unity
  读取输入报告
  发送后坐力输出报告
```

传感器建议：

- 扳机：
  - 微动开关
  - 霍尔传感器
  - 模拟量扳机传感器
- 枪机：
  - 线性霍尔传感器
  - 后拉到位微动开关
  - 光电传感器
- 弹匣：
  - 磁簧开关
  - 霍尔传感器
  - 机械开关
  - 可选：电阻 ID 或 NFC 识别弹匣
- 空仓挂机：
  - 机械开关
  - 霍尔传感器
- 后坐力：
  - 电磁铁
  - 线性执行器
  - 电机冲击结构
  - 早期原型可以先用偏心震动电机

建议输入报告：

```text
triggerAxis: 0.0 到 1.0
triggerPressed: bool
slidePosition: 0.0 到 1.0
slideFullyPulled: bool
magazinePresent: bool
magazineId: byte
slideLockedHardware: bool
batteryLevel: byte
```

建议输出报告：

```text
recoilPulseStrength: 0 到 255
recoilPulseDurationMs: 0 到 255
statusLedMode: byte
```

通信方案：

- USB HID：推荐第一版原型使用。
- Bluetooth HID：后期可做无线，但复杂度更高。
- Serial：调试方便，但不像标准游戏控制器那样即插即用。

验收标准：

- Unity 可以读取扳机、枪机和弹匣状态。
- Unity 可以向设备发送后坐力命令。
- 接入硬件时不需要重写手枪状态机。

### 阶段 8：弹道系统

目标：从简单 Raycast 升级到可信的弹道表现。

版本 1：快速 Raycast 弹道

- 从枪口发射射线。
- 加入可配置散布。
- 根据材质触发不同命中反馈。
- 添加弹孔贴花。
- 记录命中方向和命中点。

版本 2：混合弹道

- 用 Raycast 保证命中判定快速。
- 增加可视化曳光或子弹飞行效果。
- 远距离时计算下坠和偏移。

版本 3：实体弹丸模拟

- 生成子弹实体。
- 积分速度。
- 应用重力。
- 使用连续碰撞检测或扫掠射线。
- 如有需要，加入跳弹规则。

推荐路径：

- 先做版本 1。
- 再加入视觉飞行效果和散布。
- 只有训练目标真的需要时，再做完整弹丸模拟。

验收标准：

- 射击方向来自枪口 Transform。
- 命中点和瞄准方向一致。
- 不同材质有不同命中反馈。
- 纸靶、金属靶、墙面表现不同。

### 阶段 9：拟真手枪视觉

目标：用可信的手枪模型和可动部件替换当前原型枪。

需要的模型：

- 枪身
- 套筒
- 枪管
- 弹匣
- 扳机
- 准星和照门
- 可选：弹匣内可见子弹
- 弹壳

动画状态：

- 扳机扣动和复位
- 套筒后退
- 套筒复位
- 空仓挂机
- 插入弹匣
- 拔出弹匣
- 后坐力
- 抛壳

验收标准：

- 套筒位置和手枪状态一致。
- 空仓挂机视觉明显。
- 弹匣插拔可见。
- 扳机和套筒动画与输入时机一致。

### 阶段 10：后坐力和震动

目标：让射击产生真实可感知的反馈。

软件反馈：

- 手枪后坐动画。
- VR 中谨慎使用相机震动，避免眩晕。
- VR 控制器震动。
- 自制硬件后坐力命令。

硬件反馈：

- 成功射击时 Unity 发送后坐力脉冲。
- 空击时不发送强后坐力。
- 释放枪机或空仓挂机可以给较小反馈。

安全限制：

- 后坐力强度必须可调。
- 后坐力可以完全关闭。
- 硬件需要故障安全设计。
- 加入冷却限制，避免过热。
- 如硬件支持，监测电池和温度。

验收标准：

- 玩家每次射击都能感到反馈。
- 后坐力时机和枪口火焰、枪声同步。
- 空击反馈明显不同。

### 阶段 11：拟真靶场环境

目标：制作一个可信的 VR 室内射击场。

场景元素：

- 室内靶道
- 射击隔间
- 射击台
- 靶轨
- 后方挡弹墙
- 吸音墙面
- 顶部灯光
- 混凝土地面
- 安全线
- 控制面板
- 成绩显示屏

靶子类型：

- 纸质人形靶
- 圆形靶
- 钢板靶
- 弹出靶
- 移动靶

环境反馈：

- 纸靶弹孔
- 金属靶火花
- 墙面尘土或碎屑
- 射击场混响
- 靶子移动和命中反应

验收标准：

- 一眼能看出这是射击训练场。
- VR 帧率稳定。
- 靶子清晰、不会被环境噪音淹没。

### 阶段 12：训练模式

目标：把模拟器变成真正有训练价值的工具。

模式 1：快速反应模式

- 等待 beep。
- 尽快准确射击。
- 测量首枪反应时间。

模式 2：多目标反应模式

- 随机出现多个靶子。
- 玩家需要快速识别并命中。
- 测量目标切换时间。

模式 3：换弹训练

- 起始弹药有限。
- 打空后空仓挂机。
- 玩家必须完成换弹并继续射击。
- 测量换弹时间。

模式 4：故障排除训练

- 模拟未击发或供弹失败。
- 玩家执行排障动作。
- 测量恢复时间。

模式 5：精度分组训练

- 固定距离。
- 多发射击。
- 测量弹着点分布和散布大小。

验收标准：

- 每种模式都有清晰的开始和结果。
- 每种模式都记录有意义的训练数据。

### 阶段 13：数据和成绩系统

目标：每次训练后给用户有价值的反馈。

训练记录字段：

- 日期和时间
- 训练模式
- 总分
- 命中数
- 射击数
- 命中率
- 首枪时间
- 平均反应时间
- 最快反应时间
- 换弹时间
- 空击次数
- 误射次数
- 每枪命中位置

存储方式：

- 开发阶段先存本地 JSON。
- 支持 CSV 导出，方便分析。
- 后期可考虑云同步。

结果 UI：

- 总览面板
- 每枪日志
- 命中热力图
- 操作时间线
- 个人最佳成绩对比

验收标准：

- 每次完成训练都会保存结果。
- 用户可以查看最近训练记录。
- 数据可以导出。

## 6. 建议文件结构

```text
Assets/
  Scripts/
    Core/
      GameManager.cs
      TrainingManager.cs
    Input/
      IWeaponInput.cs
      MouseKeyboardWeaponInput.cs
      VRWeaponInput.cs
      CustomGunHandleInput.cs
    Weapons/
      WeaponProfile.cs
      WeaponFireMode.cs
      PistolConfig.cs
      PistolStateMachine.cs
      PistolMagazine.cs
      GunShooter.cs
      GunPoseDriver.cs
      SlideAnimator.cs
      RecoilController.cs
    Ballistics/
      BallisticsSystem.cs
      BallisticHit.cs
      SurfaceImpactProfile.cs
    Effects/
      MuzzleFlashController.cs
      CasingEjector.cs
      ImpactEffectSpawner.cs
      WeaponAudioController.cs
    UI/
      UIThemeController.cs
      ResultPanelController.cs
      AmmoHudController.cs
    Data/
      TrainingSessionRecord.cs
      TrainingDataStore.cs
```

## 7. 优先级顺序

推荐开发顺序：

1. 武器通用配置 `WeaponProfile`。
2. 手枪状态机。
3. 鼠标键盘输入抽象。
4. 弹药 HUD 和换弹反馈。
5. 弹道和材质命中反馈。
6. 手枪模型和套筒动画。
7. VR 基础接入。
8. 枪型手柄姿态追踪。
9. 自制枪型手柄 HID 输入。
10. 后坐力输出。
11. 拟真靶场环境。
12. 训练模式。
13. 数据导出和训练历史。

## 7.1 多枪械扩展策略

后续会加入不同枪械，所以不要把 G17 的参数写死在射击逻辑里。推荐把所有“这把枪是什么”的数据放进 `WeaponProfile`，而把“这类枪如何工作”的规则放进对应状态机。

通用武器配置：

```text
WeaponProfile
  weaponName
  fireMode
  caliber
  magazineCapacity
  fireRateRoundsPerMinute
  usesDetachableMagazine
  locksOpenOnEmpty
```

不同枪械类型可以逐步增加不同状态机：

```text
PistolStateMachine      半自动手枪，例如 G17、1911、P320
RevolverStateMachine    左轮手枪
PumpShotgunStateMachine 泵动霰弹枪
RifleStateMachine       半自动/自动步枪
BoltRifleStateMachine   栓动步枪
```

第一阶段先把 G17 作为 `WeaponProfile` 的默认配置。以后新增枪械时，优先新增配置和必要的状态机，不要复制 `GunShooter`。

## 8. 下一 Sprint 计划

Sprint 目标：

完善第一版手枪状态机的可视化反馈，让桌面调试版本可以直接验证完整操作流程。

当前进度：

- 已创建 `WeaponProfile`、`PistolConfig`、`PistolMagazine` 和 `PistolStateMachine`。
- 已创建 `IWeaponInput` 和 `MouseKeyboardWeaponInput`。
- `GunShooter` 已通过输入接口和手枪状态机触发射击，不再直接读取鼠标开火。
- 已加入弹药 HUD、操作提示、异常原因提示和自动生成的空击占位声。

任务：

- [x] 创建 `PistolConfig`。
- [x] 创建 `PistolStateMachine`。
- [x] 创建 `PistolMagazine`。
- [x] 增加膛内弹和弹匣逻辑。
- [x] 把当前直接射击改成通过手枪扳机操作触发。
- [x] 增加调试按键：插入弹匣、拔出弹匣、拉枪机、释放枪机。
- [x] 增加空击音效占位。
- [x] 在 UI 中显示弹药和手枪状态。
- [ ] 为插拔弹匣、枪机和空仓挂机准备独立音效素材。
- [ ] 记录空击次数、换弹时间和空仓挂机处理时间。
- 测试这些流程：
  - 无弹匣时扣扳机
  - 插入弹匣但未上膛时扣扳机
  - 插入弹匣后拉枪机并释放，再扣扳机
  - 一直射击直到打空
  - 空仓挂机
  - 从空仓挂机状态完成换弹

建议提交信息：

```text
Add pistol state machine and ammo flow
```

## 9. 主要风险

### 硬件追踪风险

自制 IMU 姿态容易漂移。

缓解方式：

- 用 VR 控制器或 Tracker 固定在枪型手柄上。
- 自制电路只负责机械输入和后坐力。

### 后坐力安全风险

物理后坐力可能伤手、损坏结构或导致硬件过热。

缓解方式：

- 软件限制最大强度。
- 增加硬件断电开关。
- 增加电流和温度限制。
- 后坐力可关闭。

### VR 性能风险

拟真场景、粒子和灯光可能导致帧率下降。

缓解方式：

- 尽早做性能测试。
- 控制粒子数量。
- 使用烘焙光照和优化材质。
- 避免大量透明特效。

### 项目范围风险

真实枪械模拟、硬件、VR、拟真场景都是大系统。

缓解方式：

- 每个阶段都保持可玩。
- 不要在手枪状态机稳定前接硬件。
- 保留桌面调试模式。

## 10. 完成标准

项目可以认为完成时，需要满足：

- 用户可以佩戴 VR 眼镜并手持枪型手柄。
- 游戏内手枪姿态实时跟随实体枪型手柄。
- 用户可以插入弹匣、拉枪机、射击、打空、空仓挂机、换弹并继续射击。
- 后坐力反馈和射击同步。
- 枪声、枪口火焰、抛壳、命中特效和靶子反应可信。
- 靶场场景拟真，并且 VR 运行流畅。
- 训练模式能记录有效成绩。
- 项目可以稳定打包和演示。

## 11. 参考链接

- Unity XR Interaction Toolkit 文档：https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@latest
- Unity OpenXR Plugin 文档：https://docs.unity3d.com/Packages/com.unity.xr.openxr@latest
- Unity Input System 文档：https://docs.unity3d.com/Packages/com.unity.inputsystem@latest
- Unity Input System HID 支持：https://docs.unity3d.com/Packages/com.unity.inputsystem@latest/manual/HID.html
- Unity XR Haptics API：https://docs.unity3d.com/ScriptReference/XR.InputDevice.SendHapticImpulse.html
