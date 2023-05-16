using System.Collections;
using System.IO;
using UniGLTF;
using UniVRM10;
using UnityEngine;
using UnityEngine.Networking;
using Mirror;
using Cinemachine;

public class Character : NetworkBehaviour
{
    public static ulong MAX_BYTES = 31457280; // 30 MiB

    public Transform playerCameraRoot;
    public CharacterNameText characterNameTextPrefab;
    public GameObject vrmCharacterParent;

    [SyncVar(hook = nameof(SetCharacterName))]
    public string characterName;
    public CharacterNameText characterNameText;
    [SyncVar(hook = nameof(SetVRMFileURL))]
    public string vrmFileURL;
    private RuntimeGltfInstance vrmCharacterInstance;

    public override void OnStartClient()
    {
        if (!isLocalPlayer) return;

        CmdSetPlayerData(
            NewNetworkManager.singleton.characterNameInput.text,
            NewNetworkManager.singleton.vrmFileURLInput.text
        );

        GameObject playerFollowCamera = GameObject.FindWithTag("PlayerFollowCamera");
        CinemachineVirtualCamera cinemachineVirtualCamera = playerFollowCamera.GetComponent<CinemachineVirtualCamera>();
        cinemachineVirtualCamera.Follow = playerCameraRoot;
    }

    void Update()
    {
        if (!vrmCharacterInstance) return;

        Animator vrmAnimator = vrmCharacterInstance.GetComponent<Animator>();
        GetComponent<Animator>().avatar = vrmAnimator.avatar;
    }

    void OnDestroy()
    {
        Destroy(characterNameText.gameObject);
    }

    [Command]
    void CmdSetPlayerData(string characterName, string vrmFileURL)
    {
        this.characterName = characterName;
        this.vrmFileURL = vrmFileURL;

        if (!isServerOnly) return;

        SetCharacterName("", characterName);
        SetVRMFileURL("", vrmFileURL);
    }

    void SetCharacterName(string oldCharacterName, string newCharacterName)
    {
        GameObject overlayCanvasObject = GameObject.FindWithTag("OverlayCanvas");
        characterNameText = Instantiate(characterNameTextPrefab, overlayCanvasObject.transform);
        characterNameText.setCharacter(this);
    }

    void SetVRMFileURL(string oldVRMFileURL, string newVRMFileURL)
    {
        StartCoroutine(GetVRMFile(newVRMFileURL));
    }

    IEnumerator GetVRMFile(string url)
    {
        string directoryPath = Path.Combine(Application.persistentDataPath, "vrm-cache/");

        string path = Path.Combine(directoryPath, string.Concat(url.Split(Path.GetInvalidFileNameChars())));

        if (File.Exists(path))
        {
            LoadModel(path);
            yield break;
        }

        UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
        request.downloadHandler = new DownloadHandlerFile(path);
        var async = request.SendWebRequest();

        while (true)
        {
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(request.error);
                yield break;
            }

            if (async.isDone) break;

            yield return null;

            ulong size = 0;
            var header = request.GetResponseHeader("Content-Length");
            if (header != null)
            {
                ulong.TryParse(header, out size);

                if (MAX_BYTES < size)
                {
                    request.Abort();
                    yield break;
                }
                else
                {
                    yield return async;
                    break;
                }
            }

            if (MAX_BYTES < request.downloadedBytes)
            {
                request.Abort();
                break;
            }
        }

        LoadModel(path);
    }

    async void LoadModel(string path)
    {
        var vrm10Instance = await Vrm10.LoadPathAsync(path,
            canLoadVrm0X: true,
            showMeshes: false
        );

        if (vrm10Instance == null)
        {
            Debug.LogWarning("LoadPathAsync is null");
            return;
        }

        if (!this) return;

        vrmCharacterInstance = vrm10Instance.GetComponent<RuntimeGltfInstance>();
        Destroy(vrmCharacterParent.transform.GetChild(0).gameObject);
        vrmCharacterInstance.transform.SetParent(vrmCharacterParent.transform, false);

        vrmCharacterInstance.ShowMeshes();
        vrmCharacterInstance.EnableUpdateWhenOffscreen();
    }
}
