using System.Linq;
using System.Runtime.CompilerServices;

using Eto.Parse;
using Eto.Parse.Parsers;

[assembly: InternalsVisibleTo("Test")]

namespace SomethingNeedDoing.Grammar
{
    /// <summary>
    /// A macro grammar definition.
    /// </summary>
    internal static class MacroGrammar
    {
        #region Common

        private static readonly Parser Digits = Terminals.Digit.Repeat();
        private static readonly Parser FloatingNumber = Digits & ('.' & Digits).Optional();

        private static readonly Parser Whitespace = Terminals.SingleLineWhiteSpace.Repeat();
        private static readonly Parser OptWhitespace = Whitespace.Optional();

        private static readonly Parser Eol = Terminals.Eol | Terminals.End;
        private static readonly Parser AnyUntilEol = Terminals.AnyChar.Except(Terminals.Eol).Repeat(0).Until(Eol);

        private static readonly Parser Quote = Terminals.Set('"');
        private static readonly Parser Ident = (Terminals.LetterOrDigit | "'" | "_").Repeat();

        private static readonly Parser Comment = (Terminals.Set('#') | "//") & AnyUntilEol.Named("comment");
        private static readonly Parser EolWhitespaceOrComment = OptWhitespace & Comment.Optional();

        #endregion

        #region Modifiers

        private static readonly Parser WaitModifier =
            ('<' & Literal("wait") & '.' & FloatDashFloat("wait", "until") & '>')
            .Named("waitModifier");

        private static readonly Parser MaxWaitModifier =
            ('<' & Literal("maxwait") & '.' & FloatingNumber.Named("maxWait") & '>')
            .Named("maxWaitModifier");

        private static readonly Parser UnsafeModifier =
            ('<' & Literal("unsafe") & '>')
            .Named("unsafeModifier");

        #endregion

        #region Parsers

        private static readonly Parser ActionParser =
            ((Literal("/action") | Literal("/ac"))
            & Whitespace & Identifier("actionName")
            & Modifiers(WaitModifier, UnsafeModifier)
            & EolWhitespaceOrComment)
            .Named("actionCommand");

        private static readonly Parser ClickParser =
            (Literal("/click")
            & Whitespace & Identifier("clickName")
            & Modifiers(WaitModifier)
            & EolWhitespaceOrComment)
            .Named("clickCommand");

        private static readonly Parser LoopParser =
            (Literal("/loop")
            & (Whitespace & Digits.Named("loopCount")).Optional()
            & Modifiers(WaitModifier)
            & EolWhitespaceOrComment)
            .Named("loopCommand");

        private static readonly Parser RequireParser =
            (Literal("/require")
            & Whitespace & Identifier("requireName")
            & Modifiers(WaitModifier, MaxWaitModifier)
            & EolWhitespaceOrComment)
            .Named("requireCommand");

        private static readonly Parser RunmacroParser =
            (Literal("/runmacro")
            & Whitespace & Identifier("macroName")
            & Modifiers(WaitModifier)
            & EolWhitespaceOrComment)
            .Named("runMacroCommand");

        private static readonly Parser SendParser =
            (Literal("/send")
            & Whitespace & Identifier("sendName")
            & Modifiers(WaitModifier)
            & EolWhitespaceOrComment)
            .Named("sendCommand");

        private static readonly Parser TargetParser =
            (Literal("/target")
            & Whitespace & Identifier("targetName")
            & Modifiers(WaitModifier)
            & EolWhitespaceOrComment)
            .Named("targetCommand");

        private static readonly Parser WaitParser =
            (Literal("/wait")
            & Whitespace & FloatDashFloat("wait", "until")
            & Modifiers(WaitModifier)
            & EolWhitespaceOrComment)
            .Named("waitCommand");

        private static readonly Parser WaitAddonParser =
            (Literal("/waitaddon")
            & Whitespace & Identifier("addonName")
            & Modifiers(WaitModifier, MaxWaitModifier)
            & EolWhitespaceOrComment)
            .Named("waitAddonCommand");

        private static readonly Parser NativeParser =
            (('/' & Terminals.AnyChar
                .Repeat().Until(Eol | (Whitespace & WaitModifier)))
                .Named("text")
            & Modifiers(WaitModifier)
            & EolWhitespaceOrComment)
            .Named("nativeCommand");

        #endregion

        #region Grammar

        /// <summary>
        /// Gets the macro grammar definition.
        /// </summary>
        public static Eto.Parse.Grammar Definition { get; } = new(
            Terminals.Start
            & (Whitespace | ActionParser | ClickParser | LoopParser | RequireParser | SendParser | TargetParser | WaitParser | WaitAddonParser | RunmacroParser | Comment | NativeParser)
                .Repeat(0).SeparatedBy(Terminals.Eol)
            & Terminals.End);

        #endregion

        #region Helpers

        private static Parser Literal(string value) => new LiteralTerminal(value) { CaseSensitive = false };

        private static Parser Identifier(string name) => Ident.Named(name) | (Quote & Ident.Repeat().SeparatedBy(Whitespace).Named(name) & Quote);

        private static Parser FloatDashFloat(string name1, string name2) => FloatingNumber.Named(name1) & ('-' & FloatingNumber.Named(name2)).Optional();

        private static Parser Modifiers(params Parser[] parsers) => (Whitespace & parsers.Aggregate((p1, p2) => p1 | p2)).Repeat(0);

        #endregion
    }
}