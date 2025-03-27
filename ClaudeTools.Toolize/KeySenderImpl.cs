using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ClaudeTools.Toolize
{
    internal static class KeySenderImpl
    {
        private static void Log(string msg)
        {
            Console.WriteLine($"[KeySender] {msg}");
        }

#if WINDOWS
        // Windows API declarations
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        private class ClipboardManager : IDisposable
        {
            private Dictionary<string, object> saved = new();
            private bool disposedValue;

            public ClipboardManager()
            {
                var currentData = System.Windows.Forms.Clipboard.GetDataObject();
                if (currentData is not null)
                {
                    foreach (var format in currentData.GetFormats())
                    {
                        var formattedData = currentData.GetData(format);
                        if (formattedData is not null)
                        {
                            saved[format] = formattedData;
                        }
                    }
                }
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // do nothing here
                    }

                    try
                    {
                        var restored = new System.Windows.Forms.DataObject();
                        foreach (var pair in saved)
                        {
                            restored.SetData(pair.Key, pair.Value);
                        }
                        System.Windows.Forms.Clipboard.SetDataObject(restored, true);
                    }
                    catch (Exception e)
                    {
                        Log($"Error = {e.Message}");
                    }
                    finally
                    {
                        disposedValue = true;
                    }
                }
            }

            ~ClipboardManager()
            {
                Dispose(disposing: false);
            }

            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

        public static bool SendKeys(string processName, string? windowTitle, List<InputKeys> inputKeys)
        {
            Log($"Sending text to process {processName} - {windowTitle}");

            // Store original foreground window
            IntPtr originalForegroundWindow = GetForegroundWindow();

            // Search for the process
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                Log($"Process '{processName}' was not found.");
                return false;
            }

            Process targetProcess = processes[0];
            targetProcess.WaitForInputIdle(1000);

            IntPtr mainWindowHandle = targetProcess.MainWindowHandle;

            if (windowTitle is not null || mainWindowHandle == IntPtr.Zero)
            {
                var foundHandles = new List<IntPtr>();
                bool EnumWindowsCallback(IntPtr handle, IntPtr lParam)
                {
                    GetWindowThreadProcessId(handle, out uint processId);
                    if (processId == targetProcess.Id)
                    {
                        foundHandles.Add(handle);
                    }
                    return true; // continue enumeration
                }

                if (windowTitle is null)
                {
                    Log("handle is invalid.");

                    // show all window handles for debugging
                    EnumWindows(EnumWindowsCallback, IntPtr.Zero);
                    foreach (var handle in foundHandles)
                    {
                        Log($"handle = {handle:X16}");
                    }
                    return false;
                }
                else
                {
                    EnumWindows(EnumWindowsCallback, IntPtr.Zero);
                    IntPtr found = IntPtr.Zero;
                    foreach (var handle in foundHandles)
                    {
                        var title = new StringBuilder(256);
                        GetWindowText(handle, title, title.Capacity);
                        if (title.ToString().Contains(windowTitle))
                        {
                            found = handle;
                            break;
                        }
                    }

                    if (found != IntPtr.Zero)
                    {
                        mainWindowHandle = found;
                    }
                    else
                    {
                        Log($"Window '{windowTitle}' was not found.");
                        return false;
                    }
                }
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
                ShowWindow(mainWindowHandle, 9 /* SW_RESTORE */);
                SetForegroundWindow(mainWindowHandle);

                // Send text
                foreach (var key in inputKeys)
                {
                    if (key.Raw)
                    {
                        if (!SendKeysWithApi(processName, key.Keys, true))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!SendKeysWithClipboard(processName, key.Keys))
                        {
                            return false;
                        }
                    }
                }

                Log("completed");

                return true;
            }
            finally
            {
                // Detach input thread
                if (attachResult)
                {
                    AttachThreadInput(currentThreadId, targetThreadId, false);
                }

                // Restore original foreground window
                SetForegroundWindow(originalForegroundWindow);
            }
        }

        public static bool SendKeysWithApi(string processName, string text, bool raw = false)
        {
            var escapedText = raw ? text : KeyInputParser.ParseForOS(text, KeyInputParser.OSType.Windows)[0];
            Log($"Sending text (via SendKyes) '{escapedText}' (passed: `{text}`)");
            System.Windows.Forms.SendKeys.SendWait(escapedText);
            return true;
        }

        public static bool SendKeysWithClipboard(string processName, string text)
        {
            using var _ = new ClipboardManager();
            Log($"Sending text (via clipboard) '{text}'");
            System.Windows.Forms.Clipboard.SetText(text);
            System.Windows.Forms.SendKeys.SendWait("_{BS}^v");
            return true;
        }
#elif OSX
        public static bool SendKeys(string processName, string text, bool raw = false)
        {
            try
            {
                Console.WriteLine($"Sending text to process {processName}");

                // Activate application and send text using AppleScript
                string[] lines = raw ? new[] { text } : KeyInputParser.ParseForOS(text, KeyInputParser.OSType.MacOS);
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

                Console.WriteLine("completed");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while sending keys in macOS environment: {ex.Message}");
                return false;
            }
        }
#elif LINUX
        public static bool SendKeysOnLinux(string processName, string text, bool raw = false)
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
                Console.WriteLine($"Sending text to process {processName} (PID: {pid})");

                // Use xdotool command to activate the window and send text
                // Note: xdotool must be installed
                string[] lines = raw ? new[] { text } : KeyInputParser.ParseForOS(text, KeyInputParser.OSType.Linux);
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

                Console.WriteLine("completed");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while sending keys in Linux environment: {ex.Message}");
                Console.WriteLine("Please make sure xdotool is installed.");
                return false;
            }
        }
#endif
    }
}
