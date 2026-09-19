# Architecture and behavior

## Request flow

The browser downloads `wwwroot/index.html`, CSS, and ES modules from ASP.NET Core. The JavaScript router renders account, search, application, employer, and admin screens. Forms call same-origin `/api/*` controllers using an HTTP-only authentication cookie and an anti-forgery header. Controllers validate DTOs and ownership before using EF Core parameterized SQL. SQL Server persists all business data; browser localStorage is not used for application records or passwords.

The API is the enforcement boundary. Hiding controls in the browser is not relied on for authorization. Public job browsing does not return applicant data or account password fields. The administrator is intentionally not granted an automatic CV-download or application-message bypass.

## Tables

| Table | Purpose | Relationships / constraints |
|---|---|---|
| Accounts | Login identity and editable profile | Unique normalized email; role stored server-side; framework password hash |
| Companies | Employer's public information | One company per owner account; verified flag controlled by admin |
| Jobs | Published and draft vacancies | Company FK; salary range check; rowversion; status/closing index |
| Resumes | Private PDF bytes and filename | Owner account; downloaded through authorized controller only |
| Applications | Submitted profile snapshot, CV reference and status | Unique (JobId, SeekerId); rowversion; CV content is immutable |
| ApplicationEvents | Applicant-visible stage history | Append-only application FK and timestamps |
| Feedback | Employer-authored structured feedback | Application FK; scores constrained to 1–5; dated updates |
| Messages | Two-way application conversations | Application and sender FKs; no public enumeration |
| SavedJobs | Candidate bookmarks | Composite account/job key |
| SavedSearches | Reusable keyword/location searches | Account FK; 20 searches per account |
| Notifications | In-app updates | Account FK, read state, safe application link |
| JobReports | Moderation reports | Reporter and job FKs, resolved state |
| AccountTokens | Verification and reset token hashes | Account FK, unique SHA-256 hash, purpose, expiry |
| MailItems | Durable outgoing auth email queue | Retry count, timestamps; body cleared after delivery |

All foreign keys restrict deletion. There is no silent cascade of accounts, CVs, or application history. Account deletion/data retention need a deliberate service and retention policy before a public launch. GUIDs are identifiers, not access controls.

The manual SQL schema uses the same entities and relational constraints. Additional redundant supporting FK indexes in the manual script are harmless. `--export-sql` renders the provider's exact create script; use that as the baseline when introducing EF migration tooling.

## Authentication

ASP.NET Core cookie authentication and its Data Protection system protect sessions. The built-in `PasswordHasher<Account>` handles salted password hashing; no custom password-hashing algorithm is implemented. Passwords must be 12–128 characters. Five failed password checks lock an account for 15 minutes. Login/reset/registration endpoints share an IP rate limiter. Production requires confirmed email by default.

Cookies are HTTP-only, SameSite=Strict, and Secure outside Development. Sessions expire after eight hours without sliding extension. Every authenticated request reloads suspension/security-stamp state, so changing passwords or suspending accounts invalidates older sessions. Logout clears the current session cookie.

Unsafe controller requests require an anti-forgery token obtained from `/api/auth/csrf`; the frontend refreshes it after authentication changes. Reset/verification tokens contain 32 cryptographically random bytes. The database stores only their hashes. Links use the URL fragment to avoid sending the token in ordinary HTTP URL logs; the page removes it from the address bar and submits it in a POST body. Tokens are purpose-bound, expiring, single-use, and replaced when a new link is requested. Reset and verification consumption use serializable transactions.

## Application lifecycle

| Current state | Employer's permitted next states |
|---|---|
| Submitted | Reviewing, Shortlisted, Interview, Rejected |
| Reviewing | Shortlisted, Interview, Rejected |
| Shortlisted | Interview, Offer, Rejected |
| Interview | Offer, Rejected |
| Offer | Hired, Rejected |
| Hired / Rejected / Withdrawn | None |

The applicant may withdraw any state except Hired, Rejected or Withdrawn. Withdrawal closes messaging and feedback and revokes employer resume download through the app. Existing text/profile history remains retained. It cannot revoke a file an employer already downloaded. A unique application constraint prevents reapplying to the same job, including after withdrawal. Job editing/status updates use rowversion conflict checks; candidate/profile snapshots are not rewritten by later profile changes.

An employer may add multiple dated feedback updates. Each update is immediately applicant-visible and includes three scores plus strengths, improvements, and next steps. There are no hidden recruiting notes in the feedback fields. The UI explicitly explains visibility before submission. Interview scheduling is communicated through the timeline/message fields; no external calendar integration is claimed.

## File handling

Resumes are limited to PDF, 5 MB each, and 10 per candidate. Extension and `%PDF-` signature are checked; content is stored in SQL Server and returned as an attachment through an ownership-checked endpoint. Resume bytes never sit in `wwwroot`. An employer can retrieve only CVs attached to their own non-withdrawn applications. An unused resume can be deleted; an attached one is retained. Signature validation does not prove a PDF is safe. Add quarantine and malware scanning before a public rollout. Database CV storage is convenient for this first version; for large volumes, use private blob storage with short-lived access and preserve ownership checks.

## Mail and notifications

Status changes, messages, feedback and submissions create in-app notifications. Email is implemented for account confirmation and password reset. `MailWorker` polls a database outbox every 30 seconds, retries failures with a five-minute interval, and stops after five attempts. Administrators can requeue exhausted deliveries. A process crash after SMTP delivery but before saving `SentAt` can cause duplicate mail; delivery is at least once, not exactly once. Run one web/worker replica until leased delivery or an external queue is introduced.

Development writes `.eml` messages to a local pickup folder. Production uses configured SMTP over TLS. Saved searches do not run in the background and do not send mail.

## Capacity / operations

Public job results paginate 12 at a time. Application list requests return up to 500 most recently updated matching records. Admin account/job tables show 200 recent records; notifications show 100. These are bounded first-version views, not a complete large-scale reporting system. Add pagination/index tuning, distributed rate limiting, multi-worker queue leases, a scalable search index and telemetry for larger deployment.

Official implementation references:
- [ASP.NET Core cookie authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0)
- [ASP.NET Core anti-forgery protection](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0)
- [EF Core SQL Server provider](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/)
