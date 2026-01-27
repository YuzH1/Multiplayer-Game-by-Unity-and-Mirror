using UnityEngine;
using UnityEngine.UI;
using Mirror;
using MultiplayerGame.Auth;

// 简易登录/注册 UI 脚本（示例）
// 使用方式：
// - 在场景中创建包含 InputField/TMP_InputField 的 UI，并将引用拖拽到本脚本
// - 点击 “连接(客户端)” 按钮时：设置 SimpleAuthenticator.pending，然后调用 NetworkManager.StartClient()
// - 点击 “开房(主机)” 按钮时：同理设置 pending 后调用 StartHost()

public class LoginUI : MonoBehaviour
{
    [Header("UI References")]
    public InputField usernameInput;
    public InputField passwordInput;
    public InputField displayNameInput; // 注册时可选
    public Toggle registerToggle;

    [Header("Network")]
    public NetworkManager manager;

    void Reset()
    {
        manager = Object.FindFirstObjectByType<NetworkManager>();
    }

    public void OnClickStartClient()
    {
    if (manager == null) manager = Object.FindFirstObjectByType<NetworkManager>();
        if (manager == null) { Debug.LogError("LoginUI: 未找到 NetworkManager"); return; }

        // 设置 pending 凭证，连接后由 SimpleAuthenticator 自动发送
        SimpleAuthenticator.pending = new SimpleAuthenticator.PendingAuth
        {
            operation = registerToggle != null && registerToggle.isOn ? "register" : "login",
            username = usernameInput != null ? usernameInput.text.Trim() : "",
            password = passwordInput != null ? passwordInput.text : "",
            displayName = displayNameInput != null ? displayNameInput.text.Trim() : ""
        };

        manager.StartClient();
    }

    public void OnClickStartHost()
    {
    if (manager == null) manager = Object.FindFirstObjectByType<NetworkManager>();
        if (manager == null) { Debug.LogError("LoginUI: 未找到 NetworkManager"); return; }

        SimpleAuthenticator.pending = new SimpleAuthenticator.PendingAuth
        {
            operation = registerToggle != null && registerToggle.isOn ? "register" : "login",
            username = usernameInput != null ? usernameInput.text.Trim() : "host",
            password = passwordInput != null ? passwordInput.text : "host",
            displayName = displayNameInput != null ? displayNameInput.text.Trim() : "Host"
        };

        manager.StartHost();
    }
}
