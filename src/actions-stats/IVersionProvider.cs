namespace ActionsStats;

public interface IVersionProvider
{
    string GetCurrentVersion();
    string GetVersionComments();
}
