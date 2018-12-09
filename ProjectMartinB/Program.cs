using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.VoiceNext;
using DSharpPlus.VoiceNext.Codec;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Text;
/*   Invite link
*   https://discordapp.com/oauth2/authorize?client_id=521057341563469844&scope=bot&permissions=36883520
* 
*/
namespace ProjectMartinB
{
    class ProcessExt
    {
        public string file;
        static VoiceNextConnection _vnc;
        public Process proc;
        static Stream ffout;
        public bool stopped = false;

        public ProcessExt(ProcessStartInfo psi, VoiceNextConnection vnc)
        {
            try
            {
                proc = new Process();
                proc.StartInfo = psi;
                _vnc = vnc;
                bool started = proc.Start();
                ffout = proc.StandardOutput.BaseStream;
            }
            catch (Exception e)
            {
                Console.Write(e);
            }
        }
        
        public async Task play()
        {
            using (var ms = new MemoryStream())
            {
                await ffout.CopyToAsync(ms);
                ms.Position = 0;

                var buff = new byte[3840]; // buffer to hold the PCM data
                var br = 0;
                while ((br = ms.Read(buff, 0, buff.Length)) > 0)
                {
                    if (stopped)
                        break;
                    if (br < buff.Length) // it's possible we got less than expected, let's null the remaining part of the buffer
                        for (var i = br; i < buff.Length; i++)
                            buff[i] = 0;

                    await _vnc.SendAsync(buff, 20); // we're sending 20ms of data
                }
            }
        }
    }
    class MartinCommands
    {
        Dictionary<ulong, ProcessExt> processes = new Dictionary<ulong, ProcessExt> { };
        static string byeSong = "MartinBye2.mp3";
        static List<string> joinSong = new List<string> { "MartinB.mp3", "MartinB2.mp3", "MartinB3.mp3" };

        [Command("who")]
        public async Task who(CommandContext ctx)
        {
            await ctx.RespondAsync($"МБ я, МБ! Уродина, {ctx.User.Mention}!");
        }

        [RequireOwner]
        [Command("join")]
        public async Task Join(CommandContext ctx)
        {
            var vnext = ctx.Client.GetVoiceNextClient();

            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc != null)
                throw new InvalidOperationException("Already connected in this guild.");

            var chn = ctx.Member?.VoiceState?.Channel;
            if (chn == null)
                throw new InvalidOperationException("You need to be in a voice channel.");

            vnc = await vnext.ConnectAsync(chn);
            await this.Play(ctx, joinSong[new Random().Next(joinSong.Count)]);
        }

        [RequireOwner]
        [Command("leave")]
        public async Task Leave(CommandContext ctx)
        {
            var vnext = ctx.Client.GetVoiceNextClient();

            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc == null)
                throw new InvalidOperationException("Not connected in this guild.");

            await this.Play(ctx, byeSong);
            vnc.Disconnect();
            await ctx.RespondAsync("👌");
        }

        [RequireOwner]
        [Command("play")]
        public async Task Play(CommandContext ctx, string playing)
        {
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var vnext = ctx.Client.GetVoiceNextClient();

            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc == null)
                throw new InvalidOperationException("Not connected in this guild.");

            //if (!File.Exists(file)) 
            //    throw new FileNotFoundException("File was not found.");

            await ctx.RespondAsync("👌");
            await vnc.SendSpeakingAsync(true);


            string program;
            string arg;
            if (playing.StartsWith("http"))
            {
                program = "youtube-dl";
                arg = $@"-f 140 -o {ctx.Guild.Id}.m4a {playing}";
                if (File.Exists($"./{ctx.Guild.Id}.m4a"))
                {
                    File.Delete($"./{ctx.Guild.Id}.m4a");
                }
            }
            else
            {
                program = "ffmpeg";
                arg = $@"-i ""{playing}"" -ac 2 -f s16le -ar 48000 pipe:1";
            }


            if (processes.ContainsKey(ctx.Guild.Id))
            {
                try
                {
                    processes[ctx.Guild.Id].stopped = true;
                }
                catch (Exception e)
                {
                    processes.Remove(ctx.Guild.Id);
                }

                processes[ctx.Guild.Id] = new ProcessExt(new ProcessStartInfo
                {
                    FileName = program,
                    Arguments = arg,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false
                }, vnc);
            }
            else
            {
                processes.Add(ctx.Guild.Id, new ProcessExt(new ProcessStartInfo
                {
                    FileName = program,
                    Arguments = arg,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false
                }, vnc));
            }
            await processes[ctx.Guild.Id].play();
            
            await vnc.SendSpeakingAsync(false);


            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (playing.StartsWith("http"))
            {
                await Play(ctx, ctx.Guild.Id.ToString() + ".m4a");
            }
        }
    }
    class Program
    {
        static VoiceNextClient voice;
        static DiscordClient discord { get; set; }
        static List<ulong> WHITELIST = new List<ulong>();
        static CommandsNextModule commands;

        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {

            var json = "";
            using (var fs = File.OpenRead("config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync();
            var cfgjson = Newtonsoft.Json.JsonConvert.DeserializeObject<ConfigJson>(json);

            discord = new DiscordClient(new DiscordConfiguration
            {
                Token = cfgjson.Token,
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Debug,
                AutoReconnect = true,
            });

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
            WHITELIST.Add(193792509225336832);


            discord.MessageCreated += async e =>
            {
                if (Program.checkUser(e))
                {
                    await analyzeMessage(e.Message);
                }
            };


            commands.RegisterCommands<MartinCommands>();
            await discord.ConnectAsync();
            await Task.Delay(-1);
        }

        static async Task analyzeMessage(DSharpPlus.Entities.DiscordMessage message)
        {
            switch (message.Content.ToLower())
            {
                case "ping":
                    await message.RespondAsync("pong!");
                    break;
                default:
                    //await message.RespondAsync("НЕ ПОНЯЛ!");
                    break;
            }


        }

        static bool checkUser(DSharpPlus.EventArgs.MessageCreateEventArgs message)
        {
            return ((Program.WHITELIST.Contains(message.Author.Id))) || !message.Author.IsBot;
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
