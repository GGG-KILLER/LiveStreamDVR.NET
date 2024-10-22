
using LiveStreamDVR.Api.Helpers;

namespace LiveStreamDVR.Api.Tests.Helpers;

public class CommandLineSplitterTests
{
    [Theory]
    [InlineData("-asd", new[] { "-asd" })]
    [InlineData("--something \"hello\" --else hi", new[] { "--something", "hello", "--else", "hi" })]
    [InlineData("--hello \"'there'\" --else hi ", new[] { "--hello", "'there'", "--else", "hi" })]
    [InlineData("--hello there\\ friend --how=\"are you doing\" --my='good'\\''friend'", new[] { "--hello", "there friend", "--how=are you doing", "--my=good'friend" })]
    [InlineData("          trimmed               properly                   ", new[] { "trimmed", "properly" })]
    public void SplitArguments_Should_ParseThingsCorrectly(string arguments, string[] expected)
    {
        // Setup

        // Act
        var parsed = CommandLineSplitter.SplitArguments(arguments);

        // Check
        Assert.Equal(expected, parsed);
    }
}
