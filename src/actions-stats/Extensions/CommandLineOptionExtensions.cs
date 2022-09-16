using System.CommandLine;

namespace ActionsStats.Extensions;

public static class CommandLineOptionExtensions
{
    public static string GetLogFriendlyName(this Option option) => option?.ArgumentHelpName.ToUpper().Replace("-", " ");
}
