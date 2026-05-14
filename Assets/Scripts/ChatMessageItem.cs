using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatMessageItem : MonoBehaviour
{
    [SerializeField] private TMP_Text contentText;

    private CanvasGroup _canvasGroup;
    private static readonly Color MineNameColor = new(0.4f, 0.7f, 1f);
    private static readonly Color OtherNameColor = new(0.6f, 0.6f, 0.6f);

    public void Setup(string sender, string message, bool isMine)
    {
        if (contentText == null) return;

        var nameColor = isMine ? MineNameColor : OtherNameColor;
        var hex = ColorUtility.ToHtmlStringRGB(nameColor);
        contentText.text = $"<color=#{hex}><b>{sender}</b></color>  {message}";
    }

    public void SetPending()
    {
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0.5f;

        if (contentText != null)
            contentText.text += "  <i><color=#999999>sending...</color></i>";
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
        }
    }
}
