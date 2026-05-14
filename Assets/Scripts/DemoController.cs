using UnityEngine;

/// <summary>
/// Legacy entry point - replaced by ChatManager + ChatUI.
/// Use menu VyinChat > Setup Dashboard Scene to create the chat UI.
/// </summary>
public class DemoController : MonoBehaviour
{
    [SerializeField] private ChatManager chatManager;

    void Start()
    {
        if (chatManager == null)
            chatManager = FindAnyObjectByType<ChatManager>();

        if (chatManager == null)
            Debug.LogWarning("ChatManager not found. Use VyinChat > Setup Dashboard Scene to set up.");
    }
}
