using System.Text;

namespace ClaudeTools.Toolize;

/// <summary>
/// Parser class for handling escape sequences and converting strings for SendKeys
/// </summary>
public abstract class KeyInputParser
{
    /// <summary>
    /// Target OS types
    /// </summary>
    public enum OSType
    {
        Windows,
        Linux,
        MacOS
    }

    /// <summary>
    /// Processes escape sequences in a string and converts it to the appropriate format for each OS
    /// </summary>
    /// <param name="input">The string to process</param>
    /// <param name="osType">The target OS</param>
    /// <returns>List of strings split by newlines (or a single string for Windows)</returns>
    public static string[] ParseForOS(string input, OSType osType)
    {
        if (input == null)
            return Array.Empty<string>();

        if (osType == OSType.Windows)
        {
            // For Windows, return a single string escaped for SendKeys
            return new WindowsKeyInputParser().Parse(input);
        }
        else
        {
            // For Linux/macOS, return a list of strings split by newlines
            return new UnixKeyInputParser().Parse(input);
        }
    }

    protected abstract void OnChar(
        string input,
        int index,
        List<string> lines,
        StringBuilder currentLine,
        ReadOnlySpan<char> current
    );

    public virtual string[] Parse(string input)
    {
        var currentLine = new StringBuilder();
        var lines = new List<string>();

        Span<char> hexChar = stackalloc char[2];

        var index = 0;
        while (index < input.Length)
        {
            // エスケープシーケンスの検出
            if (input[index] == '\\' && index + 1 < input.Length)
            {
                char c = input[index + 1];

                switch (c)
                {
                    case 'n': // 改行
                        OnChar(input, index, lines, currentLine, "\n");
                        index += 2;
                        break;
                    case 'r': // キャリッジリターン
                        if (index + 3 < input.Length && input[index + 2] == '\\' && input[index + 3] == 'n')
                        {
                            OnChar(input, index, lines, currentLine, "\n");
                            index += 4;
                        }
                        else
                        {
                            OnChar(input, index, lines, currentLine, "\n");
                            index += 2;
                        }
                        break;
                    case 't': // 水平タブ
                        OnChar(input, index, lines, currentLine, "\t");
                        index += 2;
                        break;
                    case '\\': // バックスラッシュ
                        OnChar(input, index, lines, currentLine, "\\");
                        index += 2;
                        break;
                    case 'x': // 16進数エスケープ（2桁の16進数）
                        if (index + 4 < input.Length)
                        {
                            var hex = input.AsSpan()[(index + 2)..(index + 4)];
                            if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int code))
                            {
                                hexChar[0] = (char)code;
                                OnChar(input, index, lines, currentLine, hexChar[0..1]);
                                index += 4;
                            }
                            else
                            {
                                // 不正な16進数エスケープ
                                OnChar(input, index, lines, currentLine, "x");
                                index += 2;
                            }
                        }
                        else
                        {
                            // 文字列が短すぎる場合
                            OnChar(input, index, lines, currentLine, "x");
                            index += 2;
                        }
                        break;
                    case 'u': // Unicodeエスケープ（4桁の16進数）
                        if (index + 6 < input.Length)
                        {
                            var hex = input.AsSpan()[(index + 2)..(index + 6)];
                            if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int code))
                            {
                                hexChar[0] = (char)(code & 0xffff);
                                hexChar[1] = (char)(code >> 16);
                                if (code < 0x1_0000)
                                {
                                    OnChar(input, index, lines, currentLine, hexChar[0..1]);
                                }
                                else
                                {
                                    OnChar(input, index, lines, currentLine, hexChar);
                                }
                                index += 6;
                            }
                            else
                            {
                                // 不正なUnicodeエスケープ
                                OnChar(input, index, lines, currentLine, "u");
                                index += 2;
                            }
                        }
                        else
                        {
                            // 文字列が短すぎる場合
                            OnChar(input, index, lines, currentLine, "u");
                            index += 2;
                        }
                        break;
                    case 'U': // Unicodeエスケープ（8桁の16進数）
                        if (index + 10 < input.Length)
                        {
                            var hex = input.AsSpan()[(index + 2)..(index + 10)];
                            if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int code))
                            {
                                hexChar[0] = (char)(code & 0xffff);
                                hexChar[1] = (char)(code >> 16);
                                if (code < 0x1_0000)
                                {
                                    OnChar(input, index, lines, currentLine, hexChar[0..1]);
                                }
                                else
                                {
                                    OnChar(input, index, lines, currentLine, hexChar);
                                }
                                index += 10;
                            }
                            else
                            {
                                // 不正なUnicodeエスケープ
                                OnChar(input, index, lines, currentLine, "U");
                                index += 2;
                            }
                        }
                        else
                        {
                            // 文字列が短すぎる場合
                            OnChar(input, index, lines, currentLine, "U");
                            index += 2;
                        }
                        break;
                    default: // 認識できないエスケープシーケンス
                        OnChar(input, index, lines, currentLine, input.AsSpan()[(index + 1)..(index + 2)]);
                        index += 2;
                        break;
                }
            }
            else if (input[index] == '\r')
            {
                if (index + 1 < input.Length && input[index + 1] == '\n')
                {
                    OnChar(input, index, lines, currentLine, "\n");
                    index += 2;
                }
                else if (index + 2 < input.Length && input[index + 1] == '\\' && input[index + 2] == 'n')
                {
                    OnChar(input, index, lines, currentLine, "\n");
                    index += 3;
                }
                else
                {
                    OnChar(input, index, lines, currentLine, "\n");
                    index += 1;
                }
            }
            else
            {
                // 通常の文字
                OnChar(input, index, lines, currentLine, input.AsSpan()[index..(index + 1)]);
                index += 1;
            }
        }

        // 末尾に改行があれば最後に空文字列を足す
        lines.Add(currentLine.ToString());

        return lines.ToArray();
    }
}


public class WindowsKeyInputParser : KeyInputParser
{
    protected override void OnChar(string input, int index, List<string> lines, StringBuilder currentLine, ReadOnlySpan<char> current)
    {
        if (current[0] == '\n')
        {
            currentLine.Append("{ENTER}");
        }
        else if (current[0] == '\t')
        {
            currentLine.Append("{TAB}");
        }
        else
        {
            // Escape special characters for SendKeys
            // +, ^, %, ~, (, ), [, ], {, }
            if ("+^%~()[]{}".AsSpan().Contains(current, StringComparison.InvariantCulture))
            {
                currentLine.Append('{');
                currentLine.Append(current);
                currentLine.Append('}');
            }
            else
            {
                currentLine.Append(current);
            }
        }
    }
}


public class UnixKeyInputParser : KeyInputParser
{
    protected override void OnChar(string input, int index, List<string> lines, StringBuilder currentLine, ReadOnlySpan<char> current)
    {
        if (current[0] == '\n')
        {
            lines.Add(currentLine.ToString());
            currentLine.Clear();
        }
        else
        {
            currentLine.Append(current);
        }
    }
}
