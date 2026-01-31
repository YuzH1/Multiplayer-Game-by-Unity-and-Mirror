using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MultiplayerGame.Database;
using MultiplayerGame.Models;
using UnityEngine;
using Newtonsoft.Json;

namespace MultiplayerGame.Services
{
    /// <summary>
    /// 物品服务 - 处理玩家物品的增删改查
    /// </summary>
    public class ItemService
    {
        private readonly IDatabaseProvider database;

        public ItemService(IDatabaseProvider databaseProvider)
        {
            database = databaseProvider ?? throw new ArgumentNullException(nameof(databaseProvider));
        }

        /// <summary>
        /// 获取玩家所有物品
        /// </summary>
        public async Task<List<PlayerItem>> GetPlayerItemsAsync(int userId)
        {
            return await database.GetPlayerItemsAsync(userId);
        }

        /// <summary>
        /// 添加物品到玩家背包
        /// </summary>
        public async Task<PlayerItem> AddItemAsync(int userId, string itemId, string itemType, int count = 1, int level = 1, string extraData = null)
        {
            var item = await database.AddItemAsync(userId, itemId, itemType, count, level, extraData);
            Debug.Log($"[ItemService] 添加物品: userId={userId}, itemId={itemId}, count={count}");
            return item;
        }

        /// <summary>
        /// 批量添加物品
        /// </summary>
        public async Task<List<PlayerItem>> AddItemsAsync(int userId, List<ItemReward> items)
        {
            var result = new List<PlayerItem>();
            foreach (var item in items)
            {
                var added = await AddItemAsync(userId, item.ItemId, "reward", item.Count, item.Level);
                if (added != null)
                    result.Add(added);
            }
            return result;
        }

        /// <summary>
        /// 更新物品信息
        /// </summary>
        public async Task UpdateItemAsync(PlayerItem item)
        {
            await database.UpdateItemAsync(item);
        }

        /// <summary>
        /// 删除物品
        /// </summary>
        public async Task DeleteItemAsync(int itemId)
        {
            await database.DeleteItemAsync(itemId);
        }

        /// <summary>
        /// 获取特定物品
        /// </summary>
        public async Task<PlayerItem> GetItemByIdAsync(int itemId)
        {
            return await database.GetItemByIdAsync(itemId);
        }

        /// <summary>
        /// 修改物品数量
        /// </summary>
        public async Task<bool> UpdateItemCountAsync(int itemId, int deltaCount)
        {
            return await database.UpdateItemCountAsync(itemId, deltaCount);
        }

        /// <summary>
        /// 使用物品（消耗品）
        /// </summary>
        public async Task<bool> UseItemAsync(int userId, int itemId, int count = 1)
        {
            var item = await database.GetItemByIdAsync(itemId);
            if (item == null || item.UserId != userId)
                return false;

            if (item.Count < count)
                return false;

            return await database.UpdateItemCountAsync(itemId, -count);
        }

        /// <summary>
        /// 装备物品
        /// </summary>
        public async Task<bool> EquipItemAsync(int userId, int itemId)
        {
            var item = await database.GetItemByIdAsync(itemId);
            if (item == null || item.UserId != userId)
                return false;

            item.IsEquipped = true;
            await database.UpdateItemAsync(item);
            return true;
        }

        /// <summary>
        /// 卸下装备
        /// </summary>
        public async Task<bool> UnequipItemAsync(int userId, int itemId)
        {
            var item = await database.GetItemByIdAsync(itemId);
            if (item == null || item.UserId != userId)
                return false;

            item.IsEquipped = false;
            await database.UpdateItemAsync(item);
            return true;
        }
    }

    /// <summary>
    /// 奖励服务 - 处理奖励的发放和领取
    /// </summary>
    public class RewardService
    {
        private readonly IDatabaseProvider database;
        private readonly ItemService itemService;

        public RewardService(IDatabaseProvider databaseProvider, ItemService itemService)
        {
            database = databaseProvider ?? throw new ArgumentNullException(nameof(databaseProvider));
            this.itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));
        }

        /// <summary>
        /// 创建奖励记录
        /// </summary>
        public async Task<RewardRecord> CreateRewardAsync(int userId, string rewardType, RewardContent content, string description, DateTime? expiresAt = null)
        {
            var rewardData = JsonConvert.SerializeObject(content);
            var reward = await database.CreateRewardAsync(userId, rewardType, rewardData, description, expiresAt);
            Debug.Log($"[RewardService] 创建奖励: userId={userId}, type={rewardType}");
            return reward;
        }

        /// <summary>
        /// 获取未领取的奖励
        /// </summary>
        public async Task<List<RewardRecord>> GetUnclaimedRewardsAsync(int userId)
        {
            return await database.GetUnclaimedRewardsAsync(userId);
        }

        /// <summary>
        /// 领取奖励
        /// </summary>
        public async Task<(bool success, string message, RewardContent content)> ClaimRewardAsync(int userId, int rewardId)
        {
            try
            {
                // 获取奖励信息
                var reward = await database.GetRewardByIdAsync(rewardId);
                if (reward == null)
                    return (false, "奖励不存在", null);

                if (reward.UserId != userId)
                    return (false, "无权领取此奖励", null);

                if (reward.IsClaimed)
                    return (false, "奖励已领取", null);

                if (reward.ExpiresAt.HasValue && reward.ExpiresAt.Value < DateTime.UtcNow)
                    return (false, "奖励已过期", null);

                // 解析奖励内容
                var content = JsonConvert.DeserializeObject<RewardContent>(reward.RewardData);
                if (content == null)
                    return (false, "奖励数据解析失败", null);

                // 发放奖励
                if (content.Gold > 0)
                    await database.AddGoldAsync(userId, content.Gold, $"领取奖励: {reward.Description}");

                if (content.Diamond > 0)
                    await database.AddDiamondAsync(userId, content.Diamond, $"领取奖励: {reward.Description}");

                if (content.Experience > 0)
                    await database.AddExperienceAsync(userId, content.Experience);

                // 发放物品
                if (content.Items != null && content.Items.Count > 0)
                {
                    await itemService.AddItemsAsync(userId, content.Items);
                }

                // 标记已领取
                await database.ClaimRewardAsync(rewardId, userId);

                Debug.Log($"[RewardService] 领取奖励成功: userId={userId}, rewardId={rewardId}");
                return (true, "领取成功", content);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RewardService] 领取奖励异常: {ex.Message}");
                return (false, "领取过程发生错误", null);
            }
        }

        /// <summary>
        /// 批量发送奖励（管理员/系统用）
        /// </summary>
        public async Task BatchSendRewardsAsync(List<int> userIds, string rewardType, RewardContent content, string description, DateTime? expiresAt = null)
        {
            var rewardData = JsonConvert.SerializeObject(content);
            await database.BatchSendRewardsAsync(userIds, rewardType, rewardData, description, expiresAt);
            Debug.Log($"[RewardService] 批量发送奖励: {userIds.Count}人, type={rewardType}");
        }

        /// <summary>
        /// 发送每日登录奖励
        /// </summary>
        public async Task<RewardRecord> SendDailyLoginRewardAsync(int userId, int consecutiveDays)
        {
            // 根据连续登录天数计算奖励
            var content = new RewardContent
            {
                Gold = 100 * consecutiveDays,
                Experience = 50 * consecutiveDays
            };

            // 每7天额外奖励
            if (consecutiveDays % 7 == 0)
            {
                content.Diamond = 10;
                content.Items.Add(new ItemReward { ItemId = "reward_box_001", Count = 1 });
            }

            return await CreateRewardAsync(userId, "daily_login", content, $"连续登录第{consecutiveDays}天奖励", DateTime.UtcNow.AddDays(7));
        }
    }

    /// <summary>
    /// 邮件服务 - 处理游戏内邮件
    /// </summary>
    public class MailService
    {
        private readonly IDatabaseProvider database;
        private readonly ItemService itemService;

        public MailService(IDatabaseProvider databaseProvider, ItemService itemService)
        {
            database = databaseProvider ?? throw new ArgumentNullException(nameof(databaseProvider));
            this.itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));
        }

        /// <summary>
        /// 发送系统邮件
        /// </summary>
        public async Task<MailItem> SendSystemMailAsync(int toUserId, string title, string content, RewardContent attachments = null, DateTime? expiresAt = null)
        {
            var attachmentJson = attachments != null ? JsonConvert.SerializeObject(attachments) : null;
            var mail = await database.SendMailAsync(toUserId, null, title, content, attachmentJson, expiresAt);
            Debug.Log($"[MailService] 发送系统邮件: toUserId={toUserId}, title={title}");
            return mail;
        }

        /// <summary>
        /// 发送玩家邮件
        /// </summary>
        public async Task<MailItem> SendPlayerMailAsync(int fromUserId, int toUserId, string title, string content)
        {
            var mail = await database.SendMailAsync(toUserId, fromUserId, title, content, null, DateTime.UtcNow.AddDays(30));
            Debug.Log($"[MailService] 发送玩家邮件: from={fromUserId}, to={toUserId}");
            return mail;
        }

        /// <summary>
        /// 获取邮件列表
        /// </summary>
        public async Task<List<MailItem>> GetMailsAsync(int userId)
        {
            return await database.GetMailsAsync(userId, false);
        }

        /// <summary>
        /// 标记邮件已读
        /// </summary>
        public async Task MarkMailReadAsync(int mailId)
        {
            await database.MarkMailReadAsync(mailId);
        }

        /// <summary>
        /// 领取邮件附件
        /// </summary>
        public async Task<(bool success, string message, RewardContent content)> ClaimMailAttachmentsAsync(int userId, int mailId)
        {
            try
            {
                var mails = await database.GetMailsAsync(userId, false);
                var mail = mails.Find(m => m.Id == mailId);

                if (mail == null)
                    return (false, "邮件不存在", null);

                if (mail.ToUserId != userId)
                    return (false, "无权操作此邮件", null);

                if (string.IsNullOrEmpty(mail.Attachments))
                    return (false, "邮件没有附件", null);

                if (mail.AttachmentsClaimed)
                    return (false, "附件已领取", null);

                // 解析附件
                var content = JsonConvert.DeserializeObject<RewardContent>(mail.Attachments);
                if (content == null)
                    return (false, "附件数据解析失败", null);

                // 发放奖励
                if (content.Gold > 0)
                    await database.AddGoldAsync(userId, content.Gold, $"邮件附件: {mail.Title}");

                if (content.Diamond > 0)
                    await database.AddDiamondAsync(userId, content.Diamond, $"邮件附件: {mail.Title}");

                if (content.Experience > 0)
                    await database.AddExperienceAsync(userId, content.Experience);

                if (content.Items != null && content.Items.Count > 0)
                {
                    await itemService.AddItemsAsync(userId, content.Items);
                }

                // 标记附件已领取
                await database.ClaimMailAttachmentsAsync(mailId, userId);

                Debug.Log($"[MailService] 领取邮件附件: userId={userId}, mailId={mailId}");
                return (true, "领取成功", content);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MailService] 领取邮件附件异常: {ex.Message}");
                return (false, "领取过程发生错误", null);
            }
        }

        /// <summary>
        /// 删除邮件
        /// </summary>
        public async Task DeleteMailAsync(int mailId)
        {
            await database.DeleteMailAsync(mailId);
        }
    }
}
