CREATE TABLE [dbo].[WorkflowRuns]
(
	[Id] BIGINT NOT NULL PRIMARY KEY, 
    [RunNumber] BIGINT NOT NULL, 
    [Org] NVARCHAR(250) NOT NULL, 
    [Repo] NVARCHAR(250) NOT NULL, 
    [WorkflowId] BIGINT NOT NULL, 
    [WorkflowName] NVARCHAR(250) NOT NULL, 
    [Actor] NVARCHAR(250) NOT NULL, 
    [Branch] NVARCHAR(250) NULL, 
    [Event] NVARCHAR(250) NOT NULL, 
    [RunDate] DATETIME NOT NULL, 
    [Conclusion] NVARCHAR(250) NULL, 
    [Url] NVARCHAR(250) NOT NULL
)
