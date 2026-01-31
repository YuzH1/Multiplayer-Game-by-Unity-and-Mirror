# 数据库账号系统使用说明

## 概述

本系统实现了基于 JSON 文件存储的账号管理功能，支持：

- 用户注册/登录（密码加盐哈希存储）
- 物品持有信息存储
- 奖励发放和领取
- 邮件系统（带附件）
- 货币管理（金币、钻石、经验值）

## 文件结构

```
Assets/Scripts/
├── Database/
│   ├── IDatabaseProvider.cs      # 数据库接口定义
│   └── JsonDatabaseProvider.cs   # JSON 文件存储实现
├── Models/
│   └── AccountModels.cs          # 数据模型（UserAccount, PlayerItem, RewardRecord 等）
├── Services/
│   ├── AccountService.cs         # 账号服务（注册、登录、密码验证）
│   ├── GameServices.cs           # 物品、奖励、邮件服务
│   └── GameServiceManager.cs     # 服务管理器（单例）
├── Network/
│   ├── GameMessages.cs           # 网络消息定义
│   └── GameDataNetworkHandler.cs # 网络数据处理器
├── Admin/
│   └── AdminTools.cs             # 管理员工具
├── SimpleAuthenticator.cs        # 认证器（已集成数据库）
└── LoginUI.cs                    # 登录界面
```

## 快速开始

### 1. 场景设置

1. 在场景中创建一个空 GameObject，命名为 `GameServices`
2. 添加 `GameServiceManager` 组件
3. 添加 `AdminTools` 组件（可选，用于管理功能）
4. 在 NetworkManager 对象上：

   - 添加 `GameDataNetworkHandler` 组件
   - 确保 `SimpleAuthenticator` 的 `useDatabaseStorage` 已勾选

### 2. 登录 UI 设置

LoginUI 脚本新增了以下可选字段：

- `statusText`: 显示状态信息的 Text
- `loginPanel`: 登录界面面板
- `userInfoPanel`: 登录后的用户信息面板
- `userDisplayNameText`, `userLevelText`, `userGoldText`, `userDiamondText`: 用户信息显示

### 3. 数据库位置

JSON 数据文件默认存储在：

- Windows: `%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>/GameData/`
- macOS: `~/Library/Application Support/<CompanyName>/<ProductName>/GameData/`

包含以下文件：

- `users.json` - 用户账号数据
- `items.json` - 物品数据
- `rewards.json` - 奖励数据
- `mails.json` - 邮件数据

## API 使用

### 客户端 API

```csharp
using MultiplayerGame;
using MultiplayerGame.Network;

// 请求物品列表
GameDataNetworkHandler.RequestInventory();

// 使用物品
GameDataNetworkHandler.UseItem(itemId, count);

// 装备物品
GameDataNetworkHandler.EquipItem(itemId, true);

// 请求奖励列表
GameDataNetworkHandler.RequestRewards();

// 领取奖励
GameDataNetworkHandler.ClaimReward(rewardId);

// 请求邮件列表
GameDataNetworkHandler.RequestMails();

// 领取邮件附件
GameDataNetworkHandler.ClaimMailAttachments(mailId);

// 请求用户数据
GameDataNetworkHandler.RequestUserData();
```

### 客户端事件订阅

```csharp
void Start()
{
    GameDataNetworkHandler.OnInventoryReceived += OnInventoryReceived;
    GameDataNetworkHandler.OnRewardsReceived += OnRewardsReceived;
    GameDataNetworkHandler.OnMailsReceived += OnMailsReceived;
    GameDataNetworkHandler.OnUserDataReceived += OnUserDataReceived;
    GameDataNetworkHandler.OnCurrencyUpdated += OnCurrencyUpdated;
}

void OnInventoryReceived(List<PlayerItem> items)
{
    // 处理物品列表
}
```

### 服务端 API（管理员工具）

```csharp
using MultiplayerGame.Admin;
using MultiplayerGame.Models;

// 发送奖励
var content = new RewardContent
{
    Gold = 100,
    Diamond = 10,
    Items = new List<ItemReward>
    {
        new ItemReward { ItemId = "sword_001", Count = 1 }
    }
};
await AdminTools.Instance.SendRewardToUserAsync(userId, "event", content, "活动奖励");

// 发送系统邮件
await AdminTools.Instance.SendSystemMailAsync(userId, "欢迎", "欢迎来到游戏！", attachments);

// 直接添加货币
await AdminTools.Instance.GiveGoldAsync(userId, 1000);
await AdminTools.Instance.GiveDiamondAsync(userId, 100);

// 封禁用户
await AdminTools.Instance.BanUserAsync(userId, "违规操作");
```

## 数据模型

### UserAccount（用户账号）

| 字段         | 类型   | 说明           |
| ------------ | ------ | -------------- |
| Id           | int    | 用户ID         |
| Username     | string | 用户名（唯一） |
| PasswordHash | string | 密码哈希       |
| Salt         | string | 密码盐值       |
| DisplayName  | string | 显示名称       |
| Level        | int    | 等级           |
| Experience   | int    | 经验值         |
| Gold         | int    | 金币           |
| Diamond      | int    | 钻石           |
| IsBanned     | bool   | 是否封禁       |

### PlayerItem（玩家物品）

| 字段       | 类型   | 说明           |
| ---------- | ------ | -------------- |
| Id         | int    | 物品实例ID     |
| UserId     | int    | 所属用户ID     |
| ItemId     | string | 物品配置ID     |
| ItemType   | string | 物品类型       |
| Count      | int    | 数量           |
| Level      | int    | 强化等级       |
| IsEquipped | bool   | 是否装备       |
| ExtraData  | string | 额外数据(JSON) |

### RewardRecord（奖励记录）

| 字段       | 类型      | 说明           |
| ---------- | --------- | -------------- |
| Id         | int       | 奖励ID         |
| UserId     | int       | 用户ID         |
| RewardType | string    | 奖励类型       |
| RewardData | string    | 奖励内容(JSON) |
| IsClaimed  | bool      | 是否已领取     |
| ExpiresAt  | DateTime? | 过期时间       |

### MailItem（邮件）

| 字段               | 类型   | 说明                |
| ------------------ | ------ | ------------------- |
| Id                 | int    | 邮件ID              |
| ToUserId           | int    | 收件人ID            |
| FromUserId         | int?   | 发件人ID(null=系统) |
| Title              | string | 标题                |
| Content            | string | 内容                |
| Attachments        | string | 附件(JSON)          |
| IsRead             | bool   | 是否已读            |
| AttachmentsClaimed | bool   | 附件是否已领取      |

## 扩展指南

### 添加 MySQL 支持

1. 创建 `MySqlDatabaseProvider.cs` 实现 `IDatabaseProvider` 接口
2. 使用 MySqlConnector NuGet 包
3. 在 `GameServiceManager` 中添加数据库类型选项

### 添加新的奖励类型

1. 在 `RewardService.cs` 中添加新的发送方法
2. 在 `AdminTools.cs` 中添加对应的管理方法

### 添加新的物品类型

1. 在物品配置中定义新类型
2. 修改 `ItemService` 添加类型特定处理逻辑

## 安全注意事项

1. **密码存储**: 使用 SHA256 + 随机盐值。生产环境建议使用 BCrypt 或 Argon2
2. **会话管理**: 当前使用简单的 GUID 令牌，可扩展为 JWT
3. **传输安全**: 建议启用 Mirror 的加密传输（TLS）
4. **输入验证**: 服务端已包含基本验证，可根据需要加强

## 依赖

- Mirror Networking
- Newtonsoft.Json (com.unity.nuget.newtonsoft-json)
