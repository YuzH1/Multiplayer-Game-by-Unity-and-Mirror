using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MultiplayerGame.Models;

namespace MultiplayerGame.Database
{
    /// <summary>
    /// 数据库提供者接口 - 定义所有数据库操作
    /// 可实现 SQLite、MySQL、PostgreSQL 等不同后端
    /// </summary>
    public interface IDatabaseProvider : IDisposable
    {
        /// <summary>
        /// 初始化数据库连接和表结构
        /// </summary>
        Task InitializeAsync();

        #region 账号操作

        /// <summary>
        /// 创建新用户
        /// </summary>
        Task<UserAccount> CreateUserAsync(string username, string passwordHash, string salt, string displayName, string email = null);

        /// <summary>
        /// 根据用户名查找用户
        /// </summary>
        Task<UserAccount> GetUserByUsernameAsync(string username);

        /// <summary>
        /// 根据ID查找用户
        /// </summary>
        Task<UserAccount> GetUserByIdAsync(int userId);

        /// <summary>
        /// 更新用户信息
        /// </summary>
        Task UpdateUserAsync(UserAccount user);

        /// <summary>
        /// 更新最后登录时间
        /// </summary>
        Task UpdateLastLoginAsync(int userId);

        /// <summary>
        /// 检查用户名是否存在
        /// </summary>
        Task<bool> UsernameExistsAsync(string username);

        /// <summary>
        /// 封禁用户
        /// </summary>
        Task BanUserAsync(int userId, string reason);

        /// <summary>
        /// 解封用户
        /// </summary>
        Task UnbanUserAsync(int userId);

        #endregion

        #region 物品操作

        /// <summary>
        /// 获取玩家所有物品
        /// </summary>
        Task<List<PlayerItem>> GetPlayerItemsAsync(int userId);

        /// <summary>
        /// 添加物品到玩家背包
        /// </summary>
        Task<PlayerItem> AddItemAsync(int userId, string itemId, string itemType, int count = 1, int level = 1, string extraData = null);

        /// <summary>
        /// 更新物品信息
        /// </summary>
        Task UpdateItemAsync(PlayerItem item);

        /// <summary>
        /// 删除物品
        /// </summary>
        Task DeleteItemAsync(int itemId);

        /// <summary>
        /// 获取特定物品
        /// </summary>
        Task<PlayerItem> GetItemByIdAsync(int itemId);

        /// <summary>
        /// 修改物品数量（堆叠物品）
        /// </summary>
        Task<bool> UpdateItemCountAsync(int itemId, int deltaCount);

        #endregion

        #region 奖励操作

        /// <summary>
        /// 创建奖励记录
        /// </summary>
        Task<RewardRecord> CreateRewardAsync(int userId, string rewardType, string rewardData, string description, DateTime? expiresAt = null);

        /// <summary>
        /// 获取用户未领取的奖励
        /// </summary>
        Task<List<RewardRecord>> GetUnclaimedRewardsAsync(int userId);

        /// <summary>
        /// 领取奖励
        /// </summary>
        Task<bool> ClaimRewardAsync(int rewardId, int userId);

        /// <summary>
        /// 获取奖励详情
        /// </summary>
        Task<RewardRecord> GetRewardByIdAsync(int rewardId);

        /// <summary>
        /// 批量发送奖励给多个用户
        /// </summary>
        Task BatchSendRewardsAsync(List<int> userIds, string rewardType, string rewardData, string description, DateTime? expiresAt = null);

        #endregion

        #region 邮件操作

        /// <summary>
        /// 发送邮件
        /// </summary>
        Task<MailItem> SendMailAsync(int toUserId, int? fromUserId, string title, string content, string attachments = null, DateTime? expiresAt = null);

        /// <summary>
        /// 获取用户邮件列表
        /// </summary>
        Task<List<MailItem>> GetMailsAsync(int userId, bool includeDeleted = false);

        /// <summary>
        /// 标记邮件已读
        /// </summary>
        Task MarkMailReadAsync(int mailId);

        /// <summary>
        /// 领取邮件附件
        /// </summary>
        Task<bool> ClaimMailAttachmentsAsync(int mailId, int userId);

        /// <summary>
        /// 删除邮件（软删除）
        /// </summary>
        Task DeleteMailAsync(int mailId);

        #endregion

        #region 日志操作

        /// <summary>
        /// 记录登录日志
        /// </summary>
        Task LogLoginAsync(int userId, string ipAddress, string deviceInfo, bool success, string failReason = null);

        /// <summary>
        /// 获取登录日志
        /// </summary>
        Task<List<LoginLog>> GetLoginLogsAsync(int userId, int limit = 10);

        #endregion

        #region 货币操作

        /// <summary>
        /// 增加金币
        /// </summary>
        Task<bool> AddGoldAsync(int userId, int amount, string reason = null);

        /// <summary>
        /// 扣除金币
        /// </summary>
        Task<bool> DeductGoldAsync(int userId, int amount, string reason = null);

        /// <summary>
        /// 增加钻石
        /// </summary>
        Task<bool> AddDiamondAsync(int userId, int amount, string reason = null);

        /// <summary>
        /// 扣除钻石
        /// </summary>
        Task<bool> DeductDiamondAsync(int userId, int amount, string reason = null);

        /// <summary>
        /// 增加经验值
        /// </summary>
        Task<bool> AddExperienceAsync(int userId, int amount);

        #endregion
    }
}
