using System;
using System.Collections.Generic;
using UnityEngine;
using Gamania.GIMChat;

public class ChatManager : MonoBehaviour
{
    [Header("Connection")]
    [SerializeField] private string appId = "YOUR_APP_ID";
    [SerializeField] private string userId = "test-user-01";
    [SerializeField] private string authToken = "YOUR_AUTH_TOKEN";
    [SerializeField] private string channelName = "General Room";
    [SerializeField] private string channelUrl = "general-room";

    public string UserId => userId;
    public bool IsConnected { get; private set; }
    public bool IsChannelReady => _currentChannel != null;

    public event Action<string> OnConnectionStateChanged;
    public event Action<string, string> OnChannelJoined;
    public event Action<string, string, long> OnMessageReceived;
    public event Action<string, long> OnMessageSent;
    public event Action<string, long, string> OnMessageSentWithId;
    public event Action<string> OnError;

    private GimOpenChannel _currentChannel;

    void Start()
    {
        GIMChat.Init(new GimInitParams(appId));
        GIMChat.SetBackgroundDisconnectionConfig(GimBackgroundDisconnectionConfig.IgnoreLifecycle);
        RegisterConnectionHandler();
        RegisterMessageHandler();
        OnConnectionStateChanged?.Invoke("Connecting...");
        Connect();
    }

    void OnDestroy()
    {
        GIMChat.RemoveConnectionHandler("connection-handler");
        GimOpenChannel.RemoveOpenChannelHandler("chat-handler");
    }

    private void RegisterConnectionHandler()
    {
        GIMChat.AddConnectionHandler("connection-handler", new GimConnectionHandler
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
                OnConnectionStateChanged?.Invoke($"Connected as {userId}");
                JoinChannel();
            },
            OnReconnectFailed = () =>
            {
                Debug.Log("[ChatManager] Reconnect failed, retrying...");
                IsConnected = false;
                OnConnectionStateChanged?.Invoke("Disconnected");
                Invoke(nameof(Reconnect), 3f);
            }
        });
    }

    private void Reconnect()
    {
        if (GIMChat.GetConnectionState().IsConnected())
        {
            JoinChannel();
            return;
        }

        OnConnectionStateChanged?.Invoke("Connecting...");
        Connect();
    }

    private void Connect()
    {
        GIMChat.Connect(userId, authToken, (user, error) =>
        {
            if (error != null)
            {
                IsConnected = false;
                OnConnectionStateChanged?.Invoke("Disconnected");
                OnError?.Invoke($"Connection failed: {error}");
                Invoke(nameof(Reconnect), 5f);
                return;
            }

            IsConnected = true;
            OnConnectionStateChanged?.Invoke($"Connected as {user.UserId}");
            JoinChannel();
        });
    }

    private void RegisterMessageHandler()
    {
        GimOpenChannel.AddOpenChannelHandler("chat-handler", new GimOpenChannelHandler
        {
            OnMessageReceivedAction = (channel, message) =>
            {
                Debug.Log($"[ChatManager] Message received: sender={message.Sender?.UserId}, text={message.Message}");
                if (_currentChannel == null || channel.ChannelUrl != _currentChannel.ChannelUrl)
                    return;
                OnMessageReceived?.Invoke(message.Message, message.Sender?.UserId ?? "unknown", message.MessageId);
            }
        });
    }

    private void JoinChannel()
    {
        GimOpenChannel.GetChannel(channelUrl, (channel, error) =>
        {
            if (error != null)
            {
                CreateChannel();
                return;
            }

            EnterChannel(channel);
        });
    }

    private void CreateChannel()
    {
        var createParams = new GimOpenChannelCreateParams
        {
            Name = channelName,
            ChannelUrl = channelUrl,
            OperatorUserIds = new List<string> { userId }
        };

        GimOpenChannel.CreateChannel(createParams, (channel, error) =>
        {
            if (error != null)
            {
                OnError?.Invoke($"Create channel failed: {error}");
                return;
            }

            EnterChannel(channel);
        });
    }

    private void EnterChannel(GimOpenChannel channel)
    {
        channel.Enter(error =>
        {
            if (error != null)
            {
                Debug.LogWarning($"[ChatManager] Enter failed: {error}");
                OnError?.Invoke($"Enter channel failed: {error}");
                return;
            }

            Debug.Log($"[ChatManager] Entered channel: {channel.ChannelUrl}");
            _currentChannel = channel;
            OnChannelJoined?.Invoke(channelName, channel.ChannelUrl);
        });
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
}
