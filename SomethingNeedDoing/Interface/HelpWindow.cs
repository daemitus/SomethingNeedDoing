using System;
using System.Collections.Generic;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace SomethingNeedDoing.Interface
{
    /// <summary>
    /// Help window for macro creation.
    /// </summary>
    internal class HelpWindow : Window
    {
        private readonly Vector4 shadedColor = new(0.68f, 0.68f, 0.68f, 1.0f);

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
                "Send an arbitrary keystroke. You can discover the valid names by using the \"KeyState\" view inside the \"/xldata\" window.",
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
                    "/ac \"Precise Touch\" <condition.good>",
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
            ImGui.PushFont(UiBuilder.MonoFont);

            ImGui.Text("2022-01-16");
            ImGui.PushStyleColor(ImGuiCol.Text, this.shadedColor);
            ImGui.TextWrapped(
                "- The help menu now has a /click listing.\n" +
                "- Various quality increasing skills are skipped when at 100% HQ. Please open an issue if you encounter issues with this. " +
                "- /loop # will reset after reaching the desired amount of loops. This allows for nested looping. You can test this with the following:\n" +
                "    /echo 111 <wait.1>\n" +
                "    /loop 1\n" +
                "    /echo 222 <wait.1>\n" +
                "    /loop 1\n" +
                "    /echo 333 <wait.1>\n");
            ImGui.PopStyleColor();
            ImGui.Separator();

            ImGui.Text("2022-01-01");
            ImGui.PushStyleColor(ImGuiCol.Text, this.shadedColor);
            ImGui.TextWrapped(
                "- Various /pcraft commands have been added. View the help menu for more details.\n" +
                "- There is also a help menu.\n");
            ImGui.PopStyleColor();

            ImGui.PopFont();
        }

        private void DrawOptions()
        {
            ImGui.PushFont(UiBuilder.MonoFont);

            var qualitySkip = Service.Configuration.QualitySkip;
            if (ImGui.Checkbox("Quality Skip", ref qualitySkip))
            {
                Service.Configuration.QualitySkip = qualitySkip;
                Service.Configuration.Save();
            }

            ImGui.PushStyleColor(ImGuiCol.Text, this.shadedColor);
            ImGui.TextWrapped("- Skip quality increasing actions when the HQ chance is at 100%. If you depend on durability increases from Manipulation towards the end of your macro, you will likely want to disable this.");
            ImGui.PopStyleColor();

            ImGui.PopFont();
        }

        private void DrawCommands()
        {
            ImGui.PushFont(UiBuilder.MonoFont);

            foreach (var (name, alias, desc, modifiers, examples) in this.commandData)
            {
                ImGui.Text($"/{name}");

                ImGui.PushStyleColor(ImGuiCol.Text, this.shadedColor);

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

                ImGui.PushStyleColor(ImGuiCol.Text, this.shadedColor);

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

                ImGui.PushStyleColor(ImGuiCol.Text, this.shadedColor);

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
    }
}
