# 待实现功能清单

## 🔐 权限与管理系统

### 管理员功能

- [ ] **首个用户自动成为管理员**

  - 在 `UserAccount` 模型中添加 `IsAdmin` 字段
  - 数据库 `CreateUserAsync` 判断是否为第一个用户，自动设为管理员
- [ ] **根据权限显示UI**

  - 管理员登录后显示"开启服务器(Host)"按钮
  - 普通用户只显示"加入游戏(Client)"按钮
  - 在 `AuthResponseMessage` 中添加 `isAdmin` 字段传递给客户端
  - `LoginUI` 根据 `isAdmin` 控制按钮显示/隐藏
- [ ] **管理员授权功能（扩展）**

  - 管理员可以将其他用户提升为管理员
  - 管理员管理界面

### 专用服务器与管理面板（Dedicated Server & Admin Panel）

- [ ] **专用服务器模式（Headless Server）**
  - 使用 `NetworkManager.StartServer()` 启动纯服务端，不创建本地玩家
  - 支持命令行参数启动（`-batchmode -nographics`）用于云服务器部署
  - 服务端不渲染游戏画面，仅处理网络逻辑，降低资源占用
  - 区分 Host（服务器+客户端）与 Dedicated Server（纯服务器）

- [ ] **服务器管理面板（Admin Dashboard）**
  - 独立的管理界面，实时显示服务器状态
  - 功能模块：
    - 📊 **服务器监控**：在线玩家数、连接数、帧率、内存/CPU占用
    - 👥 **玩家管理**：在线玩家列表、踢出玩家、封禁账号、查看玩家信息
    - 💬 **聊天监控**：查看/过滤聊天记录、全服公告广播
    - 🎮 **游戏管理**：调整游戏参数、触发事件、重置世界状态
    - 📜 **日志查看**：实时日志流、错误告警、登录记录
    - ⚙️ **配置管理**：热更新服务器配置（无需重启）
  - 可选实现方式：
    - Unity UI 面板（Editor 或 Runtime）
    - Web 管理后台（通过 REST API 或 WebSocket 连接服务器）

- [ ] **服务器控制台命令系统**
  - 支持文本命令控制服务器（如 `/kick player`, `/ban user`, `/announce msg`）
  - 命令权限分级
  - 命令历史记录与自动补全

---

## 🎮 客户端玩法（当前优先）

*在此添加客户端玩法相关的待办事项*

---

## 📝 其他改进

### 数据存储升级
- [ ] **将本地JSON存储改为数据库/云数据库**
  - 当前使用 `JsonDatabaseProvider` 存储在本地 JSON 文件
  - 可选方案：
    - SQLite（本地轻量数据库）
    - MySQL/PostgreSQL（自建服务器）
    - Firebase Realtime Database / Firestore（云数据库）
    - PlayFab / LeanCloud（游戏后端服务）
  - 需要实现 `IDatabaseProvider` 接口的新实现类
  - 支持多服务器共享用户数据

---

> 最后更新: 2026年2月1日
