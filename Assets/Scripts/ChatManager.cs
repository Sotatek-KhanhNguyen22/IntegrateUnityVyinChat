using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Gamania.GIMChat;

public enum ChatChannelKind
{
    Group,
    Open
}

public enum ChatChannelMode
{
    Create,
    Join
}

public class ChatChannelRequest
{
    public string UserId;
    public ChatChannelMode Mode;
    public ChatChannelKind ChannelKind;
    public string ChannelName;
    public string ChannelUrl;
    public List<string> UserIds = new();
}

public class ChatMessageRecord
{
    public string Message;
    public string SenderId;
    public long MessageId;
}

public class ChatManager : MonoBehaviour
{
    [Header("Connection")]
    [SerializeField] private string appId = "YOUR_APP_ID";
    [SerializeField] private string authToken = "YOUR_AUTH_TOKEN";

    public string UserId => _userId;
    public bool IsConnected { get; private set; }
    public bool IsChannelReady => _currentChannel != null;

    public event Action<string> OnConnectionStateChanged;
    public event Action<string, string> OnChannelJoined;
    public event Action<IReadOnlyList<ChatMessageRecord>> OnMessageHistoryLoaded;
    public event Action<string, string, long> OnMessageReceived;
    public event Action<string, long> OnMessageSent;
    public event Action<string, long, string> OnMessageSentWithId;
    public event Action<string> OnError;
    public event Action<string> OnSetupError;

    private const string ConnectionHandlerId = "connection-handler";
    private const string OpenChannelHandlerId = "open-channel-handler";
    private const string GroupChannelHandlerId = "group-channel-handler";

    private GimBaseChannel _currentChannel;
    private GimOpenChannel _currentOpenChannel;
    private ChatChannelKind? _currentChannelKind;
    private ChatChannelRequest _pendingChannelRequest;
    private ChatChannelRequest _activeChannelRequest;
    private string _userId = string.Empty;
    private bool _shouldReconnect;
    private bool _isDisconnecting;

    void Start()
    {
        EnsureSdkReady();
        OnConnectionStateChanged?.Invoke("Disconnected");
    }

    void OnDestroy()
    {
        CancelInvoke(nameof(Reconnect));
        UnregisterHandlers();
    }

    public bool ConnectAndJoin(ChatChannelRequest request)
    {
        if (!ValidateRequest(request))
            return false;

        _userId = request.UserId.Trim();
        _pendingChannelRequest = request;
        _activeChannelRequest = request;
        _shouldReconnect = true;
        _isDisconnecting = false;

        if (GIMChat.GetConnectionState().IsConnected())
        {
            IsConnected = true;
            JoinFromRequest(request);
            return true;
        }

        EnsureSdkReady();
        OnConnectionStateChanged?.Invoke("Connecting...");
        Connect();
        return true;
    }

    public void Disconnect()
    {
        _shouldReconnect = false;
        _isDisconnecting = true;
        CancelInvoke(nameof(Reconnect));

        if (_currentOpenChannel != null && _currentOpenChannel.IsEntered)
            _currentOpenChannel.Exit(_ => { });

        _currentChannel = null;
        _currentOpenChannel = null;
        _currentChannelKind = null;
        _pendingChannelRequest = null;
        _activeChannelRequest = null;
        _userId = string.Empty;
        IsConnected = false;

        UnregisterHandlers();
        DisconnectSdk();
        _isDisconnecting = false;
        OnConnectionStateChanged?.Invoke("Disconnected");
    }

    private void EnsureSdkReady()
    {
        if (!GIMChat.IsInitialized)
        {
            GIMChat.Init(new GimInitParams(appId));
            GIMChat.SetBackgroundDisconnectionConfig(GimBackgroundDisconnectionConfig.IgnoreLifecycle);
        }

        RegisterConnectionHandler();
        RegisterMessageHandlers();
    }

    private void RegisterConnectionHandler()
    {
        GIMChat.RemoveConnectionHandler(ConnectionHandlerId);
        GIMChat.AddConnectionHandler(ConnectionHandlerId, new GimConnectionHandler
        {
            OnConnected = uid =>
            {
                Debug.Log($"[ChatManager] OnConnected: {uid}");
                IsConnected = true;
            },
            OnDisconnected = uid =>
            {
                Debug.Log($"[ChatManager] OnDisconnected: {uid}");
                IsConnected = false;
                _currentChannel = null;
                _currentOpenChannel = null;
                _currentChannelKind = null;
                OnConnectionStateChanged?.Invoke("Disconnected");
            },
            OnReconnectStarted = () =>
            {
                Debug.Log("[ChatManager] Reconnecting...");
                OnConnectionStateChanged?.Invoke("Reconnecting...");
            },
            OnReconnectSucceeded = () =>
            {
                Debug.Log("[ChatManager] Reconnect succeeded, re-entering channel...");
                IsConnected = true;
                OnConnectionStateChanged?.Invoke($"Connected as {_userId}");
                if (_activeChannelRequest != null)
                    JoinFromRequest(_activeChannelRequest);
            },
            OnReconnectFailed = () =>
            {
                Debug.Log("[ChatManager] Reconnect failed, retrying...");
                IsConnected = false;
                OnConnectionStateChanged?.Invoke("Disconnected");
                if (_shouldReconnect && !_isDisconnecting)
                    Invoke(nameof(Reconnect), 3f);
            }
        });
    }

    private void RegisterMessageHandlers()
    {
        GimOpenChannel.RemoveOpenChannelHandler(OpenChannelHandlerId);
        GimOpenChannel.AddOpenChannelHandler(OpenChannelHandlerId, new GimOpenChannelHandler
        {
            OnMessageReceivedAction = (channel, message) =>
            {
                if (!IsCurrentChannel(channel.ChannelUrl, ChatChannelKind.Open))
                    return;

                PublishIncomingMessage(message);
            }
        });

        if (GimGroupChannel.GetGroupChannelHandler(GroupChannelHandlerId) != null)
            GimGroupChannel.RemoveGroupChannelHandler(GroupChannelHandlerId);

        GimGroupChannel.AddGroupChannelHandler(GroupChannelHandlerId, new GimGroupChannelHandler
        {
            OnMessageReceived = (channel, message) =>
            {
                if (!IsCurrentChannel(channel.ChannelUrl, ChatChannelKind.Group))
                    return;

                PublishIncomingMessage(message);
            }
        });
    }

    private void UnregisterHandlers()
    {
        GIMChat.RemoveConnectionHandler(ConnectionHandlerId);
        GimOpenChannel.RemoveOpenChannelHandler(OpenChannelHandlerId);
        if (GimGroupChannel.GetGroupChannelHandler(GroupChannelHandlerId) != null)
            GimGroupChannel.RemoveGroupChannelHandler(GroupChannelHandlerId);
    }

    private void Reconnect()
    {
        if (!_shouldReconnect || _isDisconnecting)
            return;

        EnsureSdkReady();
        if (GIMChat.GetConnectionState().IsConnected())
        {
            if (_activeChannelRequest != null)
                JoinFromRequest(_activeChannelRequest);
            return;
        }

        OnConnectionStateChanged?.Invoke("Connecting...");
        Connect();
    }

    private void Connect()
    {
        var token = string.IsNullOrWhiteSpace(authToken) ? null : authToken;
        GIMChat.Connect(_userId, token, (user, error) =>
        {
            if (error != null)
            {
                IsConnected = false;
                OnConnectionStateChanged?.Invoke("Disconnected");
                FailSetup($"Connection failed: {error.Message}", false);
                return;
            }

            IsConnected = true;
            OnConnectionStateChanged?.Invoke($"Connected as {user.UserId}");
            if (_pendingChannelRequest != null)
                JoinFromRequest(_pendingChannelRequest);
        });
    }

    private void JoinFromRequest(ChatChannelRequest request)
    {
        if (request == null)
        {
            FailSetup("Channel request is missing", false);
            return;
        }

        _currentChannel = null;
        _currentOpenChannel = null;
        _currentChannelKind = null;

        if (request.Mode == ChatChannelMode.Join)
        {
            JoinChannelByUrl(request.ChannelUrl);
            return;
        }

        if (request.ChannelKind == ChatChannelKind.Group)
            CreateGroupChannel(request);
        else
            CreateOpenChannel(request);
    }

    private void CreateGroupChannel(ChatChannelRequest request)
    {
        var createParams = new GimGroupChannelCreateParams
        {
            Name = request.ChannelName.Trim(),
            UserIds = BuildGroupUserIds(request.UserIds),
            OperatorUserIds = new List<string> { _userId },
            IsDistinct = false
        };

        GimGroupChannel.CreateChannel(createParams, (channel, error) =>
        {
            if (error != null)
            {
                FailSetup($"Create group channel failed: {error.Message}", true);
                return;
            }

            SetCurrentGroupChannel(channel);
        });
    }

    private void CreateOpenChannel(ChatChannelRequest request)
    {
        var createParams = new GimOpenChannelCreateParams
        {
            Name = request.ChannelName.Trim(),
            OperatorUserIds = new List<string> { _userId }
        };

        if (!string.IsNullOrWhiteSpace(request.ChannelUrl))
            createParams.ChannelUrl = request.ChannelUrl.Trim();

        GimOpenChannel.CreateChannel(createParams, (channel, error) =>
        {
            if (error != null)
            {
                FailSetup($"Create open channel failed: {error.Message}", true);
                return;
            }

            EnterOpenChannel(channel);
        });
    }

    private async void JoinChannelByUrl(string channelUrl)
    {
        var trimmedUrl = channelUrl.Trim();

        try
        {
            var openChannel = await GimOpenChannel.GetChannelAsync(trimmedUrl);
            EnterOpenChannel(openChannel);
            return;
        }
        catch (Exception)
        {
            // A missing open channel is user input, not a system error. Try group channel next.
        }

        try
        {
            var groupChannel = await GimGroupChannel.GetChannelAsync(trimmedUrl);
            SetCurrentGroupChannel(groupChannel);
            return;
        }
        catch (Exception)
        {
            StopConnectForUserInput($"Channel Url '{trimmedUrl}' was not found. Check the URL and try again.");
        }
    }

    private void StopConnectForUserInput(string message)
    {
        FailSetup(message, true);
    }

    private void EnterOpenChannel(GimOpenChannel channel)
    {
        channel.Enter(error =>
        {
            if (error != null)
            {
                Debug.LogWarning($"[ChatManager] Enter failed: {error}");
                FailSetup($"Enter channel failed: {error.Message}", true);
                return;
            }

            Debug.Log($"[ChatManager] Entered channel: {channel.ChannelUrl}");
            _currentChannel = channel;
            _currentOpenChannel = channel;
            _currentChannelKind = ChatChannelKind.Open;
            OnChannelJoined?.Invoke(channel.Name ?? channel.ChannelUrl, channel.ChannelUrl);
            LoadMessageHistory();
        });
    }

    private void SetCurrentGroupChannel(GimGroupChannel channel)
    {
        Debug.Log($"[ChatManager] Joined group channel: {channel.ChannelUrl}");
        _currentChannel = channel;
        _currentOpenChannel = null;
        _currentChannelKind = ChatChannelKind.Group;
        OnChannelJoined?.Invoke(channel.Name ?? channel.ChannelUrl, channel.ChannelUrl);
        LoadMessageHistory();
    }

    public void SendChatMessage(string text)
    {
        SendChatMessage(text, null);
    }

    public void SendChatMessage(string text, string pendingId)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        if (_currentChannel == null)
        {
            OnError?.Invoke("Channel not ready");
            return;
        }

        var messageParams = new GimUserMessageCreateParams { Message = text };

        _currentChannel.SendUserMessage(messageParams, (message, error) =>
        {
            if (error != null)
            {
                OnError?.Invoke($"Send failed: {error}");
                return;
            }

            OnMessageSent?.Invoke(message.Message, message.MessageId);
            if (pendingId != null)
                OnMessageSentWithId?.Invoke(message.Message, message.MessageId, pendingId);
        });
    }

    private bool ValidateRequest(ChatChannelRequest request)
    {
        if (request == null)
        {
            FailSetup("Channel request is missing", false);
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            FailSetup("User Id is required", false);
            return false;
        }

        if (request.Mode == ChatChannelMode.Join)
        {
            if (string.IsNullOrWhiteSpace(request.ChannelUrl))
            {
                FailSetup("Channel Url is required", false);
                return false;
            }

            return true;
        }

        if (string.IsNullOrWhiteSpace(request.ChannelName))
        {
            FailSetup("Channel Name is required", false);
            return false;
        }

        return true;
    }

    private void FailSetup(string message, bool disconnect)
    {
        OnSetupError?.Invoke(message);
        if (disconnect)
            Disconnect();
    }

    private List<string> BuildGroupUserIds(List<string> requestedUserIds)
    {
        var users = new List<string>();
        AddUniqueUser(users, _userId);

        if (requestedUserIds == null)
            return users;

        foreach (var id in requestedUserIds)
            AddUniqueUser(users, id);

        return users;
    }

    private static void AddUniqueUser(List<string> users, string id)
    {
        var trimmed = id?.Trim();
        if (string.IsNullOrEmpty(trimmed) || users.Contains(trimmed))
            return;

        users.Add(trimmed);
    }

    private bool IsCurrentChannel(string channelUrl, ChatChannelKind kind)
    {
        return _currentChannel != null
            && _currentChannelKind == kind
            && _currentChannel.ChannelUrl == channelUrl;
    }

    private void PublishIncomingMessage(GimBaseMessage message)
    {
        Debug.Log($"[ChatManager] Message received: sender={message.Sender?.UserId}, text={message.Message}");
        OnMessageReceived?.Invoke(message.Message, message.Sender?.UserId ?? "unknown", message.MessageId);
    }

    private async void LoadMessageHistory()
    {
        if (_currentChannel == null)
            return;

        var channel = _currentChannel;
        var channelUrl = channel.ChannelUrl;
        var channelKind = _currentChannelKind;

        try
        {
            var messages = await channel.GetMessagesByTimestampAsync(
                long.MaxValue,
                new GimMessageListParams
                {
                    PreviousResultSize = 100,
                    NextResultSize = 0,
                    Reverse = false
                });

            if (_currentChannel == null
                || _currentChannel.ChannelUrl != channelUrl
                || _currentChannelKind != channelKind)
                return;

            var records = new List<ChatMessageRecord>();
            foreach (var message in messages)
            {
                records.Add(new ChatMessageRecord
                {
                    Message = message.Message,
                    SenderId = message.Sender?.UserId ?? "unknown",
                    MessageId = message.MessageId
                });
            }

            records.Sort((a, b) => a.MessageId.CompareTo(b.MessageId));
            OnMessageHistoryLoaded?.Invoke(records);
        }
        catch (Exception ex)
        {
            if (_currentChannel != null
                && _currentChannel.ChannelUrl == channelUrl
                && _currentChannelKind == channelKind)
                OnError?.Invoke($"Load message history failed: {ex.Message}");
        }
    }

    private void DisconnectSdk()
    {
        try
        {
            var implField = typeof(GIMChat).GetField("_impl", BindingFlags.Static | BindingFlags.NonPublic);
            var impl = implField?.GetValue(null);
            var resetMethod = impl?.GetType().GetMethod("Reset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            resetMethod?.Invoke(impl, null);
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Disconnect failed: {ex.Message}");
        }
    }
}
