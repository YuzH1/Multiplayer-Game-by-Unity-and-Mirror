using UnityEngine;
using UnityEngine.UI;
using Mirror;
using MultiplayerGame;
using MultiplayerGame.Auth;
using MultiplayerGame.Network;
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
    public Toggle registerToggle;
    
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
        
        // 初始状态
        ShowLoginPanel();
    }

    void OnDestroy()
    {
        GameDataNetworkHandler.OnUserDataReceived -= OnUserDataReceived;
        GameDataNetworkHandler.OnCurrencyUpdated -= OnCurrencyUpdated;
        
        // 取消网络事件订阅
        NetworkClient.OnDisconnectedEvent -= OnClientDisconnected;
    }

    void Update()
    {
        // 检查连接状态变化
        if (isConnecting)
        {
            if (NetworkClient.isConnected && NetworkClient.connection != null && NetworkClient.connection.isAuthenticated)
            {
                isConnecting = false;
                OnLoginSuccess();
            }
            else if (!NetworkClient.active && !NetworkClient.isConnecting)
            {
                // 连接失败或被断开
                isConnecting = false;
                SetStatus("连接失败，请检查服务器是否运行");
            }
        }
    }

    private void OnClientDisconnected()
    {
        isConnecting = false;
        SetStatus("与服务器断开连接");
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

        SetStatus(registerToggle?.isOn == true ? "正在注册..." : "正在登录...");
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
