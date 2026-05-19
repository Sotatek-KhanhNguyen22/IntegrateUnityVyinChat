using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatMessageItem : MonoBehaviour
{
    [SerializeField] private TMP_Text contentText;

    private CanvasGroup _canvasGroup;
    private LayoutElement _layoutElement;
    private static readonly Color MineNameColor = new(0.4f, 0.7f, 1f);
    private static readonly Color OtherNameColor = new(0.6f, 0.6f, 0.6f);

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _layoutElement = GetComponent<LayoutElement>();
    }

    public void Setup(string sender, string message, bool isMine)
    {
        if (contentText == null) return;

        var nameColor = isMine ? MineNameColor : OtherNameColor;
        var hex = ColorUtility.ToHtmlStringRGB(nameColor);
        contentText.text = $"<color=#{hex}><b>{sender}</b></color>\n{message}";
        UpdatePreferredHeight();
    }

    public void SetPending()
    {
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0.5f;

        if (contentText != null)
        {
            contentText.text += "  <i><color=#999999>sending...</color></i>";
            UpdatePreferredHeight();
        }
    }

    public void SetConfirmed()
    {
        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        if (contentText != null)
        {
            var idx = contentText.text.LastIndexOf("  <i><color=#999999>sending...</color></i>");
            if (idx >= 0)
                contentText.text = contentText.text.Substring(0, idx);
            UpdatePreferredHeight();
        }
    }

    private void UpdatePreferredHeight()
    {
        if (contentText == null)
            return;

        if (_layoutElement == null)
            _layoutElement = GetComponent<LayoutElement>();

        if (_layoutElement == null)
            return;

        var width = contentText.rectTransform.rect.width;
        if (width <= 0f && transform is RectTransform rectTransform)
            width = Mathf.Max(1f, rectTransform.rect.width - 4f);

        var preferred = contentText.GetPreferredValues(contentText.text, width, 0f);
        _layoutElement.preferredHeight = Mathf.Max(40f, preferred.y + 6f);
    }
}
