using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.VoiceNext;
using DSharpPlus.VoiceNext.Codec;
using DSharpPlus.CommandsNext;
using System.IO;
using System.Text;
using DSharpPlus.CommandsNext.Attributes;
using System.Diagnostics;

/*   Invite link
*   https://discordapp.com/oauth2/authorize?client_id=521057341563469844&scope=bot&permissions=36883520
* 
*/
namespace ProjectMartinB
{
    
    class Program
    {
        static MartinMention mention = new MartinMention(Path.Combine(Environment.CurrentDirectory, "Data", "Mention.zip"));
        //static MartinClassify classify = new MartinClassify(Path.Combine(Environment.CurrentDirectory, "Data", "Classify.zip"));

        static VoiceNextClient voice;
        static DiscordClient discord;
        static List<ulong> WHITELIST = new List<ulong>();
        static CommandsNextModule commands;

        static void Main(string[] args)
        {
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {

            /*
            var http = (HttpWebRequest)WebRequest.Create(new Uri("https://discordapp.com/api/v6/auth/login"));
            http.Accept = "application/json";
            http.ContentType = "application/json";
            http.Method = "POST";
            string parsedContent = Newtonsoft.Json.JsonSerializer.
            ASCIIEncoding encoding = new ASCIIEncoding();
            Byte[] bytes = encoding.GetBytes(parsedContent);
            Stream newStream = http.GetRequestStream();
            newStream.Write(bytes, 0, bytes.Length);
            newStream.Close();
            var response = http.GetResponse();

            var stream = response.GetResponseStream();
            var srr = new StreamReader(stream);
            var content = srr.ReadToEnd();

            using (var wb = new WebClient())
            {
                var data = new NameValueCollection();
                data["email"] = "email";
                data["password"] = "pass";
                wb.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                var res = wb.UploadValues("https://discordapp.com/api/v6/auth/login", data);
                response = Encoding.Default.GetString(res);
            }
            */
            var json = "";
            using (var fs = File.OpenRead("config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync();
            var cfgjson = Newtonsoft.Json.JsonConvert.DeserializeObject<ConfigJson>(json);
            var conf = new DiscordConfiguration
            {
                //Token = cfgjson.Token,
                //TokenType = TokenType.Bot,
                Token = cfgjson.Token,
                TokenType = TokenType.User,
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Debug,
                AutoReconnect = true,
            };
            try
            {
                discord = new DiscordClient(conf);
            }
            catch (Exception e)
            {
                Console.Write(e);
            }

            var commands = discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefix = cfgjson.CommandPrefix,
                EnableDms = false,
                EnableMentionPrefix = true
            });

            var vcfg = new VoiceNextConfiguration
            {
                VoiceApplication = VoiceApplication.Music
            };

            var voice = discord.UseVoiceNext(vcfg);


            discord.MessageCreated += async ev =>
            {
                if (checkUser(ev))
                {
                    try
                    {
                    await analyzeMessage(ev);
                    }
                    catch (Exception ex)
                    {
                        Console.Write(ex);
                    }
                }
            };


            commands.RegisterCommands<MartinCommands>();
            await discord.ConnectAsync();
            await Task.Delay(-1);
        }

        static async Task analyzeMessage(DSharpPlus.EventArgs.MessageCreateEventArgs message)
        {
            var channel = message.Channel;
            switch (message.Message.Content.ToLower())
            {
                case "ping":
                    await message.Message.RespondAsync("pong!");
                    break;
                default:
                    if (true)//mention.Predict(message.Message.Content.ToLower()))//mentionedMartinB(message.Message.Content.ToLower()))
                    {
                        await discord.SendMessageAsync(channel, "Кто тут такой смелый?");
                    }
                    break;
            }


        }

        static bool checkUser(DSharpPlus.EventArgs.MessageCreateEventArgs message)
        {
            return (message.Author.Id == 193792509225336832) && !message.Author.IsBot;
        }
    }
    public struct ConfigJson
    {
        [Newtonsoft.Json.JsonProperty("token")]
        public string Token { get; private set; }

        [Newtonsoft.Json.JsonProperty("prefix")]
        public string CommandPrefix { get; private set; }
    }
}
