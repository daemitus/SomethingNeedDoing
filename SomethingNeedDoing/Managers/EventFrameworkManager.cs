using System;
using System.Runtime.InteropServices;
using System.Threading;

using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using SomethingNeedDoing.CraftingData;

namespace SomethingNeedDoing.Managers;

/// <summary>
/// Manager that handles the FFXIV Event Framework.
/// </summary>
internal class EventFrameworkManager : IDisposable
{
    // "48 8D 0D ?? ?? ?? ?? 48 8B AC 24 ?? ?? ?? ?? 33 C0";  // g_EventFramework + 0x44
    [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 54 24 ?? 56 57 41 56 48 83 EC 50", DetourName = nameof(EventFrameworkDetour))]
    private readonly Hook<EventFrameworkDelegate> eventFrameworkHook = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventFrameworkManager"/> class.
    /// </summary>
    public EventFrameworkManager()
    {
        SignatureHelper.Initialise(this);
        this.eventFrameworkHook.Enable();
    }

    private delegate IntPtr EventFrameworkDelegate(IntPtr a1, IntPtr a2, uint a3, ushort a4, IntPtr a5, IntPtr dataPtr, byte dataSize);

    /// <summary>
    /// Gets a waiter that is released when an action or crafting action is received through the Event Framework.
    /// </summary>
    public ManualResetEvent DataAvailableWaiter { get; } = new(false);

    /// <summary>
    /// Gets the crafting data received through the Event Framework.
    /// </summary>
    public CraftingState CraftingData { get; private set; } = default;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.eventFrameworkHook.Dispose();
        this.DataAvailableWaiter.Dispose();
    }

    private unsafe IntPtr EventFrameworkDetour(IntPtr a1, IntPtr a2, uint a3, ushort a4, IntPtr a5, IntPtr dataPtr, byte dataSize)
    {
        try
        {
            if (dataSize >= 4)
            {
                var dataType = *(ActionCategory*)dataPtr;
                if (dataType == ActionCategory.Action || dataType == ActionCategory.CraftAction)
                {
                    this.CraftingData = *(CraftingState*)dataPtr;
                    this.DataAvailableWaiter.Set();
                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Don't crash the game.");
        }

        return this.eventFrameworkHook.Original(a1, a2, a3, a4, a5, dataPtr, dataSize);
    }
}
