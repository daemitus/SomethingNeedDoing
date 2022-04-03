using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace SomethingNeedDoing.Interface;

/// <summary>
/// Help window for macro creation.
/// </summary>
internal class HelpWindow : Window
{
    private static readonly Vector4 ShadedColor = new(0.68f, 0.68f, 0.68f, 1.0f);

    private readonly (string Name, string? Alias, string Description, string[] Modifiers, string[] Examples)[] commandData = new[]
    {
        (
            "action", "ac",
            "Execute an action and wait for the server to respond.",
            new[] { "wait", "unsafe", "condition" },
            new[]
            {
                "/ac Groundwork",
                "/ac \"Tricks of the Trade\"",
            }),
        (
            "click", null,
            "Click a pre-defined button in an addon or window.",
            new[] { "wait" },
            new[]
            {
                "/click synthesize",
            }),
        (
            "loop", null,
            "Loop the current macro forever, or a certain amount of times.",
            new[] { "wait", "echo" },
            new[]
            {
                "/loop",
                "/loop 5",
            }),
        (
            "require", null,
            "Require a certain effect to be present before continuing.",
            new[] { "wait", "maxwait" },
            new[]
            {
                "/require \"Well Fed\"",
            }),
        (
            "requirestats", null,
            "Require a certain amount of stats effect to be present before continuing. Syntax is Craftsmanship, Control, then CP.",
            new[] { "wait", "maxwait" },
            new[]
            {
                "/requirestats 2700 2600 500",
            }),
        (
            "runmacro", null,
            "Start a macro from within another macro.",
            new[] { "wait" },
            new[]
            {
                "/runmacro \"Sub macro\"",
            }),
        (
            "send", null,
            "Send an arbitrary keystroke.",
            new[] { "wait" },
            new[]
            {
                "/send MULTIPLY",
                "/send NUMPAD0",
            }),
        (
            "target", null,
            "Target anyone and anything that can be selected.",
            new[] { "wait" },
            new[]
            {
                "/target Eirikur",
                "/target Moyce",
            }),
        (
            "waitaddon", null,
            "Wait for an addon, otherwise known as a UI component to be present. You can discover these names by using the \"Addon Inspector\" view inside the \"/xldata\" window.",
            new[] { "wait", "maxwait" },
            new[]
            {
                "/waitaddon RecipeNote",
            }),
        (
            "wait", null,
            "The same as the wait modifier, but as a command.",
            Array.Empty<string>(),
            new[]
            {
                "/wait 1-5",
            }),
    };

    private readonly (string Name, string Description, string[] Examples)[] modifierData = new[]
    {
        (
            "wait",
            "Wait a certain amount of time, or a random time within a range.",
            new[]
            {
                "/ac Groundwork <wait.3>       # Wait 3 seconds",
                "/ac Groundwork <wait.3.5>     # Wait 3.5 seconds",
                "/ac Groundwork <wait.1-5>     # Wait between 1 and 5 seconds",
                "/ac Groundwork <wait.1.5-5.5> # Wait between 1.5 and 5.5 seconds",
            }),
        (
            "maxwait",
            "For certain commands, the maximum time to wait for a certain state to be achieved. By default, this is 5 seconds.",
            new[]
            {
                "/waitaddon RecipeNote <maxwait.10>",
            }),
        (
            "condition",
            "Require a crafting condition to perform the action specified. This is taken from the Synthesis window and may be localized to your client language.",
            new[]
            {
                "/ac Observe <condition.poor>",
                "/ac \"Precise Touch\" <condition.good,excellent>",
                "/ac \"Byregot's Blessing\" <condition.not.poor>",
                "/ac \"Byregot's Blessing\" <condition.!poor>",
            }),
        (
            "unsafe",
            "Prevent the /action command from waiting for a positive server response and attempting to execute the command anyways.",
            new[]
            {
                "/ac \"Tricks of the Trade\" <unsafe>",
            }),
        (
            "echo",
            "Echo the amount of loops remaining after executing a /loop command.",
            new[]
            {
                "/loop 5 <echo>",
            }),
    };

    private readonly (string Name, string Description, string? Example)[] cliData = new[]
    {
        ("help", "Show this window.", null),
        ("run", "Run a macro, the name must be unique.", "/pcraft run MyMacro"),
        ("run loop #", "Run a macro and then loop N times, the name must be unique. Only the last /loop in the macro is replaced", "/pcraft run loop 5 MyMacro"),
        ("pause", "Pause the currently executing macro.", null),
        ("pause loop", "Pause the currently executing macro at the next /loop.", null),
        ("resume", "Resume the currently paused macro.", null),
        ("stop", "Clear the currently executing macro list.", null),
        ("stop loop", "Clear the currently executing macro list at the next /loop.", null),
    };

    private readonly List<string> clickNames;

    /// <summary>
    /// Initializes a new instance of the <see cref="HelpWindow"/> class.
    /// </summary>
    public HelpWindow()
        : base("Something Need Doing Help")
    {
        this.Flags |= ImGuiWindowFlags.NoScrollbar;

        this.Size = new Vector2(400, 600);
        this.SizeCondition = ImGuiCond.FirstUseEver;
        this.RespectCloseHotkey = false;

        this.clickNames = ClickLib.Click.GetClickNames();
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        if (ImGui.BeginTabBar("HelpTab"))
        {
            var tabs = new (string Title, Action Dele)[]
            {
                ("Changelog", this.DrawChangelog),
                ("Options", this.DrawOptions),
                ("Commands", this.DrawCommands),
                ("Modifiers", this.DrawModifiers),
                ("CLI", this.DrawCli),
                ("Clicks", this.DrawClicks),
                ("Sends", this.DrawVirtualKeys),
            };

            foreach (var (title, dele) in tabs)
            {
                if (ImGui.BeginTabItem(title))
                {
                    ImGui.BeginChild("scrolling", new Vector2(0, -1), false);

                    dele();

                    ImGui.EndChild();

                    ImGui.EndTabItem();
                }
            }

            ImGui.EndTabBar();
        }

        ImGui.EndChild();
    }

    private void DrawChangelog()
    {
        static void DisplayChangelog(string date, string changes, bool separator = true)
        {
            ImGui.Text(date);
            ImGui.PushStyleColor(ImGuiCol.Text, ShadedColor);
            ImGui.TextWrapped(changes);
            ImGui.PopStyleColor();

            if (separator)
                ImGui.Separator();
        }

        ImGui.PushFont(UiBuilder.MonoFont);

        DisplayChangelog(
            "2022-03-31",
            "- Added an option to disable monospaced font for JP users.\n");

        DisplayChangelog(
            "2022-03-03",
            "- Added an intelligent wait option that waits until your crafting action is complete, rather than what is in the <wait> modifier.\n" +
            "- Updated the <condition> modifier to accept a comma delimited list of names.\n");

        DisplayChangelog(
            "2022-02-02",
            "- Added /send help pane.\n" +
            "- Fixed /loop echo commands not being sent to the echo channel.\n");

        DisplayChangelog(
            "2022-01-30",
            "- Added a \"Step\" button to the control bar that lets you skip to the next step when a macro is paused.\n");

        DisplayChangelog(
            "2022-01-25",
            "- The help menu now has an options pane.\n" +
            "- Added an option to disable skipping craft actions when not crafting or at max progress.\n" +
            "- Added an option to disable the automatic quality increasing action skip, when at max quality.\n" +
            "- Added an option to treat /loop as the total iterations, rather than the amount to repeat.\n" +
            "- Added an option to always treat /loop commands as having an <echo> modifier.\n");

        DisplayChangelog(
            "2022-01-16",
            "- The help menu now has a /click listing.\n" +
            "- Various quality increasing skills are skipped when at max quality. Please open an issue if you encounter issues with this.\n" +
            "- /loop # will reset after reaching the desired amount of loops. This allows for nested looping. You can test this with the following:\n" +
            "    /echo 111 <wait.1>\n" +
            "    /loop 1\n" +
            "    /echo 222 <wait.1>\n" +
            "    /loop 1\n" +
            "    /echo 333 <wait.1>\n");

        DisplayChangelog(
            "2022-01-01",
            "- Various /pcraft commands have been added. View the help menu for more details.\n" +
            "- There is also a help menu.\n",
            false);

        ImGui.PopFont();
    }

    private void DrawOptions()
    {
        ImGui.PushFont(UiBuilder.MonoFont);

        static void DisplayOption(params string[] lines)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ShadedColor);

            foreach (var line in lines)
                ImGui.TextWrapped(line);

            ImGui.PopStyleColor();
        }

        #region CraftSkip

        var craftSkip = Service.Configuration.CraftSkip;
        if (ImGui.Checkbox("Craft Skip", ref craftSkip))
        {
            Service.Configuration.CraftSkip = craftSkip;
            Service.Configuration.Save();
        }

        DisplayOption("- Skip craft actions when not crafting.");

        #endregion
        #region SmartWait

        var smartWait = Service.Configuration.SmartWait;
        if (ImGui.Checkbox("Smart Wait", ref smartWait))
        {
            Service.Configuration.SmartWait = smartWait;
            Service.Configuration.Save();
        }

        DisplayOption("- Intelligently wait for crafting actions to complete instead of using the <wait> or <unsafe> modifiers.");

        #endregion
        #region QualitySkip

        var qualitySkip = Service.Configuration.QualitySkip;
        if (ImGui.Checkbox("Quality Skip", ref qualitySkip))
        {
            Service.Configuration.QualitySkip = qualitySkip;
            Service.Configuration.Save();
        }

        DisplayOption("- Skip quality increasing actions when the HQ chance is at 100%%. If you depend on durability increases from Manipulation towards the end of your macro, you will likely want to disable this.");

        #endregion
        #region LoopTotal

        var loopTotal = Service.Configuration.LoopTotal;
        if (ImGui.Checkbox("Loop Total", ref loopTotal))
        {
            Service.Configuration.LoopTotal = loopTotal;
            Service.Configuration.Save();
        }

        DisplayOption("- The numeric option provided to /loop will be considered as the total number of iterations, rather than the amount of times to loop. Internally, this will just subtract 1 from your /loop <amount> command.");

        #endregion
        #region LoopEcho

        var loopEcho = Service.Configuration.LoopEcho;
        if (ImGui.Checkbox("Loop Echo", ref loopEcho))
        {
            Service.Configuration.LoopEcho = loopEcho;
            Service.Configuration.Save();
        }

        DisplayOption("- Loop commands will always have an <echo> tag applied.");

        #endregion
        #region DisableMonospaced

        var disableMonospaced = Service.Configuration.DisableMonospaced;
        if (ImGui.Checkbox("Disable Monospaced fonts", ref disableMonospaced))
        {
            Service.Configuration.DisableMonospaced = disableMonospaced;
            Service.Configuration.Save();
        }

        DisplayOption("- Use the regular font instead of monospaced in the macro window. This may be handy for JP users so as to prevent missing unicode errors.");

        #endregion

        ImGui.PopFont();
    }

    private void DrawCommands()
    {
        ImGui.PushFont(UiBuilder.MonoFont);

        foreach (var (name, alias, desc, modifiers, examples) in this.commandData)
        {
            ImGui.Text($"/{name}");

            ImGui.PushStyleColor(ImGuiCol.Text, ShadedColor);

            if (alias != null)
                ImGui.Text($"- Alias: /{alias}");

            ImGui.TextWrapped($"- Description: {desc}");

            ImGui.Text("- Modifiers:");
            foreach (var mod in modifiers)
                ImGui.Text($"  - <{mod}>");

            ImGui.Text("- Examples:");
            foreach (var example in examples)
                ImGui.Text($"  - {example}");

            ImGui.PopStyleColor();

            ImGui.Separator();
        }

        ImGui.PopFont();
    }

    private void DrawModifiers()
    {
        ImGui.PushFont(UiBuilder.MonoFont);

        foreach (var (name, desc, examples) in this.modifierData)
        {
            ImGui.Text($"<{name}>");

            ImGui.PushStyleColor(ImGuiCol.Text, ShadedColor);

            ImGui.TextWrapped($"- Description: {desc}");

            ImGui.Text("- Examples:");
            foreach (var example in examples)
                ImGui.Text($"  - {example}");

            ImGui.PopStyleColor();

            ImGui.Separator();
        }

        ImGui.PopFont();
    }

    private void DrawCli()
    {
        ImGui.PushFont(UiBuilder.MonoFont);

        foreach (var (name, desc, example) in this.cliData)
        {
            ImGui.Text($"/pcraft {name}");

            ImGui.PushStyleColor(ImGuiCol.Text, ShadedColor);

            ImGui.TextWrapped($"- Description: {desc}");

            if (example != null)
            {
                ImGui.Text($"- Example: {example}");
            }

            ImGui.PopStyleColor();

            ImGui.Separator();
        }

        ImGui.PopFont();
    }

    private void DrawClicks()
    {
        ImGui.PushFont(UiBuilder.MonoFont);

        ImGui.TextWrapped("Refer to https://github.com/daemitus/ClickLib/tree/master/ClickLib/Clicks for any details.");
        ImGui.Separator();

        foreach (var name in this.clickNames)
        {
            ImGui.Text($"/click {name}");
        }

        ImGui.PopFont();
    }

    private void DrawVirtualKeys()
    {
        ImGui.PushFont(UiBuilder.MonoFont);

        ImGui.TextWrapped("Active keys will highlight green.");
        ImGui.Separator();

        var validKeys = Service.KeyState.GetValidVirtualKeys().ToHashSet();

        var names = Enum.GetNames<VirtualKey>();
        var values = Enum.GetValues<VirtualKey>();

        for (var i = 0; i < names.Length; i++)
        {
            var name = names[i];
            var vkCode = values[i];

            if (!validKeys.Contains(vkCode))
                continue;

            var isActive = Service.KeyState[vkCode];

            if (isActive)
                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.HealerGreen);

            ImGui.Text($"/send {name}");

            if (isActive)
                ImGui.PopStyleColor();
        }

        ImGui.PopFont();
    }
}
