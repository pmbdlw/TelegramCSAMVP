using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSAFacade;
using TL;
using WTelegram;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

/// <summary>
/// Telegram Facade for CSA
/// </summary>
// Telegram管理类 - 单例模式
public class TelegramManager
{
    private static readonly Lazy<TelegramManager> _instance =
        new Lazy<TelegramManager>(() => new TelegramManager());

    private readonly ConcurrentDictionary<string, Client> _clients;
    public event EventHandler<LoginCodeRequestEventArgs> OnLoginCodeRequired;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingLoginCodes;
    private readonly ConcurrentDictionary<string, (string CodeHash, string PhoneCodeHash)> _pendingLogins;

    private readonly int _apiId;
    private readonly string _apiHash;

    #region  MQ

    private readonly IChannel _channel;
    private readonly RabbitMQConfig _config;

    #endregion
    public static TelegramManager Instance => _instance.Value;

    private TelegramManager()
    {
        _clients = new ConcurrentDictionary<string, Client>();
        _pendingLogins = new ConcurrentDictionary<string, (string, string)>();
        var apiIdStr = Environment.GetEnvironmentVariable("TELEGRAM_API_ID");
        if (!int.TryParse(apiIdStr, out _apiId))
        {
            throw new InvalidOperationException("Invalid API ID format. Must be an integer.");
        }

        _apiHash = Environment.GetEnvironmentVariable("TELEGRAM_API_HASH");

        if (string.IsNullOrEmpty(_apiHash))
        {
            throw new InvalidOperationException("API Hash not found in environment variables.");
        }
    }

    private string ConfigHandlerWithPhone(string what, string phoneNumber, TaskCompletionSource<string> codeTask = null)
    {
        switch (what)
        {
            case "api_id": return _apiId.ToString();
            case "api_hash": return _apiHash;
            case "phone_number": return phoneNumber;
            case "verification_code":
                if (codeTask != null)
                {
                    // 抛出异常以暂停登录流程
                    throw new LoginCodeRequiredException(phoneNumber, "code_hash", "phone_code_hash");
                }

                return null;
            case "session_pathname": return $"sessions/session_{phoneNumber}.dat";
            default: return null;
        }
    }

    private string ConfigHandlerWithPhone(string what, string phoneNumber)
    {
        switch (what)
        {
            case "api_id": return _apiId.ToString();
            case "api_hash": return _apiHash;
            case "phone_number": return phoneNumber;
            case "session_pathname": return $"sessions/session_{phoneNumber}.dat";
            default: return null;
        }
    }

    public async Task LoadPhoneNumbers(IEnumerable<string> phoneNumbers)
    {
        foreach (var phoneNumber in phoneNumbers)
        {
            if (!_clients.ContainsKey(phoneNumber))
            {
                await LoginClient(phoneNumber);
            }
        }
    }

    #region Login and Logout TG Account methods

    private async Task<string> GetCodeFromUserInterface(string phoneNumber)
    {
        var tcs = new TaskCompletionSource<string>();
        _pendingLoginCodes.TryAdd(phoneNumber, tcs);
        
        var args = new LoginCodeRequestEventArgs(phoneNumber);
        OnLoginCodeRequired?.Invoke(this, args);
        
        // 等待验证码输入完成
        return await tcs.Task;
    }

    public async Task LoginClientInteractively(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number cannot be empty", nameof(phoneNumber));

        if (_clients.ContainsKey(phoneNumber))
            return; // Client already logged in

        var client = new Client(what =>
        {
            switch (what)
            {
                case "api_id": return _apiId.ToString();
                case "api_hash": return _apiHash;
                case "phone_number": return phoneNumber;
                case "verification_code":
                    // 异步获取验证码
                    var code = GetCodeFromUserInterface(phoneNumber).GetAwaiter().GetResult();
                    return code;
                case "session_pathname": return $"session_{phoneNumber}.dat";
                default: return null;
            }
        });

        try
        {
            var user = await client.LoginUserIfNeeded();
            if (user != null)
            {
                _clients.TryAdd(phoneNumber, client);
            }
        }
        catch (Exception ex)
        {
            await client.DisposeAsync();
            throw new Exception($"Failed to login for phone number {phoneNumber}: {ex.Message}", ex);
        }
        finally
        {
            // 清理验证码任务
            _pendingLoginCodes.TryRemove(phoneNumber, out _);
        }
    }

    // 提供给API调用的方法，用于提交验证码
    public void SubmitLoginCode(string phoneNumber, string code)
    {
        if (_pendingLoginCodes.TryGetValue(phoneNumber, out var tcs))
        {
            
            tcs.SetResult(code);
        }
        else
        {
            throw new InvalidOperationException($"No pending login found for phone number {phoneNumber}");
        }
    }

    public async Task LoginClient(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number cannot be empty", nameof(phoneNumber));

        if (_clients.ContainsKey(phoneNumber))
            return; // Client already logged in

        var client = new Client(what => ConfigHandlerWithPhone(what, phoneNumber));

        try
        {
            var user = await client.LoginUserIfNeeded();
            if (user != null)
            {
                _clients.TryAdd(phoneNumber, client);
                await StartMessageMonitoring(phoneNumber, client);
            }
        }
        catch (Exception ex)
        {
            await client.DisposeAsync();
            throw new Exception($"Failed to login for phone number {phoneNumber}: {ex.Message}", ex);
        }
    }
    
    private async Task StartMessageMonitoring(string phoneNumber, Client client)
    {
        client.OnUpdates += async (update) => await HandleUpdate(phoneNumber, update);
    }
    private async Task HandleUpdate(string phoneNumber, IObject update)
    {
        try
        {
            switch (update)
            {
                case UpdateNewMessage unm:
                    await HandleNewMessage(phoneNumber, unm.message);
                    break;
                // case UpdateNewChannelMessage uncm:
                //     await HandleNewMessage(phoneNumber, uncm.message);
                //     break;
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling update: {ex.Message}");
        }
    }
    
    private async Task HandleNewMessage(string phoneNumber, MessageBase messageBase)
    {
        if (messageBase is not Message message)
            return;

        if (!_clients.TryGetValue(phoneNumber, out var client))
            return;

        var messageEvent = new TelegramMessageEvent
        {
            PhoneNumber = phoneNumber,
            MessageId = message.ID,
            Text = message.message,
            Date = message.date,
            FromId = message.from_id?.ID ?? 0,
            ChatId = 0,
            IsOutgoing = message.flags.HasFlag(Message.Flags.out_),
            MessageType = ""
        };

        // 获取额外信息
        // try
        // {
        //     // 获取发送者信息
        //     if (message.from_id != null)
        //     {
        //         var sender = await GetUser(client, message.from_id.ID);
        //         messageEvent.FromName = GetUserDisplayName(sender);
        //     }
        //
        //     // 获取聊天标题
        //     var chat = await GetChat(client, messageEvent.ChatId);
        //     messageEvent.ChatTitle = GetChatDisplayName(chat);
        // }
        // catch (Exception ex)
        // {
        //     Console.WriteLine($"Error getting additional info: {ex.Message}");
        // }

        // 发送到RabbitMQ
        PublishMessage(messageEvent);
    }
    private void PublishMessage(TelegramMessageEvent messageEvent)
    {
        var json =  await  JsonSerializer.SerializeAsync((messageEvent); //JsonConvert.SerializeObject(messageEvent);
        var body = Encoding.UTF8.GetBytes(json);

        await _channel.BasicPublishAsync(
            exchange: _config.ExchangeName,
            routingKey: _config.RoutingKey,
            basicProperties: null,
            body: body);
    }
    public async Task LogoutClient(string phoneNumber)
    {
        if (_clients.TryRemove(phoneNumber, out var client))
        {
            try
            {
                await client.Auth_LogOut();
            }
            finally
            {
                await client.DisposeAsync();
            }
        }
    }

    #endregion

    public TelegramAccountInfo GetAccountInfo(string phoneNumber)
    {
        if (!_clients.TryGetValue(phoneNumber, out var client))
            throw new KeyNotFoundException($"No client found for phone number {phoneNumber}");

        try
        {
            // 直接使用已登录客户端的用户信息
            var user = client.User;
            if (user == null)
                throw new Exception("User information not available");

            return new TelegramAccountInfo
            {
                Id = user.ID,
                FirstName = user.first_name,
                LastName = user.last_name,
                Username = user.username,
                PhoneNumber = user.phone,
                IsVerified = user.flags.HasFlag(User.Flags.verified),
                IsPremium = user.flags.HasFlag(User.Flags.premium)
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get account info for {phoneNumber}: {ex.Message}", ex);
        }
    }

    public async Task<List<User>> GetContacts(string phoneNumber)
    {
        if (!_clients.TryGetValue(phoneNumber, out var client))
            throw new KeyNotFoundException($"No client found for phone number {phoneNumber}");

        var contacts = await client.Contacts_GetContacts();
        return contacts.users.Values.ToList();
    }

    public async Task<Dictionary<string, List<User>>> GetContactsForMultipleAccounts(IEnumerable<string> phoneNumbers)
    {
        var result = new Dictionary<string, List<User>>();
        foreach (var phoneNumber in phoneNumbers)
        {
            try
            {
                result[phoneNumber] = await GetContacts(phoneNumber);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting contacts for {phoneNumber}: {ex.Message}");
            }
        }

        return result;
    }

    public async Task<List<Conversation>> GetConversations(string phoneNumber)
    {
        if (!_clients.TryGetValue(phoneNumber, out var client))
            throw new KeyNotFoundException($"No client found for phone number {phoneNumber}");

        var dialogs = await client.Messages_GetAllDialogs();
        var conversations = new List<Conversation>();

        foreach (var dialog in dialogs.dialogs)
        {
            var peer = dialog.Peer;
            var lastMessage = dialogs.messages.FirstOrDefault(m => m.ID == dialog.TopMessage) as Message;

            var conversation = new Conversation
            {
                Id = GetPeerId(peer),
                Title = GetChatTitle(dialogs, peer),
                Type = DetermineConversationType(peer),
                UnreadCount = (dialog as Dialog).unread_count,
                LastMessageDate = lastMessage?.date ?? DateTime.Now,
                LastMessageText = lastMessage?.message
            };

            conversations.Add(conversation);
        }

        return conversations;
    }

    public async Task<List<TelegramMessage>> GetMessages(string phoneNumber, long conversationId, int limit = 100)
    {
        if (!_clients.TryGetValue(phoneNumber, out var client))
            throw new KeyNotFoundException($"No client found for phone number {phoneNumber}");

        InputPeer peer = await GetInputPeer(client, conversationId);
        var messages = await client.Messages_GetHistory(peer, limit: limit);

        return messages.Messages
            .Select(msg => TelegramMessage.FromMessageBase(msg))
            .Where(msg => msg != null)
            .ToList();
    }

    public async Task<TelegramMessage> SendMessage(string phoneNumber, long conversationId, string message)
    {
        if (!_clients.TryGetValue(phoneNumber, out var client))
            throw new KeyNotFoundException($"No client found for phone number {phoneNumber}");

        if (string.IsNullOrEmpty(message))
            throw new ArgumentException("Message cannot be empty", nameof(message));

        InputPeer peer = await GetInputPeer(client, conversationId);
        var sentMessage = await client.SendMessageAsync(peer, message);
        return TelegramMessage.FromMessageBase(sentMessage);
    }

    private async Task<InputPeer> GetInputPeer(Client client, long conversationId)
    {
        var dialogs = await client.Messages_GetAllDialogs();
        var dialog = dialogs.dialogs.FirstOrDefault(d => GetPeerId(d.Peer) == conversationId);

        if (dialog == null)
            throw new KeyNotFoundException($"Conversation with ID {conversationId} not found");

        var peer = dialog.Peer;

        if (peer is PeerUser userPeer)
        {
            var user = dialogs.users.FirstOrDefault(u => u.Value.ID == userPeer.user_id).Value as User;
            return new InputPeerUser(user.ID, user.access_hash);
        }
        else if (peer is PeerChat chatPeer)
        {
            return new InputPeerChat(chatPeer.chat_id);
        }
        else if (peer is PeerChannel channelPeer)
        {
            var channel = dialogs.chats.FirstOrDefault(c => c.Value.ID == channelPeer.channel_id).Value as Channel;
            return new InputPeerChannel(channel.ID, channel.access_hash);
        }

        throw new ArgumentException($"Unknown peer type for conversation {conversationId}");
    }

    private ConversationType DetermineConversationType(Peer peer)
    {
        if (peer is PeerUser) return ConversationType.PrivateChat;
        if (peer is PeerChat) return ConversationType.Group;
        if (peer is PeerChannel) return ConversationType.Channel;
        throw new ArgumentException("Unknown peer type");
    }

    private long GetPeerId(Peer peer)
    {
        if (peer is PeerUser userPeer) return userPeer.user_id;
        if (peer is PeerChat chatPeer) return chatPeer.chat_id;
        if (peer is PeerChannel channelPeer) return channelPeer.channel_id;
        throw new ArgumentException("Unknown peer type");
    }

    private string GetChatTitle(Messages_Dialogs dialogs, Peer peer)
    {
        if (peer is PeerUser userPeer)
        {
            var userInfo = dialogs.users.FirstOrDefault(u => u.Value.ID == userPeer.user_id).Value as User;
            return $"{userInfo?.first_name} {userInfo?.last_name}".Trim();
        }
        else if (peer is PeerChat chatPeer)
        {
            var chatInfo = dialogs.chats.FirstOrDefault(c => c.Value.ID == chatPeer.chat_id).Value as Chat;
            return chatInfo?.title ?? "Unknown Chat";
        }
        else if (peer is PeerChannel channelPeer)
        {
            var channelInfo = dialogs.chats.FirstOrDefault(c => c.Value.ID == channelPeer.channel_id).Value as Channel;
            return channelInfo?.title ?? "Unknown Channel";
        }

        return "Unknown";
    }

    public bool IsClientLoggedIn(string phoneNumber)
    {
        return _clients.ContainsKey(phoneNumber);
    }

    public async Task DisconnectAllClients()
    {
        foreach (var client in _clients.Values)
        {
            await client.DisposeAsync();
        }

        _clients.Clear();
    }

    ~TelegramManager()
    {
        DisconnectAllClients().Wait();
    }
}