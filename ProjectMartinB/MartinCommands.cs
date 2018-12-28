using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.VoiceNext;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ProjectMartinB
{
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

        [Command("exe")]
        public async Task execute(CommandContext ctx, string command)
        {
            await ctx.RespondAsync(command);
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
}
