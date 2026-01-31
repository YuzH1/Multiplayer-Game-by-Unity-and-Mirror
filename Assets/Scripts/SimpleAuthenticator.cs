using System.Collections.Generic;
using System.Threading.Tasks;
using Mirror;
using UnityEngine;
using MultiplayerGame.Auth;
using MultiplayerGame.Services;
using MultiplayerGame.Models;

// 自定义简单认证器：支持数据库持久化
// - 支持 login / register 两种操作
// - 使用 SQLite 数据库存储账号信息
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

        // 可选：允许重复登录策略
        [Header("Auth Options")]
        public bool allowDuplicateLogin = false;
        
        [Tooltip("是否使用数据库存储（关闭则使用内存存储，仅用于测试）")]
        public bool useDatabaseStorage = true;
        
        // 内存用户库（仅在不使用数据库时作为备选）
        private static readonly Dictionary<string, (string password, string displayName)> memoryUsers = new();

        public override void OnStartServer()
        {
            // 初始化游戏服务（包括数据库）
            if (useDatabaseStorage)
            {
                GameServiceManager.Instance.InitializeServices();
            }
            
            // 服务器注册消息处理
            NetworkServer.RegisterHandler<AuthRequestMessage>(OnServerAuthRequest, false);
        }

        public override void OnStopServer()
        {
            NetworkServer.UnregisterHandler<AuthRequestMessage>();
            
            // 关闭服务
            if (useDatabaseStorage)
            {
                GameServiceManager.Instance.ShutdownServices();
            }
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

            if (useDatabaseStorage)
            {
                // 使用数据库认证
                HandleDatabaseAuthAsync(conn, msg);
            }
            else
            {
                // 使用内存认证（备选/测试用）
                HandleMemoryAuth(conn, msg);
            }
        }

        // 使用数据库进行认证
        private async void HandleDatabaseAuthAsync(NetworkConnectionToClient conn, AuthRequestMessage msg)
        {
            // 等待服务初始化完成
            int waitCount = 0;
            while (!GameServiceManager.Instance.IsInitialized && waitCount < 50) // 最多等待5秒
            {
                await Task.Delay(100);
                waitCount++;
            }
            
            if (!GameServiceManager.Instance.IsInitialized)
            {
                Debug.LogError("[Auth] 服务初始化超时");
                ServerRejectWithReason(conn, "服务器初始化中，请稍后重试");
                return;
            }
            
            var accountService = GameServiceManager.Instance.Account;
            
            if (msg.operation == "register")
            {
                Debug.Log($"[Auth] 开始注册用户: {msg.username}");
                var result = await accountService.RegisterAsync(
                    msg.username, 
                    msg.password, 
                    msg.displayName
                );

                if (result.Success)
                {
                    Debug.Log($"[Auth] 注册成功: {result.User.Username}, ID={result.User.Id}");
                    AcceptAndAttach(conn, result.User.Id, result.User.Username, result.User.DisplayName, result.SessionToken);
                }
                else
                {
                    Debug.LogWarning($"[Auth] 注册失败: {result.Message}");
                    ServerRejectWithReason(conn, result.Message);
                }
            }
            else // login
            {
                // 重复登录检查
                if (!allowDuplicateLogin)
                {
                    foreach (var c in NetworkServer.connections.Values)
                    {
                        if (c != null && c.isAuthenticated && c.authenticationData is PlayerAuthData pad && pad.username == msg.username)
                        {
                            ServerRejectWithReason(conn, "该账号已在线");
                            return;
                        }
                    }
                }

                var result = await accountService.LoginAsync(
                    msg.username, 
                    msg.password,
                    conn.address,
                    "Unity Client"
                );

                if (result.Success)
                {
                    AcceptAndAttach(conn, result.User.Id, result.User.Username, result.User.DisplayName, result.SessionToken);
                }
                else
                {
                    ServerRejectWithReason(conn, result.Message);
                }
            }
        }

        // 使用内存进行认证（备选方案）
        private void HandleMemoryAuth(NetworkConnectionToClient conn, AuthRequestMessage msg)
        {
            if (msg.operation == "register")
            {
                if (string.IsNullOrWhiteSpace(msg.username) || string.IsNullOrWhiteSpace(msg.password))
                {
                    ServerRejectWithReason(conn, "用户名或密码为空");
                    return;
                }
                if (memoryUsers.ContainsKey(msg.username))
                {
                    ServerRejectWithReason(conn, "用户名已存在");
                    return;
                }
                var dn = string.IsNullOrWhiteSpace(msg.displayName) ? msg.username : msg.displayName.Trim();
                memoryUsers[msg.username] = (msg.password, dn);
                AcceptAndAttach(conn, 0, msg.username, dn, System.Guid.NewGuid().ToString("N"));
            }
            else // login
            {
                if (!memoryUsers.TryGetValue(msg.username, out var record) || record.password != msg.password)
                {
                    ServerRejectWithReason(conn, "用户名或密码错误");
                    return;
                }
                // 重复登录检查
                if (!allowDuplicateLogin)
                {
                    foreach (var c in NetworkServer.connections.Values)
                    {
                        if (c != null && c.isAuthenticated && c.authenticationData is PlayerAuthData pad && pad.username == msg.username)
                        {
                            ServerRejectWithReason(conn, "该账号已在线");
                            return;
                        }
                    }
                }
                AcceptAndAttach(conn, 0, msg.username, record.displayName, System.Guid.NewGuid().ToString("N"));
            }
        }

        private void AcceptAndAttach(NetworkConnectionToClient conn, int userId, string username, string displayName, string sessionToken)
        {
            conn.authenticationData = new PlayerAuthData
            {
                userId = userId,
                username = username,
                displayName = displayName,
                sessionToken = sessionToken
            };

            Debug.Log($"[Auth] Success userId={userId} user={username} display={displayName}");
            ServerAccept(conn);

            var resp = new AuthResponseMessage
            {
                success = true,
                reason = "OK",
                displayName = displayName,
                sessionToken = sessionToken
            };
            conn.Send(resp);
        }

        private void ServerRejectWithReason(NetworkConnectionToClient conn, string reason)
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
        public int userId;          // 数据库用户ID
        public string username;
        public string displayName;
        public string sessionToken; // 预留重连/刷新
    }
}
