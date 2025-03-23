using ClaudeTools.Toolize;

[TestClass]
public class KeyInputParserTests
{
    #region Windows Tests

    [TestMethod]
    public void Windows_SimpleText_ParsedCorrectly()
    {
        // Arrange
        string input = "Hello world";
        string expected = "Hello world";

        // Act
        string[] result = KeyInputParser.ParseForOS(input, KeyInputParser.OSType.Windows);

        // Assert
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(expected, result[0]);
    }

    [TestMethod]
    public void Windows_NewlineEscapeSequence_ConvertedToEnter()
    {
        // Arrange
        string input = "Line1\\nLine2";
        string expected = "Line1{ENTER}Line2";

        // Act
        string[] result = KeyInputParser.ParseForOS(input, KeyInputParser.OSType.Windows);

        // Assert
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(expected, result[0]);
    }

    [TestMethod]
    public void Windows_TabEscapeSequence_ConvertedToTab()
    {
        // Arrange
        string input = "Tab\\tCharacter";
        string expected = "Tab{TAB}Character";

        // Act
        string[] result = KeyInputParser.ParseForOS(input, KeyInputParser.OSType.Windows);

        // Assert
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(expected, result[0]);
    }

    [TestMethod]
    public void Windows_BackslashEscapeSequence_ConvertedToBackslash()
    {
        // Arrange
        string input = "Backslash\\\\Character";
        string expected = "Backslash\\Character";

        // Act
        string[] result = KeyInputParser.ParseForOS(input, KeyInputParser.OSType.Windows);

        // Assert
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(expected, result[0]);
    }

    [TestMethod]
    public void Windows_CombinedCRLFEscapeSequence_ConvertedToEnter()
    {
        // Arrange
        string input = "Combined\\r\\nSequence";
        string expected = "Combined{ENTER}Sequence";

        // Act
        string[] result = KeyInputParser.ParseForOS(input, KeyInputParser.OSType.Windows);

        // Assert
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(expected, result[0]);
    }

    [TestMethod]
    public void Windows_ActualNewline_ConvertedToEnter()
    {
        // Arrange
        string input = "Line1\nLine2";
        string expected = "Line1{ENTER}Line2";

        // Act
        string[] result = KeyInputParser.ParseForOS(input, KeyInputParser.OSType.Windows);

        // Assert
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(expected, result[0]);
    }

    [TestMethod]
    public void Windows_ActualCRLF_ConvertedToEnter()
    {
        // Arrange
        string input = "Line1\r\nLine2";
        string expected = "Line1{ENTER}Line2";

        // Act
        string[] result = KeyInputParser.ParseForOS(input, KeyInputParser.OSType.Windows);

        // Assert
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(expected, result[0]);
    }

    [TestMethod]
    public void Windows_CurlyBrackets_Escaped()
    {
        // Arrange
        string input = "Special {brackets}";
        string expected = "Special {{}brackets{}}";

        // Act
        string[] result = KeyInputParser.ParseForOS(input, KeyInputParser.OSType.Windows);

        // Assert
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(expected, result[0]);
    }

    [TestMethod]
    public void Windows_SpecialCharacters_Escaped()
    {
        // Test various special characters
        TestWindowsSpecialChar("Plus+Sign", "Plus{+}Sign");
        TestWindowsSpecialChar("Caret^Character", "Caret{^}Character");
        TestWindowsSpecialChar("Percent%Character", "Percent{%}Character");
        TestWindowsSpecialChar("Tilde~Character", "Tilde{~}Character");
        TestWindowsSpecialChar("Parentheses()", "Parentheses{(}{)}");
        TestWindowsSpecialChar("Brackets[]", "Brackets{[}{]}");
    }

    private void TestWindowsSpecialChar(string input, string expected)
    {
        string[] result = KeyInputParser.ParseForOS(input, KeyInputParser.OSType.Windows);
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(expected, result[0]);
    }

    [TestMethod]
    public void Windows_EdgeCases_HandledCorrectly()
    {
        // Test trailing backslash
        TestWindowsSpecialChar("Trailing backslash\\", "Trailing backslash\\");

        // Test single backslash
        TestWindowsSpecialChar("\\", "\\");

        // Test \r without \n
        TestWindowsSpecialChar("\\r without \\n", "{ENTER} without {ENTER}");

        // Test empty string
        TestWindowsSpecialChar("", "");

        // Test complex mix
        TestWindowsSpecialChar("Complex\\nWith{Special}+Chars",
                              "Complex{ENTER}With{{}Special{}}{+}Chars");
    }

    #endregion

    #region Unix Tests

    [TestMethod]
    public void Unix_SimpleText_ReturnedAsIs()
    {
        // Arrange
        string input = "Hello world";
        string[] expected = new[] { "Hello world" };

        // Act
        string[] result = KeyInputParser.ParseForOS(input, KeyInputParser.OSType.Linux);

        // Assert
        CollectionAssert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Unix_NewlineEscapeSequence_ReturnedAsLiteral()
    {
        // Arrange
        string input = @"Line1\\nLine2";
        string[] expected = new[] { @"Line1\nLine2" };

        // Act
        string[] result = KeyInputParser.ParseForOS(input, KeyInputParser.OSType.Linux);

        // Assert
        CollectionAssert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Unix_TabEscapeSequence_ConvertedToTab()
    {
        // Arrange
        string input = "Tab\\tCharacter";
        string[] expected = new[] { "Tab\tCharacter" };

        // Act
        string[] result = KeyInputParser.ParseForOS(input, KeyInputParser.OSType.Linux);

        // Assert
        CollectionAssert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Unix_BackslashEscapeSequence_ConvertedToBackslash()
    {
        // Arrange
        string input = "Backslash\\\\Character";
        string[] expected = new[] { "Backslash\\Character" };

        // Act
        string[] result = KeyInputParser.ParseForOS(input, KeyInputParser.OSType.Linux);

        // Assert
        CollectionAssert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Unix_ActualNewline_SplitsIntoLines()
    {
        // Arrange
        string input = "Line1\nLine2";
        string[] expected = new[] { "Line1", "Line2" };

        // Act
        string[] result = KeyInputParser.ParseForOS(input, KeyInputParser.OSType.Linux);

        // Assert
        CollectionAssert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Unix_ActualCRLF_SplitsIntoLines()
    {
        // Arrange
        string input = "Line1\r\nLine2";
        string[] expected = new[] { "Line1", "Line2" };

        // Act
        string[] result = KeyInputParser.ParseForOS(input, KeyInputParser.OSType.Linux);

        // Assert
        CollectionAssert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Unix_MultipleNewlines_SplitsCorrectly()
    {
        // Arrange
        string input = "Line1\nLine2\nLine3";
        string[] expected = new[] { "Line1", "Line2", "Line3" };

        // Act
        string[] result = KeyInputParser.ParseForOS(input, KeyInputParser.OSType.Linux);

        // Assert
        CollectionAssert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Unix_EdgeCases_HandledCorrectly()
    {
        // Test trailing newline
        string[] result1 = KeyInputParser.ParseForOS("Trailing newline\n", KeyInputParser.OSType.Linux);
        CollectionAssert.AreEqual(new[] { "Trailing newline", "" }, result1);

        // Test leading newline
        string[] result2 = KeyInputParser.ParseForOS("\nLeading newline", KeyInputParser.OSType.Linux);
        CollectionAssert.AreEqual(new[] { "", "Leading newline" }, result2);

        // Test trailing backslash
        string[] result3 = KeyInputParser.ParseForOS("Trailing backslash\\", KeyInputParser.OSType.Linux);
        CollectionAssert.AreEqual(new[] { "Trailing backslash\\" }, result3);

        // Test empty string
        string[] result4 = KeyInputParser.ParseForOS("", KeyInputParser.OSType.Linux);
        CollectionAssert.AreEqual(new[] { "" }, result4);
    }

    #endregion
}
