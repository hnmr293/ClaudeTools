using System.Runtime.InteropServices;

namespace ClaudeTools.Cui;

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

        ClaudeTools.Toolize.KeySender.SendKeys(processName, textToSend);

        Console.WriteLine("Completed. Closing...");
    }
}