namespace GGPOLauncher.Core.Models;

public record DiagnosticResult(string Name, bool Passed, string Message, string? FixSteps = null);
