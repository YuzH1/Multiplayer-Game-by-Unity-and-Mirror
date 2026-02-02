using Mirror;
using System;

// 认证阶段使用的网络消息定义。
// Mirror 消息要求实现 NetworkMessage 接口（标记接口）。
// 可以根据需要继续扩展，比如添加客户端版本、设备信息、一次性挑战码等。

namespace MultiplayerGame.Auth
{
    /// <summary>
    /// 客户端 -> 服务器：请求认证（登录或注册）。
    /// op: "login" | "register" | 后期可加入 "refresh" 等。
    /// </summary>
    public struct AuthRequestMessage : NetworkMessage
    {
        public string operation;      // login / register
        public string username;
        public string password;       // 明文仅用于演示，生产需改为哈希 + 随机盐；或双向加密通道。
        public string displayName;    // 注册时可选的显示名
    }

    /// <summary>
    /// 服务器 -> 客户端：认证回应
    /// </summary>
    public struct AuthResponseMessage : NetworkMessage
    {
        public bool success;
        public string reason;         // 失败原因或成功提示
        public string displayName;    // 成功时返回标准化后的显示名
        public string sessionToken;   // 未来可用于重连/刷新
    }

    /// <summary>
    /// 服务器 -> 客户端：聊天消息广播（也可放在单独文件，可选）。
    /// channel: global / system / private:username
    /// </summary>
    public struct ChatMessage : NetworkMessage
    {
        public string channel;
        public string from;
        public string text;
        public DateTime serverTime;
    }
}
