using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;

class Program
{
    static void Main(string[] args)
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
            return;
        }

        string processName = args[0];
        string textToSend = args[1];

        Console.WriteLine($"Process name: '{processName}'");
        Console.WriteLine($"Text to send: '{textToSend}'");
        Console.WriteLine($"Current OS: {RuntimeInformation.OSDescription}");

        // Branch processing according to OS
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            KeySender.SendKeysOnWindows(processName, textToSend);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            KeySender.SendKeysOnLinux(processName, textToSend);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            KeySender.SendKeysOnMacOS(processName, textToSend);
        }
        else
        {
            Console.WriteLine("Unsupported OS.");
        }

        Console.WriteLine("Completed. Press Enter key to exit.");
        Console.ReadLine();
    }
}