using System;
using System.Collections.Generic;

namespace MultiplayerGame.Models
{
    /// <summary>
    /// 用户账号信息
    /// </summary>
    [Serializable]
    public class UserAccount
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }        // 存储哈希后的密码
        public string Salt { get; set; }                // 密码盐值
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastLoginAt { get; set; }
        public bool IsBanned { get; set; }
        public string BanReason { get; set; }
        public int Level { get; set; } = 1;
        public int Experience { get; set; } = 0;
        public int Gold { get; set; } = 0;
        public int Diamond { get; set; } = 0;
    }

    /// <summary>
    /// 玩家物品数据
    /// </summary>
    [Serializable]
    public class PlayerItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ItemId { get; set; }              // 物品配置ID
        public string ItemType { get; set; }            // 物品类型: weapon, armor, consumable, material, etc.
        public int Count { get; set; }                  // 数量（堆叠物品）
        public int Level { get; set; }                  // 物品等级/强化等级
        public string ExtraData { get; set; }           // JSON格式的额外数据（词缀、附魔等）
        public DateTime AcquiredAt { get; set; }        // 获得时间
        public bool IsEquipped { get; set; }            // 是否装备中
        public int SlotIndex { get; set; }              // 背包槽位 (-1表示未放置)
    }

    /// <summary>
    /// 奖励记录
    /// </summary>
    [Serializable]
    public class RewardRecord
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string RewardType { get; set; }          // 奖励类型: daily_login, achievement, event, admin, etc.
        public string RewardData { get; set; }          // JSON格式的奖励内容
        public string Description { get; set; }         // 奖励描述
        public DateTime CreatedAt { get; set; }
        public DateTime? ClaimedAt { get; set; }        // 领取时间
        public DateTime? ExpiresAt { get; set; }        // 过期时间
        public bool IsClaimed { get; set; }
    }

    /// <summary>
    /// 邮件系统
    /// </summary>
    [Serializable]
    public class MailItem
    {
        public int Id { get; set; }
        public int ToUserId { get; set; }
        public int? FromUserId { get; set; }            // null 表示系统邮件
        public string Title { get; set; }
        public string Content { get; set; }
        public string Attachments { get; set; }         // JSON格式的附件物品列表
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsRead { get; set; }
        public bool AttachmentsClaimed { get; set; }    // 附件是否已领取
        public bool IsDeleted { get; set; }
    }

    /// <summary>
    /// 登录日志
    /// </summary>
    [Serializable]
    public class LoginLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime LoginTime { get; set; }
        public string IpAddress { get; set; }
        public string DeviceInfo { get; set; }
        public bool Success { get; set; }
        public string FailReason { get; set; }
    }

    /// <summary>
    /// 奖励内容（用于JSON序列化）
    /// </summary>
    [Serializable]
    public class RewardContent
    {
        public int Gold { get; set; }
        public int Diamond { get; set; }
        public int Experience { get; set; }
        public List<ItemReward> Items { get; set; } = new List<ItemReward>();
    }

    [Serializable]
    public class ItemReward
    {
        public string ItemId { get; set; }
        public int Count { get; set; }
        public int Level { get; set; } = 1;
    }

    /// <summary>
    /// 登录/注册结果
    /// </summary>
    public class AuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public UserAccount User { get; set; }
        public string SessionToken { get; set; }
    }
}
