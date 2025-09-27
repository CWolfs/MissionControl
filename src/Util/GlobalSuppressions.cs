using System.Diagnostics.CodeAnalysis;

// This file is used to suppress Code Analysis warnings for the Util folder
// Extension methods are intentionally kept in the global namespace for convenience
[assembly: SuppressMessage("Design", "CA1050:Declare types in namespaces", Justification = "Extension methods are intentionally global for convenience", Scope = "type", Target = "~T:ListExtensions")]
[assembly: SuppressMessage("Design", "RCS1110:Declare type inside namespace", Justification = "Extension methods are intentionally global for convenience", Scope = "type", Target = "~T:ListExtensions")]