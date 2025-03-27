namespace ClaudeTools.Toolize;

public class KeySender
{
    public static bool SendKeys(string processName, string? windowTitle, List<InputKeys> inputKeys)
    {
        return KeySenderImpl.SendKeys(processName, windowTitle, inputKeys);
    }
}

public record InputKeys(string Keys, bool Raw = false);
