# Unity 6 + Mirror 下一步准备清单（行动版）

> 目标：在不破坏现有工程的前提下，快速把项目推进到“可稳定联机、可迭代动作玩法”的状态。

---

## 0. 关键决策（先定方向，避免返工）

- 拓扑结构
  - 初期：Listen Server（Host + 客户端）便于调试与演示。
  - 最终：Dedicated Server（专用服）更稳定、可控（反作弊、房间托管）。
- 传输层
  - 动作类游戏推荐：kcp2k（低延迟、丢包友好）。
  - 默认 Telepathy 亦可先跑通 Demo，后续切换。
- 物理/网络节拍
  - `Time.fixedDeltaTime` 建议 1/60 或 1/50；Mirror `send rate` 与插值缓冲配合。
- 服务器权威等级
  - 强烈建议“服务器权威移动 + 客户端预测/回滚”，避免加速/穿墙等外挂。

---

## 1. 场景与 NetworkManager 基线

- 在主测试场景中放置 `NetworkManager`（可用 Mirror 提供预制），并：
  - 指定玩家预制体（含 `NetworkIdentity`）。
  - 选择传输层（初期 Telepathy，计划切换 kcp2k）。
- 将测试场景加入 Build Settings 的 `Scenes In Build` 列表。
- 建议创建一个极简测试场景（地面 + 出生点 + 摄像机 + 玩家）专注网络验证。

---

## 2. 角色脚本的结构化（不直接改代码，先规划）

将本地 `Player` 拆为三层（便于网络化）：

- Input（读取输入，纯客户端，支持禁用/启用）
- Motor（真正移动/动画驱动，未来由服务器权威执行）
- Network（`NetworkBehaviour`，处理 SyncVar、Cmd、Rpc、预测/回滚）

这能让你先写出本地可玩的移动，再平滑接入 Mirror。

> 你现有 `Assets/Scripts/Player.cs` 只有空的 `Start/Update`，建议先补齐“本地离线版”输入 + 移动，随后照下文模板接入 Network。

---

## 3. Mirror 脚本模板（仅供粘贴使用）

> 说明：下面代码片段放在文档中以避免影响当前编译。导入 Mirror 后，你可以新建 `NetworkPlayer.cs` 并粘贴使用。

### 3.1 基础网络玩家（服务器生成、名称同步）

```csharp
using Mirror;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [SyncVar] public string displayName;

    // 仅本地拥有者启用输入
    public override void OnStartAuthority()
    {
        enabled = true; // 打开本脚本里的本地输入逻辑（若有）
    }

    public override void OnStartClient()
    {
        // 客户端初始化，例如 UI 绑定
    }

    // 由客户端请求，服务器校验并执行
    [Command]
    public void CmdSetName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName) || newName.Length > 16) return;
        displayName = newName.Trim(); // 服务器改 SyncVar，会自动同步至所有客户端
    }
}
```

### 3.2 服务器权威移动（客户端发送输入 → 服务器模拟 → 广播）

```csharp
using Mirror;
using UnityEngine;

public class NetworkMovement : NetworkBehaviour
{
    [Header("Move")]
    public float moveSpeed = 6f;

    [SyncVar] private Vector3 serverPosition;
    [SyncVar] private Quaternion serverRotation;

    private Vector2 input;

    void Update()
    {
        if (hasAuthority)
        {
            // 仅本地读输入
            input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (input != Vector2.zero)
                CmdSendInput(input);
        }
        else
        {
            // 客户端插值到服务器状态（简单版，可扩展时间戳插值）
            transform.position = Vector3.Lerp(transform.position, serverPosition, 0.1f);
            transform.rotation = Quaternion.Slerp(transform.rotation, serverRotation, 0.1f);
        }
    }

    [Command]
    void CmdSendInput(Vector2 input)
    {
        // 服务器进行权威移动
        Vector3 dir = new Vector3(input.x, 0, input.y).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;
        serverPosition = transform.position;
        serverRotation = transform.rotation;
    }
}
```

> 提示：实际项目建议使用“输入打包 + 时间戳 + 预测/回滚”的成熟结构，上面为最小可用版。

---

## 4. 本地联机与专用服测试（Windows）

- 编辑器 Host + 构建客户端
  1. 在编辑器点击 `Play`，`NetworkManager HUD` 选择 Host（或 Start Host）。
  2. 构建一次 Windows 可执行文件，双开 EXE，选择 `Client` 加入。
- 专用服（Headless）
  1. 在 Build Settings 勾选 `Server Build`（Unity 6 支持独立 Server 构建）。
  2. 运行服务端时可使用参数（示例）：

```powershell
# 启动专用服示例（替换为你的 EXE 路径与端口）
./Builds/Windows/Server/MyGameServer.exe -batchmode -nographics -logFile server.log -port 7777
```

  3. 客户端通过 `NetworkManager` 指定的地址与端口连接。

---

## 5. 性能与带宽

- SyncVar 只用于小状态；Transform 建议用 `NetworkTransform` 或自定义压缩同步。
- 动作游戏建议 30~60Hz 网络发送率；插值缓冲 100~200ms 视延迟而定。
- 粒子/特效只在本地触发与广播事件，不要逐帧同步其参数。

---

## 6. 安全与公平

- 服务器校验关键动作（攻击命中、位移上限、冷却等）。
- 输入节流/速率限制，避免恶意刷包。
- 重要计算仅在服务器进行（伤害、得分、掉落）。

---

## 7. 内容与流程

- 出生点系统：多点随机/阵营点位。
- 房间/匹配：先用 `NetworkRoomManager` 快速出 Lobby；后续接第三方匹配或自建后端。
- 断线重连/重生：玩家对象与 UI 状态重建流程。

---

## 8. 两周里程碑（可按需微调）

- 第 1 周
  - D1-D2：NetworkManager + 最小玩家生成，Host/Client 互见。
  - D3-D4：服务器权威移动 + 客户端插值（含摄像机跟随）。
  - D5：基础近战/射击交互（命中判定先走服务器直判）。
  - D6：房间/换装 UI（可用 RoomManager 样例）。
  - D7：稳定性回归（多客户端同时进入/退出）。
- 第 2 周
  - D8-D9：命中延迟补偿（简单版回溯或服务器胶囊重检）。
  - D10：同步优化（带宽统计、压缩、发送率整定）。
  - D11：专用服构建与运行脚本（日志与崩溃收集）。
  - D12：基础反作弊（速度阈值、技能冷却服务端强校验）。
  - D13-D14：打包演示场景与操作教程文档。

---

## 9. 常见坑清单（Unity 6/Mirror）

- 忘记给玩家预制体加 `NetworkIdentity` → 客户端看不到或不同步。
- 未在 `Scenes In Build` 添加测试场景 → 构建端进不去。
- 在客户端直接改 Transform（非权威）→ 抖动或被服务器回弹。
- 过度使用 SyncVar（大字段、高频）→ 带宽爆炸，改事件/序列化。
- URP/HDRP 转换后材质/后处理异常 → 逐项核对管线资产与材质 Shader。

---

## 10. 下一步执行建议

1. 按“1、2”节完成 NetworkManager 与玩家预制体接线，验证 Host/Client 能互见。
2. 用“3.2 模板”做出最小服务器权威移动（可直接粘贴新脚本，不要覆盖现有 `Player.cs`）。
3. 跑一轮“4 节”中的本地联机与专用服测试，确认日志与端口可用。
4. 开始实现你的核心动作（翻滚、轻/重击、格挡），服务端校验为先。

若你希望，我可以把模板整理为实际脚本文件并放在 `Assets/Scripts/` 中（确保 Mirror 已导入），或根据你的玩法原型直接改造 `Player.cs`。