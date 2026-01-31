using UnityEngine;
using MultiplayerGame.Database;
using MultiplayerGame.Services;

namespace MultiplayerGame
{
    /// <summary>
    /// 游戏服务管理器 - 单例模式管理所有游戏服务
    /// 挂载到场景中的 GameObject 上，确保在服务器和客户端都能访问
    /// </summary>
    public class GameServiceManager : MonoBehaviour
    {
        private static GameServiceManager instance;
        public static GameServiceManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<GameServiceManager>();
                    if (instance == null)
                    {
                        var go = new GameObject("GameServiceManager");
                        instance = go.AddComponent<GameServiceManager>();
                    }
                }
                return instance;
            }
        }

        [Header("数据库设置")]
        [Tooltip("数据库文件名（SQLite）")]
        public string databaseFileName = "game_data.db";

        [Header("服务状态")]
        [SerializeField] private bool isInitialized = false;
        public bool IsInitialized => isInitialized;

        // 数据库提供者
        private IDatabaseProvider databaseProvider;
        public IDatabaseProvider Database => databaseProvider;

        // 服务实例
        private AccountService accountService;
        public AccountService Account => accountService;

        private ItemService itemService;
        public ItemService Items => itemService;

        private RewardService rewardService;
        public RewardService Rewards => rewardService;

        private MailService mailService;
        public MailService Mail => mailService;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 自动初始化服务
            InitializeServices();
        }

        /// <summary>
        /// 初始化所有服务（服务器端调用）
        /// </summary>
        public async void InitializeServices()
        {
            if (isInitialized)
            {
                Debug.Log("[GameServiceManager] 服务已初始化");
                return;
            }

            Debug.Log("[GameServiceManager] 开始初始化服务...");

            try
            {
                // 创建数据库提供者（使用 JSON 文件存储，跨平台兼容）
                databaseProvider = new JsonDatabaseProvider(databaseFileName);

                // 创建服务实例
                accountService = new AccountService(databaseProvider);
                itemService = new ItemService(databaseProvider);
                rewardService = new RewardService(databaseProvider, itemService);
                mailService = new MailService(databaseProvider, itemService);

                // 初始化账号服务（会初始化数据库）
                await accountService.InitializeAsync();

                isInitialized = true;
                Debug.Log("[GameServiceManager] 所有服务初始化完成");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameServiceManager] 服务初始化失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 关闭所有服务
        /// </summary>
        public void ShutdownServices()
        {
            if (!isInitialized)
                return;

            Debug.Log("[GameServiceManager] 关闭服务...");

            accountService?.Dispose();
            databaseProvider?.Dispose();

            accountService = null;
            itemService = null;
            rewardService = null;
            mailService = null;
            databaseProvider = null;

            isInitialized = false;
            Debug.Log("[GameServiceManager] 服务已关闭");
        }

        private void OnDestroy()
        {
            ShutdownServices();
        }

        private void OnApplicationQuit()
        {
            ShutdownServices();
        }
    }
}
