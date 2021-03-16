using System;
using System.Linq;
using System.Threading.Tasks;
using GrainInterfaces;
using GrainInterfaces.Models.Chat;
using Orleans;
using Orleans.Streams;
using Utils;
using static System.Console;

namespace Client.Console
{
    public class ConsoleSession
    {
        private Guid _currentChat = Guid.Empty;
        private Guid _userId = Guid.Empty;
        private IUser _user;
        private string _userNickname = "";

        private readonly IClusterClient _client;

        private StreamSubscriptionHandle<ChatMessageModel> _chatMessageSubscriptionHandle; 
        
        public ConsoleSession(IClusterClient client)
        {
            _client = client;
        }

        private void PrintHints()
        {
            const ConsoleColor menuColor = ConsoleColor.Magenta;

            PrettyConsole.WriteLine("Type '/l <id>' to login", menuColor);
            PrettyConsole.WriteLine("Type /menu to see menu list", menuColor);
            PrettyConsole.WriteLine("Type '/n <username>' to set your user name", menuColor);
            PrettyConsole.WriteLine("Type '/cj <chat>' to create and join specific chat", menuColor);
            PrettyConsole.WriteLine("Type '/j <chat>' to join specific chat", menuColor);
            PrettyConsole.WriteLine("Type '/leave' to leave specific chat", menuColor);
            PrettyConsole.WriteLine("Type '/invite' to invite user to specific chat", menuColor);
            PrettyConsole.WriteLine("Type '/conn' to connect specific chat", menuColor);
            PrettyConsole.WriteLine("Type '/disc' to disconnect from specific chat", menuColor);
            PrettyConsole.WriteLine("Type '/l' to leave specific chat", menuColor);
            PrettyConsole.WriteLine("Type '/list' to show list of all chats", menuColor);
            PrettyConsole.WriteLine("Type '/sm' for send message to specific chat", menuColor);
            PrettyConsole.WriteLine("Type '/exit' to exit client.", menuColor);
        }
        
        public async Task Start()
        {
            PrintHints();

            string input;
            do
            {
                input = ReadLine();

                if (string.IsNullOrWhiteSpace(input)) continue;

                try
                {
                    if (input.StartsWith("/j"))
                    {
                        await JoinChat(Guid.Parse(input.Replace("/j", "").Trim()));
                    }
                    else if (input.StartsWith("/menu"))
                    {
                        PrintHints();
                    }
                    else if (input.StartsWith("/cj"))
                    {
                        await CreatAndJoinToChat(input.Replace("/cj", "").TrimStart());
                    }
                    else if (input.StartsWith("/leave"))
                    {
                        await LeaveChat();
                    }
                    else if (input.StartsWith("/list"))
                    {
                        await ShowChats();
                    }
                    else if (input.StartsWith("/l"))
                    {
                        await Login(input.Replace("/l", "").Trim());
                    }
                    else if (input.StartsWith("/n"))
                    {
                        await Login(input.Replace("/n", "").Trim());
                    }
                    else if (input.StartsWith("/sm"))
                    {
                        await SendMessage(input.Replace("/sm", "").TrimStart());
                    }
                    else if (input.StartsWith("/conn"))
                    {
                        await ConnectTo(Guid.Parse(input.Replace("/conn", "").Trim()));
                    }
                    else if (input.StartsWith("/disc"))
                    {
                        await Disconnect();
                    }
                    else if (input.StartsWith("/invite"))
                    {
                        await InviteAsync(input.Replace("/invite", "").Trim());
                    }
                    else if (!input.StartsWith("/exit"))
                    {
                    }
                }
                catch (Exception e)
                {
                    WriteLine(e);
                }
            } while (input != "/exit");
        }

        private async Task InviteAsync(string user)
        {
            var chat = _client.GetGrain<IChat>(_currentChat);
            var invitedUser = _client.GetGrain<IUser>(user);

            await chat.JoinAsync(_user, invitedUser);
        }

        public async Task ShowChats()
        {
            var chatListGrain = _client.GetGrain<IChatList>(Constants.ChatListId);
            var chats = await chatListGrain.GetAllAsync();

            if (!chats.Any())
            {
                PrettyConsole.WriteLine($"Chats are empty", ConsoleColor.Magenta);
            }
            
            foreach (var chat in chats)
            {
                PrettyConsole.WriteLine($"{chat.Id} - {chat.Name}", ConsoleColor.Magenta);
            }
        }

        public async Task SendMessage(string message)
        {
            if (_currentChat == Guid.Empty)
            {
                PrettyConsole.WriteLine($"You need to join chat for sending messages");
                return;
            }

            var chat = _client.GetGrain<IChat>(_currentChat);

            var messageModel = new ChatMessageModel
            {
                Text = message,
                User = _userNickname,
                UserId = _userId
            };
            
            await chat.SendMessage(messageModel);
        }

        public async Task CreatAndJoinToChat(string name)
        {
            var chatId = Guid.NewGuid();
            var chat = _client.GetGrain<IChat>(chatId);

            await chat.CreateAsync(new ChatSettingsModel
            {
                OwnerNickName = _userNickname,
                Name = name
            });

            PrettyConsole.WriteLine($"You are creating chat <{name}> with id <{chat.GetPrimaryKey()}>", ConsoleColor.Cyan);
            
            await JoinToAndConnect(chat);
        }

        public async Task JoinChat(Guid id)
        {
            var chat = _client.GetGrain<IChat>(id);

            await JoinToAndConnect(chat);
        }
        
        public async Task LeaveChat()
        {
            var chat = _client.GetGrain<IChat>(_currentChat);

            await chat.Leave(_user);
            await _chatMessageSubscriptionHandle.UnsubscribeAsync();
            
            _currentChat = Guid.Empty;
        }

        public async Task Login(string name)
        {
            var user = _client.GetGrain<IUser>(name);

            await Login(user);
        }

        public async Task Disconnect()
        {
            var chat = _client.GetGrain<IChat>(_currentChat);

            await chat.Disconnect(_user);
            await _chatMessageSubscriptionHandle.UnsubscribeAsync();
            
            ClearConsoleAndPrintHints();
        }

        public async Task ConnectTo(Guid chatId)
        {
            var chat = _client.GetGrain<IChat>(chatId);

            await chat.ConnectAsync(_user);
            
            var chatMessageStream = GetChatMessageStream(chat.GetPrimaryKey());

            _chatMessageSubscriptionHandle = await chatMessageStream.SubscribeAsync(ChatMessageHandle);

            var messages = await chat.GetHistory(20);

            PrettyConsole.WriteLine($"Online count members: {await chat.GetOnlineCountMembersAsync()}");
            
            foreach (var message in messages)
            {
                ShowMessage(message);
            }

            _currentChat = chatId;
        }
        
        private async Task JoinToAndConnect(IChat chat)
        {
            var id = chat.GetPrimaryKey();
            
            var chatMessageStream = GetChatMessageStream(chat.GetPrimaryKey());

            _chatMessageSubscriptionHandle = await chatMessageStream.SubscribeAsync(ChatMessageHandle);

            if (_currentChat == id)
            {
                PrettyConsole.WriteLine($"You already joined chat {id}");
                return;
            }

            _currentChat = id;

            await chat.JoinAsync(_user);
            await chat.ConnectAsync(_user);

            var info = await chat.GetInfoAsync();
            
            PrettyConsole.WriteLine($"You join to chat <{info.Name}>", ConsoleColor.Cyan);
            PrettyConsole.WriteLine($"Online count members: {await chat.GetOnlineCountMembersAsync()}");
        }
        
        private async Task Login(IUser user)
        {
            _userNickname = user.GetPrimaryKeyString();
            _userId = await user.GetUserIdAsync();
            _user = user;

            PrettyConsole.WriteLine($"Your nickname is {_userNickname}, id - {_userId}", ConsoleColor.Gray);
        }

        private Task ChatMessageHandle(ChatMessageModel model, StreamSequenceToken token)
        {
            ShowMessage(model);
            
            return Task.CompletedTask;
        }

        private void ShowMessage(ChatMessageModel model)
        {
            PrettyConsole.Line($"{model.User}: ",
                _userNickname == model.User ? ConsoleColor.Yellow : ConsoleColor.Green);
            PrettyConsole.Line(model.Text, ConsoleColor.Blue);
            PrettyConsole.WriteLine("");            
        }

        private void ClearConsoleAndPrintHints()
        {
            Clear();
            PrintHints();
        }

        private IAsyncStream<ChatMessageModel> GetChatMessageStream(Guid chatId)
        {
            var streamProvider = _client.GetStreamProvider(Constants.StreamProvider);

            return streamProvider.GetStream<ChatMessageModel>(chatId, Constants.ChatMessagesNamespace);
        }
    }
}