using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using static RTSPTest.PingMonitor;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Schema;

namespace RTSPTest
{
    class Program
    {
        public static Logger Log = new Logger();
        public static Process ffmpegProcess = new Process();
        static void Main(string[] args)
        {
            // Параметры для теста
            string rtspUrl = "rtsp://example.com:8554/live"; // URL потока RTSP
            string protocol = "tcp"; // Протокол транспорта (tcp или udp)
            string VideoFileName = "";
            int duration = 3600; // Длительность теста в секундах
            int pingInterval = 2000;
            float hours = 1;

            try
            {
                Console.Write("Введите RTSP:");
                rtspUrl = Console.ReadLine();
                string ip = ParseIp(rtspUrl);
                PingMonitor monitor = new PingMonitor(ip, pingInterval);
                Console.Write("tcp или udp:");
                protocol = Console.ReadLine().ToLower().Contains("udp") ? "udp" : "tcp";
                Log = new Logger(ip, protocol);
                VideoFileName = NameOfVideoFile(ip, protocol);
                Console.Write("Время тестирования в часах: ");
                var h = Console.ReadLine();
                hours = float.Parse(!String.IsNullOrWhiteSpace(h) ? h : "1,0");
                duration = (int) (hours * 3600);
                // Команда для запуска ffmpeg
                string ffmpegCmd = $" -loglevel warning -rtsp_transport {protocol} -i {rtspUrl} -t {duration} -vf scale=640:-1 {VideoFileName}.mp4";
                monitor.PingFailed += Monitor_PingFailed;
                // Создание процесса ffmpeg
                Log.WriteLog($"Запуск теста {ffmpegCmd}", ConsoleColor.Green);
                Log.WriteLog("ip = " + ip, ConsoleColor.Green);
                
                Log.WriteLog("Длительность теста = " + ( hours > 1.0 ? (hours + "ч" ): (hours * 3600) + "c"), ConsoleColor.Green);
                Log.WriteLog("Запись лога в файл " + Log.logFile, ConsoleColor.Blue);
                AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
                Console.CancelKeyPress += Console_CancelKeyPress;
                ffmpegProcess.StartInfo.FileName = "ffmpeg.exe";
                ffmpegProcess.StartInfo.Arguments = ffmpegCmd;
                ffmpegProcess.StartInfo.UseShellExecute = false;
                ffmpegProcess.StartInfo.RedirectStandardOutput = true;
                ffmpegProcess.StartInfo.RedirectStandardError = true;
                ffmpegProcess.StartInfo.CreateNoWindow = false;
                // Обработка событий вывода и ошибок
                ffmpegProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Log.WriteLog(e.Data);
                    }
                };

                ffmpegProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Log.WriteLog(e.Data);
                    }
                };


                        // Запуск процесса ffmpeg
                    ffmpegProcess.Start();
                    ffmpegProcess.BeginOutputReadLine();
                    ffmpegProcess.BeginErrorReadLine();
                    monitor.Start();
                    // Ожидание завершения процесса ffmpeg
                    ffmpegProcess.WaitForExit();

                    // Закрытие процесса ffmpeg
                    ffmpegProcess.Close();
                    monitor.Stop();
                Log.WriteLog("Тест завершен. Логи в файле " + Log.logFile, ConsoleColor.Green);

                Console.ReadKey();

            }catch(Exception ex)
            {
                Log.WriteLog("Exit: catch exception");
                ffmpegProcess.Close();
                Log.WriteLog("ffmpegProcess closed");
            }
            
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            //Log.WriteLog("Exit: Console_CancelKeyPress");
            //ffmpegProcess.Close();
            //Log.WriteLog("ffmpegProcess closed");
            //Environment.Exit(0);
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Log.WriteLog("CurrentDomain_ProcessExit");
           if (!ffmpegProcess.HasExited) ffmpegProcess.Close();
            Log.WriteLog("ffmpegProcess closed");
            Environment.Exit(0);
        }

        private static void Monitor_PingFailed(object sender, PingFailedEventArgs e)
        {
            Log.WriteLog($"Ping failed: {e.ipAddress}");
        }

        public static string ParseIp(string input)
        {
            if (String.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            var regex = new Regex(@"\b((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b");
            var match = regex.Match(input);
            return match.Success ? match.Value : null;
        }

        public static string NameOfVideoFile(string ip, string protocol)
        {
            return $"{ip}_{protocol}_{DateTime.Now.ToString("g").Replace(":",".").Replace(" ","")}";
        }
    }

    class Logger
    {
        public string logFile;
        public string ip;
        public Logger(string ip)
        {
            this.ip = ip;
            logFile = $"{ip}_{DateTime.Now.ToString("d")}.txt";
        }

        public Logger(string ip, string protocol)
        {
            this.ip = ip;
            logFile = $"{ip}_{DateTime.Now.ToString("d")}_{protocol}.txt";
        }

        public Logger()
        {

        }

        public void WriteLog(string msg)
        {
            string fullMsg = DateTime.Now.ToString("G") + " ==> \t" + msg;
            WriteConsole(fullMsg);
            using( var writer = new StreamWriter(logFile, true) )
            {
                var synchronizedWriter = TextWriter.Synchronized(writer);
                synchronizedWriter.WriteLine(fullMsg);
            }
            //File.AppendAllText(logFile, fullMsg + "\n");
        }

        public void WriteLog(string msg, ConsoleColor color)
        {
            string fullMsg = DateTime.Now.ToString("G") + " ==> \t" + msg;
            WriteConsoleColor(fullMsg, color);
            using (var writer = new StreamWriter(logFile, true))
            {
                var synchronizedWriter = TextWriter.Synchronized(writer);
                synchronizedWriter.WriteLine(fullMsg);
            }
            //File.AppendAllText(logFile, fullMsg + "\n");
        }


        public static void WriteConsoleColor(string msg, ConsoleColor color)
        {
            
                Console.ForegroundColor = color;
                Console.WriteLine(msg);
                Console.ResetColor();
            
        }

        private void WriteConsole(string msg)
        {
            if (msg.Contains("error") || msg.Contains("failed"))
            {
                WriteConsoleColor(msg, ConsoleColor.Red);
            }
            else if (msg.Contains("missed") || msg.Contains("corrupt"))
            {
                WriteConsoleColor(msg, ConsoleColor.Yellow);
            }else
            {
                Console.WriteLine(msg);
            }
        }
    }

 
}
