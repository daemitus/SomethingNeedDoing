using System;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using ImGuiNET;
using SomethingNeedDoing.Exceptions;
using SomethingNeedDoing.Managers;

namespace SomethingNeedDoing.Interface
{
    /// <summary>
    /// Main window for macro execution.
    /// </summary>
    internal class MacroWindow : Window
    {
        private readonly Regex incrementalName = new(@"(?<all> \((?<index>\d+)\))$", RegexOptions.Compiled);

        private INode? draggedNode = null;
        private MacroNode? activeMacroNode = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="MacroWindow"/> class.
        /// </summary>
        public MacroWindow()
            : base("Something Need Doing")
        {
            this.Size = new Vector2(525, 600);
            this.SizeCondition = ImGuiCond.FirstUseEver;
            this.RespectCloseHotkey = false;
        }

        private static FolderNode RootFolder => Service.Configuration.RootFolder;

        /// <inheritdoc/>
        public override void PreDraw()
        {
            ImGui.PushStyleColor(ImGuiCol.ResizeGrip, 0);
        }

        /// <inheritdoc/>
        public override void PostDraw()
        {
            ImGui.PopStyleColor();
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            ImGui.Columns(2);

            this.DisplayNode(RootFolder);

            ImGui.NextColumn();

            this.DisplayRunningMacros();

            this.DisplayMacroEdit();

            ImGui.Columns(1);
        }

        #region node tree

        private void DisplayNode(INode node)
        {
            ImGui.PushID(node.Name);

            if (node is FolderNode folderNode)
            {
                this.DisplayFolderNode(folderNode);
            }
            else if (node is MacroNode macroNode)
            {
                this.DisplayMacroNode(macroNode);
            }

            ImGui.PopID();
        }

        private void DisplayMacroNode(MacroNode node)
        {
            var flags = ImGuiTreeNodeFlags.Leaf;
            if (node == this.activeMacroNode)
            {
                flags |= ImGuiTreeNodeFlags.Selected;
            }

            ImGui.TreeNodeEx($"{node.Name}##tree", flags);

            this.NodePopup(node);
            this.NodeDragDrop(node);

            if (ImGui.IsItemClicked())
            {
                this.activeMacroNode = node;
            }

            ImGui.TreePop();
        }

        private void DisplayFolderNode(FolderNode node)
        {
            if (node == RootFolder)
            {
                ImGui.SetNextItemOpen(true, ImGuiCond.FirstUseEver);
            }

            var expanded = ImGui.TreeNodeEx($"{node.Name}##tree");

            this.NodePopup(node);
            this.NodeDragDrop(node);

            if (expanded)
            {
                foreach (var childNode in node.Children.ToArray())
                {
                    this.DisplayNode(childNode);
                }

                ImGui.TreePop();
            }
        }

        private string GetUniqueNodeName(string name)
        {
            var nodeNames = Service.Configuration.GetAllNodes().Select(node => node.Name).ToList();
            while (nodeNames.Contains(name))
            {
                Match match = this.incrementalName.Match(name);
                if (match.Success)
                {
                    var all = match.Groups["all"].Value;
                    var index = int.Parse(match.Groups["index"].Value);
                    name = name[..^all.Length];
                    name = $"{name} ({index + 1})";
                }
                else
                {
                    name = $"{name} (1)";
                }
            }

            return name.Trim();
        }

        private void NodePopup(INode node)
        {
            if (ImGui.BeginPopupContextItem($"##{node.Name}-popup"))
            {
                var name = node.Name;
                if (ImGui.InputText($"##rename", ref name, 100, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    node.Name = this.GetUniqueNodeName(name);
                    Service.Configuration.Save();
                }

                if (node is MacroNode macroNode)
                {
                    if (ImGuiEx.IconButton(FontAwesomeIcon.Play, "Run"))
                    {
                        this.RunMacro(macroNode);
                    }
                }

                if (node is FolderNode folderNode)
                {
                    if (ImGuiEx.IconButton(FontAwesomeIcon.Plus, "Add macro"))
                    {
                        var newNode = new MacroNode { Name = this.GetUniqueNodeName("Untitled macro") };
                        folderNode.Children.Add(newNode);
                        Service.Configuration.Save();
                    }

                    ImGui.SameLine();
                    if (ImGuiEx.IconButton(FontAwesomeIcon.FolderPlus, "Add folder"))
                    {
                        var newNode = new FolderNode { Name = this.GetUniqueNodeName("Untitled folder") };
                        folderNode.Children.Add(newNode);
                        Service.Configuration.Save();
                    }
                }

                if (node != RootFolder)
                {
                    ImGui.SameLine();
                    if (ImGuiEx.IconButton(FontAwesomeIcon.Copy, "Copy Name"))
                    {
                        ImGui.SetClipboardText(node.Name);
                    }

                    ImGui.SameLine();
                    if (ImGuiEx.IconButton(FontAwesomeIcon.TrashAlt, "Delete"))
                    {
                        if (Service.Configuration.TryFindParent(node, out var parentNode))
                        {
                            parentNode!.Children.Remove(node);
                            Service.Configuration.Save();
                        }
                    }

                    ImGui.SameLine();
                }

                ImGui.EndPopup();
            }
        }

        private void NodeDragDrop(INode node)
        {
            if (node != RootFolder)
            {
                if (ImGui.BeginDragDropSource())
                {
                    this.draggedNode = node;
                    ImGui.Text(node.Name);
                    ImGui.SetDragDropPayload("NodePayload", IntPtr.Zero, 0);
                    ImGui.EndDragDropSource();
                }
            }

            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload("NodePayload");

                bool nullPtr;
                unsafe
                {
                    nullPtr = payload.NativePtr == null;
                }

                var targetNode = node;
                if (!nullPtr && payload.IsDelivery() && this.draggedNode != null)
                {
                    if (Service.Configuration.TryFindParent(this.draggedNode, out var draggedNodeParent))
                    {
                        if (targetNode is FolderNode targetFolderNode)
                        {
                            draggedNodeParent!.Children.Remove(this.draggedNode);
                            targetFolderNode.Children.Add(this.draggedNode);
                            Service.Configuration.Save();
                        }
                        else
                        {
                            if (Service.Configuration.TryFindParent(targetNode, out var targetNodeParent))
                            {
                                var targetNodeIndex = targetNodeParent!.Children.IndexOf(targetNode);
                                if (targetNodeParent == draggedNodeParent)
                                {
                                    var draggedNodeIndex = targetNodeParent.Children.IndexOf(this.draggedNode);
                                    if (draggedNodeIndex < targetNodeIndex)
                                    {
                                        targetNodeIndex -= 1;
                                    }
                                }

                                draggedNodeParent!.Children.Remove(this.draggedNode);
                                targetNodeParent.Children.Insert(targetNodeIndex, this.draggedNode);
                                Service.Configuration.Save();
                            }
                            else
                            {
                                throw new Exception($"Could not find parent of node \"{targetNode.Name}\"");
                            }
                        }
                    }
                    else
                    {
                        throw new Exception($"Could not find parent of node \"{this.draggedNode.Name}\"");
                    }

                    this.draggedNode = null;
                }

                ImGui.EndDragDropTarget();
            }
        }

        private void RunMacro(MacroNode node)
        {
            try
            {
                Service.MacroManager.EnqueueMacro(node);
            }
            catch (MacroSyntaxError ex)
            {
                Service.ChatManager.PrintError($"[SND] {ex.Message}");
            }
            catch (Exception ex)
            {
                Service.ChatManager.PrintError($"[SND] Unexpected error");
                PluginLog.Error(ex, "Unexpected error");
            }
        }

        #endregion

        #region running macros

        private void DisplayRunningMacros()
        {
            ImGui.Text("Macro Queue");

            var state = Service.MacroManager.State;

            var stateName = state switch
            {
                LoopState.NotLoggedIn => "Not Logged In",
                LoopState.Running when Service.MacroManager.PauseAtLoop => "Pausing Soon",
                LoopState.Running when Service.MacroManager.StopAtLoop => "Stopping Soon",
                _ => Enum.GetName(state),
            };

            Vector4 buttonCol;
            unsafe
            {
                buttonCol = *ImGui.GetStyleColorVec4(ImGuiCol.Button);
            }

            ImGui.PushStyleColor(ImGuiCol.ButtonActive, buttonCol);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, buttonCol);
            ImGui.Button($"{stateName}##LoopState", new Vector2(100, 0));
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();

            ImGui.SameLine();
            if (ImGuiEx.IconButton(FontAwesomeIcon.QuestionCircle, "Help"))
                Service.Plugin.OpenHelpWindow();

            if (Service.MacroManager.State == LoopState.NotLoggedIn)
            { /* Nothing to do */
            }
            else if (Service.MacroManager.State == LoopState.Stopped)
            { /* Nothing to do */
            }
            else if (Service.MacroManager.State == LoopState.Waiting)
            { /* Nothing to do */
            }
            else if (Service.MacroManager.State == LoopState.Paused)
            {
                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.Play, "Resume"))
                    Service.MacroManager.Resume();

                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.StepForward, "Step"))
                    Service.MacroManager.NextStep();

                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.TrashAlt, "Clear"))
                    Service.MacroManager.Stop();
            }
            else if (Service.MacroManager.State == LoopState.Running)
            {
                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.Pause, "Pause (hold control to pause at next /loop)"))
                {
                    var io = ImGui.GetIO();
                    var ctrlHeld = io.KeyCtrl;

                    Service.MacroManager.Pause(ctrlHeld);
                }

                ImGui.SameLine();
                if (ImGuiEx.IconButton(FontAwesomeIcon.Stop, "Stop (hold control to stop at next /loop)"))
                {
                    var io = ImGui.GetIO();
                    var ctrlHeld = io.KeyCtrl;

                    Service.MacroManager.Stop(ctrlHeld);
                }
            }

            ImGui.PushItemWidth(-1);

            var style = ImGui.GetStyle();
            var runningHeight = (ImGui.CalcTextSize("CalcTextSize").Y * ImGuiHelpers.GlobalScale * 3) + (style.FramePadding.Y * 2) + (style.ItemSpacing.Y * 2);
            if (ImGui.BeginListBox("##running-macros", new Vector2(-1, runningHeight)))
            {
                var macroStatus = Service.MacroManager.MacroStatus;
                for (int i = 0; i < macroStatus.Length; i++)
                {
                    var (name, stepIndex) = macroStatus[i];
                    string text = name;
                    if (i == 0 || stepIndex > 1)
                        text += $" (step {stepIndex})";
                    ImGui.Selectable($"{text}##{Guid.NewGuid()}", i == 0);
                }

                ImGui.EndListBox();
            }

            var contentHeight = (ImGui.CalcTextSize("CalcTextSize").Y * ImGuiHelpers.GlobalScale * 5) + (style.FramePadding.Y * 2) + (style.ItemSpacing.Y * 4);
            var macroContent = Service.MacroManager.CurrentMacroContent();
            if (ImGui.BeginListBox("##current-macro", new Vector2(-1, contentHeight)))
            {
                var stepIndex = Service.MacroManager.CurrentMacroStep();
                if (stepIndex == -1)
                {
                    ImGui.Selectable("Looping", true);
                }
                else
                {
                    for (int i = stepIndex; i < macroContent.Length; i++)
                    {
                        var step = macroContent[i];
                        var isCurrentStep = i == stepIndex;
                        ImGui.Selectable(step, isCurrentStep);
                    }
                }

                ImGui.EndListBox();
            }

            ImGui.PopItemWidth();
        }

        #endregion

        #region macro edit

        private void DisplayMacroEdit()
        {
            var node = this.activeMacroNode;
            if (node is null)
                return;

            ImGui.Text("Macro Editor");

            if (ImGuiEx.IconButton(FontAwesomeIcon.Play, "Run"))
            {
                this.RunMacro(node);
            }

            ImGui.SameLine();
            if (ImGuiEx.IconButton(FontAwesomeIcon.FileImport, "Import from clipboard"))
            {
                var text = ImGui.GetClipboardText();

                // Replace \r with \r\n, usually from copy/pasting from the in-game macro window
                var rex = new Regex("\r(?!\n)", RegexOptions.Compiled);
                var matches = from Match match in rex.Matches(text)
                              let index = match.Index
                              orderby index descending
                              select index;
                foreach (var index in matches)
                    text = text.Remove(index, 1).Insert(index, "\r\n");

                node.Contents = text;
                Service.Configuration.Save();
            }

            ImGui.SameLine();
            if (ImGuiEx.IconButton(FontAwesomeIcon.TimesCircle, "Close"))
            {
                this.activeMacroNode = null;
            }

            ImGui.PushItemWidth(-1);
            ImGui.PushFont(UiBuilder.MonoFont);

            var contents = node.Contents;
            if (ImGui.InputTextMultiline($"##{node.Name}-editor", ref contents, 100_000, new Vector2(-1, -1)))
            {
                node.Contents = contents;
                Service.Configuration.Save();
            }

            ImGui.PopFont();
            ImGui.PopItemWidth();
        }

        #endregion
    }
}
