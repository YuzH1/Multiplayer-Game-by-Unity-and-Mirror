using Mirror;
using UnityEngine;

// 入门近战/普攻：请求-确认-伤害
// - 客户端：点击攻击键，调用 CmdAttack
// - 服务器：检测冷却与命中，调用目标的 PlayerHealth.TakeDamage
public class PlayerCombat : NetworkBehaviour
{
    public int damage = 20;
    public float cooldown = 0.6f;
    public float range = 2.2f;
    public float radius = 1.2f; // 近战扇形简化为球

    float lastAttackTimeServer;

    void Update()
    {
        if (!isLocalPlayer) return;
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J))
        {
            CmdAttack();
        }
    }

    [Command]
    void CmdAttack()
    {
        if (Time.time - lastAttackTimeServer < cooldown) return;
        lastAttackTimeServer = Time.time;

        // 服务器在角色前方做范围检测
        Vector3 center = transform.position + transform.forward * range * 0.5f + Vector3.up * 0.9f;
        Collider[] hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore);
        foreach (var col in hits)
        {
            if (!col || col.attachedRigidbody && col.attachedRigidbody.gameObject == gameObject) continue;
            var targetHealth = col.GetComponentInParent<PlayerHealth>();
            if (targetHealth != null && targetHealth != GetComponent<PlayerHealth>())
            {
                targetHealth.TakeDamage(damage);
            }
        }

        RpcOnAttackEffect();
    }

    [ClientRpc]
    void RpcOnAttackEffect()
    {
        // 客户端表现（动画/粒子/音效），此处留空供美术接入
    }

    void OnDrawGizmosSelected()
    {
        // 在编辑器中可视化攻击范围
        Gizmos.color = Color.red;
        Vector3 center = transform.position + transform.forward * range * 0.5f + Vector3.up * 0.9f;
        Gizmos.DrawWireSphere(center, radius);
    }
}
