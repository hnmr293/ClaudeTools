using System;
using System.Text;
using System.Collections.Generic;

namespace ClaudeTools.Toolize;

/// <summary>
/// Parser class for handling escape sequences and converting strings for SendKeys
/// </summary>
public static class KeyInputParser
{
    /// <summary>
    /// Processes escape sequences in a string and converts it to the appropriate format for each OS
    /// </summary>
    /// <param name="input">The string to process</param>
    /// <param name="osType">The target OS</param>
    /// <returns>List of strings split by newlines (or a single string for Windows)</returns>
    public static string[] ParseForOS(string input, OSType osType)
    {
        if (input == null)
            return new string[0];

        if (osType == OSType.Windows)
        {
            // For Windows, return a single string escaped for SendKeys
            return new string[] { ParseForWindows(input) };
        }
        else
        {
            // For Linux/macOS, return a list of strings split by newlines
            return ParseForUnixLike(input);
        }
    }

    /// <summary>
    /// Converts a string for Windows SendKeys
    /// </summary>
    /// <param name="input">The string to process</param>
    /// <returns>Escaped string for SendKeys</returns>
    private static string ParseForWindows(string input)
    {
        StringBuilder result = new StringBuilder();
        bool escaped = false;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (escaped)
            {
                // Process escaped characters
                switch (c)
                {
                    case 'n': // \n -> {ENTER}
                        result.Append("{ENTER}");
                        break;
                    case 'r': // \r is ignored (\r\n is processed as \n)
                        if (i + 1 < input.Length && input[i + 1] == '\n')
                        {
                            i++; // Skip \n
                        }
                        break;
                    case 't': // \t -> {TAB}
                        result.Append("{TAB}");
                        break;
                    case '\\': // \\ -> \
                        result.Append("\\");
                        break;
                    default: // Other escape sequences
                        result.Append('\\').Append(c);
                        break;
                }
                escaped = false;
            }
            else if (c == '\\')
            {
                // Enter escape mode when a backslash is detected
                escaped = true;
            }
            else if (c == '\r')
            {
                // Actual \r character is ignored
                if (i + 1 < input.Length && input[i + 1] == '\n')
                {
                    result.Append("{ENTER}");
                    i++; // Skip \n
                }
            }
            else if (c == '\n')
            {
                // Actual \n character is converted to {ENTER}
                result.Append("{ENTER}");
            }
            else
            {
                // Escape special characters for SendKeys
                switch (c)
                {
                    case '+':
                    case '^':
                    case '%':
                    case '~':
                    case '(':
                    case ')':
                    case '[':
                    case ']':
                        result.Append('{').Append(c).Append('}');
                        break;
                    case '{':
                        result.Append("{{");
                        break;
                    case '}':
                        result.Append("}}");
                        break;
                    default:
                        result.Append(c);
                        break;
                }
            }
        }

        // Handle case where string ends with an escape character
        if (escaped)
        {
            result.Append('\\');
        }

        return result.ToString();
    }

    /// <summary>
    /// Splits a string by newlines for Linux/macOS
    /// </summary>
    /// <param name="input">The string to process</param>
    /// <returns>List of strings split by newlines</returns>
    private static string[] ParseForUnixLike(string input)
    {
        StringBuilder currentLine = new StringBuilder();
        List<string> lines = new List<string>();
        bool escaped = false;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (escaped)
            {
                // Process escaped characters
                switch (c)
                {
                    case 'n': // \n - treated as a literal character, not a newline
                        currentLine.Append("\\n");
                        break;
                    case 'r': // \r - treated as a literal character, not a newline
                        currentLine.Append("\\r");
                        break;
                    case 't': // \t - add as a tab character
                        currentLine.Append("\t");
                        break;
                    case '\\': // \\ - add as a backslash
                        currentLine.Append("\\");
                        break;
                    default: // Other escape sequences
                        currentLine.Append('\\').Append(c);
                        break;
                }
                escaped = false;
            }
            else if (c == '\\')
            {
                // Enter escape mode when a backslash is detected
                escaped = true;
            }
            else if (c == '\r')
            {
                // Actual \r character is ignored
                if (i + 1 < input.Length && input[i + 1] == '\n')
                {
                    lines.Add(currentLine.ToString());
                    currentLine.Clear();
                    i++; // Skip \n
                }
            }
            else if (c == '\n')
            {
                // Actual \n character is treated as a line separator
                lines.Add(currentLine.ToString());
                currentLine.Clear();
            }
            else
            {
                // Normal characters are added as-is
                currentLine.Append(c);
            }
        }

        // Add the last line
        if (currentLine.Length > 0 || escaped)
        {
            if (escaped)
            {
                currentLine.Append('\\');
            }
            lines.Add(currentLine.ToString());
        }

        return lines.ToArray();
    }

    /// <summary>
    /// Target OS types
    /// </summary>
    public enum OSType
    {
        Windows,
        Linux,
        MacOS
    }
}