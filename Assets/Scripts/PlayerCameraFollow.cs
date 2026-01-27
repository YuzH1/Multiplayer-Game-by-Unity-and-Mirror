using UnityEngine;
using Mirror;

// 非 Cinemachine 的简单相机跟随
public class PlayerCameraFollow : NetworkBehaviour
{
    public Transform cameraPivot; // 为空则使用自身
    public Vector3 offset = new Vector3(0, 2.2f, -4.5f);
    public float followLerp = 12f;

    Camera cam;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("No Main Camera found for PlayerCameraFollow");
            return;
        }
        if (cameraPivot == null) cameraPivot = transform;
        // 确保相机激活
        cam.gameObject.SetActive(true);
    }

    void LateUpdate()
    {
        if (!isLocalPlayer || cam == null || cameraPivot == null) return;
        Vector3 targetPos = cameraPivot.position + cameraPivot.rotation * offset;
        cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, Time.deltaTime * followLerp);
        cam.transform.rotation = Quaternion.Lerp(cam.transform.rotation, cameraPivot.rotation, Time.deltaTime * followLerp);
    }
}
