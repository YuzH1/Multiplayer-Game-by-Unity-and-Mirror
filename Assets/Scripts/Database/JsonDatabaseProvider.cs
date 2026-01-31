using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MultiplayerGame.Models;
using UnityEngine;
using Newtonsoft.Json;

namespace MultiplayerGame.Database
{
    /// <summary>
    /// JSON 文件数据库提供者实现
    /// 适用于开发测试、小规模应用
    /// 数据以 JSON 文件形式存储在 persistentDataPath
    /// </summary>
    public class JsonDatabaseProvider : IDatabaseProvider
    {
        private readonly string dataFolder;
        private readonly object lockObj = new object();

        // 数据存储
        private Dictionary<int, UserAccount> users = new Dictionary<int, UserAccount>();
        private Dictionary<string, int> usernameIndex = new Dictionary<string, int>();
        private Dictionary<int, List<PlayerItem>> playerItems = new Dictionary<int, List<PlayerItem>>();
        private Dictionary<int, List<RewardRecord>> rewards = new Dictionary<int, List<RewardRecord>>();
        private Dictionary<int, List<MailItem>> mails = new Dictionary<int, List<MailItem>>();
        private List<LoginLog> loginLogs = new List<LoginLog>();

        private int nextUserId = 1;
        private int nextItemId = 1;
        private int nextRewardId = 1;
        private int nextMailId = 1;
        private int nextLogId = 1;

        public JsonDatabaseProvider(string folderName = "GameData")
        {
            dataFolder = Path.Combine(Application.persistentDataPath, folderName);
            Debug.Log($"[JsonDB] 数据文件夹: {dataFolder}");
        }

        public async Task InitializeAsync()
        {
            await Task.Run(() =>
            {
                // 确保数据文件夹存在
                if (!Directory.Exists(dataFolder))
                {
                    Directory.CreateDirectory(dataFolder);
                }

                // 加载已有数据
                LoadData();
            });

            Debug.Log("[JsonDB] 数据库初始化完成");
        }

        #region 数据持久化

        private void LoadData()
        {
            lock (lockObj)
            {
                try
                {
                    // 加载用户数据
                    var usersFile = Path.Combine(dataFolder, "users.json");
                    if (File.Exists(usersFile))
                    {
                        var json = File.ReadAllText(usersFile);
                        var data = JsonConvert.DeserializeObject<UserDataStore>(json);
                        if (data != null)
                        {
                            users = data.Users ?? new Dictionary<int, UserAccount>();
                            usernameIndex = data.UsernameIndex ?? new Dictionary<string, int>();
                            nextUserId = data.NextUserId;
                        }
                    }

                    // 加载物品数据
                    var itemsFile = Path.Combine(dataFolder, "items.json");
                    if (File.Exists(itemsFile))
                    {
                        var json = File.ReadAllText(itemsFile);
                        var data = JsonConvert.DeserializeObject<ItemDataStore>(json);
                        if (data != null)
                        {
                            playerItems = data.Items ?? new Dictionary<int, List<PlayerItem>>();
                            nextItemId = data.NextItemId;
                        }
                    }

                    // 加载奖励数据
                    var rewardsFile = Path.Combine(dataFolder, "rewards.json");
                    if (File.Exists(rewardsFile))
                    {
                        var json = File.ReadAllText(rewardsFile);
                        var data = JsonConvert.DeserializeObject<RewardDataStore>(json);
                        if (data != null)
                        {
                            rewards = data.Rewards ?? new Dictionary<int, List<RewardRecord>>();
                            nextRewardId = data.NextRewardId;
                        }
                    }

                    // 加载邮件数据
                    var mailsFile = Path.Combine(dataFolder, "mails.json");
                    if (File.Exists(mailsFile))
                    {
                        var json = File.ReadAllText(mailsFile);
                        var data = JsonConvert.DeserializeObject<MailDataStore>(json);
                        if (data != null)
                        {
                            mails = data.Mails ?? new Dictionary<int, List<MailItem>>();
                            nextMailId = data.NextMailId;
                        }
                    }

                    Debug.Log($"[JsonDB] 数据加载完成: {users.Count} 用户");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[JsonDB] 加载数据失败: {ex.Message}");
                }
            }
        }

        private void SaveUsers()
        {
            lock (lockObj)
            {
                try
                {
                    var data = new UserDataStore
                    {
                        Users = users,
                        UsernameIndex = usernameIndex,
                        NextUserId = nextUserId
                    };
                    var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                    File.WriteAllText(Path.Combine(dataFolder, "users.json"), json);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[JsonDB] 保存用户数据失败: {ex.Message}");
                }
            }
        }

        private void SaveItems()
        {
            lock (lockObj)
            {
                try
                {
                    var data = new ItemDataStore
                    {
                        Items = playerItems,
                        NextItemId = nextItemId
                    };
                    var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                    File.WriteAllText(Path.Combine(dataFolder, "items.json"), json);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[JsonDB] 保存物品数据失败: {ex.Message}");
                }
            }
        }

        private void SaveRewards()
        {
            lock (lockObj)
            {
                try
                {
                    var data = new RewardDataStore
                    {
                        Rewards = rewards,
                        NextRewardId = nextRewardId
                    };
                    var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                    File.WriteAllText(Path.Combine(dataFolder, "rewards.json"), json);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[JsonDB] 保存奖励数据失败: {ex.Message}");
                }
            }
        }

        private void SaveMails()
        {
            lock (lockObj)
            {
                try
                {
                    var data = new MailDataStore
                    {
                        Mails = mails,
                        NextMailId = nextMailId
                    };
                    var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                    File.WriteAllText(Path.Combine(dataFolder, "mails.json"), json);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[JsonDB] 保存邮件数据失败: {ex.Message}");
                }
            }
        }

        #endregion

        #region 账号操作

        public Task<UserAccount> CreateUserAsync(string username, string passwordHash, string salt, string displayName, string email = null)
        {
            lock (lockObj)
            {
                var user = new UserAccount
                {
                    Id = nextUserId++,
                    Username = username,
                    PasswordHash = passwordHash,
                    Salt = salt,
                    DisplayName = displayName ?? username,
                    Email = email,
                    CreatedAt = DateTime.UtcNow,
                    Level = 1,
                    Experience = 0,
                    Gold = 0,
                    Diamond = 0
                };

                users[user.Id] = user;
                usernameIndex[username.ToLowerInvariant()] = user.Id;
                SaveUsers();

                return Task.FromResult(user);
            }
        }

        public Task<UserAccount> GetUserByUsernameAsync(string username)
        {
            lock (lockObj)
            {
                if (usernameIndex.TryGetValue(username.ToLowerInvariant(), out var userId))
                {
                    if (users.TryGetValue(userId, out var user))
                    {
                        return Task.FromResult(user);
                    }
                }
                return Task.FromResult<UserAccount>(null);
            }
        }

        public Task<UserAccount> GetUserByIdAsync(int userId)
        {
            lock (lockObj)
            {
                users.TryGetValue(userId, out var user);
                return Task.FromResult(user);
            }
        }

        public Task UpdateUserAsync(UserAccount user)
        {
            lock (lockObj)
            {
                if (users.ContainsKey(user.Id))
                {
                    users[user.Id] = user;
                    SaveUsers();
                }
            }
            return Task.CompletedTask;
        }

        public Task UpdateLastLoginAsync(int userId)
        {
            lock (lockObj)
            {
                if (users.TryGetValue(userId, out var user))
                {
                    user.LastLoginAt = DateTime.UtcNow;
                    SaveUsers();
                }
            }
            return Task.CompletedTask;
        }

        public Task<bool> UsernameExistsAsync(string username)
        {
            lock (lockObj)
            {
                return Task.FromResult(usernameIndex.ContainsKey(username.ToLowerInvariant()));
            }
        }

        public Task BanUserAsync(int userId, string reason)
        {
            lock (lockObj)
            {
                if (users.TryGetValue(userId, out var user))
                {
                    user.IsBanned = true;
                    user.BanReason = reason;
                    SaveUsers();
                }
            }
            return Task.CompletedTask;
        }

        public Task UnbanUserAsync(int userId)
        {
            lock (lockObj)
            {
                if (users.TryGetValue(userId, out var user))
                {
                    user.IsBanned = false;
                    user.BanReason = null;
                    SaveUsers();
                }
            }
            return Task.CompletedTask;
        }

        #endregion

        #region 物品操作

        public Task<List<PlayerItem>> GetPlayerItemsAsync(int userId)
        {
            lock (lockObj)
            {
                if (playerItems.TryGetValue(userId, out var items))
                {
                    return Task.FromResult(items.ToList());
                }
                return Task.FromResult(new List<PlayerItem>());
            }
        }

        public Task<PlayerItem> AddItemAsync(int userId, string itemId, string itemType, int count = 1, int level = 1, string extraData = null)
        {
            lock (lockObj)
            {
                var item = new PlayerItem
                {
                    Id = nextItemId++,
                    UserId = userId,
                    ItemId = itemId,
                    ItemType = itemType,
                    Count = count,
                    Level = level,
                    ExtraData = extraData,
                    AcquiredAt = DateTime.UtcNow,
                    SlotIndex = -1
                };

                if (!playerItems.ContainsKey(userId))
                {
                    playerItems[userId] = new List<PlayerItem>();
                }
                playerItems[userId].Add(item);
                SaveItems();

                return Task.FromResult(item);
            }
        }

        public Task UpdateItemAsync(PlayerItem item)
        {
            lock (lockObj)
            {
                if (playerItems.TryGetValue(item.UserId, out var items))
                {
                    var index = items.FindIndex(i => i.Id == item.Id);
                    if (index >= 0)
                    {
                        items[index] = item;
                        SaveItems();
                    }
                }
            }
            return Task.CompletedTask;
        }

        public Task DeleteItemAsync(int itemId)
        {
            lock (lockObj)
            {
                foreach (var items in playerItems.Values)
                {
                    var index = items.FindIndex(i => i.Id == itemId);
                    if (index >= 0)
                    {
                        items.RemoveAt(index);
                        SaveItems();
                        break;
                    }
                }
            }
            return Task.CompletedTask;
        }

        public Task<PlayerItem> GetItemByIdAsync(int itemId)
        {
            lock (lockObj)
            {
                foreach (var items in playerItems.Values)
                {
                    var item = items.FirstOrDefault(i => i.Id == itemId);
                    if (item != null)
                    {
                        return Task.FromResult(item);
                    }
                }
                return Task.FromResult<PlayerItem>(null);
            }
        }

        public async Task<bool> UpdateItemCountAsync(int itemId, int deltaCount)
        {
            var item = await GetItemByIdAsync(itemId);
            if (item == null) return false;

            if (item.Count + deltaCount < 0) return false;

            if (item.Count + deltaCount == 0)
            {
                await DeleteItemAsync(itemId);
                return true;
            }

            item.Count += deltaCount;
            await UpdateItemAsync(item);
            return true;
        }

        #endregion

        #region 奖励操作

        public Task<RewardRecord> CreateRewardAsync(int userId, string rewardType, string rewardData, string description, DateTime? expiresAt = null)
        {
            lock (lockObj)
            {
                var reward = new RewardRecord
                {
                    Id = nextRewardId++,
                    UserId = userId,
                    RewardType = rewardType,
                    RewardData = rewardData,
                    Description = description,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt,
                    IsClaimed = false
                };

                if (!rewards.ContainsKey(userId))
                {
                    rewards[userId] = new List<RewardRecord>();
                }
                rewards[userId].Add(reward);
                SaveRewards();

                return Task.FromResult(reward);
            }
        }

        public Task<List<RewardRecord>> GetUnclaimedRewardsAsync(int userId)
        {
            lock (lockObj)
            {
                var now = DateTime.UtcNow;
                if (rewards.TryGetValue(userId, out var userRewards))
                {
                    var unclaimed = userRewards
                        .Where(r => !r.IsClaimed && (!r.ExpiresAt.HasValue || r.ExpiresAt.Value > now))
                        .OrderByDescending(r => r.CreatedAt)
                        .ToList();
                    return Task.FromResult(unclaimed);
                }
                return Task.FromResult(new List<RewardRecord>());
            }
        }

        public Task<bool> ClaimRewardAsync(int rewardId, int userId)
        {
            lock (lockObj)
            {
                if (rewards.TryGetValue(userId, out var userRewards))
                {
                    var reward = userRewards.FirstOrDefault(r => r.Id == rewardId);
                    if (reward != null && !reward.IsClaimed)
                    {
                        if (reward.ExpiresAt.HasValue && reward.ExpiresAt.Value < DateTime.UtcNow)
                        {
                            return Task.FromResult(false);
                        }

                        reward.IsClaimed = true;
                        reward.ClaimedAt = DateTime.UtcNow;
                        SaveRewards();
                        return Task.FromResult(true);
                    }
                }
                return Task.FromResult(false);
            }
        }

        public Task<RewardRecord> GetRewardByIdAsync(int rewardId)
        {
            lock (lockObj)
            {
                foreach (var userRewards in rewards.Values)
                {
                    var reward = userRewards.FirstOrDefault(r => r.Id == rewardId);
                    if (reward != null)
                    {
                        return Task.FromResult(reward);
                    }
                }
                return Task.FromResult<RewardRecord>(null);
            }
        }

        public Task BatchSendRewardsAsync(List<int> userIds, string rewardType, string rewardData, string description, DateTime? expiresAt = null)
        {
            lock (lockObj)
            {
                foreach (var userId in userIds)
                {
                    var reward = new RewardRecord
                    {
                        Id = nextRewardId++,
                        UserId = userId,
                        RewardType = rewardType,
                        RewardData = rewardData,
                        Description = description,
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = expiresAt,
                        IsClaimed = false
                    };

                    if (!rewards.ContainsKey(userId))
                    {
                        rewards[userId] = new List<RewardRecord>();
                    }
                    rewards[userId].Add(reward);
                }
                SaveRewards();
            }
            return Task.CompletedTask;
        }

        #endregion

        #region 邮件操作

        public Task<MailItem> SendMailAsync(int toUserId, int? fromUserId, string title, string content, string attachments = null, DateTime? expiresAt = null)
        {
            lock (lockObj)
            {
                var mail = new MailItem
                {
                    Id = nextMailId++,
                    ToUserId = toUserId,
                    FromUserId = fromUserId,
                    Title = title,
                    Content = content,
                    Attachments = attachments,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt,
                    IsRead = false,
                    AttachmentsClaimed = false,
                    IsDeleted = false
                };

                if (!mails.ContainsKey(toUserId))
                {
                    mails[toUserId] = new List<MailItem>();
                }
                mails[toUserId].Add(mail);
                SaveMails();

                return Task.FromResult(mail);
            }
        }

        public Task<List<MailItem>> GetMailsAsync(int userId, bool includeDeleted = false)
        {
            lock (lockObj)
            {
                if (mails.TryGetValue(userId, out var userMails))
                {
                    var result = includeDeleted
                        ? userMails.ToList()
                        : userMails.Where(m => !m.IsDeleted).ToList();
                    return Task.FromResult(result.OrderByDescending(m => m.CreatedAt).ToList());
                }
                return Task.FromResult(new List<MailItem>());
            }
        }

        public Task MarkMailReadAsync(int mailId)
        {
            lock (lockObj)
            {
                foreach (var userMails in mails.Values)
                {
                    var mail = userMails.FirstOrDefault(m => m.Id == mailId);
                    if (mail != null)
                    {
                        mail.IsRead = true;
                        mail.ReadAt = DateTime.UtcNow;
                        SaveMails();
                        break;
                    }
                }
            }
            return Task.CompletedTask;
        }

        public Task<bool> ClaimMailAttachmentsAsync(int mailId, int userId)
        {
            lock (lockObj)
            {
                if (mails.TryGetValue(userId, out var userMails))
                {
                    var mail = userMails.FirstOrDefault(m => m.Id == mailId);
                    if (mail != null && !mail.AttachmentsClaimed && !string.IsNullOrEmpty(mail.Attachments))
                    {
                        mail.AttachmentsClaimed = true;
                        SaveMails();
                        return Task.FromResult(true);
                    }
                }
                return Task.FromResult(false);
            }
        }

        public Task DeleteMailAsync(int mailId)
        {
            lock (lockObj)
            {
                foreach (var userMails in mails.Values)
                {
                    var mail = userMails.FirstOrDefault(m => m.Id == mailId);
                    if (mail != null)
                    {
                        mail.IsDeleted = true;
                        SaveMails();
                        break;
                    }
                }
            }
            return Task.CompletedTask;
        }

        #endregion

        #region 日志操作

        public Task LogLoginAsync(int userId, string ipAddress, string deviceInfo, bool success, string failReason = null)
        {
            lock (lockObj)
            {
                loginLogs.Add(new LoginLog
                {
                    Id = nextLogId++,
                    UserId = userId,
                    LoginTime = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    DeviceInfo = deviceInfo,
                    Success = success,
                    FailReason = failReason
                });

                // 只保留最近1000条日志
                if (loginLogs.Count > 1000)
                {
                    loginLogs = loginLogs.Skip(loginLogs.Count - 1000).ToList();
                }
            }
            return Task.CompletedTask;
        }

        public Task<List<LoginLog>> GetLoginLogsAsync(int userId, int limit = 10)
        {
            lock (lockObj)
            {
                var logs = loginLogs
                    .Where(l => l.UserId == userId)
                    .OrderByDescending(l => l.LoginTime)
                    .Take(limit)
                    .ToList();
                return Task.FromResult(logs);
            }
        }

        #endregion

        #region 货币操作

        public Task<bool> AddGoldAsync(int userId, int amount, string reason = null)
        {
            if (amount <= 0) return Task.FromResult(false);

            lock (lockObj)
            {
                if (users.TryGetValue(userId, out var user))
                {
                    user.Gold += amount;
                    SaveUsers();
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
        }

        public Task<bool> DeductGoldAsync(int userId, int amount, string reason = null)
        {
            if (amount <= 0) return Task.FromResult(false);

            lock (lockObj)
            {
                if (users.TryGetValue(userId, out var user))
                {
                    if (user.Gold < amount) return Task.FromResult(false);
                    user.Gold -= amount;
                    SaveUsers();
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
        }

        public Task<bool> AddDiamondAsync(int userId, int amount, string reason = null)
        {
            if (amount <= 0) return Task.FromResult(false);

            lock (lockObj)
            {
                if (users.TryGetValue(userId, out var user))
                {
                    user.Diamond += amount;
                    SaveUsers();
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
        }

        public Task<bool> DeductDiamondAsync(int userId, int amount, string reason = null)
        {
            if (amount <= 0) return Task.FromResult(false);

            lock (lockObj)
            {
                if (users.TryGetValue(userId, out var user))
                {
                    if (user.Diamond < amount) return Task.FromResult(false);
                    user.Diamond -= amount;
                    SaveUsers();
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
        }

        public Task<bool> AddExperienceAsync(int userId, int amount)
        {
            if (amount <= 0) return Task.FromResult(false);

            lock (lockObj)
            {
                if (users.TryGetValue(userId, out var user))
                {
                    user.Experience += amount;
                    // 简单升级逻辑：每100经验升1级
                    user.Level = 1 + user.Experience / 100;
                    SaveUsers();
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
        }

        #endregion

        public void Dispose()
        {
            // 无需特殊清理
        }

        #region 数据存储类

        private class UserDataStore
        {
            public Dictionary<int, UserAccount> Users { get; set; }
            public Dictionary<string, int> UsernameIndex { get; set; }
            public int NextUserId { get; set; }
        }

        private class ItemDataStore
        {
            public Dictionary<int, List<PlayerItem>> Items { get; set; }
            public int NextItemId { get; set; }
        }

        private class RewardDataStore
        {
            public Dictionary<int, List<RewardRecord>> Rewards { get; set; }
            public int NextRewardId { get; set; }
        }

        private class MailDataStore
        {
            public Dictionary<int, List<MailItem>> Mails { get; set; }
            public int NextMailId { get; set; }
        }

        #endregion
    }
}
