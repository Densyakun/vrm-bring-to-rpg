using TMPro;
using UnityEngine;

public class CharacterNameText : MonoBehaviour
{
    public static Vector3 characterOffset = new Vector3(0, 1.8f, 0);

    [SerializeField]
    private TMP_Text text;

    public Character character;
    private RectTransform overlayCanvasTransform;

    void Start()
    {
        GameObject overlayCanvasObject = GameObject.FindWithTag("OverlayCanvas");
        overlayCanvasTransform = overlayCanvasObject.GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        if (!character) return;

        RectTransform rectTransform = GetComponent<RectTransform>();
        Vector3 viewportPoint = Camera.main.WorldToViewportPoint(character.transform.position + characterOffset);
        rectTransform.anchoredPosition = overlayCanvasTransform.sizeDelta * viewportPoint;
    }

    public void setCharacter(Character character)
    {
        this.character = character;
        text.text = character.characterName;
    }
}
