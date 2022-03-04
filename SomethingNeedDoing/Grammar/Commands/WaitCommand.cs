using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging;
using SomethingNeedDoing.Exceptions;

namespace SomethingNeedDoing.Grammar.Commands;

/// <summary>
/// The /wait command.
/// </summary>
internal class WaitCommand : MacroCommand
{
    private static readonly Regex Regex = new(@"^/wait\s+(?<wait>\d+(?:\.\d+)?)(?:-(?<until>\d+(?:\.\d+)?))?\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="WaitCommand"/> class.
    /// </summary>
    /// <param name="text">Original text.</param>
    /// <param name="wait">Wait value.</param>
    /// <param name="waitUntil">WaitUntil value.</param>
    private WaitCommand(string text, int wait, int waitUntil)
        : base(text, wait, waitUntil)
    {
    }

    /// <summary>
    /// Parse the text as a command.
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <returns>A parsed command.</returns>
    public static WaitCommand Parse(string text)
    {
        var match = Regex.Match(text);
        if (!match.Success)
            throw new MacroSyntaxError(text);

        var waitGroup = match.Groups["wait"];
        var waitValue = waitGroup.Value;
        var wait = (int)(float.Parse(waitValue, CultureInfo.InvariantCulture) * 1000);

        var untilGroup = match.Groups["until"];
        var untilValue = untilGroup.Success ? untilGroup.Value : "0";
        var until = (int)(float.Parse(untilValue, CultureInfo.InvariantCulture) * 1000);

        if (wait > until && until > 0)
            throw new ArgumentException("Wait value cannot be lower than the until value");

        return new WaitCommand(text, wait, until);
    }

    /// <inheritdoc/>
    public async override Task Execute(CancellationToken token)
    {
        PluginLog.Debug($"Executing: {this.Text}");

        await this.PerformWait(token);
    }
}
