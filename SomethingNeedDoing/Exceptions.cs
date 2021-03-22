using System;

namespace SomethingNeedDoing
{
    internal class EffectNotPresentError : InvalidOperationException
    {
        public EffectNotPresentError(string message) : base(message) { }
    }

    internal class EventFrameworkTimeoutError : InvalidOperationException
    {
        public EventFrameworkTimeoutError(string message) : base(message) { }
    }

    internal class InvalidMacroOperationException : InvalidOperationException
    {
        public InvalidMacroOperationException(string message) : base(message) { }
    }
}
