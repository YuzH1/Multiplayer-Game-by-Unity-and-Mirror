using System.Collections.Generic;
using System.Threading.Tasks;
using Mirror;
using UnityEngine;
using MultiplayerGame.Auth;
using MultiplayerGame.Models;
using MultiplayerGame.Network;
using MultiplayerGame.Services;
using Newtonsoft.Json;

namespace MultiplayerGame
{
    /// <summary>
    /// 游戏数据网络处理器
    /// 处理物品、奖励、邮件等数据的网络同步
    /// 挂载到场景中任意 GameObject 上（推荐与 GameServiceManager 放在一起）
    /// </summary>
    public class GameDataNetworkHandler : MonoBehaviour
    {
        private static GameDataNetworkHandler instance;
        public static GameDataNetworkHandler Instance => instance;
        
        private GameServiceManager serviceManager => GameServiceManager.Instance;
        
        private bool serverHandlersRegistered = false;
        private bool clientHandlersRegistered = false;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        private void Update()
        {
            // 检测服务器启动
            if (NetworkServer.active && !serverHandlersRegistered)
            {
                RegisterServerHandlers();
                serverHandlersRegistered = true;
                Debug.Log("[GameDataHandler] 服务器消息处理器已注册");
            }
            else if (!NetworkServer.active && serverHandlersRegistered)
            {
                UnregisterServerHandlers();
                serverHandlersRegistered = false;
            }
            
            // 检测客户端连接
            if (NetworkClient.active && !clientHandlersRegistered)
            {
                RegisterClientHandlers();
                clientHandlersRegistered = true;
                Debug.Log("[GameDataHandler] 客户端消息处理器已注册");
            }
            else if (!NetworkClient.active && clientHandlersRegistered)
            {
                UnregisterClientHandlers();
                clientHandlersRegistered = false;
            }
        }

        private void OnDestroy()
        {
            if (serverHandlersRegistered)
                UnregisterServerHandlers();
            if (clientHandlersRegistered)
                UnregisterClientHandlers();
        }

        #region Server Setup

        private void RegisterServerHandlers()
        {
            NetworkServer.RegisterHandler<RequestInventoryMessage>(OnServerRequestInventory);
            NetworkServer.RegisterHandler<UseItemMessage>(OnServerUseItem);
            NetworkServer.RegisterHandler<EquipItemMessage>(OnServerEquipItem);
            NetworkServer.RegisterHandler<RequestRewardsMessage>(OnServerRequestRewards);
            NetworkServer.RegisterHandler<ClaimRewardMessage>(OnServerClaimReward);
            NetworkServer.RegisterHandler<RequestMailsMessage>(OnServerRequestMails);
            NetworkServer.RegisterHandler<ReadMailMessage>(OnServerReadMail);
            NetworkServer.RegisterHandler<ClaimMailAttachmentsMessage>(OnServerClaimMailAttachments);
            NetworkServer.RegisterHandler<DeleteMailMessage>(OnServerDeleteMail);
            NetworkServer.RegisterHandler<RequestUserDataMessage>(OnServerRequestUserData);
        }

        private void UnregisterServerHandlers()
        {
            NetworkServer.UnregisterHandler<RequestInventoryMessage>();
            NetworkServer.UnregisterHandler<UseItemMessage>();
            NetworkServer.UnregisterHandler<EquipItemMessage>();
            NetworkServer.UnregisterHandler<RequestRewardsMessage>();
            NetworkServer.UnregisterHandler<ClaimRewardMessage>();
            NetworkServer.UnregisterHandler<RequestMailsMessage>();
            NetworkServer.UnregisterHandler<ReadMailMessage>();
            NetworkServer.UnregisterHandler<ClaimMailAttachmentsMessage>();
            NetworkServer.UnregisterHandler<DeleteMailMessage>();
            NetworkServer.UnregisterHandler<RequestUserDataMessage>();
        }

        #endregion

        #region Client Setup

        private void RegisterClientHandlers()
        {
            NetworkClient.RegisterHandler<InventoryResponseMessage>(OnClientInventoryResponse);
            NetworkClient.RegisterHandler<ItemOperationResultMessage>(OnClientItemOperationResult);
            NetworkClient.RegisterHandler<RewardsResponseMessage>(OnClientRewardsResponse);
            NetworkClient.RegisterHandler<ClaimRewardResultMessage>(OnClientClaimRewardResult);
            NetworkClient.RegisterHandler<MailsResponseMessage>(OnClientMailsResponse);
            NetworkClient.RegisterHandler<MailOperationResultMessage>(OnClientMailOperationResult);
            NetworkClient.RegisterHandler<UserDataResponseMessage>(OnClientUserDataResponse);
            NetworkClient.RegisterHandler<CurrencyUpdateMessage>(OnClientCurrencyUpdate);
        }

        private void UnregisterClientHandlers()
        {
            NetworkClient.UnregisterHandler<InventoryResponseMessage>();
            NetworkClient.UnregisterHandler<ItemOperationResultMessage>();
            NetworkClient.UnregisterHandler<RewardsResponseMessage>();
            NetworkClient.UnregisterHandler<ClaimRewardResultMessage>();
            NetworkClient.UnregisterHandler<MailsResponseMessage>();
            NetworkClient.UnregisterHandler<MailOperationResultMessage>();
            NetworkClient.UnregisterHandler<UserDataResponseMessage>();
            NetworkClient.UnregisterHandler<CurrencyUpdateMessage>();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 从连接获取用户ID
        /// </summary>
        private int GetUserId(NetworkConnectionToClient conn)
        {
            if (conn?.authenticationData is PlayerAuthData authData)
            {
                return authData.userId;
            }
            return 0;
        }

        #endregion

        #region Server: Inventory Handlers

        private async void OnServerRequestInventory(NetworkConnectionToClient conn, RequestInventoryMessage msg)
        {
            int userId = GetUserId(conn);
            if (userId == 0)
            {
                conn.Send(new InventoryResponseMessage { success = false, itemsJson = "[]" });
                return;
            }

            var items = await serviceManager.Items.GetPlayerItemsAsync(userId);
            var json = JsonConvert.SerializeObject(items);
            conn.Send(new InventoryResponseMessage { success = true, itemsJson = json });
        }

        private async void OnServerUseItem(NetworkConnectionToClient conn, UseItemMessage msg)
        {
            int userId = GetUserId(conn);
            if (userId == 0)
            {
                conn.Send(new ItemOperationResultMessage
                {
                    success = false,
                    operation = "use",
                    itemId = msg.itemId,
                    message = "未认证"
                });
                return;
            }

            var success = await serviceManager.Items.UseItemAsync(userId, msg.itemId, msg.count);
            conn.Send(new ItemOperationResultMessage
            {
                success = success,
                operation = "use",
                itemId = msg.itemId,
                message = success ? "使用成功" : "使用失败"
            });
        }

        private async void OnServerEquipItem(NetworkConnectionToClient conn, EquipItemMessage msg)
        {
            int userId = GetUserId(conn);
            if (userId == 0) return;

            bool success;
            if (msg.equip)
            {
                success = await serviceManager.Items.EquipItemAsync(userId, msg.itemId);
            }
            else
            {
                success = await serviceManager.Items.UnequipItemAsync(userId, msg.itemId);
            }

            conn.Send(new ItemOperationResultMessage
            {
                success = success,
                operation = msg.equip ? "equip" : "unequip",
                itemId = msg.itemId,
                message = success ? "操作成功" : "操作失败"
            });
        }

        #endregion

        #region Server: Reward Handlers

        private async void OnServerRequestRewards(NetworkConnectionToClient conn, RequestRewardsMessage msg)
        {
            int userId = GetUserId(conn);
            if (userId == 0)
            {
                conn.Send(new RewardsResponseMessage { success = false, rewardsJson = "[]" });
                return;
            }

            var rewards = await serviceManager.Rewards.GetUnclaimedRewardsAsync(userId);
            var json = JsonConvert.SerializeObject(rewards);
            conn.Send(new RewardsResponseMessage { success = true, rewardsJson = json });
        }

        private async void OnServerClaimReward(NetworkConnectionToClient conn, ClaimRewardMessage msg)
        {
            int userId = GetUserId(conn);
            if (userId == 0)
            {
                conn.Send(new ClaimRewardResultMessage
                {
                    success = false,
                    rewardId = msg.rewardId,
                    message = "未认证",
                    contentJson = null
                });
                return;
            }

            var (success, message, content) = await serviceManager.Rewards.ClaimRewardAsync(userId, msg.rewardId);
            conn.Send(new ClaimRewardResultMessage
            {
                success = success,
                rewardId = msg.rewardId,
                message = message,
                contentJson = content != null ? JsonConvert.SerializeObject(content) : null
            });

            // 如果领取成功，发送货币更新
            if (success)
            {
                await SendCurrencyUpdateAsync(conn, userId, "领取奖励");
            }
        }

        #endregion

        #region Server: Mail Handlers

        private async void OnServerRequestMails(NetworkConnectionToClient conn, RequestMailsMessage msg)
        {
            int userId = GetUserId(conn);
            if (userId == 0)
            {
                conn.Send(new MailsResponseMessage { success = false, mailsJson = "[]" });
                return;
            }

            var mails = await serviceManager.Mail.GetMailsAsync(userId);
            var json = JsonConvert.SerializeObject(mails);
            conn.Send(new MailsResponseMessage { success = true, mailsJson = json });
        }

        private async void OnServerReadMail(NetworkConnectionToClient conn, ReadMailMessage msg)
        {
            await serviceManager.Mail.MarkMailReadAsync(msg.mailId);
            conn.Send(new MailOperationResultMessage
            {
                success = true,
                operation = "read",
                mailId = msg.mailId,
                message = "已读"
            });
        }

        private async void OnServerClaimMailAttachments(NetworkConnectionToClient conn, ClaimMailAttachmentsMessage msg)
        {
            int userId = GetUserId(conn);
            if (userId == 0)
            {
                conn.Send(new MailOperationResultMessage
                {
                    success = false,
                    operation = "claim",
                    mailId = msg.mailId,
                    message = "未认证"
                });
                return;
            }

            var (success, message, content) = await serviceManager.Mail.ClaimMailAttachmentsAsync(userId, msg.mailId);
            conn.Send(new MailOperationResultMessage
            {
                success = success,
                operation = "claim",
                mailId = msg.mailId,
                message = message,
                contentJson = content != null ? JsonConvert.SerializeObject(content) : null
            });

            if (success)
            {
                await SendCurrencyUpdateAsync(conn, userId, "领取邮件附件");
            }
        }

        private async void OnServerDeleteMail(NetworkConnectionToClient conn, DeleteMailMessage msg)
        {
            await serviceManager.Mail.DeleteMailAsync(msg.mailId);
            conn.Send(new MailOperationResultMessage
            {
                success = true,
                operation = "delete",
                mailId = msg.mailId,
                message = "已删除"
            });
        }

        #endregion

        #region Server: User Data Handlers

        private async void OnServerRequestUserData(NetworkConnectionToClient conn, RequestUserDataMessage msg)
        {
            int userId = GetUserId(conn);
            if (userId == 0)
            {
                conn.Send(new UserDataResponseMessage { success = false });
                return;
            }

            var user = await serviceManager.Account.GetUserAsync(userId);
            if (user == null)
            {
                conn.Send(new UserDataResponseMessage { success = false });
                return;
            }

            var rewards = await serviceManager.Rewards.GetUnclaimedRewardsAsync(userId);
            var mails = await serviceManager.Mail.GetMailsAsync(userId);
            var unreadMails = mails.FindAll(m => !m.IsRead);

            conn.Send(new UserDataResponseMessage
            {
                success = true,
                userId = user.Id,
                username = user.Username,
                displayName = user.DisplayName,
                level = user.Level,
                experience = user.Experience,
                gold = user.Gold,
                diamond = user.Diamond,
                unclaimedRewardCount = rewards.Count,
                unreadMailCount = unreadMails.Count
            });
        }

        private async Task SendCurrencyUpdateAsync(NetworkConnectionToClient conn, int userId, string reason)
        {
            var user = await serviceManager.Account.GetUserAsync(userId);
            if (user != null)
            {
                conn.Send(new CurrencyUpdateMessage
                {
                    gold = user.Gold,
                    diamond = user.Diamond,
                    experience = user.Experience,
                    level = user.Level,
                    reason = reason
                });
            }
        }

        #endregion

        #region Client Event Handlers

        // 客户端收到的消息通过事件通知 UI
        public static event System.Action<List<PlayerItem>> OnInventoryReceived;
        public static event System.Action<string, int, string> OnItemOperationResult; // operation, itemId, message
        public static event System.Action<List<RewardRecord>> OnRewardsReceived;
        public static event System.Action<bool, int, string, RewardContent> OnRewardClaimResult;
        public static event System.Action<List<MailItem>> OnMailsReceived;
        public static event System.Action<string, int, string, RewardContent> OnMailOperationResult;
        public static event System.Action<UserDataResponseMessage> OnUserDataReceived;
        public static event System.Action<CurrencyUpdateMessage> OnCurrencyUpdated;

        private void OnClientInventoryResponse(InventoryResponseMessage msg)
        {
            if (msg.success)
            {
                var items = JsonConvert.DeserializeObject<List<PlayerItem>>(msg.itemsJson);
                OnInventoryReceived?.Invoke(items);
            }
        }

        private void OnClientItemOperationResult(ItemOperationResultMessage msg)
        {
            OnItemOperationResult?.Invoke(msg.operation, msg.itemId, msg.message);
        }

        private void OnClientRewardsResponse(RewardsResponseMessage msg)
        {
            if (msg.success)
            {
                var rewards = JsonConvert.DeserializeObject<List<RewardRecord>>(msg.rewardsJson);
                OnRewardsReceived?.Invoke(rewards);
            }
        }

        private void OnClientClaimRewardResult(ClaimRewardResultMessage msg)
        {
            RewardContent content = null;
            if (!string.IsNullOrEmpty(msg.contentJson))
            {
                content = JsonConvert.DeserializeObject<RewardContent>(msg.contentJson);
            }
            OnRewardClaimResult?.Invoke(msg.success, msg.rewardId, msg.message, content);
        }

        private void OnClientMailsResponse(MailsResponseMessage msg)
        {
            if (msg.success)
            {
                var mails = JsonConvert.DeserializeObject<List<MailItem>>(msg.mailsJson);
                OnMailsReceived?.Invoke(mails);
            }
        }

        private void OnClientMailOperationResult(MailOperationResultMessage msg)
        {
            RewardContent content = null;
            if (!string.IsNullOrEmpty(msg.contentJson))
            {
                content = JsonConvert.DeserializeObject<RewardContent>(msg.contentJson);
            }
            OnMailOperationResult?.Invoke(msg.operation, msg.mailId, msg.message, content);
        }

        private void OnClientUserDataResponse(UserDataResponseMessage msg)
        {
            OnUserDataReceived?.Invoke(msg);
        }

        private void OnClientCurrencyUpdate(CurrencyUpdateMessage msg)
        {
            OnCurrencyUpdated?.Invoke(msg);
            Debug.Log($"[Client] 货币更新: Gold={msg.gold}, Diamond={msg.diamond}, 原因={msg.reason}");
        }

        #endregion

        #region Client API (静态方法供 UI 调用)

        /// <summary>
        /// 请求物品列表
        /// </summary>
        public static void RequestInventory()
        {
            if (NetworkClient.isConnected)
            {
                NetworkClient.Send(new RequestInventoryMessage());
            }
        }

        /// <summary>
        /// 使用物品
        /// </summary>
        public static void UseItem(int itemId, int count = 1)
        {
            if (NetworkClient.isConnected)
            {
                NetworkClient.Send(new UseItemMessage { itemId = itemId, count = count });
            }
        }

        /// <summary>
        /// 装备/卸下物品
        /// </summary>
        public static void EquipItem(int itemId, bool equip)
        {
            if (NetworkClient.isConnected)
            {
                NetworkClient.Send(new EquipItemMessage { itemId = itemId, equip = equip });
            }
        }

        /// <summary>
        /// 请求奖励列表
        /// </summary>
        public static void RequestRewards()
        {
            if (NetworkClient.isConnected)
            {
                NetworkClient.Send(new RequestRewardsMessage());
            }
        }

        /// <summary>
        /// 领取奖励
        /// </summary>
        public static void ClaimReward(int rewardId)
        {
            if (NetworkClient.isConnected)
            {
                NetworkClient.Send(new ClaimRewardMessage { rewardId = rewardId });
            }
        }

        /// <summary>
        /// 请求邮件列表
        /// </summary>
        public static void RequestMails()
        {
            if (NetworkClient.isConnected)
            {
                NetworkClient.Send(new RequestMailsMessage());
            }
        }

        /// <summary>
        /// 标记邮件已读
        /// </summary>
        public static void ReadMail(int mailId)
        {
            if (NetworkClient.isConnected)
            {
                NetworkClient.Send(new ReadMailMessage { mailId = mailId });
            }
        }

        /// <summary>
        /// 领取邮件附件
        /// </summary>
        public static void ClaimMailAttachments(int mailId)
        {
            if (NetworkClient.isConnected)
            {
                NetworkClient.Send(new ClaimMailAttachmentsMessage { mailId = mailId });
            }
        }

        /// <summary>
        /// 删除邮件
        /// </summary>
        public static void DeleteMail(int mailId)
        {
            if (NetworkClient.isConnected)
            {
                NetworkClient.Send(new DeleteMailMessage { mailId = mailId });
            }
        }

        /// <summary>
        /// 请求用户数据
        /// </summary>
        public static void RequestUserData()
        {
            if (NetworkClient.isConnected)
            {
                NetworkClient.Send(new RequestUserDataMessage());
            }
        }

        #endregion
    }
}
