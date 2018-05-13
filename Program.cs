using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EspController
{
    class Program
    {


        private static Dictionary<int, string> dicCommands = new Dictionary<int, string>()
        {
            [1] = "Spegni tutte le GPIO",
            [2] = "Switch GPIO",
            [3] = "Accendi tutte le GPIO",
            [4] = "Pulse GPIO",
            [5] = "Clear Console",
            [6] = "Quit"
        };
        private static List<int> listGPIOs = new List<int>() { 16, 5, 4, 0, 2, 14, 12, 13, 15, 3, 1 };
        private static bool IsESP(IPAddress ip)
        {
            string macAddress = string.Empty;
            System.Diagnostics.Process pProcess = new System.Diagnostics.Process();
            pProcess.StartInfo.FileName = "arp";
            pProcess.StartInfo.Arguments = "-a " + ip;
            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.StartInfo.CreateNoWindow = true;
            pProcess.Start();
            string strOutput = pProcess.StandardOutput.ReadToEnd();
            string[] substrings = strOutput.Split('-');
            if (substrings.Length >= 8)
            {
                macAddress = substrings[3].Substring(Math.Max(0, substrings[3].Length - 2))
                    + "-" + substrings[4] + "-" + substrings[5] + "-" + substrings[6]
                    + "-" + substrings[7] + "-"
                    + substrings[8].Substring(0, 2);
                if (macAddress.StartsWith("DC-4F-22", StringComparison.OrdinalIgnoreCase)) return true;
                else return false;
            }
            else
                return false;
        }

        async static Task<bool> OffGPIOs(IPAddress ip)
        {
            List<HttpResponseMessage> responses = new List<HttpResponseMessage>();
            using (HttpClient client = new HttpClient())
            {
                foreach (var gpio in listGPIOs)
                    responses.Add(await client.GetAsync($"http://{ip}/control?cmd=GPIO,{gpio},0"));
            }
            if (responses.Any(r => !r.IsSuccessStatusCode)) return false;
            else return true;
        }
        async static Task<bool> OnGPIOs(IPAddress ip)
        {
            List<HttpResponseMessage> responses = new List<HttpResponseMessage>();
            using (HttpClient client = new HttpClient())
            {
                foreach (var gpio in listGPIOs)
                    responses.Add(await client.GetAsync($"http://{ip}/control?cmd=GPIO,{gpio},1"));
            }
            if (responses.Any(r => !r.IsSuccessStatusCode)) return false;
            else return true;
        }
        async static Task<bool> SwitchGPIO(IPAddress ip, int GPIO, int State)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            using (HttpClient client = new HttpClient())
            {
                response = await client.GetAsync($"http://{ip}/control?cmd=GPIO,{GPIO},{State}");
            }
            if (!response.IsSuccessStatusCode) return false;
            else return true;
        }
        async static Task<bool> PulseGPIO(IPAddress ip, int GPIO, int ms)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            using (HttpClient client = new HttpClient())
            {
                response = await client.GetAsync($"http://{ip}/control?cmd=Pulse,{GPIO},{ms}");
            }
            if (!response.IsSuccessStatusCode) return false;
            else return true;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Inserisci IP ESP");
            var ip = IPAddress.TryParse(Console.ReadLine(), out IPAddress tryip) ? tryip : IPAddress.Any;


            if (ip == IPAddress.Any || !IsESP(ip))
                Console.WriteLine("IP inserito non corretto o non corrisponde a un ESP");

            while (true)
            {

                Console.WriteLine('\n');
                foreach (var kv in dicCommands)
                    Console.WriteLine($"{kv.Key} - {kv.Value}");

                var scelta = int.TryParse(Console.ReadLine(), out int res) ? res : 0;

                switch (scelta)
                {
                    default:
                        Console.WriteLine("Scelta non presente");
                        break;
                    case 0:
                        Console.WriteLine("Inserire un numero");
                        break;
                    case 1:
                        if (OffGPIOs(ip).Result) Console.WriteLine("GPIOs spente");
                        else Console.WriteLine("Errore");
                        break;

                    case 2:
                        Console.WriteLine("Inserisci GPIO");
                        var gpioswitch = int.TryParse(Console.ReadLine(), out int trygpio) ? trygpio : -1;
                        if (gpioswitch == -1) { Console.WriteLine("Numero non valido"); break; }
                        Console.WriteLine("Inserisci stato");
                        var state = int.TryParse(Console.ReadLine(), out int trystate) ? trystate : -1;
                        if (state == -1) { Console.WriteLine("Stato non valido"); break; }
                        if (SwitchGPIO(ip, gpioswitch, state).Result) Console.WriteLine("GPIO switchata");
                        else Console.WriteLine("Errore");
                        break;

                    case 3:
                        if (OnGPIOs(ip).Result) Console.WriteLine("GPIOs accese");
                        else Console.WriteLine("Errore");
                        break;
                    case 4:
                        Console.WriteLine("Inserisci GPIO");
                        var gpio = int.TryParse(Console.ReadLine(), out int trygpiopulse) ? trygpiopulse : -1;
                        if (gpio == -1) { Console.WriteLine("Numero non valido"); break; }
                        Console.WriteLine("Inserisci stato");
                        var ms = int.TryParse(Console.ReadLine(), out int tryms) ? tryms : -1;
                        if (ms == -1) { Console.WriteLine("Durata non valida"); break; }
                        if (PulseGPIO(ip, gpio, ms).Result) Console.WriteLine("GPIO in pulse");
                        else Console.WriteLine("Errore");
                        break;
                    case 5:
                        Console.Clear();
                        break;
                    case 6:
                        Thread.CurrentThread.Abort();
                        break;
                }
            }
        }
    }
}
