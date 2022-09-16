using System;

namespace ActionsStats;

public class ActionStatsException : Exception
{
    public ActionStatsException()
    {
    }

    public ActionStatsException(string message) : base(message)
    {
    }

    public ActionStatsException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
