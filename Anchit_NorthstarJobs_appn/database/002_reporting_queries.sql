-- Run in NorthstarJobs. These queries do not change data.
-- Jobs by company and their application counts.
SELECT c.Name AS Company, j.Title, j.Status, COUNT(a.Id) AS Applications
FROM Jobs j JOIN Companies c ON c.Id=j.CompanyId
LEFT JOIN Applications a ON a.JobId=j.Id
GROUP BY c.Name,j.Id,j.Title,j.Status
ORDER BY Applications DESC;

-- Hiring funnel. This is current stage, not lifetime stage conversions.
SELECT Status,COUNT(*) AS Applications FROM Applications GROUP BY Status;

-- Feedback coverage: application share with at least one employer update.
SELECT COUNT(*) AS TotalApplications,
 SUM(CASE WHEN f.ApplicationId IS NOT NULL THEN 1 ELSE 0 END) AS ApplicationsWithFeedback
FROM Applications a
LEFT JOIN (SELECT DISTINCT ApplicationId FROM Feedback) f ON f.ApplicationId=a.Id;

-- Listings that expired but retain Published status (public API excludes these).
SELECT Id,Title,ClosesAt FROM Jobs WHERE Status=N'Published' AND ClosesAt<=SYSUTCDATETIME();

-- Email queue operations; does not expose token-containing email bodies.
SELECT Id,Subject,Attempts,CreatedAt,LastAttemptAt FROM MailItems
WHERE SentAt IS NULL ORDER BY CreatedAt;

-- Duplicate applications: unique constraint should make this return zero rows.
SELECT JobId,SeekerId,COUNT(*) AS Copies FROM Applications
GROUP BY JobId,SeekerId HAVING COUNT(*)>1;
