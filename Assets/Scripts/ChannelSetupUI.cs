using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChannelSetupUI : MonoBehaviour
{
    private static readonly Color ButtonColor = new(0.18f, 0.18f, 0.22f);
    private static readonly Color SelectedColor = new(0.2f, 0.55f, 0.95f);
    private static readonly Color DisabledColor = new(0.25f, 0.25f, 0.28f);

    [Header("References")]
    [SerializeField] private ChatManager chatManager;
    [SerializeField] private Button createTabButton;
    [SerializeField] private Button joinTabButton;
    [SerializeField] private Button groupTypeButton;
    [SerializeField] private Button openTypeButton;
    [SerializeField] private Button connectButton;
    [SerializeField] private TMP_Text connectButtonText;
    [SerializeField] private TMP_Text setupErrorText;
    [SerializeField] private GameObject createContent;
    [SerializeField] private GameObject joinContent;
    [SerializeField] private GameObject groupFields;
    [SerializeField] private GameObject openFields;
    [SerializeField] private TMP_InputField userIdInput;
    [SerializeField] private TMP_InputField groupNameInput;
    [SerializeField] private TMP_InputField groupUsersInput;
    [SerializeField] private TMP_InputField openNameInput;
    [SerializeField] private TMP_InputField openUrlInput;
    [SerializeField] private TMP_InputField joinUrlInput;

    private ChatChannelMode _selectedMode = ChatChannelMode.Create;
    private ChatChannelKind _selectedKind = ChatChannelKind.Open;
    private bool _isConnecting;
    private bool _isSubscribed;
    private bool _isInitialized;

    public static ChannelSetupUI Ensure(ChatManager manager, RectTransform dashboardRect)
    {
        if (manager == null || dashboardRect == null || dashboardRect.parent == null)
            return null;

        var setup = dashboardRect.parent.GetComponentInChildren<ChannelSetupUI>(true);
        if (setup == null)
        {
            Debug.LogWarning("ChannelSetupUI not found in Canvas. Delete the old Canvas and run VyinChat > Setup Dashboard Scene.");
            return null;
        }

        setup.AssignChatManager(manager);
        return setup;
    }

    private void Start()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    public void AssignChatManager(ChatManager manager)
    {
        chatManager = manager;
        if (_isInitialized)
        {
            Subscribe();
            RefreshConnectButton();
        }
    }

    private void Initialize()
    {
        if (_isInitialized)
            return;

        ResolveMissingReferences();
        RegisterButtonListeners();
        Subscribe();
        RefreshView();
        RefreshConnectButton();
        _isInitialized = true;
    }

    private void ResolveMissingReferences()
    {
        if (chatManager == null)
            chatManager = FindAnyObjectByType<ChatManager>();

        createTabButton ??= FindChildButton("CreateTab");
        joinTabButton ??= FindChildButton("JoinTab");
        groupTypeButton ??= FindChildButton("GroupType");
        openTypeButton ??= FindChildButton("OpenType");
        connectButton ??= FindChildButton("ConnectButton");
        connectButtonText ??= connectButton != null ? connectButton.GetComponentInChildren<TMP_Text>(true) : null;
        setupErrorText ??= FindChildText("SetupErrorText");
        createContent ??= FindChild("CreateContent");
        joinContent ??= FindChild("JoinContent");
        groupFields ??= FindChild("GroupFields");
        openFields ??= FindChild("OpenFields");
        userIdInput ??= FindInput("UserIdInput");
        groupNameInput ??= FindInput("GroupNameInput");
        groupUsersInput ??= FindInput("GroupUsersInput");
        openNameInput ??= FindInput("OpenNameInput");
        openUrlInput ??= FindInput("OpenUrlInput");
        joinUrlInput ??= FindInput("JoinUrlInput");
    }

    private void RegisterButtonListeners()
    {
        createTabButton?.onClick.AddListener(() =>
        {
            _selectedMode = ChatChannelMode.Create;
            RefreshView();
        });
        joinTabButton?.onClick.AddListener(() =>
        {
            _selectedMode = ChatChannelMode.Join;
            RefreshView();
        });
        groupTypeButton?.onClick.AddListener(() =>
        {
            _selectedKind = ChatChannelKind.Group;
            RefreshView();
        });
        openTypeButton?.onClick.AddListener(() =>
        {
            _selectedKind = ChatChannelKind.Open;
            RefreshView();
        });
        connectButton?.onClick.AddListener(OnConnectClicked);
    }

    private void Subscribe()
    {
        if (_isSubscribed || chatManager == null)
            return;

        chatManager.OnConnectionStateChanged += OnConnectionStateChanged;
        chatManager.OnSetupError += OnSetupError;
        _isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed || chatManager == null)
            return;

        chatManager.OnConnectionStateChanged -= OnConnectionStateChanged;
        chatManager.OnSetupError -= OnSetupError;
        _isSubscribed = false;
    }

    private void OnConnectClicked()
    {
        if (chatManager == null)
            return;

        if (chatManager.IsConnected)
        {
            chatManager.Disconnect();
            return;
        }

        if (_isConnecting)
            return;

        HideSetupError();
        if (chatManager.ConnectAndJoin(BuildRequest()))
            _isConnecting = !chatManager.IsConnected;
        else
            _isConnecting = false;

        RefreshConnectButton();
    }

    private void OnSetupError(string message)
    {
        ShowSetupError(message);
    }

    private void ShowSetupError(string message)
    {
        if (setupErrorText == null)
            return;

        setupErrorText.text = message;
        setupErrorText.gameObject.SetActive(true);
        CancelInvoke(nameof(HideSetupError));
        Invoke(nameof(HideSetupError), 5f);
    }

    private void HideSetupError()
    {
        if (setupErrorText != null)
            setupErrorText.gameObject.SetActive(false);
    }

    private ChatChannelRequest BuildRequest()
    {
        if (_selectedMode == ChatChannelMode.Join)
        {
            return new ChatChannelRequest
            {
                UserId = userIdInput != null ? userIdInput.text : string.Empty,
                Mode = ChatChannelMode.Join,
                ChannelUrl = joinUrlInput != null ? joinUrlInput.text : string.Empty
            };
        }

        if (_selectedKind == ChatChannelKind.Group)
        {
            return new ChatChannelRequest
            {
                UserId = userIdInput != null ? userIdInput.text : string.Empty,
                Mode = ChatChannelMode.Create,
                ChannelKind = ChatChannelKind.Group,
                ChannelName = groupNameInput != null ? groupNameInput.text : string.Empty,
                UserIds = ParseUserIds(groupUsersInput != null ? groupUsersInput.text : string.Empty)
            };
        }

        return new ChatChannelRequest
        {
            UserId = userIdInput != null ? userIdInput.text : string.Empty,
            Mode = ChatChannelMode.Create,
            ChannelKind = ChatChannelKind.Open,
            ChannelName = openNameInput != null ? openNameInput.text : string.Empty,
            ChannelUrl = openUrlInput != null ? openUrlInput.text : string.Empty
        };
    }

    private static List<string> ParseUserIds(string text)
    {
        var users = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
            return users;

        var parts = text.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (!string.IsNullOrEmpty(trimmed) && !users.Contains(trimmed))
                users.Add(trimmed);
        }

        return users;
    }

    private void OnConnectionStateChanged(string state)
    {
        if (state == "Disconnected")
            _isConnecting = false;
        else if (state.StartsWith("Connected as", StringComparison.Ordinal))
            _isConnecting = false;
        else if (state == "Connecting...")
            _isConnecting = true;

        RefreshConnectButton();
    }

    private void RefreshView()
    {
        var createSelected = _selectedMode == ChatChannelMode.Create;
        var groupSelected = _selectedKind == ChatChannelKind.Group;

        SetActive(createContent, createSelected);
        SetActive(joinContent, !createSelected);
        SetActive(groupFields, createSelected && groupSelected);
        SetActive(openFields, createSelected && !groupSelected);

        SetSelected(createTabButton, createSelected);
        SetSelected(joinTabButton, !createSelected);
        SetSelected(groupTypeButton, groupSelected);
        SetSelected(openTypeButton, !groupSelected);
    }

    private void RefreshConnectButton()
    {
        if (connectButton == null || connectButtonText == null || chatManager == null)
            return;

        if (chatManager.IsConnected)
        {
            connectButton.interactable = true;
            connectButtonText.text = "Disconnect";
            connectButton.image.color = new Color(0.75f, 0.22f, 0.22f);
            SetInputControlsEnabled(false);
            return;
        }

        connectButton.interactable = !_isConnecting;
        connectButtonText.text = _isConnecting ? "Connecting..." : "Connect";
        connectButton.image.color = _isConnecting ? DisabledColor : SelectedColor;
        SetInputControlsEnabled(!_isConnecting);
    }

    private void SetInputControlsEnabled(bool enabled)
    {
        SetButtonEnabled(createTabButton, enabled);
        SetButtonEnabled(joinTabButton, enabled);
        SetButtonEnabled(groupTypeButton, enabled);
        SetButtonEnabled(openTypeButton, enabled);
        SetFieldEnabled(userIdInput, enabled);
        SetFieldEnabled(groupNameInput, enabled);
        SetFieldEnabled(groupUsersInput, enabled);
        SetFieldEnabled(openNameInput, enabled);
        SetFieldEnabled(openUrlInput, enabled);
        SetFieldEnabled(joinUrlInput, enabled);
    }

    private GameObject FindChild(string childName)
    {
        var children = GetComponentsInChildren<Transform>(true);
        foreach (var child in children)
        {
            if (child.name == childName)
                return child.gameObject;
        }

        return null;
    }

    private Button FindChildButton(string childName)
    {
        var child = FindChild(childName);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private TMP_InputField FindInput(string childName)
    {
        var child = FindChild(childName);
        return child != null ? child.GetComponent<TMP_InputField>() : null;
    }

    private TMP_Text FindChildText(string childName)
    {
        var child = FindChild(childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go != null)
            go.SetActive(active);
    }

    private static void SetFieldEnabled(TMP_InputField field, bool enabled)
    {
        if (field != null)
            field.interactable = enabled;
    }

    private static void SetButtonEnabled(Button button, bool enabled)
    {
        if (button != null)
            button.interactable = enabled;
    }

    private static void SetSelected(Button button, bool selected)
    {
        if (button?.image != null)
            button.image.color = selected ? SelectedColor : ButtonColor;
    }
}
