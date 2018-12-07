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
/*   Invite link
*   https://discordapp.com/oauth2/authorize?client_id=519613874564104202&scope=bot&permissions=8
* 
*/
namespace ProjectMartinB
{
    class MartinCommands
    {
        [Command("who")]
        public async Task who(CommandContext ctx)
        {
            await ctx.RespondAsync($"МБ я, МБ! Уродина, {ctx.User.Mention}!");
        }

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
            await ctx.RespondAsync("👌");
        }

        [Command("leave")]
        public async Task Leave(CommandContext ctx)
        {
            var vnext = ctx.Client.GetVoiceNextClient();

            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc == null)
                throw new InvalidOperationException("Not connected in this guild.");

            vnc.Disconnect();
            await ctx.RespondAsync("👌");
        }
        [Command("play")]
        public async Task Play(CommandContext ctx, [RemainingText] string file)
        {
            var vnext = ctx.Client.GetVoiceNextClient();

            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc == null)
                throw new InvalidOperationException("Not connected in this guild.");

            if (!File.Exists(file))
                throw new FileNotFoundException("File was not found.");

            await ctx.RespondAsync("👌");
            await vnc.SendSpeakingAsync(true); // send a speaking indicator

            var psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $@"-i ""{file}"" -ac 2 -f s16le -ar 48000 pipe:1",
                RedirectStandardOutput = true,
                UseShellExecute = false 
            };
            var ffmpeg = Process.Start(psi);
            var ffout = ffmpeg.StandardOutput.BaseStream;

            var buff = new byte[3840];
            var br = 0;
            while ((br = ffout.Read(buff, 0, buff.Length)) > 0)
            {
                if (br < buff.Length) // not a full sample, mute the rest
                    for (var i = br; i < buff.Length; i++)
                        buff[i] = 0;

                await vnc.SendAsync(buff, 20);
            }

            await vnc.SendSpeakingAsync(false); // we're not speaking anymore
        }
    }
    class Program
    {
        internal static VoiceNextConnection VNC;
        static VoiceNextClient voice;
        static DiscordClient discord { get; set; }
        static List<Int64> BLACKLIST = new List<Int64>();
        static CommandsNextModule commands;
        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task analyzeMessage(DSharpPlus.Entities.DiscordMessage message)
        {
            switch (message.Content.ToLower()) {
                case "ping":
                    await message.RespondAsync("pong!");
                    break;
                default:
                    //await message.RespondAsync("НЕ ПОНЯЛ!");
                    break;
            }


        }
        static async Task<bool> checkUser(DSharpPlus.EventArgs.MessageCreateEventArgs message)
        {
            return ((BLACKLIST.Contains(message.Author.GetHashCode())) || !message.Author.IsBot);
        }
        static async Task MainAsync(string[] args)
        {
            discord = new DiscordClient(new DiscordConfiguration
            {
                Token = "NTE5NjEzODc0NTY0MTA0MjAy.DusPDg.4k6sxJ0hHnRmuIdD17TZr5DJOJ0",
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Debug,
                AutoReconnect = true,
            });
            var commands = discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefix = "m/",
                EnableDms = false,
                EnableMentionPrefix = true
            });
            var vcfg = new VoiceNextConfiguration
            {
                VoiceApplication = VoiceApplication.Music
            };
            var voice = discord.UseVoiceNext(vcfg);
            BLACKLIST.Add(519613874564104202);

            discord.MessageCreated += async e =>
            {
                if (await checkUser(e))
                {
                    await analyzeMessage(e.Message);
                }
            };
            commands.RegisterCommands<MartinCommands>();
            await discord.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}
