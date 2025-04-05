using Discord.WebSocket;
using Discord;
using OpenAI.Chat;
using static OWENAI.Consts;
using System.ClientModel;
using Newtonsoft.Json;

namespace OWENAI
{
    internal class Program
    {
        static int Attention = 0;
        static long lastMessageTime = 0;
        static Dictionary<ulong, string> UserToName = new Dictionary<ulong, string>()
        {
            { 695384359812857906, "Ben" },
            { 1341650078015688726, "Chloe" },
            { 725587898698694707, "Zane"},
            { 760378119306739722, "Roman" },
            { 817919585687961601, "Retard" },
            { 1055238654857060543, "Jess" },
            { 1087235331448905798, "Michael" },
            { 1024162544631414785, "Aiden"},
            { 1087228177539149834, "Evie" },
            { 989427694221553694, "Claudia" },
            { 909520817216438302, "Mark" },
            { 848809248788185128, "The fake owen" }
        };
        static List<ChatMessage> ChatMessages = new List<ChatMessage>();

        static DiscordSocketClient _client;
        static ChatClient _chatClient;
        public static Dictionary<string, string> ValuesByKey = new Dictionary<string, string>();
        public static void GetConfig()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "config.json");

            if (File.Exists(path))
            {
                ValuesByKey = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));
            }
            else
            {
                var val = JsonConvert.SerializeObject(new Dictionary<string, string>()
                {
                    { "OPENAITOKEN", "" },
                    { "MODEL", "" },
                    { "DISCORDTOKEN", "" },
                    { "CHANNEL", "" },
                    { "CHARINFO", "" }
                });
                File.WriteAllText(path, val);
                Environment.Exit(0);
            }
        }
        public static async Task Main()
        {
            GetConfig();

            ChatMessages.Add(new DeveloperChatMessage(ValuesByKey["CHARINFO"]));

            _client = new DiscordSocketClient(new DiscordSocketConfig { GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent });
            _chatClient = new ChatClient(ValuesByKey["MODEL"], ValuesByKey["OPENAITOKEN"]);
            _client.MessageReceived += Message;


            await _client.LoginAsync(TokenType.Bot, ValuesByKey["DISCORDTOKEN"]);
            await _client.StartAsync();
            await _client.SetStatusAsync(UserStatus.Online);

            Console.WriteLine("Bot is online!");

            await Task.Delay(-1);
        }
        private async static Task Message(SocketMessage msg)
        {
            if (msg.Channel.Id == ulong.Parse(ValuesByKey["CHANNEL"]) && msg.Author.Id != 1357575134298374374)
            {
                await AddMessage(msg);
                await UpdateAttention(msg);
                await TryReply(msg);
            }
        }

        public async static Task UpdateAttention(SocketMessage msg)
        {
            if (Attention > 0)
                Attention -= 1;

            var timespan = new TimeSpan(lastMessageTime);
            var currenttime = new TimeSpan(DateTime.Now.Ticks);
            var timesincelastmessage = currenttime.TotalSeconds - timespan.TotalSeconds;
            Console.WriteLine($"Time since last message: {Math.Round(timesincelastmessage, 2)}");
            if (timesincelastmessage < 30)
            {
                Attention = Math.Max(1, Attention);
                Console.WriteLine("Set 1 Attention for recency");
            }

            if (timesincelastmessage > 600)
            {
                Attention = 0;
            }

            if (msg.Content.ToLower().Contains("owen") || msg.MentionedUsers.Any(a => a.Id == 848809248788185128 || a.Id == 1357575134298374374))
            {
                Console.WriteLine("Set 5 Attention for directly mentioned");
                Attention = Math.Max(5, Attention);
            }

            Random random = new Random();
            if (msg.Author.Id == 1055238654857060543 || random.Next(0, 5) == 0)
            {
                Attention = Math.Max(2, Attention);
                Console.WriteLine("Set 2 Attention for jess message/random");
            }
        }
        private async static Task AddMessage(SocketMessage msg)
        {
            ChatMessages.Add(new UserChatMessage(msg.Content) { ParticipantName = GetNameFromId(msg.Author) });
            while (ChatMessages.Count > 20)
            {
                ChatMessages.Remove(ChatMessages.Where(a => a.GetType() != typeof(DeveloperChatMessage)).First());
            }
        }
        private async static Task TryReply(SocketMessage msg)
        {
            Random random = new Random();

            if (Attention > 0)
            {
                await ReplyWithMessage(msg);
            }
        }

        private async static Task ReplyWithMessage(SocketMessage msg)
        {
            Random random = new Random();
            ClientResult<ChatCompletion> result = await _chatClient.CompleteChatAsync(ChatMessages);
            if (result.Value.FinishReason == ChatFinishReason.ContentFilter)
                return;
            ChatMessages.Add(new AssistantChatMessage(result.Value.Content.First().Text));
            await Task.Delay(random.Next(500, 2000));
            await msg.Channel.SendMessageAsync(result.Value.Content.First().Text);
            if (random.Next(0, 6) == 0)
                ReplyWithMessage(msg);
            lastMessageTime = DateTime.Now.Ticks;
        }

        private static string GetNameFromId(SocketUser user)
        {
            if (UserToName.TryGetValue(user.Id, out string value))
            {
                return value;
            }
            return user.GlobalName;
        }
    }
}
