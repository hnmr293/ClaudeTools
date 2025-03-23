using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("KeySender起動しました。引数の数: " + args.Length);
        foreach (var arg in args)
        {
            Console.WriteLine($"引数: '{arg}'");
        }

        if (args.Length < 2)
        {
            Console.WriteLine("使用方法: dotnet run -- <アプリケーション名> <送信する文字列>");
            Console.WriteLine("例: dotnet run -- notepad こんにちは");
            return;
        }

        string processName = args[0];
        string textToSend = args[1];

        Console.WriteLine($"プロセス名: '{processName}'");
        Console.WriteLine($"送信テキスト: '{textToSend}'");
        Console.WriteLine($"現在のOS: {RuntimeInformation.OSDescription}");

        // OSに応じた処理の分岐
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
            Console.WriteLine("サポートされていないOSです。");
        }

        Console.WriteLine("完了しました。Enterキーを押すと終了します。");
        Console.ReadLine();
    }
}