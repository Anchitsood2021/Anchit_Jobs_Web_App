# Deployment guide

## Supported hosting shape

Host the ASP.NET Core app on IIS/Windows, an ASP.NET-capable Linux service, or a container platform, and connect it to SQL Server or Azure SQL. One app instance is the intended first deployment. All requests should arrive through HTTPS at a hostname you control. The source package does not provision paid cloud resources, register a domain, or publish a production service.

## Required configuration

Use the host's secret manager or securely injected environment variables. Double underscores map to configuration sections.

| Setting | Production value / purpose |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__Jobs` | Encrypted connection to production SQL Server; restricted runtime DB user |
| `AllowedHosts` | Your actual hostname(s), separated by semicolons |
| `Site__BaseUrl` | Full public HTTPS origin, e.g. `https://jobs.your-domain.example` |
| `Site__RequireConfirmedEmail` | `true` |
| `Site__KeyPath` | Durable private directory for Data Protection keys |
| `Site__TrustedProxies__0` | Exact trusted reverse proxy IP, if relevant; repeat numbered entries for known proxies |
| `Mail__Host`, `Mail__Port` | SMTP host and TLS port, typically 587 |
| `Mail__UseSsl` | `true` for SMTP STARTTLS |
| `Mail__Username`, `Mail__Password` | SMTP credential secret |
| `Mail__From` | Verified sender mailbox/domain |
| `Mail__DevelopmentPickup` | `false` |

Do not set untrusted forwarding headers to be accepted from every address. The code processes `X-Forwarded-For` and `X-Forwarded-Proto` only for framework-trusted/local or explicitly configured proxy IPs. Configure the platform's actual trusted proxy addresses. Otherwise HTTPS redirection may loop behind a TLS-terminating proxy, and IP-based limits may group all requests under the proxy. If Kestrel terminates TLS directly, provision a trusted certificate using the host's normal certificate mechanism.

## Release sequence

1. Run the provided build, unit tests, and SQL-backed integration tests in a disposable test environment. Resolve failures before release. They were not run in the authoring environment.
2. Review pinned NuGet/container dependencies for supported patched versions and vulnerability findings. The package targets .NET 10 and pins EF Core 10.0.0 as a reproducible baseline, not a claim of the latest security patch. Set your chosen tested patch versions and lock image digests for releases.
3. Provision the database with a separate provisioning account, using `--init-db` or the SQL schema script against an empty database. Back up an existing database before changing its schema. Ordinary application startup does not upgrade a schema.
4. Give the runtime DB user only necessary SELECT/INSERT/UPDATE/DELETE rights on the application tables; do not run the public service as `sa` or a database owner. Bootstrap/setup commands may need separate credentials.
5. Publish with `dotnet publish src/NorthstarJobs -c Release -o artifacts/publish`, or build the included Dockerfile. Configure production settings and durable key storage. Restrict and protect the key directory; the sample filesystem key store has no certificate-based at-rest encryption configured.
6. Configure SMTP and sender-domain authentication. Test verification and reset emails, including expired and reused links. Confirm that passwords/tokens are not included in logs.
7. Bootstrap an administrator using temporary `Bootstrap__Email` / `Bootstrap__Password` secrets. Remove these secrets after the command succeeds. Never enable demo seeding or reuse demo credentials in production.
8. Configure the domain, HTTPS, trusted proxy, process restart/health checks, and a readiness probe at `/health/ready`. `/health` is a process liveness check only.
9. Back up SQL data and Data Protection keys; test restore procedures. Monitor application errors, queue failures, failed login patterns, latency, storage usage and database capacity.
10. Conduct an independent security and accessibility review and complete the product-specific privacy, retention, terms, support and abuse-reporting arrangements before collecting real applicant data.

## Work still required before accepting public CV uploads

- A PDF signature check is not a malware scanner. Add a scanning/quarantine stage and refuse download until a file passes.
- Define retention, account export/deletion, employer obligations and lawful handling of applicant information with appropriate local advice.
- Validate authorization under multiple real accounts and check every CV/application/message route. Run concurrent submission and status-update tests.
- Ensure the frontend and database can recover from mail/network failures without exposing stack traces or sensitive data.
- Do not assume automated tests replace a production security review.

## Scaling beyond the initial deployment

Use a leased durable queue or a dedicated worker for mail, shared protected Data Protection keys, distributed rate limiting, paginated application/admin views, private object storage for CVs, monitoring, and load testing before adding multiple app replicas. The current mail worker is intentionally documented for a single instance.

## Local operational commands

```bash
docker compose logs --tail=100 web
docker compose logs --tail=100 db
docker compose stop web
docker compose up -d web
```

Docker Compose uses local-only port bindings and Development mode. It is not a production deployment template. Avoid `docker compose down -v` unless you explicitly intend to erase all local SQL and application data.
