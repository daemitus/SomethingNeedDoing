using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Dalamud.Logging;
using NLua;
using SomethingNeedDoing.Exceptions;
using SomethingNeedDoing.Grammar;
using SomethingNeedDoing.Grammar.Commands;

namespace SomethingNeedDoing.Misc;

/// <summary>
/// A macro node queued for interaction.
/// </summary>
internal partial class ActiveMacro : IDisposable
{
    private Lua? lua;
    private LuaFunction? luaGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveMacro"/> class.
    /// </summary>
    /// <param name="node">The node to run.</param>
    public ActiveMacro(MacroNode node)
    {
        this.Node = node;

        if (node.IsLua)
        {
            this.Steps = new List<MacroCommand>();
            return;
        }

        var contents = ModifyMacroForCraftLoop(node.Contents, node.CraftingLoop, node.CraftLoopCount);
        this.Steps = MacroParser.Parse(contents).ToList();
    }

    /// <summary>
    /// Gets the underlying node.
    /// </summary>
    public MacroNode Node { get; private set; }

    /// <summary>
    /// Gets the command steps.
    /// </summary>
    public List<MacroCommand> Steps { get; private set; }

    /// <summary>
    /// Gets the current step number.
    /// </summary>
    public int StepIndex { get; private set; }

    /// <summary>
    /// Modify a macro for craft looping.
    /// </summary>
    /// <param name="contents">Contents of a macroNode.</param>
    /// <param name="craftLoop">A value indicating whether craftLooping is enabled.</param>
    /// <param name="craftCount">Amount to craftLoop.</param>
    /// <returns>The modified macro.</returns>
    public static string ModifyMacroForCraftLoop(string contents, bool craftLoop, int craftCount)
    {
        if (!craftLoop)
            return contents;

        if (Service.Configuration.UseCraftLoopTemplate)
        {
            var template = Service.Configuration.CraftLoopTemplate;

            if (craftCount == 0)
                return contents;

            if (craftCount == -1)
                craftCount = 999_999;

            if (!template.Contains("{{macro}}"))
                throw new MacroCommandError("CraftLoop template does not contain the {{macro}} placeholder");

            return template
                .Replace("{{macro}}", contents)
                .Replace("{{count}}", craftCount.ToString());
        }

        var maxwait = Service.Configuration.CraftLoopMaxWait;
        var maxwaitMod = maxwait > 0 ? $" <maxwait.{maxwait}>" : string.Empty;

        var echo = Service.Configuration.CraftLoopEcho;
        var echoMod = echo ? $" <echo>" : string.Empty;

        var craftGateStep = Service.Configuration.CraftLoopFromRecipeNote
            ? $"/craft {craftCount}{echoMod}"
            : $"/gate {craftCount - 1}{echoMod}";

        var clickSteps = string.Join("\n", new string[]
        {
            $@"/waitaddon ""RecipeNote""{maxwaitMod}",
            $@"/click ""synthesize""",
            $@"/waitaddon ""Synthesis""{maxwaitMod}",
        });

        var loopStep = $"/loop{echoMod}";

        var sb = new StringBuilder();

        if (Service.Configuration.CraftLoopFromRecipeNote)
        {
            if (craftCount == -1)
            {
                sb.AppendLine(clickSteps);
                sb.AppendLine(contents);
                sb.AppendLine(loopStep);
            }
            else if (craftCount == 0)
            {
                sb.AppendLine(contents);
            }
            else if (craftCount == 1)
            {
                sb.AppendLine(clickSteps);
                sb.AppendLine(contents);
            }
            else
            {
                sb.AppendLine(craftGateStep);
                sb.AppendLine(clickSteps);
                sb.AppendLine(contents);
                sb.AppendLine(loopStep);
            }
        }
        else
        {
            if (craftCount == -1)
            {
                sb.AppendLine(contents);
                sb.AppendLine(clickSteps);
                sb.AppendLine(loopStep);
            }
            else if (craftCount == 0 || craftCount == 1)
            {
                sb.AppendLine(contents);
            }
            else
            {
                sb.AppendLine(contents);
                sb.AppendLine(craftGateStep);
                sb.AppendLine(clickSteps);
                sb.AppendLine(loopStep);
            }
        }

        return sb.ToString().Trim();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.luaGenerator?.Dispose();
        this.lua?.Dispose();
    }

    /// <summary>
    /// Go to the next step.
    /// </summary>
    public void NextStep()
    {
        this.StepIndex++;
    }

    /// <summary>
    /// Loop.
    /// </summary>
    public void Loop()
    {
        if (this.Node.IsLua)
            throw new MacroCommandError("Loop is not supported for Lua scripts");

        this.StepIndex = -1;
    }

    /// <summary>
    /// Get the current step.
    /// </summary>
    /// <returns>A command.</returns>
    public MacroCommand? GetCurrentStep()
    {
        if (this.Node.IsLua)
        {
            if (this.lua == null)
                this.InitLuaScript();

            var results = this.luaGenerator!.Call();
            if (results.Length == 0)
                return null;

            if (results[0] is not string text)
                throw new MacroCommandError("Lua macro yielded a non-string");

            var command = MacroParser.ParseLine(text);

            if (command != null)
                this.Steps.Add(command);

            return command;
        }

        if (this.StepIndex < 0 || this.StepIndex >= this.Steps.Count)
            return null;

        return this.Steps[this.StepIndex];
    }

    private void InitLuaScript()
    {
        var script = this.Node.Contents
            .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
            .Select(line => $"  {line}")
            .Join('\n');

        static void RegisterClassMethods(Lua lua, object obj)
        {
            var type = obj.GetType();
            var isStatic = type.IsAbstract && type.IsSealed;
            var flags = BindingFlags.Public | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
            var methods = type.GetMethods(flags);
            foreach (var method in methods)
            {
                PluginLog.Debug($"Adding Lua method: {method.Name}");
                lua.RegisterFunction(method.Name, obj, method);
            }
        }

        this.lua = new Lua();
        this.lua.State.Encoding = Encoding.UTF8;
        this.lua.LoadCLRPackage();

        RegisterClassMethods(this.lua, CommandInterface.Instance);

        script = string.Format(EntrypointTemplate, script);

        this.lua.DoString(FStringSnippet);
        var results = this.lua.DoString(script);

        if (results.Length == 0 || results[0] is not LuaFunction coro)
            throw new MacroCommandError("Could not get Lua entrypoint.");

        this.luaGenerator = coro;
    }
}

/// <summary>
/// Lua code snippets.
/// </summary>
internal partial class ActiveMacro
{
    private const string EntrypointTemplate = @"
yield = coroutine.yield
--
function entrypoint()
{0}
end
--
return coroutine.wrap(entrypoint)";

    private const string FStringSnippet = @"
function f(str)
   local outer_env = _ENV
   return (str:gsub(""%b{}"", function(block)
      local code = block:match(""{(.*)}"")
      local exp_env = {}
      setmetatable(exp_env, { __index = function(_, k)
         local stack_level = 5
         while debug.getinfo(stack_level, """") ~= nil do
            local i = 1
            repeat
               local name, value = debug.getlocal(stack_level, i)
               if name == k then
                  return value
               end
               i = i + 1
            until name == nil
            stack_level = stack_level + 1
         end
         return rawget(outer_env, k)
      end })
      local fn, err = load(""return ""..code, ""expression `""..code..""`"", ""t"", exp_env)
      if fn then
         return tostring(fn())
      else
         error(err, 0)
      end
   end))
end";
}