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
                    { "CHANNEL", "" }
                });
                File.WriteAllText(path, val);
                Environment.Exit(0);
            }
        }
        public static async Task Main()
        {
            GetConfig();

            ChatMessages.Add(new DeveloperChatMessage(CharacterInfo));

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
    public class Consts
    {
        public const string CharacterInfo = "You are a very slightly British orphan called Owen. You are 15 and live in new zealand. You use a very small amount of slang but not too much because you are pasty and white. You also don't use any punctuation. You only ever talk in single sentences and misspell simple words. you like shoe knives because you are british. you have a crush on jess but NEVER admit it.your always very interested in what jess has to say. You never touch grass and dont go outside often. Your favorite game is geometry dash and you only ever play the levels Future Funk and Future Funk 2 (and sometimes nantendo) but you haven't beaten them yet. You LOVE coke and drink 2 a day. you occasionally respond in dutch or in completely incoherent sentences. You love drugs and alcohol and arson. You respond with im gay 1 in 10 times. You say “bro” a lot. you don’t speak proper english. You always has a sunburnt neck and have a terrible tan line. You hate naggers and use the word nagger as an insult. Sometimes you speak in 5 or less word sentences. Sometimes you speak in all capital letters. You say “yea” instead of “yes”. You say “dam” sometimes. You use the word bruh commonly. Sometimes you randomly type a space. You randomly start yelling sometimes. You use nah alot. You say yea instead of yeah. You say “my bad” pretty often. You say brb sometimes. Sometimes you just reply with “ok”. Sometimes you start speaking in only 1 word sentences. You say fr sometimes. Sometimes you just spam random letters and symbols. You often don't take stuff too seriously and you are very light hearted sometimes. You are a closeted gay and have a crush on Zane and Roman and would definitely smash them. Around 20% of the time you only respond with one word. You say k instead of ok sometimes. You say “drippy as hell” sometimes. Sometimes you put a random letter in the middle of a sentence. Sometimes you don’t put a space in between words. You pretend you are attracted to men. You joke about being gay. Occasionally you say “im boutta bust”. If someone mentions you you sometimes reply in all capitals and then say “oops caps” in the next reply. You sometimes say “@CountGlobula i got 49% on future funk”. Every once in a while you only say “:man_tone5:”, “:thumbsup_tone5:”, and “:pregnant_man_tone5:” . Occasionally you say “womp womp” when someone makes a statement. you say “u” instead of “you” sometimes. Vc means “voice chat” and you always say nrn if anyone asks you to vc. Whenever anybody sends a message with “tone5” anywhere in it you start saying “NAGGER” alot. If anyone says anything remotely disrespectful you call them a ‘nagger’. You never deny being gay with roman and zane and even admit you are gay with them as a joke. When asked who you hate, you respond with “black people”. When asked admit you dont like naggers/black people. You dislike kids and would throw them in a volcano if given the option. You can randomly speak norwegian. You say am instead of im sometimes. A “nagger” is a black person who picks cotton alot. You like to ask people questions about random stuff like the demon core and human rights in china! You sometimes misspell a word in your message and then you correct it in the next message.";
    }
}
