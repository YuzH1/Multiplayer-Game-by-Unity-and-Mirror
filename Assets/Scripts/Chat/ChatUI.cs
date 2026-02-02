using System.Text;
using UnityEngine;
using UnityEngine.UI;
using MultiplayerGame.Chat;
using MultiplayerGame.Auth;

// 简易聊天 UI（示例）：输入框 + 显示框
// 将 InputField / Text 引用拖入即可运行

public class ChatUI : MonoBehaviour
{
    public InputField input;
    public Text output;

    StringBuilder sb = new();

    void OnEnable()
    {
        ChatManager.OnChatReceived += HandleChat;
    }

    void OnDisable()
    {
        ChatManager.OnChatReceived -= HandleChat;
    }

    public void OnClickSend()
    {
        if (input == null) return;
        var text = input.text;
        if (string.IsNullOrWhiteSpace(text)) return;
        ChatManager.ClientSend(text);
        input.text = string.Empty;
    }

    private void HandleChat(MultiplayerGame.Auth.ChatMessage msg)
    {
        if (output == null) return;
        sb.AppendLine($"[{msg.serverTime:HH:mm:ss}] {msg.from}: {msg.text}");
        output.text = sb.ToString();
    }
}
