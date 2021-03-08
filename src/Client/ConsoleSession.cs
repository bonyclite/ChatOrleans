using System;
using System.Linq;
using System.Threading.Tasks;
using DAL.Repositories;
using GrainInterfaces;
using GrainInterfaces.Models.Chat;
using GrainInterfaces.Models.User;
using Microsoft.EntityFrameworkCore;
using Orleans;
using Orleans.Streams;
using Utils;
using ChatModel = DAL.Models.ChatModel;

namespace Client
{
    public class ConsoleSession
    {
        private Guid _currentChat = Guid.Empty;
        private Guid _userId = Guid.Empty;
        private string _userNickname = "";

        private readonly IClusterClient _client;
        private readonly IGenericRepository<ChatModel> _chatsRepository;

        private StreamSubscriptionHandle<ChatMessageModel> _chatMessageSubscriptionHandle; 
        
        public ConsoleSession(IClusterClient client
            , IGenericRepository<ChatModel> chatsRepository)
        {
            _client = client;
            _chatsRepository = chatsRepository;
        }

        private void PrintHints()
        {
            const ConsoleColor menuColor = ConsoleColor.Magenta;

            PrettyConsole.WriteLine("Type '/l <id>' to login", menuColor);
            PrettyConsole.WriteLine("Type '/n <username>' to set your user name", menuColor);
            PrettyConsole.WriteLine("Type '/cj <chat>' to create and join specific chat", menuColor);
            PrettyConsole.WriteLine("Type '/j <chat>' to join specific chat", menuColor);
            PrettyConsole.WriteLine("Type '/l' to leave specific chat", menuColor);
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
                input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input)) continue;

                try
                {
                    if (input.StartsWith("/j"))
                    {
                        await JoinChat(Guid.Parse(input.Replace("/j", "").Trim()));
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
                    else if (!input.StartsWith("/exit"))
                    {
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            } while (input != "/exit");
        }

        public async Task ShowChats()
        {
            var chats = await _chatsRepository.GetAll().ToArrayAsync();

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
            var user = _client.GetGrain<IUser>(_userNickname);

            var messageModel = new ChatMessageModel
            {
                Text = message,
                User = user.GetPrimaryKeyString(),
                UserId = await user.GetUserIdAsync()
            };
            
            await chat.SendMessage(messageModel);
        }

        public async Task CreatAndJoinToChat(string name)
        {
            var chat = _client.GetGrain<IChat>(Guid.NewGuid());

            await chat.Init(new ChatSettingsModel
            {
                OwnerNickName = _userNickname,
                Name = name
            });

            PrettyConsole.WriteLine($"You are creating chat <{name}> with id <{chat.GetPrimaryKey()}>", ConsoleColor.Cyan);
            
            await JoinTo(chat);
        }

        public async Task JoinChat(Guid id)
        {
            var chat = _client.GetGrain<IChat>(id);

            await JoinTo(chat);
        }
        
        public async Task LeaveChat()
        {
            var chat = _client.GetGrain<IChat>(_currentChat);
            var user = _client.GetGrain<IUser>(_userNickname);

            await chat.Leave(user);
            await _chatMessageSubscriptionHandle.UnsubscribeAsync();
            
            _currentChat = Guid.Empty;
        }

        public async Task Login(string name)
        {
            var user = _client.GetGrain<IUser>(name);

            await Login(user);
        }

        public async Task ConnectTo(Guid chatId)
        {
            var chat = _client.GetGrain<IChat>(chatId);
            var user = _client.GetGrain<IUser>(_userNickname);

            await chat.Connect(user);
            
            var chatMessageStream = GetChatMessageStream(chat.GetPrimaryKey());

            _chatMessageSubscriptionHandle = await chatMessageStream.SubscribeAsync(ChatMessageHandle);

            var messages = await chat.GetHistory(20);

            foreach (var message in messages)
            {
                ShowMessage(message);
            }

            _currentChat = chatId;
        }

        public async Task Disconnect()
        {
            var chat = _client.GetGrain<IChat>(_currentChat);
            var user = _client.GetGrain<IUser>(_userNickname);

            await chat.Disconnect(user);
            await _chatMessageSubscriptionHandle.UnsubscribeAsync();
            
            ClearConsoleAndPrintHints();
        }
        
        private async Task Login(IUser user)
        {
            _userNickname = user.GetPrimaryKeyString();
            _userId = await user.GetUserIdAsync();

            PrettyConsole.WriteLine($"Your nickname is {_userNickname}, id - {_userId}", ConsoleColor.Gray);
        }
        
        private async Task JoinTo(IChat chat)
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

            var user = _client.GetGrain<IUser>(_userNickname);

            await chat.Join(user);
            await chat.Connect(user);

            var info = await chat.GetInfo();
            
            PrettyConsole.WriteLine($"You join to chat <{info.Name}>", ConsoleColor.Cyan);
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
            Console.Clear();
            PrintHints();
        }

        private IAsyncStream<ChatMessageModel> GetChatMessageStream(Guid chatId)
        {
            var streamProvider = _client.GetStreamProvider(Constants.StreamProvider);

            return streamProvider.GetStream<ChatMessageModel>(chatId, Constants.ChatMessagesNamespace);
        }
    }
}