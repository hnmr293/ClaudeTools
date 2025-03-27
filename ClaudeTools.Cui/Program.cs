using ClaudeTools.Toolize;
using System.Runtime.InteropServices;

namespace ClaudeTools.Cui;

class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        Console.WriteLine("KeySender started. Number of arguments: " + args.Length);
        foreach (var arg in args)
        {
            Console.WriteLine($"Argument: '{arg}'");
        }

        if (args.Length < 2)
        {
            Console.WriteLine("Usage: dotnet run -- <application name> <text to send>");
            Console.WriteLine("Example: dotnet run -- notepad hello");
            return 1;
        }

        string processName = args[0];
        string? windowTitle = null;
        var keys = new List<InputKeys>();
        var i = 1;

        if (args.Length >= 2 && (args[1] == "--window" || args[1] == "-w"))
        {
            if (args.Length == 2)
            {
                Console.WriteLine("-w/--window was specified but no window title was provided.");
                return 1;
            }
            windowTitle = args[2];
            i += 2;
        }

        while (i < args.Length)
        {
            var raw = false;
            if (args[i] == "--raw" || args[i] == "-r")
            {
                raw = true;
                i += 1;
                if (i == args.Length)
                {
                    Console.WriteLine($"Invalid argument: {string.Join(", ", args)}");
                    return 1;
                }
            }
            var key = args[i];
            keys.Add(new InputKeys(key, raw));
            i += 1;
        }

        Console.WriteLine($"Process name: '{processName}'");
        for (var j = 0; j < keys.Count; ++j)
        {
            Console.WriteLine($"Text to send {j}: '{keys[j].Keys}' (raw={keys[j].Raw})");
        }
        Console.WriteLine($"Current OS: {RuntimeInformation.OSDescription}");

        var success = KeySender.SendKeys(processName, windowTitle, keys);
        if (success)
        {
            Console.WriteLine("Completed. Closing...");
            return 0;
        }
        else
        {
            return 1;
        }
    }
}