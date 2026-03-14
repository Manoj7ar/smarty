namespace SmartThingsMxConsole.Core.Models;

public enum FailureKind
{
    None,
    Validation,
    Auth,
    RateLimited,
    Transient,
    Unsupported,
    Unknown,
}

public sealed record CommandExecutionResult(bool Success, string Message, FailureKind FailureKind = FailureKind.None)
{
    public static CommandExecutionResult Ok(string message) => new(true, message);

    public static CommandExecutionResult Fail(string message, FailureKind failureKind) => new(false, message, failureKind);
}
