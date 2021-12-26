using Dalamud.Interface;
using ImGuiNET;

namespace SomethingNeedDoing.Interface
{
    /// <summary>
    /// ImGui extensions.
    /// </summary>
    internal static class ImGuiEx
    {
        /// <summary>
        /// An icon button.
        /// </summary>
        /// <param name="icon">Icon value.</param>
        /// <returns>Result from ImGui.Button.</returns>
        public static bool IconButton(FontAwesomeIcon icon) => IconButton(icon);

        /// <summary>
        /// An icon button.
        /// </summary>
        /// <param name="icon">Icon value.</param>
        /// <param name="tooltip">Simple tooltip.</param>
        /// <returns>Result from ImGui.Button.</returns>
        public static bool IconButton(FontAwesomeIcon icon, string tooltip)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            var result = ImGui.Button($"{icon.ToIconString()}##{icon.ToIconString()}-{tooltip}");
            ImGui.PopFont();

            if (tooltip != null)
                TextTooltip(tooltip);

            return result;
        }

        /// <summary>
        /// Show a simple text tooltip if hovered.
        /// </summary>
        /// <param name="text">Text to display.</param>
        public static void TextTooltip(string text)
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted(text);
                ImGui.EndTooltip();
            }
        }
    }
}
