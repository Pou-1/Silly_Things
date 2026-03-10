using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonColorChanger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Button targetButton;
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color clickColor = Color.green;
    public Color normalTextColor = Color.black;
    public Color hoverTextColor = Color.blue;
    public Color clickTextColor = Color.red;

    private Text buttonText;

    void Start()
    {
        if (targetButton == null)
            targetButton = GetComponent<Button>();

        buttonText = targetButton.GetComponentInChildren<Text>();
        SetNormal();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetButton.image.color = hoverColor;
        if (buttonText != null)
            buttonText.color = hoverTextColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetNormal();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        targetButton.image.color = clickColor;
        if (buttonText != null)
            buttonText.color = clickTextColor;
    }

    private void SetNormal()
    {
        targetButton.image.color = normalColor;
        if (buttonText != null)
            buttonText.color = normalTextColor;
    }
}
