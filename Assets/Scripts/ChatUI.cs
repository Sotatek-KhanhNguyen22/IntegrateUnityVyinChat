using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ChatManager chatManager;
    [SerializeField] private ScrollRect messageScrollRect;
    [SerializeField] private RectTransform messageContainer;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private TMP_Text userStatusText;
    [SerializeField] private TMP_Text channelNameText;
    [SerializeField] private TMP_Text channelUrlText;
    [SerializeField] private Button channelUrlCopyButton;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private GameObject messagePrefab;

    [Header("Settings")]
    [SerializeField] private int maxMessages = 100;

    private readonly List<GameObject> _messageObjects = new();
    private readonly Dictionary<string, ChatMessageItem> _pendingMessages = new();
    private readonly HashSet<long> _renderedMessageIds = new();
    private string _currentChannelUrl = string.Empty;
    private int _pendingCounter;

    void Start()
    {
        sendButton.onClick.AddListener(OnSendClicked);
        if (channelUrlCopyButton != null)
            channelUrlCopyButton.onClick.AddListener(OnCopyChannelUrlClicked);

        inputField.onSubmit.AddListener(_ => OnSendClicked());

        chatManager.OnConnectionStateChanged += UpdateStatus;
        chatManager.OnChannelJoined += OnChannelJoined;
        chatManager.OnMessageHistoryLoaded += OnMessageHistoryLoaded;
        chatManager.OnMessageReceived += OnMessageReceived;
        chatManager.OnMessageSentWithId += OnMessageSent;
        chatManager.OnError += OnErrorReceived;

        if (userStatusText != null)
            userStatusText.text = "User: ---";
        if (channelNameText != null)
            channelNameText.text = "Channel: ---";
        if (channelUrlText != null)
            channelUrlText.text = "URL: ---";
        if (channelUrlCopyButton != null)
            channelUrlCopyButton.interactable = false;
        if (sendButton != null)
            sendButton.interactable = false;
    }

    void OnDestroy()
    {
        if (channelUrlCopyButton != null)
            channelUrlCopyButton.onClick.RemoveListener(OnCopyChannelUrlClicked);

        if (chatManager != null)
        {
            chatManager.OnConnectionStateChanged -= UpdateStatus;
            chatManager.OnChannelJoined -= OnChannelJoined;
            chatManager.OnMessageHistoryLoaded -= OnMessageHistoryLoaded;
            chatManager.OnMessageReceived -= OnMessageReceived;
            chatManager.OnMessageSentWithId -= OnMessageSent;
            chatManager.OnError -= OnErrorReceived;
        }
    }

    private void OnSendClicked()
    {
        var text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        if (!chatManager.IsChannelReady)
        {
            ShowError("Channel not ready");
            return;
        }

        var pendingId = $"pending_{_pendingCounter++}";
        AddPendingMessage(chatManager.UserId, text, pendingId);
        chatManager.SendChatMessage(text, pendingId);
        inputField.text = "";
        inputField.ActivateInputField();
    }

    private void OnCopyChannelUrlClicked()
    {
        if (string.IsNullOrWhiteSpace(_currentChannelUrl))
            return;

        GUIUtility.systemCopyBuffer = _currentChannelUrl;
    }

    private void OnMessageReceived(string message, string senderId, long messageId)
    {
        bool isMine = senderId == chatManager.UserId;
        if (!isMine)
            AddMessageBubble(senderId, message, false, messageId);
    }

    private void OnMessageSent(string message, long messageId, string pendingId)
    {
        if (_pendingMessages.TryGetValue(pendingId, out var item))
        {
            item.SetConfirmed();
            _pendingMessages.Remove(pendingId);
            if (messageId != 0)
                _renderedMessageIds.Add(messageId);
        }
    }

    private void OnErrorReceived(string error)
    {
        ShowError(error);
    }

    private void ShowError(string message)
    {
        if (errorText == null) return;
        errorText.text = message;
        errorText.gameObject.SetActive(true);
        CancelInvoke(nameof(HideError));
        Invoke(nameof(HideError), 5f);
    }

    private void HideError()
    {
        if (errorText != null)
            errorText.gameObject.SetActive(false);
    }

    private void UpdateStatus(string status)
    {
        if (status.StartsWith("Connected as"))
        {
            if (userStatusText != null)
                userStatusText.text = $"User: {chatManager.UserId}";
        }
        else if (status == "Disconnected")
        {
            if (userStatusText != null)
                userStatusText.text = "User: disconnected";
            if (channelNameText != null)
                channelNameText.text = "Channel: ---";
            if (channelUrlText != null)
                channelUrlText.text = "URL: ---";
            if (channelUrlCopyButton != null)
                channelUrlCopyButton.interactable = false;
            _currentChannelUrl = string.Empty;
            if (sendButton != null)
                sendButton.interactable = false;
            ClearMessages();
        }
        else
        {
            if (userStatusText != null)
                userStatusText.text = $"User: {status}";
            if (sendButton != null)
                sendButton.interactable = false;
        }
    }

    private void OnChannelJoined(string name, string url)
    {
        ClearMessages();
        _currentChannelUrl = url;
        if (channelNameText != null)
            channelNameText.text = $"Channel: {name}";
        if (channelUrlText != null)
            channelUrlText.text = $"URL: {url}";
        if (channelUrlCopyButton != null)
            channelUrlCopyButton.interactable = !string.IsNullOrWhiteSpace(url);
        if (sendButton != null)
            sendButton.interactable = true;
    }

    private void OnMessageHistoryLoaded(IReadOnlyList<ChatMessageRecord> messages)
    {
        foreach (var message in messages)
        {
            var isMine = message.SenderId == chatManager.UserId;
            AddMessageBubble(message.SenderId, message.Message, isMine, message.MessageId);
        }
    }

    private void AddMessageBubble(string sender, string message, bool isMine, long messageId = 0)
    {
        if (messagePrefab == null || messageContainer == null) return;
        if (messageId != 0 && !_renderedMessageIds.Add(messageId)) return;

        var go = Instantiate(messagePrefab, messageContainer);
        var item = go.GetComponent<ChatMessageItem>();
        if (item != null)
            item.Setup(sender, message, isMine);

        _messageObjects.Add(go);
        TrimMessages();
        ScrollToBottom();
    }

    private void AddPendingMessage(string sender, string message, string pendingId)
    {
        if (messagePrefab == null || messageContainer == null) return;

        var go = Instantiate(messagePrefab, messageContainer);
        var item = go.GetComponent<ChatMessageItem>();
        if (item != null)
        {
            item.Setup(sender, message, true);
            item.SetPending();
            _pendingMessages[pendingId] = item;
        }

        _messageObjects.Add(go);
        TrimMessages();
        ScrollToBottom();
    }

    private void TrimMessages()
    {
        while (_messageObjects.Count > maxMessages)
        {
            Destroy(_messageObjects[0]);
            _messageObjects.RemoveAt(0);
        }
    }

    private void ClearMessages()
    {
        foreach (var messageObject in _messageObjects)
            Destroy(messageObject);

        _messageObjects.Clear();
        _pendingMessages.Clear();
        _renderedMessageIds.Clear();
    }

    private void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        messageScrollRect.verticalNormalizedPosition = 0f;
    }
}
