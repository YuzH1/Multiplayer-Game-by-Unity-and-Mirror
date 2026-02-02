using UnityEngine;
using Mirror;
using Unity.Cinemachine;

// 使用 Cinemachine 的相机跟随
public class PlayerCameraFollow : NetworkBehaviour
{
    [Header("调试信息")]
    public bool showDebugInfo = true;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        
        if (showDebugInfo)
            Debug.Log($"[PlayerCameraFollow] OnStartLocalPlayer 被调用, isLocalPlayer={isLocalPlayer}");
        
        // 检查 Main Camera 是否有 CinemachineBrain
        var mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("[PlayerCameraFollow] 场景中没有 Main Camera！");
            return;
        }

        var brain = mainCam.GetComponent<CinemachineBrain>();
        if (brain == null)
        {
            Debug.LogError("[PlayerCameraFollow] Main Camera 上没有 CinemachineBrain 组件！请添加该组件。");
            return;
        }

        if (showDebugInfo)
            Debug.Log($"[PlayerCameraFollow] CinemachineBrain 已找到, IsBlending={brain.IsBlending}");

        // 查找 CinemachineCamera
        var vcam = FindFirstObjectByType<CinemachineCamera>();

        if (vcam != null)
        {
            // 设置跟随目标和注视目标
            vcam.Follow = transform;
            vcam.LookAt = transform;
            
            // 确保虚拟相机是激活的
            vcam.gameObject.SetActive(true);
            
            // 提升优先级确保这个相机被使用
            vcam.Priority = 10;

            if (showDebugInfo)
            {
                Debug.Log($"[PlayerCameraFollow] CinemachineCamera 设置完成:");
                Debug.Log($"  - Follow = {vcam.Follow?.name}");
                Debug.Log($"  - LookAt = {vcam.LookAt?.name}");
                Debug.Log($"  - Priority = {vcam.Priority}");
                Debug.Log($"  - GameObject Active = {vcam.gameObject.activeInHierarchy}");
                
                // 检查是否有 Body 组件（如 CinemachineFollow、CinemachineOrbitalFollow 等）
                var follow3rdPerson = vcam.GetComponent<CinemachineFollow>();
                var orbitalFollow = vcam.GetComponent<CinemachineOrbitalFollow>();
                var positionComposer = vcam.GetComponent<CinemachinePositionComposer>();
                
                if (follow3rdPerson == null && orbitalFollow == null && positionComposer == null)
                {
                    Debug.LogWarning("[PlayerCameraFollow] CinemachineCamera 上没有跟随组件！请添加以下之一：\n" +
                        "- CinemachineFollow (第三人称跟随)\n" +
                        "- CinemachineOrbitalFollow (轨道跟随)\n" +
                        "- CinemachinePositionComposer (位置合成)");
                }
                else
                {
                    Debug.Log($"  - 跟随组件: {(follow3rdPerson != null ? "CinemachineFollow" : orbitalFollow != null ? "CinemachineOrbitalFollow" : "CinemachinePositionComposer")}");
                }
            }
        }
        else
        {
            Debug.LogError("[PlayerCameraFollow] 场景里没找到 CinemachineCamera！请在场景中创建一个带有 CinemachineCamera 组件的 GameObject。");
        }
    }
}
