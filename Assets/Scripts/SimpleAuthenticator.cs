using System.Collections.Generic;
using Mirror;
using UnityEngine;
using MultiplayerGame.Auth;

// 自定义简单认证器：演示用途
// - 支持 login / register 两种操作
// - 使用内存字典模拟账号库（仅开发期），未来请替换为外部持久化服务
// - 通过 NetworkConnectionToClient.authenticationData 传递认证后的用户数据给 NetworkManager

namespace MultiplayerGame.Auth
{
    public class SimpleAuthenticator : NetworkAuthenticator
    {
        // 客户端在连接前由 UI 设置的临时凭证，OnClientAuthenticate 会自动发送
        public static PendingAuth? pending;

        public struct PendingAuth
        {
            public string operation;   // login / register
            public string username;
            public string password;
            public string displayName;
        }
        // 简易内存用户库（仅示例）。Key: username, Value: (passwordHashPlain, displayName)
        // 注意：仅用于演示。生产环境：使用哈希+盐（BCrypt/Argon2）并放在服务端/数据库。
        private static readonly Dictionary<string, (string password, string displayName)> users = new();

        // 可选：允许重复登录策略
        [Header("Auth Options")]
        public bool allowDuplicateLogin = false;

        public override void OnStartServer()
        {
            // 服务器注册消息处理
            NetworkServer.RegisterHandler<AuthRequestMessage>(OnServerAuthRequest, false);
        }

        public override void OnStopServer()
        {
            NetworkServer.UnregisterHandler<AuthRequestMessage>();
        }

        public override void OnStartClient()
        {
            // 客户端可在此预注册回包处理（若需要单独处理）
            NetworkClient.RegisterHandler<AuthResponseMessage>(OnClientAuthResponse, false);
        }

        public override void OnStopClient()
        {
            NetworkClient.UnregisterHandler<AuthResponseMessage>();
        }

        // 客户端发起认证流程
        public override void OnClientAuthenticate()
        {
            if (pending.HasValue)
            {
                var p = pending.Value;
                Debug.Log($"SimpleAuthenticator: 使用 pending 凭证自动认证 op={p.operation}");
                if (p.operation == "register")
                    ClientSendRegister(p.username, p.password, p.displayName);
                else
                    ClientSendLogin(p.username, p.password);
                pending = null; // 一次性
            }
            else
            {
                Debug.Log("SimpleAuthenticator: OnClientAuthenticate - 等待外部调用 SendLogin/SendRegister");
            }
        }

        // 服务器处理认证
        public override void OnServerAuthenticate(NetworkConnectionToClient conn)
        {
            // 等待客户端发来 AuthRequestMessage
            Debug.Log($"SimpleAuthenticator: OnServerAuthenticate waiting message, conn={conn.connectionId}");
        }

        // 公开给 UI 的方法：客户端发送登录
        public void ClientSendLogin(string username, string password)
        {
            var msg = new AuthRequestMessage
            {
                operation = "login",
                username = username,
                password = password,
                displayName = string.Empty
            };
            NetworkClient.Send(msg);
        }

        // 公开给 UI 的方法：客户端发送注册
        public void ClientSendRegister(string username, string password, string displayName)
        {
            var msg = new AuthRequestMessage
            {
                operation = "register",
                username = username,
                password = password,
                displayName = displayName
            };
            NetworkClient.Send(msg);
        }

        // 服务器收到认证请求
        private void OnServerAuthRequest(NetworkConnectionToClient conn, AuthRequestMessage msg)
        {
            Debug.Log($"[Auth] Req op={msg.operation} user={msg.username} conn={conn.connectionId}");

            if (msg.operation == "register")
            {
                if (string.IsNullOrWhiteSpace(msg.username) || string.IsNullOrWhiteSpace(msg.password))
                {
                    ServerReject(conn, "用户名或密码为空");
                    return;
                }
                if (users.ContainsKey(msg.username))
                {
                    ServerReject(conn, "用户名已存在");
                    return;
                }
                var dn = string.IsNullOrWhiteSpace(msg.displayName) ? msg.username : msg.displayName.Trim();
                users[msg.username] = (msg.password, dn);
                // 注册完成后直接当做已登录
                AcceptAndAttach(conn, msg.username, dn);
            }
            else // login
            {
                if (!users.TryGetValue(msg.username, out var record) || record.password != msg.password)
                {
                    ServerReject(conn, "用户名或密码错误");
                    return;
                }
                // 重复登录检查
                if (!allowDuplicateLogin)
                {
                    foreach (var c in NetworkServer.connections.Values)
                    {
                        if (c != null && c.isAuthenticated && c.authenticationData is PlayerAuthData pad && pad.username == msg.username)
                        {
                            ServerReject(conn, "该账号已在线");
                            return;
                        }
                    }
                }
                AcceptAndAttach(conn, msg.username, record.displayName);
            }
        }

        private void AcceptAndAttach(NetworkConnectionToClient conn, string username, string displayName)
        {
            conn.authenticationData = new PlayerAuthData
            {
                username = username,
                displayName = displayName,
                sessionToken = System.Guid.NewGuid().ToString("N")
            };

            Debug.Log($"[Auth] Success user={username} display={displayName}");
            ServerAccept(conn);

            var resp = new AuthResponseMessage
            {
                success = true,
                reason = "OK",
                displayName = displayName,
                sessionToken = (conn.authenticationData as PlayerAuthData)?.sessionToken
            };
            conn.Send(resp);
        }

        private void ServerReject(NetworkConnectionToClient conn, string reason)
        {
            var resp = new AuthResponseMessage
            {
                success = false,
                reason = reason,
                displayName = string.Empty,
                sessionToken = string.Empty
            };
            conn.Send(resp);
            ServerReject(conn);
        }

        // 客户端收到认证结果（可用于 UI）
        private void OnClientAuthResponse(AuthResponseMessage msg)
        {
            if (msg.success)
            {
                Debug.Log($"[Auth] 客户端认证成功, display={msg.displayName}");
                ClientAccept();
            }
            else
            {
                Debug.LogWarning($"[Auth] 客户端认证失败: {msg.reason}");
                ClientReject();
            }
        }
    }

    // 认证通过后挂在 connection.authenticationData 的数据模型
    public class PlayerAuthData
    {
        public string username;
        public string displayName;
        public string sessionToken; // 预留重连/刷新
    }
}
