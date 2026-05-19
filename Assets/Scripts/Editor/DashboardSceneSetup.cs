using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DashboardSceneSetup : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("VyinChat/Setup Dashboard Scene")]
    public static void SetupScene()
    {
        var canvas = CreateCanvas();
        EnsureEventSystem();
        var messagePrefab = CreateMessagePrefab();
        CreateCanvasBackground(canvas.transform);
        var dashboard = CreateDashboardPanel(canvas.transform, messagePrefab);
        CreateChannelSetupPanel(canvas.transform, dashboard.GetComponent<ChatManager>());
        CreatePanelDivider(canvas.transform);

        Debug.Log("VyinChat Dashboard scene setup complete.");
        Selection.activeGameObject = dashboard;
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;

        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<InputSystemUIInputModule>();
    }

    private static Canvas CreateCanvas()
    {
        var existing = FindAnyObjectByType<Canvas>();
        if (existing != null) return existing;

        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void CreateCanvasBackground(Transform parent)
    {
        var background = new GameObject("AppBackground");
        background.transform.SetParent(parent, false);
        var backgroundRect = background.AddComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        background.AddComponent<Image>().color = new Color(0.76f, 0.8f, 0.84f);
        background.transform.SetAsFirstSibling();

        var accent = new GameObject("BackgroundAccent");
        accent.transform.SetParent(parent, false);
        var accentRect = accent.AddComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0.24f, 1f);
        accentRect.offsetMin = Vector2.zero;
        accentRect.offsetMax = Vector2.zero;
        accent.AddComponent<Image>().color = new Color(0.62f, 0.78f, 0.76f, 0.45f);
        accent.transform.SetSiblingIndex(1);
    }

    private static GameObject CreateDashboardPanel(Transform parent, GameObject messagePrefab)
    {
        var dashboard = CreatePanel("ChatDashboard", parent);
        var dashboardRect = dashboard.GetComponent<RectTransform>();
        dashboardRect.anchorMin = new Vector2(0.63f, 0f);
        dashboardRect.anchorMax = Vector2.one;
        dashboardRect.offsetMin = Vector2.zero;
        dashboardRect.offsetMax = Vector2.zero;

        var vlg = dashboard.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 4;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        // Status: User ID line
        var userStatusGo = CreateTextElement("UserStatusText", dashboard.transform, "User: ---");
        var userStatusLayout = userStatusGo.AddComponent<LayoutElement>();
        userStatusLayout.preferredHeight = 16;
        var userTmp = userStatusGo.GetComponent<TextMeshProUGUI>();
        userTmp.fontSize = 11;
        userTmp.color = new Color(0.7f, 0.7f, 0.7f);

        // Status: Channel Name line
        var channelNameGo = CreateTextElement("ChannelNameText", dashboard.transform, "Channel: ---");
        var channelNameLayout = channelNameGo.AddComponent<LayoutElement>();
        channelNameLayout.preferredHeight = 16;
        var channelNameTmp = channelNameGo.GetComponent<TextMeshProUGUI>();
        channelNameTmp.fontSize = 11;
        channelNameTmp.color = new Color(0.7f, 0.7f, 0.7f);

        // Status: Channel URL line
        var channelUrlRow = CreateHorizontalGroup("ChannelUrlRow", dashboard.transform, 6, 16, false);
        var channelUrlRowLayout = channelUrlRow.GetComponent<LayoutElement>();
        channelUrlRowLayout.minHeight = 16;
        channelUrlRowLayout.preferredHeight = -1;
        var channelUrlRowGroup = channelUrlRow.GetComponent<HorizontalLayoutGroup>();
        channelUrlRowGroup.childAlignment = TextAnchor.UpperLeft;
        channelUrlRowGroup.childForceExpandHeight = false;
        var channelUrlGo = CreateTextElement("ChannelUrlText", channelUrlRow.transform, "URL: ---");
        var channelUrlLayout = channelUrlGo.AddComponent<LayoutElement>();
        channelUrlLayout.minHeight = 16;
        channelUrlLayout.flexibleWidth = 1;
        var channelUrlTmp = channelUrlGo.GetComponent<TextMeshProUGUI>();
        channelUrlTmp.fontSize = 11;
        channelUrlTmp.color = new Color(0.7f, 0.7f, 0.7f);
        channelUrlTmp.textWrappingMode = TextWrappingModes.Normal;
        channelUrlTmp.overflowMode = TextOverflowModes.Overflow;
        channelUrlTmp.verticalAlignment = VerticalAlignmentOptions.Top;
        var channelUrlCopyButton = CreateButton("CopyChannelUrlButton", channelUrlRow.transform, "Copy");
        var channelUrlCopyLayout = channelUrlCopyButton.gameObject.AddComponent<LayoutElement>();
        channelUrlCopyLayout.preferredWidth = 54;
        channelUrlCopyLayout.minWidth = 54;
        channelUrlCopyLayout.preferredHeight = 16;
        channelUrlCopyLayout.minHeight = 16;
        channelUrlCopyLayout.flexibleWidth = 0;
        channelUrlCopyLayout.flexibleHeight = 0;
        channelUrlCopyButton.interactable = false;
        channelUrlCopyButton.GetComponentInChildren<TMP_Text>().fontSize = 10;

        // Message scroll area
        var scrollGo = CreateScrollArea(dashboard.transform);

        // Input area
        var inputArea = CreateInputArea(dashboard.transform);

        // Error display
        var errorGo = CreateTextElement("ErrorText", dashboard.transform, "");
        var errorLayout = errorGo.AddComponent<LayoutElement>();
        errorLayout.preferredHeight = 16;
        var errorTmp = errorGo.GetComponent<TextMeshProUGUI>();
        errorTmp.fontSize = 11;
        errorTmp.color = new Color(1f, 0.4f, 0.4f);
        errorGo.SetActive(false);

        // Add ChatManager and ChatUI
        var chatManager = dashboard.AddComponent<ChatManager>();
        var chatUI = dashboard.AddComponent<ChatUI>();

        // Wire references via SerializedObject
        var so = new SerializedObject(chatUI);
        so.FindProperty("chatManager").objectReferenceValue = chatManager;
        so.FindProperty("messageScrollRect").objectReferenceValue = scrollGo.GetComponent<ScrollRect>();
        so.FindProperty("messageContainer").objectReferenceValue = scrollGo.transform.Find("Viewport/Content");
        so.FindProperty("inputField").objectReferenceValue = inputArea.GetComponentInChildren<TMP_InputField>();
        so.FindProperty("sendButton").objectReferenceValue = inputArea.GetComponentInChildren<Button>();
        so.FindProperty("userStatusText").objectReferenceValue = userStatusGo.GetComponent<TMP_Text>();
        so.FindProperty("channelNameText").objectReferenceValue = channelNameGo.GetComponent<TMP_Text>();
        so.FindProperty("channelUrlText").objectReferenceValue = channelUrlGo.GetComponent<TMP_Text>();
        so.FindProperty("channelUrlCopyButton").objectReferenceValue = channelUrlCopyButton;
        so.FindProperty("errorText").objectReferenceValue = errorTmp;
        so.FindProperty("messagePrefab").objectReferenceValue = messagePrefab;
        so.ApplyModifiedProperties();

        return dashboard;
    }

    private static GameObject CreateChannelSetupPanel(Transform parent, ChatManager chatManager)
    {
        var panel = CreatePanel("ChannelSetupPanel", parent);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.25f, 0f);
        panelRect.anchorMax = new Vector2(0.57f, 1f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 8;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        var title = CreateTextElement("Title", panel.transform, "Setup Panel");
        title.GetComponent<TextMeshProUGUI>().fontSize = 16;
        title.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        AddLayout(title, 24);

        var userIdInput = CreateLabeledInput("UserIdInput", panel.transform, "User Id");

        var tabRow = CreateHorizontalGroup("Tabs", panel.transform, 4, 32);
        var createTabButton = CreateButton("CreateTab", tabRow.transform, "Create Channel");
        var joinTabButton = CreateButton("JoinTab", tabRow.transform, "Join Channel");

        var createContent = CreateVerticalGroup("CreateContent", panel.transform, 8);
        var typeRow = CreateHorizontalGroup("ChannelType", createContent.transform, 4, 32);
        var groupTypeButton = CreateButton("GroupType", typeRow.transform, "Group Channel");
        var openTypeButton = CreateButton("OpenType", typeRow.transform, "Open Channel");

        var groupFields = CreateVerticalGroup("GroupFields", createContent.transform, 6);
        var groupNameInput = CreateLabeledInput("GroupNameInput", groupFields.transform, "Channel Name");
        var groupUsersInput = CreateLabeledInput("GroupUsersInput", groupFields.transform, "Users");

        var openFields = CreateVerticalGroup("OpenFields", createContent.transform, 6);
        var openNameInput = CreateLabeledInput("OpenNameInput", openFields.transform, "Channel Name");
        var openUrlInput = CreateLabeledInput("OpenUrlInput", openFields.transform, "Channel Url");

        var joinContent = CreateVerticalGroup("JoinContent", panel.transform, 6);
        var joinUrlInput = CreateLabeledInput("JoinUrlInput", joinContent.transform, "Channel Url");

        var setupErrorGo = CreateTextElement("SetupErrorText", panel.transform, "");
        var setupErrorLayout = setupErrorGo.AddComponent<LayoutElement>();
        setupErrorLayout.preferredHeight = 32;
        var setupErrorText = setupErrorGo.GetComponent<TextMeshProUGUI>();
        setupErrorText.fontSize = 11;
        setupErrorText.color = new Color(1f, 0.4f, 0.4f);
        setupErrorGo.SetActive(false);

        var spacer = new GameObject("Spacer");
        spacer.transform.SetParent(panel.transform, false);
        spacer.AddComponent<RectTransform>();
        spacer.AddComponent<LayoutElement>().flexibleHeight = 1;

        var connectButton = CreateButton("ConnectButton", panel.transform, "Connect");
        AddLayout(connectButton.gameObject, 36);

        groupFields.SetActive(false);
        joinContent.SetActive(false);
        SetButtonColor(createTabButton, new Color(0.2f, 0.55f, 0.95f));
        SetButtonColor(openTypeButton, new Color(0.2f, 0.55f, 0.95f));
        SetButtonColor(connectButton, new Color(0.2f, 0.55f, 0.95f));

        var setupUI = panel.AddComponent<ChannelSetupUI>();
        var so = new SerializedObject(setupUI);
        so.FindProperty("chatManager").objectReferenceValue = chatManager;
        so.FindProperty("createTabButton").objectReferenceValue = createTabButton;
        so.FindProperty("joinTabButton").objectReferenceValue = joinTabButton;
        so.FindProperty("groupTypeButton").objectReferenceValue = groupTypeButton;
        so.FindProperty("openTypeButton").objectReferenceValue = openTypeButton;
        so.FindProperty("connectButton").objectReferenceValue = connectButton;
        so.FindProperty("connectButtonText").objectReferenceValue = connectButton.GetComponentInChildren<TMP_Text>();
        so.FindProperty("setupErrorText").objectReferenceValue = setupErrorText;
        so.FindProperty("createContent").objectReferenceValue = createContent;
        so.FindProperty("joinContent").objectReferenceValue = joinContent;
        so.FindProperty("groupFields").objectReferenceValue = groupFields;
        so.FindProperty("openFields").objectReferenceValue = openFields;
        so.FindProperty("userIdInput").objectReferenceValue = userIdInput;
        so.FindProperty("groupNameInput").objectReferenceValue = groupNameInput;
        so.FindProperty("groupUsersInput").objectReferenceValue = groupUsersInput;
        so.FindProperty("openNameInput").objectReferenceValue = openNameInput;
        so.FindProperty("openUrlInput").objectReferenceValue = openUrlInput;
        so.FindProperty("joinUrlInput").objectReferenceValue = joinUrlInput;
        so.ApplyModifiedProperties();

        return panel;
    }

    private static GameObject CreatePanelDivider(Transform parent)
    {
        var divider = new GameObject("SetupChatDivider");
        divider.transform.SetParent(parent, false);
        var dividerRect = divider.AddComponent<RectTransform>();
        dividerRect.anchorMin = new Vector2(0.595f, 0.04f);
        dividerRect.anchorMax = new Vector2(0.605f, 0.96f);
        dividerRect.offsetMin = Vector2.zero;
        dividerRect.offsetMax = Vector2.zero;
        divider.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.07f, 0.9f);
        return divider;
    }

    private static GameObject CreateScrollArea(Transform parent)
    {
        var scrollGo = new GameObject("MessageScroll");
        scrollGo.transform.SetParent(parent, false);
        var scrollRect = scrollGo.AddComponent<RectTransform>();
        var layout = scrollGo.AddComponent<LayoutElement>();
        layout.flexibleHeight = 1;
        var sr = scrollGo.AddComponent<ScrollRect>();
        sr.horizontal = false;
        scrollGo.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f);

        // Viewport
        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGo.transform, false);
        var vpRect = viewport.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        viewport.AddComponent<Image>();

        // Content
        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = Vector2.one;
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        var contentVlg = content.AddComponent<VerticalLayoutGroup>();
        contentVlg.spacing = 2;
        contentVlg.padding = new RectOffset(2, 2, 2, 2);
        contentVlg.childForceExpandWidth = true;
        contentVlg.childForceExpandHeight = false;
        contentVlg.childControlWidth = true;
        contentVlg.childControlHeight = true;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.viewport = vpRect;
        sr.content = contentRect;

        return scrollGo;
    }

    private static GameObject CreateInputArea(Transform parent)
    {
        var inputArea = new GameObject("InputArea");
        inputArea.transform.SetParent(parent, false);
        inputArea.AddComponent<RectTransform>();
        var layout = inputArea.AddComponent<LayoutElement>();
        layout.preferredHeight = 30;
        layout.flexibleHeight = 0;
        var hlg = inputArea.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4;
        hlg.padding = new RectOffset(0, 0, 0, 0);
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        // Input field container
        var inputGo = new GameObject("InputField");
        inputGo.transform.SetParent(inputArea.transform, false);
        inputGo.AddComponent<RectTransform>();
        var inputLayout = inputGo.AddComponent<LayoutElement>();
        inputLayout.flexibleWidth = 1;
        inputLayout.minWidth = 80;
        inputGo.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f);

        // Text Area (viewport for the input text)
        var textArea = new GameObject("Text Area");
        textArea.transform.SetParent(inputGo.transform, false);
        var taRect = textArea.AddComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero;
        taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(8, 2);
        taRect.offsetMax = new Vector2(-8, -2);

        // Placeholder text (child of Text Area)
        var placeholderGo = new GameObject("Placeholder");
        placeholderGo.transform.SetParent(textArea.transform, false);
        var phRect = placeholderGo.AddComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = Vector2.zero;
        phRect.offsetMax = Vector2.zero;
        var phText = placeholderGo.AddComponent<TextMeshProUGUI>();
        phText.text = "Type a message...";
        phText.fontSize = 13;
        phText.fontStyle = FontStyles.Italic;
        phText.color = new Color(0.5f, 0.5f, 0.5f);
        phText.verticalAlignment = VerticalAlignmentOptions.Middle;

        // Input text (child of Text Area)
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(textArea.transform, false);
        var txtRect = textGo.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;
        var inputTmp = textGo.AddComponent<TextMeshProUGUI>();
        inputTmp.text = "";
        inputTmp.fontSize = 13;
        inputTmp.color = Color.white;
        inputTmp.verticalAlignment = VerticalAlignmentOptions.Middle;

        // Configure TMP_InputField
        var inputField = inputGo.AddComponent<TMP_InputField>();
        inputField.textViewport = taRect;
        inputField.textComponent = inputTmp;
        inputField.placeholder = phText;
        inputField.fontAsset = inputTmp.font;

        // Send button — compact
        var btnGo = new GameObject("SendButton");
        btnGo.transform.SetParent(inputArea.transform, false);
        btnGo.AddComponent<RectTransform>();
        var btnLayout = btnGo.AddComponent<LayoutElement>();
        btnLayout.preferredWidth = 40;
        btnLayout.minWidth = 40;
        btnLayout.flexibleWidth = 0;
        btnGo.AddComponent<Image>().color = new Color(0.2f, 0.6f, 1f);
        btnGo.AddComponent<Button>();
        var btnLabel = CreateTextChild("Label", btnGo.transform, "Send", Color.white);
        btnLabel.alignment = TextAlignmentOptions.Center;
        btnLabel.fontSize = 12;

        return inputArea;
    }

    private static GameObject CreateVerticalGroup(string name, Transform parent, int spacing)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return go;
    }

    private static GameObject CreateHorizontalGroup(string name, Transform parent, int spacing, float preferredHeight, bool forceExpandWidth = true)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        AddLayout(go, preferredHeight);
        var layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = forceExpandWidth;
        layout.childForceExpandHeight = true;
        return go;
    }

    private static TMP_InputField CreateLabeledInput(string inputName, Transform parent, string label)
    {
        var wrapper = CreateVerticalGroup($"{inputName}Field", parent, 2);
        AddLayout(wrapper, 58);

        var labelGo = CreateTextElement("Label", wrapper.transform, label);
        var labelText = labelGo.GetComponent<TextMeshProUGUI>();
        labelText.fontSize = 11;
        labelText.color = new Color(0.72f, 0.72f, 0.76f);
        AddLayout(labelGo, 16);

        var inputGo = new GameObject(inputName);
        inputGo.transform.SetParent(wrapper.transform, false);
        inputGo.AddComponent<RectTransform>();
        AddLayout(inputGo, 34);
        inputGo.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f);

        var textArea = new GameObject("Text Area");
        textArea.transform.SetParent(inputGo.transform, false);
        var textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(8, 2);
        textAreaRect.offsetMax = new Vector2(-8, -2);

        var placeholder = CreateTextChild("Placeholder", textArea.transform, "", new Color(0.45f, 0.45f, 0.48f));
        placeholder.fontSize = 13;
        placeholder.fontStyle = FontStyles.Italic;
        placeholder.verticalAlignment = VerticalAlignmentOptions.Middle;

        var inputText = CreateTextChild("Text", textArea.transform, "", Color.white);
        inputText.fontSize = 13;
        inputText.verticalAlignment = VerticalAlignmentOptions.Middle;

        var inputField = inputGo.AddComponent<TMP_InputField>();
        inputField.textViewport = textAreaRect;
        inputField.textComponent = inputText;
        inputField.placeholder = placeholder;
        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.fontAsset = inputText.font;
        return inputField;
    }

    private static Button CreateButton(string name, Transform parent, string text)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var image = go.AddComponent<Image>();
        image.color = new Color(0.18f, 0.18f, 0.22f);
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;

        var label = CreateTextChild("Label", go.transform, text, Color.white);
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 12;
        return button;
    }

    private static LayoutElement AddLayout(GameObject go, float preferredHeight)
    {
        var layout = go.GetComponent<LayoutElement>();
        if (layout == null)
            layout = go.AddComponent<LayoutElement>();

        layout.preferredHeight = preferredHeight;
        return layout;
    }

    private static void SetButtonColor(Button button, Color color)
    {
        if (button != null && button.image != null)
            button.image.color = color;
    }

    private static GameObject CreateTextElement(string name, Transform parent, string text)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 14;
        tmp.color = Color.white;
        return go;
    }

    private static TMP_Text CreateTextChild(string name, Transform parent, string text, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 14;
        tmp.color = color;
        return tmp;
    }

    private static GameObject CreatePanel(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.15f);
        return go;
    }

    private static GameObject CreateMessagePrefab()
    {
        var prefabPath = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabPath))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        var go = new GameObject("ChatMessageItem");
        var rootRect = go.AddComponent<RectTransform>();

        var rootLayout = go.AddComponent<LayoutElement>();
        rootLayout.minHeight = 40;

        // Content text — stretches to full width of parent
        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(go.transform, false);
        var contentRect = contentGo.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(2, 0);
        contentRect.offsetMax = new Vector2(-2, 0);
        var contentText = contentGo.AddComponent<TextMeshProUGUI>();
        contentText.fontSize = 14;
        contentText.color = Color.white;
        contentText.richText = true;
        contentText.textWrappingMode = TextWrappingModes.Normal;
        contentText.overflowMode = TextOverflowModes.Overflow;
        contentText.verticalAlignment = VerticalAlignmentOptions.Top;

        // ContentSizeFitter on root to auto-height
        var csf = go.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Add ChatMessageItem component and wire
        var item = go.AddComponent<ChatMessageItem>();
        var so = new SerializedObject(item);
        so.FindProperty("contentText").objectReferenceValue = contentText;
        so.ApplyModifiedProperties();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{prefabPath}/ChatMessageItem.prefab");
        DestroyImmediate(go);

        Debug.Log("Created prefab: Assets/Prefabs/ChatMessageItem.prefab");
        return prefab;
    }
#endif
}
