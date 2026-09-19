-- Northstar Jobs — SQL Server schema v1
-- Run in an EMPTY database. Alternative to dotnet run -- --init-db.
-- SQL Server 2022 or Azure SQL. Do not run both initialization methods.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

CREATE TABLE [Accounts] (
    [Id] uniqueidentifier NOT NULL,
    [Email] nvarchar(254) NOT NULL,
    [NormalizedEmail] nvarchar(254) NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Role] nvarchar(20) NOT NULL,
    [PasswordHash] nvarchar(512) NOT NULL,
    [SecurityStamp] uniqueidentifier NOT NULL,
    [EmailConfirmed] bit NOT NULL,
    [Suspended] bit NOT NULL,
    [FailedLogins] int NOT NULL,
    [LockoutUntil] datetime2 NULL,
    [Headline] nvarchar(150) NOT NULL,
    [Location] nvarchar(120) NOT NULL,
    [Bio] nvarchar(2000) NOT NULL,
    [Skills] nvarchar(1000) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Accounts] PRIMARY KEY ([Id])
);

CREATE TABLE [Companies] (
    [Id] uniqueidentifier NOT NULL,
    [OwnerId] uniqueidentifier NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Website] nvarchar(300) NOT NULL,
    [Location] nvarchar(120) NOT NULL,
    [Description] nvarchar(3000) NOT NULL,
    [Industry] nvarchar(80) NOT NULL,
    [Verified] bit NOT NULL,
    CONSTRAINT [PK_Companies] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Companies_Accounts_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Jobs] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [Title] nvarchar(160) NOT NULL,
    [Location] nvarchar(120) NOT NULL,
    [Category] nvarchar(80) NOT NULL,
    [Type] nvarchar(30) NOT NULL,
    [WorkMode] nvarchar(30) NOT NULL,
    [Level] nvarchar(30) NOT NULL,
    [SalaryMin] int NULL,
    [SalaryMax] int NULL,
    [Currency] nvarchar(3) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Requirements] nvarchar(max) NOT NULL,
    [Benefits] nvarchar(4000) NOT NULL,
    [Skills] nvarchar(1000) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [PublishedAt] datetime2 NULL,
    [ClosesAt] datetime2 NOT NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Jobs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Jobs_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [CK_Job_Salary] CHECK (([SalaryMin] IS NULL AND [SalaryMax] IS NULL) OR ([SalaryMin] IS NOT NULL AND [SalaryMax] IS NOT NULL AND [SalaryMin] >= 0 AND [SalaryMax] >= [SalaryMin]))
);

CREATE TABLE [Resumes] (
    [Id] uniqueidentifier NOT NULL,
    [OwnerId] uniqueidentifier NOT NULL,
    [FileName] nvarchar(200) NOT NULL,
    [Content] varbinary(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Resumes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Resumes_Accounts_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Applications] (
    [Id] uniqueidentifier NOT NULL,
    [JobId] uniqueidentifier NOT NULL,
    [SeekerId] uniqueidentifier NOT NULL,
    [ResumeId] uniqueidentifier NOT NULL,
    [CoverLetter] nvarchar(max) NOT NULL,
    [ApplicantName] nvarchar(150) NOT NULL,
    [ApplicantSkills] nvarchar(1000) NOT NULL,
    [ApplicantBio] nvarchar(2000) NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Applications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Applications_Jobs_JobId] FOREIGN KEY ([JobId]) REFERENCES [Jobs] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Applications_Accounts_SeekerId] FOREIGN KEY ([SeekerId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Applications_Resumes_ResumeId] FOREIGN KEY ([ResumeId]) REFERENCES [Resumes] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ApplicationEvents] (
    [Id] uniqueidentifier NOT NULL,
    [ApplicationId] uniqueidentifier NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [Note] nvarchar(2000) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ApplicationEvents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ApplicationEvents_Applications_ApplicationId] FOREIGN KEY ([ApplicationId]) REFERENCES [Applications] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Feedback] (
    [Id] uniqueidentifier NOT NULL,
    [ApplicationId] uniqueidentifier NOT NULL,
    [SkillsScore] int NOT NULL,
    [ExperienceScore] int NOT NULL,
    [CommunicationScore] int NOT NULL,
    [Strengths] nvarchar(4000) NOT NULL,
    [Improvements] nvarchar(4000) NOT NULL,
    [NextSteps] nvarchar(4000) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Feedback] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Feedback_Applications_ApplicationId] FOREIGN KEY ([ApplicationId]) REFERENCES [Applications] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [CK_Feedback_Scores] CHECK ([SkillsScore] BETWEEN 1 AND 5 AND [ExperienceScore] BETWEEN 1 AND 5 AND [CommunicationScore] BETWEEN 1 AND 5)
);

CREATE TABLE [Messages] (
    [Id] uniqueidentifier NOT NULL,
    [ApplicationId] uniqueidentifier NOT NULL,
    [SenderId] uniqueidentifier NOT NULL,
    [Body] nvarchar(4000) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Messages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Messages_Applications_ApplicationId] FOREIGN KEY ([ApplicationId]) REFERENCES [Applications] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Messages_Accounts_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [SavedJobs] (
    [AccountId] uniqueidentifier NOT NULL,
    [JobId] uniqueidentifier NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SavedJobs] PRIMARY KEY ([AccountId], [JobId]),
    CONSTRAINT [FK_SavedJobs_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SavedJobs_Jobs_JobId] FOREIGN KEY ([JobId]) REFERENCES [Jobs] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [SavedSearches] (
    [Id] uniqueidentifier NOT NULL,
    [AccountId] uniqueidentifier NOT NULL,
    [Name] nvarchar(120) NOT NULL,
    [Keywords] nvarchar(160) NOT NULL,
    [Location] nvarchar(120) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SavedSearches] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SavedSearches_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Notifications] (
    [Id] uniqueidentifier NOT NULL,
    [AccountId] uniqueidentifier NOT NULL,
    [Text] nvarchar(400) NOT NULL,
    [Link] nvarchar(200) NOT NULL,
    [Read] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Notifications_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [JobReports] (
    [Id] uniqueidentifier NOT NULL,
    [JobId] uniqueidentifier NOT NULL,
    [ReporterId] uniqueidentifier NOT NULL,
    [Reason] nvarchar(2000) NOT NULL,
    [Resolved] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_JobReports] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_JobReports_Jobs_JobId] FOREIGN KEY ([JobId]) REFERENCES [Jobs] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_JobReports_Accounts_ReporterId] FOREIGN KEY ([ReporterId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [AccountTokens] (
    [Id] uniqueidentifier NOT NULL,
    [AccountId] uniqueidentifier NOT NULL,
    [Hash] nvarchar(64) NOT NULL,
    [Purpose] nvarchar(20) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AccountTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AccountTokens_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [MailItems] (
    [Id] uniqueidentifier NOT NULL,
    [Recipient] nvarchar(254) NOT NULL,
    [Subject] nvarchar(200) NOT NULL,
    [Body] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [SentAt] datetime2 NULL,
    [Attempts] int NOT NULL,
    [LastAttemptAt] datetime2 NULL,
    CONSTRAINT [PK_MailItems] PRIMARY KEY ([Id])
);

CREATE INDEX [IX_Jobs_CompanyId] ON [Jobs] ([CompanyId]);
CREATE INDEX [IX_Resumes_OwnerId] ON [Resumes] ([OwnerId]);
CREATE INDEX [IX_Applications_JobId] ON [Applications] ([JobId]);
CREATE INDEX [IX_Applications_SeekerId] ON [Applications] ([SeekerId]);
CREATE INDEX [IX_Applications_ResumeId] ON [Applications] ([ResumeId]);
CREATE INDEX [IX_ApplicationEvents_ApplicationId] ON [ApplicationEvents] ([ApplicationId]);
CREATE INDEX [IX_Feedback_ApplicationId] ON [Feedback] ([ApplicationId]);
CREATE INDEX [IX_Messages_ApplicationId] ON [Messages] ([ApplicationId]);
CREATE INDEX [IX_Messages_SenderId] ON [Messages] ([SenderId]);
CREATE INDEX [IX_SavedJobs_JobId] ON [SavedJobs] ([JobId]);
CREATE INDEX [IX_SavedSearches_AccountId] ON [SavedSearches] ([AccountId]);
CREATE INDEX [IX_Notifications_AccountId] ON [Notifications] ([AccountId]);
CREATE INDEX [IX_JobReports_JobId] ON [JobReports] ([JobId]);
CREATE INDEX [IX_JobReports_ReporterId] ON [JobReports] ([ReporterId]);
CREATE INDEX [IX_AccountTokens_AccountId] ON [AccountTokens] ([AccountId]);
CREATE UNIQUE INDEX [IX_Accounts_NormalizedEmail] ON [Accounts] ([NormalizedEmail]);
CREATE UNIQUE INDEX [IX_Companies_OwnerId] ON [Companies] ([OwnerId]);
CREATE UNIQUE INDEX [IX_Applications_JobId_SeekerId] ON [Applications] ([JobId], [SeekerId]);
CREATE UNIQUE INDEX [IX_AccountTokens_Hash] ON [AccountTokens] ([Hash]);
CREATE INDEX [IX_Jobs_Status_ClosesAt] ON [Jobs] ([Status], [ClosesAt]);
CREATE INDEX [IX_Notifications_AccountId_Read_CreatedAt] ON [Notifications] ([AccountId], [Read], [CreatedAt]);
CREATE INDEX [IX_MailItems_SentAt_Attempts] ON [MailItems] ([SentAt], [Attempts]);
CREATE INDEX [IX_Feedback_ApplicationId_CreatedAt] ON [Feedback] ([ApplicationId], [CreatedAt]);

COMMIT TRANSACTION;
