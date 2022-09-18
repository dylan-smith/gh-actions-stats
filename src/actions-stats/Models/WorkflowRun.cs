using System;

namespace ActionsStats.Models;

public class WorkflowRun
{
    public int Id { get; set; }
    public int RunNumber { get; set; }
    public string Org { get; set; }
    public string Repo { get; set; }
    public int WorkflowId { get; set; }
    public string WorkflowName { get; set; }
    public string Actor { get; set; }
    public string Branch { get; set; }
    public string Event { get; set; }
    public DateTime RunDate { get; set; }
    public string Conclusion { get; set; }
    public string Url { get; set; }
}
