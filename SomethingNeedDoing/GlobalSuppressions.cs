// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// General
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "Preventing long lines", Scope = "namespaceanddescendants", Target = "~N:SomethingNeedDoing")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1124:Do not use regions", Justification = "I like regions", Scope = "namespaceanddescendants", Target = "~N:SomethingNeedDoing")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1123:Do not place regions within elements", Justification = "I like regions in elements too", Scope = "namespaceanddescendants", Target = "~N:SomethingNeedDoing")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1503:Braces should not be omitted", Justification = "This is annoying", Scope = "namespaceanddescendants", Target = "~N:SomethingNeedDoing")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1512:Single-line comments should not be followed by blank line", Justification = "I like this better", Scope = "namespaceanddescendants", Target = "~N:SomethingNeedDoing")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1515:Single-line comment should be preceded by blank line", Justification = "I like this better", Scope = "namespaceanddescendants", Target = "~N:SomethingNeedDoing")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1127:Generic type constraints should be on their own line", Justification = "I like this better", Scope = "namespaceanddescendants", Target = "~N:SomethingNeedDoing")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1633:File should have header", Justification = "We don't do those yet")]

[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Grouping usage of relevant classes.", Scope = "namespaceanddescendants", Target = "~N:SomethingNeedDoing")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Name is relevant to contents.", Scope = "type", Target = "~T:SomethingNeedDoing.INode")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:Enumeration items should be documented", Justification = "Self documenting.", Scope = "type", Target = "~T:SomethingNeedDoing.ActionResultFlags")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:Enumeration items should be documented", Justification = "Self documenting.", Scope = "type", Target = "~T:SomethingNeedDoing.ActionResult")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:Enumeration items should be documented", Justification = "Self documenting.", Scope = "type", Target = "~T:SomethingNeedDoing.CraftingCondition")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:Enumeration items should be documented", Justification = "Self documenting.", Scope = "type", Target = "~T:SomethingNeedDoing.ActionCategory")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Self documenting.", Scope = "type", Target = "~T:SomethingNeedDoing.CraftingState")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "I want it there.", Scope = "type", Target = "~T:SomethingNeedDoing.ActionCategory")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "I want it there.", Scope = "type", Target = "~T:SomethingNeedDoing.EffectNotPresentError")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1134:Attributes should not share line", Justification = "StructLayout", Scope = "type", Target = "~T:SomethingNeedDoing.CraftingState")]
