using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Runtime.Versioning;

class KeySender
{
    // Windows API宣言
    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    static extern uint GetCurrentThreadId();

    public static void SendKeys(string processName, string text)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            SendKeysOnWindows(processName, text);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            SendKeysOnLinux(processName, text);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            SendKeysOnMacOS(processName, text);
        }
        else
        {
            Console.WriteLine("サポートされていないOSです。");
        }
    }

    // Linuxでのキー入力（例としてxdotoolコマンドを実行）
    [SupportedOSPlatform("linux")]
    public static void SendKeysOnLinux(string processName, string text)
    {
        try
        {
            // プロセスIDの取得（簡易的な方法）
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                Console.WriteLine($"プロセス '{processName}' が見つかりませんでした。");
                return;
            }

            int pid = processes[0].Id;
            Console.WriteLine($"Linux: プロセス {processName}（PID: {pid}）にテキストを送信します");

            // xdotoolコマンドを使用してウィンドウをアクティブにしテキストを送信
            // 注意: これにはxdotoolがインストールされている必要があります
            Process.Start(new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"xdotool search --pid {pid} windowactivate --sync && xdotool type '{text}'\"",
                UseShellExecute = false,
                CreateNoWindow = true
            })?.WaitForExit();

            Console.WriteLine("Linux: テキスト送信完了");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Linux環境でのキー送信中にエラーが発生しました: {ex.Message}");
            Console.WriteLine("xdotoolがインストールされているか確認してください。");
        }
    }

    // macOSでのキー入力（AppleScriptを使用）
    [SupportedOSPlatform("osx")]
    public static void SendKeysOnMacOS(string processName, string text)
    {
        try
        {
            Console.WriteLine($"macOS: プロセス {processName} にテキストを送信します");

            // AppleScriptを使用してアプリケーションをアクティブにしテキストを送信
            string escapedText = text.Replace("\"", "\\\"");
            string script = $"osascript -e 'tell application \"{processName}\" to activate' -e 'tell application \"System Events\" to keystroke \"{escapedText}\"'";

            Process.Start(new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"{script}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            })?.WaitForExit();

            Console.WriteLine("macOS: テキスト送信完了");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"macOS環境でのキー送信中にエラーが発生しました: {ex.Message}");
        }
    }

    // Windows専用のキー送信
    [SupportedOSPlatform("windows")]
    public static void SendKeysOnWindows(string processName, string text)
    {
        Console.WriteLine($"Windows: プロセス {processName} にテキストを送信します");

        // プロセスを検索
        Process[] processes = Process.GetProcessesByName(processName);
        if (processes.Length == 0)
        {
            Console.WriteLine($"プロセス '{processName}' が見つかりませんでした。");
            return;
        }

        Process targetProcess = processes[0];
        IntPtr mainWindowHandle = targetProcess.MainWindowHandle;

        if (mainWindowHandle == IntPtr.Zero)
        {
            Console.WriteLine("ウィンドウハンドルが無効です。");
            return;
        }

        // スレッドの入力をアタッチして確実にフォーカスを得る
        uint currentThreadId = GetCurrentThreadId();
        uint targetThreadId = GetWindowThreadProcessId(mainWindowHandle, out uint _);

        bool attachResult = false;
        try
        {
            // 入力スレッドをアタッチ
            attachResult = AttachThreadInput(currentThreadId, targetThreadId, true);

            // フォアグラウンドに設定
            SetForegroundWindow(mainWindowHandle);

            // 待機
            Thread.Sleep(500);

            // SendKeysの特殊文字をエスケープ
            string escapedText = text.Replace("{", "{{}").Replace("}", "{}}").Replace("+", "{+}")
                                .Replace("^", "{^}").Replace("%", "{%}").Replace("~", "{~}")
                                .Replace("(", "{(}").Replace(")", "{)}").Replace("[", "{[}")
                                .Replace("]", "{]}");

            // テキスト送信
            System.Windows.Forms.SendKeys.SendWait(escapedText);
        }
        finally
        {
            // 入力スレッドのデタッチ
            if (attachResult)
            {
                AttachThreadInput(currentThreadId, targetThreadId, false);
            }
        }

        Console.WriteLine("Windows: テキスト送信完了");
    }
}
