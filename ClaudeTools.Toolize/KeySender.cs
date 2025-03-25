namespace ClaudeTools.Toolize;

public class KeySender
{
    public static bool SendKeys(string processName, string text, bool raw = false)
    {
        return KeySenderImpl.SendKeys(processName, text, raw);
    }
}
