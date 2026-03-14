namespace SmartThingsMxConsole.Core.Exceptions;

public class SmartThingsException : Exception
{
    public SmartThingsException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class SmartThingsAuthException : SmartThingsException
{
    public SmartThingsAuthException(string message = "SmartThings authentication failed.", Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class SmartThingsRateLimitException : SmartThingsException
{
    public SmartThingsRateLimitException(string message = "SmartThings rate limit was exceeded.", Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class SmartThingsTransientException : SmartThingsException
{
    public SmartThingsTransientException(string message = "A transient SmartThings error occurred.", Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class SmartThingsUnsupportedCapabilityException : SmartThingsException
{
    public SmartThingsUnsupportedCapabilityException(string message = "The requested capability is not supported.", Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
