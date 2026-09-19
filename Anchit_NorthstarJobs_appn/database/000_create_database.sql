-- Run with a provisioning account, not the web application account.
IF DB_ID(N'NorthstarJobs') IS NULL
    CREATE DATABASE [NorthstarJobs];
GO
