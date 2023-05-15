using UnityEngine;
using Mirror;
using Cinemachine;

public class Character : NetworkBehaviour
{
    public Transform playerCameraRoot;
    public CharacterNameText characterNameTextPrefab;

    [SyncVar(hook = nameof(SetCharacterName))]
    public string characterName;
    public CharacterNameText characterNameText;

    public override void OnStartClient()
    {
        if (!isLocalPlayer) return;

        CmdSetCharacterName(NewNetworkManager.singleton.characterNameInput.text);

        GameObject playerFollowCamera = GameObject.FindWithTag("PlayerFollowCamera");
        CinemachineVirtualCamera cinemachineVirtualCamera = playerFollowCamera.GetComponent<CinemachineVirtualCamera>();
        cinemachineVirtualCamera.Follow = playerCameraRoot;
    }

    void OnDestroy()
    {
        Destroy(characterNameText.gameObject);
    }

    [Command]
    void CmdSetCharacterName(string characterName)
    {
        if (isServerOnly) SetCharacterName("", this.characterName = characterName);
    }

    void SetCharacterName(string oldCharacterName, string newCharacterName)
    {
        GameObject overlayCanvasObject = GameObject.FindWithTag("OverlayCanvas");
        characterNameText = Instantiate(characterNameTextPrefab, overlayCanvasObject.transform);
        characterNameText.setCharacter(this);
    }
}
