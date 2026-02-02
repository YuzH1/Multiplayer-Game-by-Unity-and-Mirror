using UnityEngine;
using UnityEngine.UI;
using Mirror;
using MultiplayerGame;
using MultiplayerGame.Auth;
using MultiplayerGame.Network;
using MultiplayerGame.UI;
using TMPro;

// 登录/注册 UI 脚本
// 使用方式：
// - 在场景中创建包含 TMP_InputField 的 UI，并将引用拖拽到本脚本
// - 点击 "连接(客户端)" 按钮时：设置 SimpleAuthenticator.pending，然后调用 NetworkManager.StartClient()
// - 点击 "开房(主机)" 按钮时：同理设置 pending 后调用 StartHost()

public class LoginUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_InputField displayNameInput; // 注册时可选
    public GameObject displayNameContainer; // 昵称输入框容器（勾选注册时显示）
    public Toggle registerToggle;
    public Button loginButton;              // 登录/注册按钮
    public TMP_Text loginButtonText;        // 按钮文字
    
    [Header("Status UI")]
    public TMP_Text statusText;
    public GameObject loginPanel;
    public GameObject userInfoPanel;
    public TMP_Text userDisplayNameText;
    public TMP_Text userLevelText;
    public TMP_Text userGoldText;
    public TMP_Text userDiamondText;

    [Header("Network")]
    public NetworkManager manager;

    private bool isConnecting = false;
    private bool isRegistering = false;        // 标记当前是否为注册操作
    private bool authResponseReceived = false; // 标记是否收到认证响应
    private string lastAuthError = "";         // 最后的认证错误信息
    private bool authErrorHandled = false;     // 标记认证错误是否已处理

    void Reset()
    {
        manager = Object.FindFirstObjectByType<NetworkManager>();
    }

    void Start()
    {
        // 订阅事件
        GameDataNetworkHandler.OnUserDataReceived += OnUserDataReceived;
        GameDataNetworkHandler.OnCurrencyUpdated += OnCurrencyUpdated;
        NetworkClient.OnDisconnectedEvent += OnClientDisconnected;
        
        // 订阅认证结果事件（通过 SimpleAuthenticator 的静态事件）
        SimpleAuthenticator.OnAuthResult += OnAuthResponse;
        Debug.Log("[LoginUI] 已订阅 SimpleAuthenticator.OnAuthResult 事件");
        
        // 监听注册切换
        if (registerToggle != null)
        {
            registerToggle.onValueChanged.AddListener(OnRegisterToggleChanged);
            // 初始化UI状态
            OnRegisterToggleChanged(registerToggle.isOn);
        }
        
        // 初始状态
        ShowLoginPanel();
    }

    void OnDestroy()
    {
        GameDataNetworkHandler.OnUserDataReceived -= OnUserDataReceived;
        GameDataNetworkHandler.OnCurrencyUpdated -= OnCurrencyUpdated;
        
        // 取消网络事件订阅
        NetworkClient.OnDisconnectedEvent -= OnClientDisconnected;
        
        // 取消认证结果事件订阅
        SimpleAuthenticator.OnAuthResult -= OnAuthResponse;
        
        // 取消Toggle监听
        if (registerToggle != null)
        {
            registerToggle.onValueChanged.RemoveListener(OnRegisterToggleChanged);
        }
    }

    // 注册Toggle切换事件
    private void OnRegisterToggleChanged(bool isRegister)
    {
        // 显示/隐藏昵称输入框
        if (displayNameContainer != null)
        {
            displayNameContainer.SetActive(isRegister);
        }
        else if (displayNameInput != null)
        {
            displayNameInput.gameObject.SetActive(isRegister);
        }
        
        // 切换按钮文字
        if (loginButtonText != null)
        {
            loginButtonText.text = isRegister ? "注册" : "登录";
        }
    }

    void Update()
    {
        // 检查连接状态变化（主要用于成功情况的检测）
        if (isConnecting)
        {
            if (NetworkClient.isConnected && NetworkClient.connection != null && NetworkClient.connection.isAuthenticated)
            {
                isConnecting = false;
                
                // 如果是注册操作，显示注册成功弹窗
                if (isRegistering)
                {
                    isRegistering = false;
                    OnRegisterSuccess();
                }
                else
                {
                    OnLoginSuccess();
                }
            }
            // 失败情况在 OnClientDisconnected 和 OnAuthResponse 中处理
        }
    }

    // 处理认证响应
    private void OnAuthResponse(AuthResponseMessage msg)
    {
        authResponseReceived = true;
        
        if (!msg.success)
        {
            lastAuthError = msg.reason;
            Debug.LogWarning($"[LoginUI] 认证失败: {msg.reason}");
            
            // 无论是 Host 还是 Client 模式，都立即显示错误信息
            authErrorHandled = true; // 标记已处理，避免 OnClientDisconnected 重复处理
            SetStatus($"认证失败: {msg.reason}");
            isConnecting = false;
            isRegistering = false;
            
            // Host模式下认证失败，需要停止服务器
            if (NetworkServer.active)
            {
                Debug.Log("[LoginUI] Host认证失败，停止服务器");
                manager.StopHost();
            }
            
            ShowLoginPanel();
        }
        else
        {
            lastAuthError = "";
            Debug.Log($"[LoginUI] 认证成功: {msg.displayName}");
        }
    }

    private void OnClientDisconnected()
    {
        isConnecting = false;
        
        // 如果认证错误已经被处理过（Host模式），跳过
        if (authErrorHandled)
        {
            authErrorHandled = false;
            authResponseReceived = false;
            lastAuthError = "";
            isRegistering = false;
            return;
        }
        
        // 如果有认证错误，显示具体原因，否则显示通用断开消息
        if (authResponseReceived && !string.IsNullOrEmpty(lastAuthError))
        {
            SetStatus($"认证失败: {lastAuthError}");
        }
        else if (!authResponseReceived)
        {
            // 没收到认证响应，说明是网络问题
            SetStatus("连接失败，请检查服务器是否运行");
        }
        else
        {
            SetStatus("与服务器断开连接");
        }
        
        // 重置状态
        authResponseReceived = false;
        lastAuthError = "";
        isRegistering = false;
        
        ShowLoginPanel();
    }

    public void OnClickStartClient()
    {
        if (manager == null) manager = Object.FindFirstObjectByType<NetworkManager>();
        if (manager == null) 
        { 
            SetStatus("错误: 未找到 NetworkManager"); 
            return; 
        }

        if (string.IsNullOrWhiteSpace(usernameInput?.text))
        {
            SetStatus("请输入用户名");
            return;
        }

        if (string.IsNullOrWhiteSpace(passwordInput?.text))
        {
            SetStatus("请输入密码");
            return;
        }

        // 设置 pending 凭证，连接后由 SimpleAuthenticator 自动发送
        SimpleAuthenticator.pending = new SimpleAuthenticator.PendingAuth
        {
            operation = registerToggle != null && registerToggle.isOn ? "register" : "login",
            username = usernameInput.text.Trim(),
            password = passwordInput.text,
            displayName = displayNameInput != null ? displayNameInput.text.Trim() : ""
        };

        // 重置认证状态
        authResponseReceived = false;
        lastAuthError = "";
        authErrorHandled = false;
        isRegistering = registerToggle?.isOn == true;

        SetStatus(isRegistering ? "正在注册..." : "正在登录...");
        isConnecting = true;
        manager.StartClient();
    }

    public void OnClickStartHost()
    {
        if (manager == null) manager = Object.FindFirstObjectByType<NetworkManager>();
        if (manager == null) 
        { 
            SetStatus("错误: 未找到 NetworkManager"); 
            return; 
        }

        SimpleAuthenticator.pending = new SimpleAuthenticator.PendingAuth
        {
            operation = registerToggle != null && registerToggle.isOn ? "register" : "login",
            username = usernameInput != null ? usernameInput.text.Trim() : "host",
            password = passwordInput != null ? passwordInput.text : "host",
            displayName = displayNameInput != null ? displayNameInput.text.Trim() : "Host"
        };

        // 重置认证状态
        authResponseReceived = false;
        lastAuthError = "";
        authErrorHandled = false;
        isRegistering = registerToggle?.isOn == true;

        SetStatus("正在启动主机...");
        isConnecting = true;
        manager.StartHost();
    }

    public void OnClickDisconnect()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            manager.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            manager.StopClient();
        }
        ShowLoginPanel();
        SetStatus("已断开连接");
    }

    private void OnLoginSuccess()
    {
        SetStatus("登录成功!");
        
        // 请求用户数据
        GameDataNetworkHandler.RequestUserData();
        
        ShowUserInfoPanel();
    }

    private void OnRegisterSuccess()
    {
        // 使用全局弹窗管理器显示注册成功
        if (PopupManager.Instance != null)
        {
            PopupManager.Instance.ShowConfirm("注册成功", "恭喜！您的账号已成功注册。点击确定开始游戏。", () =>
            {
                SetStatus("注册成功，正在进入游戏...");
                
                // 请求用户数据
                GameDataNetworkHandler.RequestUserData();
                
                ShowUserInfoPanel();
            });
        }
        else
        {
            // 如果PopupManager不存在，直接进入游戏
            Debug.LogWarning("[LoginUI] PopupManager未配置，直接进入游戏");
            SetStatus("注册成功，正在进入游戏...");
            GameDataNetworkHandler.RequestUserData();
            ShowUserInfoPanel();
        }
    }

    private void OnUserDataReceived(UserDataResponseMessage data)
    {
        if (data.success)
        {
            if (userDisplayNameText != null)
                userDisplayNameText.text = data.displayName;
            if (userLevelText != null)
                userLevelText.text = $"等级: {data.level}";
            if (userGoldText != null)
                userGoldText.text = $"金币: {data.gold}";
            if (userDiamondText != null)
                userDiamondText.text = $"钻石: {data.diamond}";

            SetStatus($"欢迎, {data.displayName}! 未读邮件: {data.unreadMailCount}, 待领奖励: {data.unclaimedRewardCount}");
        }
    }

    private void OnCurrencyUpdated(CurrencyUpdateMessage data)
    {
        if (userGoldText != null)
            userGoldText.text = $"金币: {data.gold}";
        if (userDiamondText != null)
            userDiamondText.text = $"钻石: {data.diamond}";
        if (userLevelText != null)
            userLevelText.text = $"等级: {data.level}";
    }

    private void ShowLoginPanel()
    {
        if (loginPanel != null) loginPanel.SetActive(true);
        if (userInfoPanel != null) userInfoPanel.SetActive(false);
    }

    private void ShowUserInfoPanel()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (userInfoPanel != null) userInfoPanel.SetActive(true);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
        Debug.Log($"[LoginUI] {message}");
    }
}
