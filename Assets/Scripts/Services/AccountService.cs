using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MultiplayerGame.Database;
using MultiplayerGame.Models;
using UnityEngine;

namespace MultiplayerGame.Services
{
    /// <summary>
    /// 账号服务 - 处理注册、登录、密码验证等业务逻辑
    /// </summary>
    public class AccountService : IDisposable
    {
        private readonly IDatabaseProvider database;
        private bool disposed = false;

        public AccountService(IDatabaseProvider databaseProvider)
        {
            database = databaseProvider ?? throw new ArgumentNullException(nameof(databaseProvider));
        }

        /// <summary>
        /// 初始化服务（初始化数据库）
        /// </summary>
        public async Task InitializeAsync()
        {
            await database.InitializeAsync();
            Debug.Log("[AccountService] 服务初始化完成");
        }

        #region 注册登录

        /// <summary>
        /// 注册新用户
        /// </summary>
        public async Task<AuthResult> RegisterAsync(string username, string password, string displayName = null, string email = null)
        {
            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(username))
                {
                    return new AuthResult { Success = false, Message = "用户名不能为空" };
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    return new AuthResult { Success = false, Message = "密码不能为空" };
                }

                if (username.Length < 3 || username.Length > 32)
                {
                    return new AuthResult { Success = false, Message = "用户名长度需要在3-32个字符之间" };
                }

                if (password.Length < 6)
                {
                    return new AuthResult { Success = false, Message = "密码长度至少6个字符" };
                }

                // 检查用户名是否已存在
                if (await database.UsernameExistsAsync(username))
                {
                    return new AuthResult { Success = false, Message = "用户名已存在" };
                }

                // 生成密码哈希
                var salt = GenerateSalt();
                var passwordHash = HashPassword(password, salt);

                // 创建用户
                var user = await database.CreateUserAsync(
                    username,
                    passwordHash,
                    salt,
                    displayName ?? username,
                    email
                );

                if (user == null)
                {
                    return new AuthResult { Success = false, Message = "创建用户失败" };
                }

                // 生成会话令牌
                var sessionToken = GenerateSessionToken();

                Debug.Log($"[AccountService] 用户注册成功: {username}");

                return new AuthResult
                {
                    Success = true,
                    Message = "注册成功",
                    User = user,
                    SessionToken = sessionToken
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AccountService] 注册异常: {ex.Message}");
                return new AuthResult { Success = false, Message = "注册过程发生错误" };
            }
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        public async Task<AuthResult> LoginAsync(string username, string password, string ipAddress = null, string deviceInfo = null)
        {
            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    return new AuthResult { Success = false, Message = "用户名或密码不能为空" };
                }

                // 查找用户
                var user = await database.GetUserByUsernameAsync(username);
                if (user == null)
                {
                    return new AuthResult { Success = false, Message = "用户名或密码错误" };
                }

                // 验证密码
                var passwordHash = HashPassword(password, user.Salt);
                if (passwordHash != user.PasswordHash)
                {
                    // 记录失败登录
                    await database.LogLoginAsync(user.Id, ipAddress, deviceInfo, false, "密码错误");
                    return new AuthResult { Success = false, Message = "用户名或密码错误" };
                }

                // 检查封禁状态
                if (user.IsBanned)
                {
                    await database.LogLoginAsync(user.Id, ipAddress, deviceInfo, false, "账号已封禁");
                    return new AuthResult { Success = false, Message = $"账号已封禁: {user.BanReason ?? "违规操作"}" };
                }

                // 更新最后登录时间
                await database.UpdateLastLoginAsync(user.Id);

                // 记录成功登录
                await database.LogLoginAsync(user.Id, ipAddress, deviceInfo, true);

                // 生成会话令牌
                var sessionToken = GenerateSessionToken();

                Debug.Log($"[AccountService] 用户登录成功: {username}");

                return new AuthResult
                {
                    Success = true,
                    Message = "登录成功",
                    User = user,
                    SessionToken = sessionToken
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AccountService] 登录异常: {ex.Message}");
                return new AuthResult { Success = false, Message = "登录过程发生错误" };
            }
        }

        #endregion

        #region 密码工具

        /// <summary>
        /// 生成随机盐值
        /// </summary>
        public static string GenerateSalt(int size = 32)
        {
            var bytes = new byte[size];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// 哈希密码（SHA256 + 盐）
        /// 注意：生产环境建议使用 BCrypt 或 Argon2
        /// </summary>
        public static string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var combined = password + salt;
                var bytes = Encoding.UTF8.GetBytes(combined);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// 生成会话令牌
        /// </summary>
        public static string GenerateSessionToken()
        {
            return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        }

        #endregion

        #region 用户管理

        /// <summary>
        /// 获取用户信息
        /// </summary>
        public async Task<UserAccount> GetUserAsync(int userId)
        {
            return await database.GetUserByIdAsync(userId);
        }

        /// <summary>
        /// 获取用户信息（按用户名）
        /// </summary>
        public async Task<UserAccount> GetUserByUsernameAsync(string username)
        {
            return await database.GetUserByUsernameAsync(username);
        }

        /// <summary>
        /// 更新用户信息
        /// </summary>
        public async Task UpdateUserAsync(UserAccount user)
        {
            await database.UpdateUserAsync(user);
        }

        /// <summary>
        /// 修改密码
        /// </summary>
        public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            var user = await database.GetUserByIdAsync(userId);
            if (user == null)
                return false;

            // 验证旧密码
            var oldHash = HashPassword(oldPassword, user.Salt);
            if (oldHash != user.PasswordHash)
                return false;

            // 生成新密码哈希
            var newSalt = GenerateSalt();
            var newHash = HashPassword(newPassword, newSalt);

            user.Salt = newSalt;
            user.PasswordHash = newHash;
            await database.UpdateUserAsync(user);

            return true;
        }

        /// <summary>
        /// 封禁用户
        /// </summary>
        public async Task BanUserAsync(int userId, string reason)
        {
            await database.BanUserAsync(userId, reason);
        }

        /// <summary>
        /// 解封用户
        /// </summary>
        public async Task UnbanUserAsync(int userId)
        {
            await database.UnbanUserAsync(userId);
        }

        #endregion

        public void Dispose()
        {
            if (!disposed)
            {
                database?.Dispose();
                disposed = true;
            }
        }
    }
}
