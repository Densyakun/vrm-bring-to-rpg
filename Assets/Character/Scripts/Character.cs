using UnityEngine;
using Mirror;
using Cinemachine;

public class Character : NetworkBehaviour
{
    [SyncVar]
    public string characterName;
    public Transform playerCameraRoot;

    public override void OnStartClient()
    {
        if (isLocalPlayer) {
            characterName = NewNetworkManager.singleton.characterNameInput.text;

            GameObject playerFollowCamera = GameObject.FindWithTag("PlayerFollowCamera");
            CinemachineVirtualCamera cinemachineVirtualCamera = playerFollowCamera.GetComponent<CinemachineVirtualCamera>();
            cinemachineVirtualCamera.Follow = playerCameraRoot;
        }
    }
}
