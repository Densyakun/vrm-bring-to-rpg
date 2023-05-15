using UnityEngine;
using Mirror;
using Cinemachine;

public class Character : NetworkBehaviour
{
    public Transform playerCameraRoot;

    public override void OnStartClient()
    {
        if (isLocalPlayer) {
            GameObject playerFollowCamera = GameObject.FindWithTag("PlayerFollowCamera");
            CinemachineVirtualCamera cinemachineVirtualCamera = playerFollowCamera.GetComponent<CinemachineVirtualCamera>();
            cinemachineVirtualCamera.Follow = playerCameraRoot;
        }
    }
}
