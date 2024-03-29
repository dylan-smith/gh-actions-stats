﻿using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using ActionsStats.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace ActionsStats;

public static class Program
{
    private static readonly OctoLogger Logger = new();

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "If the version check fails for any reason, we want the CLI to carry on with the current command")]
    public static async Task Main(string[] args)
    {
        Logger.LogDebug("Execution Started");

        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddCommands()
            .AddSingleton(Logger)
            .AddSingleton<EnvironmentVariableProvider>()
            .AddSingleton<GithubApiFactory>()
            .AddSingleton<RetryPolicy>()
            .AddSingleton<VersionChecker>()
            .AddSingleton<FileSystemProvider>()
            .AddSingleton<IVersionProvider, VersionChecker>(sp => sp.GetRequiredService<VersionChecker>())
            .AddSingleton<SqlServerServiceFactory>()
            .AddSingleton<DateTimeProvider>()
            .AddHttpClient();

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var parser = BuildParser(serviceProvider);

        SetContext(parser.Parse(args));

        try
        {
            await LatestVersionCheck(serviceProvider);
        }
        catch (Exception ex)
        {
            Logger.LogWarning("Could not retrieve latest actions-stats extension version from github.com, please ensure you are using the latest version by running: gh extension upgrade actions-stats");
            Logger.LogVerbose(ex.ToString());
        }

        await parser.InvokeAsync(args);
    }

    private static void SetContext(ParseResult parseResult)
    {
        CliContext.RootCommand = parseResult.RootCommandResult.Command.Name;
        CliContext.ExecutingCommand = parseResult.CommandResult.Command.Name;
    }

    private static async Task LatestVersionCheck(ServiceProvider sp)
    {
        var versionChecker = sp.GetRequiredService<VersionChecker>();

        if (await versionChecker.IsLatest())
        {
            Logger.LogInformation($"You are running the latest version of the actions-stats extension [v{await versionChecker.GetLatestVersion()}]");
        }
        else
        {
            Logger.LogWarning($"You are running an older version of the actions-stats extension [v{versionChecker.GetCurrentVersion()}]. The latest version is v{await versionChecker.GetLatestVersion()}.");
            Logger.LogWarning($"Please update by running: gh extension upgrade actions-stats");
        }
    }

    private static Parser BuildParser(ServiceProvider serviceProvider)
    {
        var root = new RootCommand("Gather stats on your GitHub Actions.");
        var commandLineBuilder = new CommandLineBuilder(root);

        foreach (var command in serviceProvider.GetServices<Command>())
        {
            commandLineBuilder.AddCommand(command);
        }

        return commandLineBuilder
            .UseDefaults()
            .UseExceptionHandler((ex, _) =>
            {
                Logger.LogError(ex);
                Environment.ExitCode = 1;
            }, 1)
            .Build();
    }

    private static IServiceCollection AddCommands(this IServiceCollection services)
    {
        var sampleCommandType = typeof(GetRunsCommand);
        var commandType = typeof(Command);

        var commands = sampleCommandType
            .Assembly
            .GetExportedTypes()
            .Where(x => x.Namespace == sampleCommandType.Namespace && commandType.IsAssignableFrom(x));

        foreach (var command in commands)
        {
            services.AddSingleton(commandType, command);
        }

        return services;
    }
}
