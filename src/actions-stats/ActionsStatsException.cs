using System;

namespace ActionsStats;

public class ActionsStatsException : Exception
{
    public ActionsStatsException()
    {
    }

    public ActionsStatsException(string message) : base(message)
    {
    }

    public ActionsStatsException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
