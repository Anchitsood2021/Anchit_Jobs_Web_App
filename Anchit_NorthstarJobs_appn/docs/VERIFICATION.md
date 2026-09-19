# Verification report

Delivery environment: 19 September 2026.

## Checks completed

- Both frontend ES modules pass Node.js syntax checking.
- Local JavaScript imports and initial HTML asset references resolve.
- Application/launch JSON and both project XML files parse.
- SQL definition statically inspected: 14 tables, valid foreign-key/index targets, application uniqueness, rowversion and score/salary checks.
- Python API integration test passes syntax parsing.
- Documentation links and final archive contents checked.
- Manual source review addressed anti-forgery MVC registration, credential rotation, private CV ownership, restricted workflow transitions, salary NULL handling, closed-job submission and email delivery scope.

These checks are **not** substitutes for compiling C# or executing SQL. See `static-check-results.json` for the machine-readable static result.

## Checks not completed in this environment

| Check | Reason / next action |
|---|---|
| .NET build | SDK absent; attempted SDK download blocked by environment network restrictions. Run `dotnet build NorthstarJobs.sln`. |
| SQL Server initialization | No SQL Server or Docker engine here. Run the documented Docker setup or use your SQL Server. |
| .NET unit tests | Require restored .NET SDK/dependencies. Run `dotnet test NorthstarJobs.sln`. |
| API integration tests | Require the running .NET app and disposable SQL database. Run `scripts/integration_test.py` as described in README. |
| Browser visual/interaction tests | Local Playwright browser binary unavailable; the provided cloud browser could not reach the local preview. No screenshot or browser pass is claimed. |
| Production hosting / SMTP | No production domain, host, or SMTP account was configured. |

The GitHub Actions workflow is supplied to automate build, unit tests and SQL-backed API checks after the project is placed in a repository. It has not been executed for this delivery. Do not describe the package as production-ready until those checks and deployment review are complete.
