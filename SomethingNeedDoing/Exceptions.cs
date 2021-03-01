using System;

namespace SomethingNeedDoing
{
    internal class InvalidMacroOperationException : InvalidOperationException
    {
        public InvalidMacroOperationException(string message) : base(message) { }
    }

    internal class InvalidClickException : InvalidOperationException
    {
        public InvalidClickException(string message) : base(message) { }
    }
}
