using Mirror;
using UnityEngine;

// 简单生命系统：服务器权威
public class PlayerHealth : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    public int current;

    public int max = 100;

    public override void OnStartServer()
    {
        current = max;
    }

    [Server]
    public void Heal(int amount)
    {
        current = Mathf.Clamp(current + Mathf.Abs(amount), 0, max);
    }

    [Server]
    public void TakeDamage(int amount)
    {
        if (current <= 0) return;
        current = Mathf.Clamp(current - Mathf.Abs(amount), 0, max);
        if (current == 0)
        {
            OnServerDeath();
            RpcOnDeath();
        }
    }

    void OnHealthChanged(int oldValue, int newValue)
    {
        // 客户端/UI 可在此刷新血条
        // Debug.Log($"Health: {oldValue} -> {newValue}");
    }

    [Server]
    void OnServerDeath()
    {
        // 服务器处理死亡逻辑：重生计时/掉落/结算等
    }

    [ClientRpc]
    void RpcOnDeath()
    {
        // 客户端表现：播放死亡动画，禁用控制等
    }
}
