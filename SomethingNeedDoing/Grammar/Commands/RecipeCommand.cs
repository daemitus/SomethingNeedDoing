using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using SomethingNeedDoing.Exceptions;
using SomethingNeedDoing.Grammar.Modifiers;

namespace SomethingNeedDoing.Grammar.Commands;

/// <summary>
/// The /recipe command.
/// </summary>
internal class RecipeCommand : MacroCommand
{
    private static readonly Regex Regex = new(@"^/recipe\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly uint InternalAgentID = 23;

    private readonly string recipeName;

    [Signature("48 89 5C 24 ?? 57 48 83 EC 20 83 B9 ?? ?? ?? ?? ?? 8B FA 48 8B D9 0F 85 ?? ?? ?? ??")]
    private readonly OpenRecipeNoteDelegate openRecipeNoteFunc = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecipeCommand"/> class.
    /// </summary>
    /// <param name="text">Original text.</param>
    /// <param name="recipeName">Recipe name.</param>
    /// <param name="wait">Wait value.</param>
    private RecipeCommand(string text, string recipeName, WaitModifier wait)
        : base(text, wait)
    {
        SignatureHelper.Initialise(this);

        this.recipeName = recipeName;
    }

    private unsafe delegate void OpenRecipeNoteDelegate(FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentRecipeNote* agent, uint recipeId);

    /// <summary>
    /// Parse the text as a command.
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <returns>A parsed command.</returns>
    public static RecipeCommand Parse(string text)
    {
        _ = WaitModifier.TryParse(ref text, out var waitModifier);

        var match = Regex.Match(text);
        if (!match.Success)
            throw new MacroSyntaxError(text);

        var nameValue = ExtractAndUnquote(match, "name");

        return new RecipeCommand(text, nameValue, waitModifier);
    }

    /// <inheritdoc/>
    public async override Task Execute(CancellationToken token)
    {
        PluginLog.Debug($"Executing: {this.Text}");

        try
        {
            var recipeId = this.SearchRecipeId(this.recipeName);
            if (recipeId == 0)
               throw new MacroCommandError("Recipe not found");

            PluginLog.Debug($"RecipeId found : {recipeId}");
            this.OpenRecipeNote(recipeId);
        }
        catch (MacroCommandError)
        {
            throw;
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Unexpected click error");
            throw new MacroCommandError("Unexpected click error", ex);
        }

        await this.PerformWait(token);
    }

    private void OpenRecipeNote(uint recipeID)
    {
        unsafe
        {
            var uiModule = (FFXIVClientStructs.FFXIV.Client.UI.UIModule*)Service.GameGui.GetUIModule();
            if (uiModule == null)
                throw new MacroCommandError("UiModule not found");
            var agentModule = uiModule->GetAgentModule();
            if (agentModule == null)
                throw new MacroCommandError("AgentModule not found");
            var agent = (FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentRecipeNote*)agentModule->GetAgentByInternalID(InternalAgentID);
            if (agent == null)
                throw new MacroCommandError("RecipeNoteAgent not found");

            var internalRecipeID = 0x10000 + recipeID;
            this.openRecipeNoteFunc(agent, internalRecipeID);
        }
    }

    private uint SearchRecipeId(string recipeName)
    {
        var recipes = Service.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Recipe>()!;
        var founds = recipes.Where(r => r.ItemResult.Value?.Name.ToString() == recipeName).ToList();
        switch (founds.Count)
        {
            case 0: return 0;
            case 1: return founds[0].RowId;
            default:
                foreach (var recipe in founds)
                {
                    if (this.GetClassJobID(recipe) == Service.ClientState.LocalPlayer?.ClassJob.GameData?.RowId)
                    {
                        return recipe.RowId;
                    }
                }

                return founds[0].RowId;
        }
    }

    private uint GetClassJobID(Lumina.Excel.GeneratedSheets.Recipe recipe)
    {
        // Name       CraftType   ClassJob
        // Carpenter      0         8
        // Blacksmith     1         9
        // Armorer        2         10
        // Goldsmith :    3         11
        // Leatherworker  4         12
        // Weaver         5         13
        // Alchemist      6         14
        // Culinarian     7         15
        return recipe.CraftType.Value!.RowId + 8;
    }
}
