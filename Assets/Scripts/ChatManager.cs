using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using MultiplayerGame.Auth;

// 聊天系统：
// - 客户端发送 ChatToServer
// - 服务器校验 + 频率限制 + 广播 ChatMessage
// - 客户端通过事件 OnChatReceived 交给 UI

namespace MultiplayerGame.Chat
{
    public class ChatManager : NetworkBehaviour
    {
        // 频率限制（秒）
        [SerializeField] private float sendInterval = 0.5f;
        private readonly Dictionary<int, double> lastSendTime = new();

        public static event Action<ChatMessage> OnChatReceived;

        public override void OnStartServer()
        {
            NetworkServer.RegisterHandler<ChatToServer>(OnChatToServer, false);
        }

        public override void OnStopServer()
        {
            NetworkServer.UnregisterHandler<ChatToServer>();
        }

        public override void OnStartClient()
        {
            NetworkClient.RegisterHandler<ChatMessage>(OnChatBroadcast, false);
        }

        public override void OnStopClient()
        {
            NetworkClient.UnregisterHandler<ChatMessage>();
        }

        // 客户端 -> 服务器 消息
        public struct ChatToServer : NetworkMessage
        {
            public string channel; // global / private:xxx / system
            public string text;
        }

        // 客户端发送接口（供 UI 调用）
        public static void ClientSend(string text, string channel = "global")
        {
            if (!NetworkClient.active) { Debug.LogWarning("Chat: client not active"); return; }
            var msg = new ChatToServer { channel = channel, text = text };
            NetworkClient.Send(msg);
        }

        // 服务器收到客户端聊天请求
        private void OnChatToServer(NetworkConnectionToClient conn, ChatToServer msg)
        {
            if (string.IsNullOrWhiteSpace(msg.text)) return;

            // 频率限制
            double now = NetworkTime.time;
            if (lastSendTime.TryGetValue(conn.connectionId, out var t) && now - t < sendInterval)
                return;
            lastSendTime[conn.connectionId] = now;

            string from = conn.authenticationData is PlayerAuthData pad ? pad.displayName : $"Player{conn.connectionId}";

            var chat = new ChatMessage
            {
                channel = string.IsNullOrEmpty(msg.channel) ? "global" : msg.channel,
                from = from,
                text = msg.text.Trim(),
                serverTime = DateTime.UtcNow
            };

            // 广播给所有已就绪客户端
            NetworkServer.SendToReady(chat);
        }

        // 客户端接收服务器广播
        private void OnChatBroadcast(ChatMessage msg)
        {
            OnChatReceived?.Invoke(msg);
            Debug.Log($"[Chat] {msg.serverTime:HH:mm:ss} [{msg.channel}] {msg.from}: {msg.text}");
        }
    }
}
