namespace ClaudeTools.Toolize;

public class KeySender
{
    public static bool SendKeys(string processName, List<InputKeys> inputKeys)
    {
        return KeySenderImpl.SendKeys(processName, inputKeys);
    }
}

public record InputKeys(string Keys, bool Raw = false);
