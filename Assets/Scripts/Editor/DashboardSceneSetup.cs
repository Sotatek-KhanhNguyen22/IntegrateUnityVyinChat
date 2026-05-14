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
        var dashboard = CreateDashboardPanel(canvas.transform);
        CreateMessagePrefab();

        Debug.Log("VyinChat Dashboard scene setup complete. Assign the MessagePrefab to ChatUI.");
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

    private static GameObject CreateDashboardPanel(Transform parent)
    {
        var dashboard = CreatePanel("ChatDashboard", parent);
        var dashboardRect = dashboard.GetComponent<RectTransform>();
        dashboardRect.anchorMin = new Vector2(0.6f, 0f);
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
        var channelUrlGo = CreateTextElement("ChannelUrlText", dashboard.transform, "URL: ---");
        var channelUrlLayout = channelUrlGo.AddComponent<LayoutElement>();
        channelUrlLayout.preferredHeight = 16;
        var channelUrlTmp = channelUrlGo.GetComponent<TextMeshProUGUI>();
        channelUrlTmp.fontSize = 11;
        channelUrlTmp.color = new Color(0.7f, 0.7f, 0.7f);

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
        so.FindProperty("errorText").objectReferenceValue = errorTmp;
        so.ApplyModifiedProperties();

        return dashboard;
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

    private static void CreateMessagePrefab()
    {
        var prefabPath = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabPath))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        var go = new GameObject("ChatMessageItem");
        var rootRect = go.AddComponent<RectTransform>();

        var rootLayout = go.AddComponent<LayoutElement>();
        rootLayout.minHeight = 20;

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
        contentText.enableWordWrapping = true;
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

        PrefabUtility.SaveAsPrefabAsset(go, $"{prefabPath}/ChatMessageItem.prefab");
        DestroyImmediate(go);

        Debug.Log("Created prefab: Assets/Prefabs/ChatMessageItem.prefab");
    }
#endif
}

