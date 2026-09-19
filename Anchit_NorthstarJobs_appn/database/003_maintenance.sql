-- Optional scheduled maintenance. Review retention requirements first.
-- Deletes expired verification/reset tokens, not user data.
DELETE FROM AccountTokens WHERE ExpiresAt<DATEADD(day,-7,SYSUTCDATETIME());
-- Delivered mail bodies are already cleared by the application.
DELETE FROM MailItems WHERE SentAt<DATEADD(day,-30,SYSUTCDATETIME());
