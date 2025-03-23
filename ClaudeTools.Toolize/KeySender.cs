using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ClaudeTools.Toolize;

public class KeySender
{
    // Windows API declarations
    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    static extern uint GetCurrentThreadId();

    public static bool SendKeys(string processName, string text)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return SendKeysOnWindows(processName, text);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return SendKeysOnLinux(processName, text);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return SendKeysOnMacOS(processName, text);
        }
        else
        {
            Console.WriteLine("Unsupported OS.");
            return false;
        }
    }

    // Key input on Linux (using xdotool command as an example)
    [SupportedOSPlatform("linux")]
    public static bool SendKeysOnLinux(string processName, string text)
    {
        try
        {
            // Get process ID (simplified method)
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                Console.WriteLine($"Process '{processName}' was not found.");
                return false;
            }

            int pid = processes[0].Id;
            Console.WriteLine($"Linux: Sending text to process {processName} (PID: {pid})");

            // Use xdotool command to activate the window and send text
            // Note: xdotool must be installed
            string[] lines = KeyInputParser.ParseForOS(text, KeyInputParser.OSType.Linux);
            var command = $"-c \"xdotool search --pid {pid} windowactivate --sync && xdotool";
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                {
                    // send newline
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "bash",
                        Arguments = $"{command} key Return\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    })?.WaitForExit();
                }

                if (!string.IsNullOrEmpty(lines[i]))
                {
                    // send text
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "bash",
                        Arguments = $"{command} type '{lines[i]}'\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    })?.WaitForExit();
                }
            }

            Console.WriteLine("Linux: Text sending completed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred while sending keys in Linux environment: {ex.Message}");
            Console.WriteLine("Please make sure xdotool is installed.");
            return false;
        }
    }

    // Key input on macOS (using AppleScript)
    [SupportedOSPlatform("osx")]
    public static bool SendKeysOnMacOS(string processName, string text)
    {
        try
        {
            Console.WriteLine($"macOS: Sending text to process {processName}");

            // Activate application and send text using AppleScript
            string[] lines = KeyInputParser.ParseForOS(text, KeyInputParser.OSType.MacOS);
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                {
                    // send newline
                    string returnKeyScript = $"osascript -e 'tell application \"System Events\" to keystroke return'";
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "bash",
                        Arguments = $"-c \"{returnKeyScript}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    })?.WaitForExit();
                }

                if (!string.IsNullOrEmpty(lines[i]))
                {
                    // send text
                    string escapedText = lines[i].Replace("\"", "\\\"");
                    string script = $"osascript -e 'tell application \"{processName}\" to activate' -e 'tell application \"System Events\" to keystroke \"{escapedText}\"'";
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "bash",
                        Arguments = $"-c \"{script}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    })?.WaitForExit();
                }
            }

            Console.WriteLine("macOS: Text sending completed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred while sending keys in macOS environment: {ex.Message}");
            return false;
        }
    }

    // Windows-specific key sending
    [SupportedOSPlatform("windows")]
    public static bool SendKeysOnWindows(string processName, string text)
    {
        Console.WriteLine($"Windows: Sending text to process {processName}");

        // Search for the process
        Process[] processes = Process.GetProcessesByName(processName);
        if (processes.Length == 0)
        {
            Console.WriteLine($"Process '{processName}' was not found.");
            return false;
        }

        Process targetProcess = processes[0];
        IntPtr mainWindowHandle = targetProcess.MainWindowHandle;

        if (mainWindowHandle == IntPtr.Zero)
        {
            Console.WriteLine("Window handle is invalid.");
            return false;
        }

        // Attach thread input to ensure focus
        uint currentThreadId = GetCurrentThreadId();
        uint targetThreadId = GetWindowThreadProcessId(mainWindowHandle, out uint _);

        bool attachResult = false;
        try
        {
            // Attach input thread
            attachResult = AttachThreadInput(currentThreadId, targetThreadId, true);

            // Set to foreground
            SetForegroundWindow(mainWindowHandle);

            var escapedText = KeyInputParser.ParseForOS(text, KeyInputParser.OSType.Windows)[0];

            // Send text
            System.Windows.Forms.SendKeys.SendWait(escapedText);
        }
        finally
        {
            // Detach input thread
            if (attachResult)
            {
                AttachThreadInput(currentThreadId, targetThreadId, false);
            }
        }

        Console.WriteLine("Windows: Text sending completed");
        return true;
    }
}