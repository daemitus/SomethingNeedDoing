using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using SomethingNeedDoing.Exceptions;
using SomethingNeedDoing.Grammar.Modifiers;

using Sheets = Lumina.Excel.GeneratedSheets;

namespace SomethingNeedDoing.Grammar.Commands;

/// <summary>
/// The /recipe command.
/// </summary>
internal class RecipeCommand : MacroCommand
{
    private static readonly Regex Regex = new(@"^/recipe\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly string recipeName;

    [Signature("48 89 5C 24 ?? 57 48 83 EC 20 83 B9 ?? ?? ?? ?? ?? 8B FA 48 8B D9 0F 85 ?? ?? ?? ??")]
    private readonly unsafe delegate* unmanaged<AgentRecipeNote*, uint, void> openRecipeNote = null!;

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

        this.recipeName = recipeName.ToLowerInvariant();
    }

    private unsafe delegate void OpenRecipeNoteDelegate(AgentRecipeNote* agent, uint recipeId);

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

        var recipeId = this.SearchRecipeId(this.recipeName);
        if (recipeId == 0)
            throw new MacroCommandError("Recipe not found");

        PluginLog.Debug($"Recipe found: {recipeId}");
        this.OpenRecipeNote(recipeId);

        await this.PerformWait(token);
    }

    private unsafe void OpenRecipeNote(uint recipeID)
    {
        var agent = AgentRecipeNote.Instance();
        if (agent == null)
            throw new MacroCommandError("AgentRecipeNote not found");

        var internalRecipeID = recipeID + 0x10000;
        this.openRecipeNote(agent, internalRecipeID);
    }

    private uint SearchRecipeId(string recipeName)
    {
        var sheet = Service.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Recipe>()!;
        var recipes = sheet.Where(r => r.ItemResult.Value?.Name.ToString().ToLowerInvariant() == recipeName).ToList();

        switch (recipes.Count)
        {
            case 0: return 0;
            case 1: return recipes.First().RowId;
            default:
                var jobId = Service.ClientState.LocalPlayer?.ClassJob.Id;

                var recipe = recipes.Where(r => this.GetClassJobID(r) == jobId).FirstOrDefault();
                if (recipe == default)
                    return recipes.First().RowId;

                return recipe.RowId;
        }
    }

    private uint GetClassJobID(Sheets.Recipe recipe)
    {
        // Name           CraftType ClassJob
        // Carpenter      0         8
        // Blacksmith     1         9
        // Armorer        2         10
        // Goldsmith      3         11
        // Leatherworker  4         12
        // Weaver         5         13
        // Alchemist      6         14
        // Culinarian     7         15
        return recipe.CraftType.Value!.RowId + 8;
    }
}
