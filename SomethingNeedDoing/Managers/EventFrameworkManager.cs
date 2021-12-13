using System;
using System.Runtime.InteropServices;
using System.Threading;

using Dalamud.Hooking;
using Dalamud.Logging;
using SomethingNeedDoing.CraftingData;

namespace SomethingNeedDoing.Managers
{
    /// <summary>
    /// Manager that handles the FFXIV Event Framework.
    /// </summary>
    internal class EventFrameworkManager : IDisposable
    {
        private readonly Hook<EventFrameworkDelegate> eventFrameworkHook;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventFrameworkManager"/> class.
        /// </summary>
        public EventFrameworkManager()
        {
            this.eventFrameworkHook = new Hook<EventFrameworkDelegate>(Service.Address.EventFrameworkFunctionAddress, this.EventFrameworkDetour);
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

        private IntPtr EventFrameworkDetour(IntPtr a1, IntPtr a2, uint a3, ushort a4, IntPtr a5, IntPtr dataPtr, byte dataSize)
        {
            try
            {
                if (dataSize >= 4)
                {
                    var dataType = (ActionCategory)Marshal.ReadInt32(dataPtr, 0);
                    if (dataType == ActionCategory.Action || dataType == ActionCategory.CraftAction)
                    {
                        this.CraftingData = Marshal.PtrToStructure<CraftingState>(dataPtr);
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
}
