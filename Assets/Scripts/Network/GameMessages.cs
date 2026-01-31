using Mirror;
using System;
using System.Collections.Generic;

namespace MultiplayerGame.Network
{
    #region 物品相关消息

    /// <summary>
    /// 客户端 -> 服务器：请求获取物品列表
    /// </summary>
    public struct RequestInventoryMessage : NetworkMessage { }

    /// <summary>
    /// 服务器 -> 客户端：物品列表响应
    /// </summary>
    public struct InventoryResponseMessage : NetworkMessage
    {
        public bool success;
        public string itemsJson;  // JSON 序列化的物品列表
    }

    /// <summary>
    /// 客户端 -> 服务器：使用物品
    /// </summary>
    public struct UseItemMessage : NetworkMessage
    {
        public int itemId;
        public int count;
    }

    /// <summary>
    /// 服务器 -> 客户端：物品操作结果
    /// </summary>
    public struct ItemOperationResultMessage : NetworkMessage
    {
        public bool success;
        public string operation;  // use, equip, unequip, delete
        public int itemId;
        public string message;
    }

    /// <summary>
    /// 客户端 -> 服务器：装备/卸下物品
    /// </summary>
    public struct EquipItemMessage : NetworkMessage
    {
        public int itemId;
        public bool equip;  // true = 装备, false = 卸下
    }

    #endregion

    #region 奖励相关消息

    /// <summary>
    /// 客户端 -> 服务器：请求获取奖励列表
    /// </summary>
    public struct RequestRewardsMessage : NetworkMessage { }

    /// <summary>
    /// 服务器 -> 客户端：奖励列表响应
    /// </summary>
    public struct RewardsResponseMessage : NetworkMessage
    {
        public bool success;
        public string rewardsJson;  // JSON 序列化的奖励列表
    }

    /// <summary>
    /// 客户端 -> 服务器：领取奖励
    /// </summary>
    public struct ClaimRewardMessage : NetworkMessage
    {
        public int rewardId;
    }

    /// <summary>
    /// 服务器 -> 客户端：领取奖励结果
    /// </summary>
    public struct ClaimRewardResultMessage : NetworkMessage
    {
        public bool success;
        public int rewardId;
        public string message;
        public string contentJson;  // 获得的奖励内容 JSON
    }

    #endregion

    #region 邮件相关消息

    /// <summary>
    /// 客户端 -> 服务器：请求获取邮件列表
    /// </summary>
    public struct RequestMailsMessage : NetworkMessage { }

    /// <summary>
    /// 服务器 -> 客户端：邮件列表响应
    /// </summary>
    public struct MailsResponseMessage : NetworkMessage
    {
        public bool success;
        public string mailsJson;  // JSON 序列化的邮件列表
    }

    /// <summary>
    /// 客户端 -> 服务器：阅读邮件
    /// </summary>
    public struct ReadMailMessage : NetworkMessage
    {
        public int mailId;
    }

    /// <summary>
    /// 客户端 -> 服务器：领取邮件附件
    /// </summary>
    public struct ClaimMailAttachmentsMessage : NetworkMessage
    {
        public int mailId;
    }

    /// <summary>
    /// 服务器 -> 客户端：邮件操作结果
    /// </summary>
    public struct MailOperationResultMessage : NetworkMessage
    {
        public bool success;
        public string operation;  // read, claim, delete
        public int mailId;
        public string message;
        public string contentJson;  // 附件内容 JSON（领取时）
    }

    /// <summary>
    /// 客户端 -> 服务器：删除邮件
    /// </summary>
    public struct DeleteMailMessage : NetworkMessage
    {
        public int mailId;
    }

    #endregion

    #region 用户数据同步

    /// <summary>
    /// 客户端 -> 服务器：请求用户数据
    /// </summary>
    public struct RequestUserDataMessage : NetworkMessage { }

    /// <summary>
    /// 服务器 -> 客户端：用户数据响应
    /// </summary>
    public struct UserDataResponseMessage : NetworkMessage
    {
        public bool success;
        public int userId;
        public string username;
        public string displayName;
        public int level;
        public int experience;
        public int gold;
        public int diamond;
        public int unclaimedRewardCount;
        public int unreadMailCount;
    }

    /// <summary>
    /// 服务器 -> 客户端：货币/经验变化通知
    /// </summary>
    public struct CurrencyUpdateMessage : NetworkMessage
    {
        public int gold;
        public int diamond;
        public int experience;
        public int level;
        public string reason;
    }

    #endregion
}
