using System.Runtime.Serialization;

namespace StackCleaner.Tests;

public class TestException : Exception
{
    public TestException() : base("Test exception message.")
    {
    }

    public TestException(Exception inner) : base("Test exception message.", inner)
    {
    }

#if NET8_0_OR_GREATER
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
#endif
    protected TestException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}