using Mirror;
using UnityEngine;

// 服务器权威移动（入门版）：
// - 客户端仅发送输入，服务器计算并更新位置/旋转
// - 支持 CharacterController 或 Rigidbody，若都无则直接改 transform
// - 可与 Mirror 的 NetworkTransform 搭配以平滑非本地显示

[RequireComponent(typeof(NetworkIdentity))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.5f;
    public float rotationSpeed = 720f; // deg/sec 根据 yaw 插值
    public float gravity = -9.81f;
    public float jumpSpeed = 5f;

    CharacterController cc;
    Rigidbody rb;

    // 服务端保存的最后输入
    struct InputState
    {
        public float x;   // -1..1
        public float y;   // -1..1
        public bool sprint;
        public bool jump;
        public float yaw; // 水平朝向（世界系）
    }

    InputState serverInput;

    // 客户端采集并节流上送
    float clientSendInterval = 1f / 60f;
    float clientSendTimer;

    // 跳跃用的简单垂直速度
    float verticalVelocity;

    void Awake()
    {
        TryGetComponent(out cc);
        TryGetComponent(out rb);
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        clientSendTimer += Time.deltaTime;
        if (clientSendTimer >= clientSendInterval)
        {
            clientSendTimer = 0f;

            float x = Mathf.Clamp(Input.GetAxisRaw("Horizontal"), -1f, 1f);
            float y = Mathf.Clamp(Input.GetAxisRaw("Vertical"), -1f, 1f);
            bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool jump = Input.GetKey(KeyCode.Space);

            // yaw 来自相机或角色朝向，这里简化为当前角色 y 轴朝向
            float yaw = transform.eulerAngles.y;

            CmdSetInput(x, y, sprint, jump, yaw);
        }
    }

    // 服务器接收输入
    [Command]
    void CmdSetInput(float x, float y, bool sprint, bool jump, float yaw)
    {
        serverInput.x = Mathf.Clamp(x, -1f, 1f);
        serverInput.y = Mathf.Clamp(y, -1f, 1f);
        serverInput.sprint = sprint;
        serverInput.jump = jump;
        serverInput.yaw = Mathf.Repeat(yaw, 360f);
    }

    void FixedUpdate()
    {
        if (!isServer) return;

        // 计算移动方向（基于 yaw 的平面前右）
        Quaternion look = Quaternion.Euler(0f, serverInput.yaw, 0f);
        Vector3 forward = look * Vector3.forward;
        Vector3 right = look * Vector3.right;
        Vector3 move = forward * serverInput.y + right * serverInput.x;
        move = Vector3.ClampMagnitude(move, 1f);

        float speed = moveSpeed * (serverInput.sprint ? sprintMultiplier : 1f);
        Vector3 horizontalVel = move * speed;

        // 跳跃与重力（简版）
        if (cc != null)
        {
            if (cc.isGrounded)
            {
                if (serverInput.jump)
                    verticalVelocity = jumpSpeed;
                else if (verticalVelocity < 0)
                    verticalVelocity = -1f; // 轻微贴地
            }
            verticalVelocity += gravity * Time.fixedDeltaTime;
            Vector3 vel = new Vector3(horizontalVel.x, verticalVelocity, horizontalVel.z);
            cc.Move(vel * Time.fixedDeltaTime);

            // 更新旋转至 yaw
            RotateTowardsYaw(look);
        }
        else if (rb != null)
        {
            Vector3 vel = rb.linearVelocity;
            vel.x = horizontalVel.x;
            vel.z = horizontalVel.z;
            if (IsGroundedRB())
            {
                if (serverInput.jump)
                    vel.y = jumpSpeed;
            }
            vel.y += gravity * Time.fixedDeltaTime;
            rb.linearVelocity = vel;

            RotateTowardsYaw(look);
        }
        else
        {
            // 直接位移（不建议用于正式物理）
            transform.position += horizontalVel * Time.fixedDeltaTime;
            RotateTowardsYaw(look);
        }
    }

    bool IsGroundedRB()
    {
        if (rb == null) return true;
        float radius = 0.2f;
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        return Physics.SphereCast(origin, radius, Vector3.down, out _, 0.2f);
    }

    void RotateTowardsYaw(Quaternion look)
    {
        transform.rotation = Quaternion.RotateTowards(transform.rotation, look, rotationSpeed * Time.fixedDeltaTime);
    }
}
