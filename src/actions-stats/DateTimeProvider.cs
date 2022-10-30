using System;

namespace ActionsStats;

public class DateTimeProvider
{
    public virtual long CurrentUnixTimeSeconds() => DateTimeOffset.Now.ToUnixTimeSeconds();
}
