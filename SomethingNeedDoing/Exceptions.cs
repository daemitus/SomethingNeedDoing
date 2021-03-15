using System;

namespace SomethingNeedDoing
{
    internal class InvalidMacroOperationException : InvalidOperationException
    {
        public InvalidMacroOperationException(string message) : base(message) { }
    }
}
