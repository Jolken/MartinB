using DSharpPlus.VoiceNext;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

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
}
