# API quick reference

All API routes are same-origin under `/api`. JSON uses camelCase. Unsafe methods require `X-CSRF-TOKEN`; request it with `GET /auth/csrf`, then refresh after sign-in/out or password changes. Browser cookies carry authentication. Responses use 400 for validation/invalid workflow, 401 for anonymous access, 403 for wrong roles, 404 for missing/unowned resources, 409 for duplicates/concurrent edits, and 429 for rate limits. Unowned private objects return 404 to avoid confirming their existence.

| Method | Route | Access / purpose |
|---|---|---|
| GET | `/auth/csrf`, `/auth/me` | CSRF token and current public user |
| POST | `/auth/register`, `/auth/login` | Create seeker/employer and sign in |
| POST | `/auth/logout` | Signed-in user |
| POST | `/auth/forgot-password`, `/auth/resend-confirmation` | Request auth email; generic response |
| POST | `/auth/reset-password`, `/auth/verify-email` | Expiring token consumption |
| POST | `/auth/change-password` | Verify current password and rotate credentials |
| GET | `/jobs` | Public filtered/paginated job search |
| GET | `/jobs/{id}` | Public active job or employer/admin access to unpublished job |
| GET | `/jobs/mine` | Owning employer's listings and counts |
| POST / PUT | `/jobs`, `/jobs/{id}` | Employer create/edit; edit requires rowVersion |
| GET | `/jobs/saved` | Seeker bookmarks |
| PUT / DELETE | `/jobs/{id}/save` | Add/remove own bookmark |
| POST | `/jobs/{id}/report` | Signed-in report of public active listing |
| PUT | `/profile` | Own profile; name/headline/location/bio/skills |
| GET / PUT | `/profile/company` | Own employer company |
| GET / POST | `/profile/resumes` | Seeker's metadata / multipart PDF upload (`file`) |
| GET | `/profile/resumes/{id}` | Owner or owning employer of non-withdrawn application |
| DELETE | `/profile/resumes/{id}` | Owner, only if unused |
| GET / POST / DELETE | `/profile/searches`, `/profile/searches/{id}` | Own saved keyword/location searches |
| GET | `/profile/notifications` | Latest 100 own notifications |
| POST | `/profile/notifications/read` | Mark own notifications read |
| GET | `/applications` | Own applications or applicants to own company jobs |
| GET | `/applications/{id}` | Owner/owning employer: snapshot, timeline, feedback, messages |
| POST | `/applications/job/{jobId}` | Seeker apply with resumeId and coverLetter |
| POST | `/applications/{id}/withdraw` | Applicant withdraw |
| POST | `/applications/{id}/status` | Owning employer; status, note, rowVersion |
| POST | `/applications/{id}/feedback` | Owning employer; structured applicant-visible feedback |
| POST | `/applications/{id}/messages` | Applicant/owning employer; body |
| GET | `/admin` | Administrator dashboard data |
| POST | `/admin/accounts/{id}/suspend` | Administrator; suspended boolean |
| POST | `/admin/companies/{id}/verify` | Administrator; verified boolean |
| POST | `/admin/jobs/{id}/hide`, `/restore` | Administrator moderation |
| POST | `/admin/reports/{id}/resolve` | Administrator resolve report |
| POST | `/admin/mail/retry` | Administrator requeue exhausted emails |

Search parameters: `q`, `location`, `category`, `type`, `mode`, `level`, `salary` (NZD), `sort=newest|salary`, `page`. Response: `{items,total,page,pageSize}`. Application list filters: `jobId`, `status`.

Job requests include title, location, category, type, workMode, level, nullable salaryMin/salaryMax, currency, description, requirements, benefits, skills, status, closesAt and (for edits) rowVersion. Allowed values are specified in `Models/Requests.cs`; the UI uses the same choices. Salary limits must be both omitted or both supplied with minimum ≤ maximum. Published jobs need a future closing date.

Feedback request example:

```json
{
  "skillsScore": 4,
  "experienceScore": 3,
  "communicationScore": 4,
  "strengths": "Your SQL examples explained the business impact clearly.",
  "improvements": "Provide one example of monitoring a production data pipeline.",
  "nextSteps": "Prepare a short project walkthrough for the interview."
}
```

Scores range from 1 to 5. All feedback fields and status notes are visible to the applicant. There is no AI-generated feedback endpoint or automated hiring decision.
