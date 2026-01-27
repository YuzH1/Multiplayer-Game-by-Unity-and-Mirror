using UnityEngine;

// 运行期玩家数据（客户端/服务器都可用的轻模型）。
// 注意：Network 同步走 NetworkBehaviour 的 SyncVar；此模型更偏向本地缓存与业务层传递。

namespace MultiplayerGame.Data
{
    [System.Serializable]
    public class PlayerData
    {
        public string userId;      // 未来可用 UUID/数据库ID
        public string username;
        public string displayName;
    }

    // 未来：持久化接口占位
    // TODO: 用外部服务实现（REST / gRPC / DB）
    public interface IPlayerPersistence
    {
        // 加载玩家数据
        // PlayerData Load(string userIdOrName);
        // 保存玩家数据
        // void Save(PlayerData data);
    }

    // 邮件系统接口占位
    public interface IMailService
    {
        // TODO: 定义：拉取邮件、发送邮件、标记已读/删除等
        // IEnumerable<MailItem> GetInbox(string userId);
        // void Send(string fromUserId, string toUserId, string subject, string body, Attachment[] attachments);
    }
}
