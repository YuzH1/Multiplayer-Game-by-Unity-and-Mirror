using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Mirror;
using MultiplayerGame.Models;
using MultiplayerGame.Services;

namespace MultiplayerGame.Admin
{
    /// <summary>
    /// 管理员工具 - 用于服务端管理操作
    /// 仅在服务器端生效，可通过控制台命令或编辑器调用
    /// </summary>
    public class AdminTools : MonoBehaviour
    {
        private static AdminTools instance;
        public static AdminTools Instance => instance;

        private GameServiceManager serviceManager => GameServiceManager.Instance;

        private void Awake()
        {
            instance = this;
        }

        #region 奖励发放

        /// <summary>
        /// 给指定用户发送奖励
        /// </summary>
        public async Task<bool> SendRewardToUserAsync(int userId, string rewardType, RewardContent content, string description, int expiryDays = 7)
        {
            if (!NetworkServer.active || !serviceManager.IsInitialized)
            {
                Debug.LogError("[AdminTools] 服务器未启动或服务未初始化");
                return false;
            }

            var expiresAt = System.DateTime.UtcNow.AddDays(expiryDays);
            var reward = await serviceManager.Rewards.CreateRewardAsync(userId, rewardType, content, description, expiresAt);
            
            if (reward != null)
            {
                Debug.Log($"[AdminTools] 奖励发送成功: userId={userId}, type={rewardType}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 给所有在线用户发送奖励
        /// </summary>
        public async Task<int> SendRewardToAllOnlineAsync(string rewardType, RewardContent content, string description, int expiryDays = 7)
        {
            if (!NetworkServer.active || !serviceManager.IsInitialized)
            {
                Debug.LogError("[AdminTools] 服务器未启动或服务未初始化");
                return 0;
            }

            var userIds = new List<int>();
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn != null && conn.isAuthenticated && conn.authenticationData is Auth.PlayerAuthData authData)
                {
                    userIds.Add(authData.userId);
                }
            }

            if (userIds.Count == 0)
            {
                Debug.Log("[AdminTools] 没有在线用户");
                return 0;
            }

            var expiresAt = System.DateTime.UtcNow.AddDays(expiryDays);
            await serviceManager.Rewards.BatchSendRewardsAsync(userIds, rewardType, content, description, expiresAt);
            
            Debug.Log($"[AdminTools] 批量奖励发送成功: {userIds.Count} 人");
            return userIds.Count;
        }

        /// <summary>
        /// 发送每日登录奖励给用户
        /// </summary>
        public async Task SendDailyLoginRewardAsync(int userId, int consecutiveDays = 1)
        {
            if (!NetworkServer.active || !serviceManager.IsInitialized) return;
            
            await serviceManager.Rewards.SendDailyLoginRewardAsync(userId, consecutiveDays);
            Debug.Log($"[AdminTools] 每日登录奖励发送: userId={userId}, 连续{consecutiveDays}天");
        }

        #endregion

        #region 邮件发送

        /// <summary>
        /// 发送系统邮件给用户
        /// </summary>
        public async Task<bool> SendSystemMailAsync(int userId, string title, string content, RewardContent attachments = null, int expiryDays = 30)
        {
            if (!NetworkServer.active || !serviceManager.IsInitialized)
            {
                Debug.LogError("[AdminTools] 服务器未启动或服务未初始化");
                return false;
            }

            var expiresAt = System.DateTime.UtcNow.AddDays(expiryDays);
            var mail = await serviceManager.Mail.SendSystemMailAsync(userId, title, content, attachments, expiresAt);
            
            if (mail != null)
            {
                Debug.Log($"[AdminTools] 系统邮件发送成功: userId={userId}, title={title}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 给所有在线用户发送系统邮件
        /// </summary>
        public async Task<int> SendSystemMailToAllOnlineAsync(string title, string content, RewardContent attachments = null)
        {
            if (!NetworkServer.active || !serviceManager.IsInitialized) return 0;

            int count = 0;
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn != null && conn.isAuthenticated && conn.authenticationData is Auth.PlayerAuthData authData)
                {
                    await serviceManager.Mail.SendSystemMailAsync(authData.userId, title, content, attachments);
                    count++;
                }
            }
            
            Debug.Log($"[AdminTools] 批量系统邮件发送: {count} 人");
            return count;
        }

        #endregion

        #region 物品管理

        /// <summary>
        /// 给用户添加物品
        /// </summary>
        public async Task<PlayerItem> GiveItemAsync(int userId, string itemId, string itemType, int count = 1, int level = 1)
        {
            if (!NetworkServer.active || !serviceManager.IsInitialized) return null;

            var item = await serviceManager.Items.AddItemAsync(userId, itemId, itemType, count, level);
            if (item != null)
            {
                Debug.Log($"[AdminTools] 物品添加成功: userId={userId}, itemId={itemId}, count={count}");
            }
            return item;
        }

        #endregion

        #region 货币管理

        /// <summary>
        /// 给用户添加金币
        /// </summary>
        public async Task<bool> GiveGoldAsync(int userId, int amount, string reason = "Admin Grant")
        {
            if (!NetworkServer.active || !serviceManager.IsInitialized) return false;
            
            var success = await serviceManager.Database.AddGoldAsync(userId, amount, reason);
            if (success)
            {
                Debug.Log($"[AdminTools] 金币添加成功: userId={userId}, amount={amount}");
                // 通知在线玩家
                NotifyCurrencyUpdate(userId);
            }
            return success;
        }

        /// <summary>
        /// 给用户添加钻石
        /// </summary>
        public async Task<bool> GiveDiamondAsync(int userId, int amount, string reason = "Admin Grant")
        {
            if (!NetworkServer.active || !serviceManager.IsInitialized) return false;
            
            var success = await serviceManager.Database.AddDiamondAsync(userId, amount, reason);
            if (success)
            {
                Debug.Log($"[AdminTools] 钻石添加成功: userId={userId}, amount={amount}");
                NotifyCurrencyUpdate(userId);
            }
            return success;
        }

        /// <summary>
        /// 给用户添加经验
        /// </summary>
        public async Task<bool> GiveExperienceAsync(int userId, int amount)
        {
            if (!NetworkServer.active || !serviceManager.IsInitialized) return false;
            
            var success = await serviceManager.Database.AddExperienceAsync(userId, amount);
            if (success)
            {
                Debug.Log($"[AdminTools] 经验添加成功: userId={userId}, amount={amount}");
                NotifyCurrencyUpdate(userId);
            }
            return success;
        }

        private async void NotifyCurrencyUpdate(int userId)
        {
            // 查找该用户的连接并发送更新
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn != null && conn.isAuthenticated && conn.authenticationData is Auth.PlayerAuthData authData && authData.userId == userId)
                {
                    var user = await serviceManager.Account.GetUserAsync(userId);
                    if (user != null)
                    {
                        conn.Send(new Network.CurrencyUpdateMessage
                        {
                            gold = user.Gold,
                            diamond = user.Diamond,
                            experience = user.Experience,
                            level = user.Level,
                            reason = "管理员操作"
                        });
                    }
                    break;
                }
            }
        }

        #endregion

        #region 用户管理

        /// <summary>
        /// 封禁用户
        /// </summary>
        public async Task BanUserAsync(int userId, string reason)
        {
            if (!NetworkServer.active || !serviceManager.IsInitialized) return;
            
            await serviceManager.Account.BanUserAsync(userId, reason);
            Debug.Log($"[AdminTools] 用户已封禁: userId={userId}, reason={reason}");

            // 踢出在线用户
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn != null && conn.isAuthenticated && conn.authenticationData is Auth.PlayerAuthData authData && authData.userId == userId)
                {
                    conn.Disconnect();
                    Debug.Log($"[AdminTools] 已踢出封禁用户: userId={userId}");
                    break;
                }
            }
        }

        /// <summary>
        /// 解封用户
        /// </summary>
        public async Task UnbanUserAsync(int userId)
        {
            if (!NetworkServer.active || !serviceManager.IsInitialized) return;
            
            await serviceManager.Account.UnbanUserAsync(userId);
            Debug.Log($"[AdminTools] 用户已解封: userId={userId}");
        }

        #endregion

        #region 编辑器测试方法

#if UNITY_EDITOR
        [Header("测试 - 奖励")]
        public int testUserId = 1;
        public int testGold = 100;
        public int testDiamond = 10;

        [ContextMenu("测试: 发送金币奖励")]
        public async void TestSendGoldReward()
        {
            var content = new RewardContent { Gold = testGold };
            await SendRewardToUserAsync(testUserId, "test", content, "测试金币奖励");
        }

        [ContextMenu("测试: 发送钻石奖励")]
        public async void TestSendDiamondReward()
        {
            var content = new RewardContent { Diamond = testDiamond };
            await SendRewardToUserAsync(testUserId, "test", content, "测试钻石奖励");
        }

        [ContextMenu("测试: 直接添加金币")]
        public async void TestGiveGold()
        {
            await GiveGoldAsync(testUserId, testGold, "编辑器测试");
        }

        [ContextMenu("测试: 发送系统邮件")]
        public async void TestSendSystemMail()
        {
            var attachments = new RewardContent
            {
                Gold = 50,
                Items = new List<ItemReward>
                {
                    new ItemReward { ItemId = "test_item_001", Count = 1 }
                }
            };
            await SendSystemMailAsync(testUserId, "测试邮件", "这是一封测试邮件", attachments);
        }

        [ContextMenu("测试: 给所有在线玩家发奖励")]
        public async void TestSendRewardToAll()
        {
            var content = new RewardContent
            {
                Gold = 100,
                Diamond = 10
            };
            await SendRewardToAllOnlineAsync("event", content, "全服奖励");
        }
#endif

        #endregion
    }
}
