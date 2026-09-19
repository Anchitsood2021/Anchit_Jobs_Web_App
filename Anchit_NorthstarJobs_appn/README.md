# Northstar Jobs

A jobs portal implemented in **C# / ASP.NET Core 10 + Entity Framework Core + Microsoft SQL Server**, with a responsive HTML/CSS/JavaScript interface served by the .NET application. No Node.js build is required to run it. “Northstar” is a replaceable working name; locations and currency default to New Zealand / NZD.

**Delivery status:** source implementation provided, with SQL database definition, local run configuration, and automated tests. This package has NOT been compiled or exercised against SQL Server in the authoring environment: there was no .NET SDK or SQL Server, and network restrictions prevented installing the SDK. JavaScript syntax and package/static consistency checks were run. Browser validation was attempted but could not connect to the local server. See [verification report](docs/VERIFICATION.md). This is not a claim of production readiness.

## Start here  -  Docker Desktop

Use Docker Desktop on an x64 Windows/Linux machine (Linux containers), or a compatible x64 Docker Engine. The included SQL Server container is an x64 image; on an ARM Mac, use a supported remote SQL Server/Azure SQL instance instead of assuming native container compatibility.

1. Extract this archive. Open a terminal in the folder containing `compose.yaml`.
2. Copy `.env.example` to `.env`. Replace **both** password placeholders. Keep this file private.
3. Run:

```bash
docker compose build web
docker compose up -d db
docker compose run --rm web --init-db --seed-demo
docker compose up -d web
```

4. Open **http://localhost:5080**.
5. Sign in using one of the accounts below and the `DEMO_PASSWORD` you supplied in `.env`.

| Account | Email | What to explore |
|---|---|---|
| Job seeker | `seeker@northstar.example` | Profile, resumes, saved jobs, application feedback |
| Employer | `employer@northstar.example` | Job posting, candidate review, feedback and messages |
| Other employers | `employer2@northstar.example`, `employer3@northstar.example` | Independent company accounts |
| Administrator | `admin@northstar.example` | Company verification, reports, suspension and moderation |

Demo people, companies, vacancies, salaries, and feedback are fictional. Demo seeding is permitted only in Development and on an empty database. No demo passwords are hard-coded in the application. `docker compose down` stops services while retaining data volumes. Do not remove volumes unless you intend to permanently erase the local database.

For an empty application, omit `--seed-demo` during initialization. Create real test accounts through the registration page. Development mode does not require email confirmation; outgoing verification/reset messages are written as `.eml` files in `App_Data/mail`, never sent externally. In Docker, inspect them with `docker compose exec web ls /app/App_Data/mail` and copy the chosen file out with `docker compose cp web:/app/App_Data/mail/<filename>.eml .`.

## Run with the .NET SDK / Visual Studio

Install the .NET 10 SDK and a supported SQL Server instance (SQL Server 2022, SQL Express, LocalDB on Windows, or Azure SQL). Use a Visual Studio version that supports the .NET 10 SDK, or VS Code with the C# tooling.

```bash
dotnet restore NorthstarJobs.sln
dotnet user-secrets set "ConnectionStrings:Jobs" "YOUR_SQL_SERVER_CONNECTION_STRING" --project src/NorthstarJobs
dotnet user-secrets set "Demo:Password" "YOUR_UNIQUE_DEMO_PASSWORD" --project src/NorthstarJobs
dotnet build NorthstarJobs.sln
dotnet run --project src/NorthstarJobs -- --init-db --seed-demo
dotnet run --project src/NorthstarJobs
```

The launch profile selects Development and http://localhost:5080. Commands run from the solution root store local `App_Data` relative to that working directory. For a Windows LocalDB development connection, for example:

```text
Server=(localdb)\MSSQLLocalDB;Database=NorthstarJobs;Trusted_Connection=True;TrustServerCertificate=True
```

Use `TrustServerCertificate=True` only for trusted local development. For a remote or production SQL Server, use a trusted certificate with `Encrypt=True;TrustServerCertificate=False`. Do not commit connection strings or passwords.

## What is implemented

| Area | Features |
|---|---|
| Accounts | Separate employer and seeker registration, sign-in/out, email confirmation, password reset/change, lockout, role enforcement |
| Job discovery | Keyword/company/skill search, location, category, employment type, workplace, seniority and NZD salary filters, sorting, pagination, detail pages |
| Job seekers | Editable profile, private PDF CV upload/download, saved jobs, saved keyword/location searches, applications, progress timeline, withdrawal |
| Employers | Company profile, draft/publish/edit/close listings, dashboard counts, applicant list, submitted profile/CV review, controlled hiring stages |
| Detailed feedback | Employer-written skills/experience/communication scores, strengths, specific improvements and next steps; retained as dated records |
| Communication | Application-specific two-way messages and in-app notifications |
| Administration | Company verification, job reports, hide/restore listings, suspend/restore accounts, failed email retry |
| Data integrity | Unique applications, relational foreign keys, private CV ownership checks, immutable submitted CV/profile snapshots, optimistic concurrency |
| UI | Responsive desktop/mobile layouts, semantic controls, keyboard focus styles, empty/loading/error states |
| Delivery | Docker Compose, SQL schema and reporting queries, demo data generator, unit tests, API integration test, CI workflow |

Applications follow a controlled lifecycle: Submitted → Reviewing/Shortlisted/Interview → Offer → Hired, with permitted rejection paths. Candidates may withdraw active applications. Rejected, Hired, and Withdrawn applications cannot be reopened. See [architecture](docs/ARCHITECTURE.md) for exact transitions.

## Scope boundaries

This first implementation includes the core workflows above. It does **not** include payment/subscription processing, social login, multi-user recruiting teams, video interviewing, background checks, automatic AI assessment, scheduled job-alert email, CV parsing, external job feeds, or a public candidate directory. Saved searches are manually rerun. Feedback comes from an employer and is not guaranteed for every application; the application does not fabricate assessments.

A single employer account owns a company. Salaries are gross annual values; hourly/day rates and prorated part-time comparisons need a separate pay-period model. Salary filtering deliberately covers NZD listings only. Candidate/application and administrative lists have bounded retrieval (500 applications; 200 recent accounts/listings), so server pagination should be added before operating at scale.

## Database setup

The application will not silently initialize or change a database on ordinary startup. Choose **one** initialization route:

- Recommended local route: `--init-db` uses EF Core `EnsureCreatedAsync` to create an empty database/schema.
- Manual route: run `database/000_create_database.sql`, select the database, then run `database/001_schema.sql` in SSMS or sqlcmd. Run the scripts against an empty database only.

`001_schema.sql` defines all 14 tables, keys, indexes, and checks. Passwords belong in `Accounts.PasswordHash`; never insert plaintext passwords using SQL. Create accounts via the application or bootstrap command. Demo records are seeded in C# so password hashing stays in the application.

To print the EF-generated authoritative schema after installing the SDK:

```bash
dotnet run --project src/NorthstarJobs -- --export-sql
```

An existing schema is not upgraded by `EnsureCreated`. This is a version-one bootstrap, **not an EF migration history**. Before evolving a deployed database, introduce reviewed versioned migrations, reconcile them with the existing schema, and back up data. Do not run `EnsureDeleted` on a real database.

## Administrator bootstrap

For a non-demo database, supply `Bootstrap:Email` and `Bootstrap:Password` using user-secrets (local) or securely injected environment variables (`Bootstrap__Email`, `Bootstrap__Password`):

```bash
dotnet run --project src/NorthstarJobs -- --create-admin
```

This creates a new administrator, and refuses to silently promote an existing account. Remove bootstrap secrets immediately after use. Registration cannot choose the Admin role.

## Tests

```bash
dotnet test NorthstarJobs.sln
```

With the application running against a **disposable Development database**, run the black-box integration test:

```bash
# Bash
ALLOW_TEST_WRITES=yes python3 scripts/integration_test.py
```

```powershell
# PowerShell
$env:ALLOW_TEST_WRITES = "yes"
python scripts/integration_test.py
```

The test creates uniquely named test accounts/jobs and retains them for inspection. It verifies ownership boundaries, CSRF rejection, application uniqueness, hiring transitions, feedback, messaging, withdrawal, resume access and closed-job behavior. It makes real HTTP calls and expects SQL Server persistence. No external email is sent in Development. The included GitHub Actions workflow starts SQL Server, builds the .NET solution, runs unit tests, and then runs this integration script. It has not been executed as part of this delivery.

## Deployment

This application needs an ASP.NET Core host and SQL Server/Azure SQL. It cannot run as a static website. Read [deployment instructions](docs/DEPLOYMENT.md) before exposing it publicly. The Docker Compose file is intentionally local-only, uses Development mode, and is not a public-production configuration.

Before public launch, complete the build and integration checks, production SMTP/domain/HTTPS configuration, upload malware scanning, operational monitoring/backups, retention/privacy requirements, and independent security/accessibility review. Current PDF validation checks extension, size and signature; it is not a virus scanner.

## Project map

```text
NorthstarJobs.sln
src/NorthstarJobs/
  Program.cs                 Application startup, cookies, CSRF, limits, CLI setup
  Models/                    Entities and validated request contracts
  Data/                      EF Core mappings and fictional demo seed
  Controllers/               Accounts, jobs, applications, profile and moderation
  Services/                  Password/token helpers, hiring workflow, mail queue
  wwwroot/                   Complete responsive interface
  appsettings*.json          Non-secret configuration defaults
  Properties/                Local launch profile
database/                   SQL Server bootstrap, reports and maintenance
tests/                      .NET tests
scripts/                    Live API integration test
docs/                       Architecture, deployment, verification and API guide
Dockerfile / compose.yaml   Local application + SQL Server environment
.github/workflows/ci.yml    Build + SQL-backed integration pipeline
```
