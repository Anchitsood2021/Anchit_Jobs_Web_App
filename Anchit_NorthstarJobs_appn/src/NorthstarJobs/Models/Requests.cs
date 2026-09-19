using System.ComponentModel.DataAnnotations;
namespace NorthstarJobs.Models;
public record RegisterRequest([Required,StringLength(150,MinimumLength=2)]string Name,[Required,EmailAddress,MaxLength(254)]string Email,[Required,StringLength(128,MinimumLength=12)]string Password,[Required,RegularExpression("^(Seeker|Employer)$")]string Role,[MaxLength(150)]string? CompanyName);
public record LoginRequest([Required,EmailAddress,MaxLength(254)]string Email,[Required,MaxLength(128)]string Password);
public record EmailRequest([Required,EmailAddress,MaxLength(254)]string Email);
public record ResetRequest([Required,MaxLength(256)]string Token,[Required,StringLength(128,MinimumLength=12)]string Password);
public record TokenRequest([Required,MaxLength(256)]string Token);
public record PasswordRequest([Required,MaxLength(128)]string CurrentPassword,[Required,StringLength(128,MinimumLength=12)]string NewPassword);
public record ProfileRequest([Required,StringLength(150,MinimumLength=2)]string Name,[MaxLength(150)]string Headline,[MaxLength(120)]string Location,[MaxLength(2000)]string Bio,[MaxLength(1000)]string Skills);
public record CompanyRequest([Required,StringLength(150,MinimumLength=2)]string Name,[MaxLength(300)]string Website,[MaxLength(120)]string Location,[MaxLength(3000)]string Description,[MaxLength(80)]string Industry);
public record JobRequest(
 [Required,StringLength(160,MinimumLength=3)]string Title,[Required,MaxLength(120)]string Location,
 [Required,MaxLength(80)]string Category,[Required,RegularExpression("^(Full-time|Part-time|Contract|Internship)$")]string Type,
 [Required,RegularExpression("^(On-site|Hybrid|Remote)$")]string WorkMode,[Required,RegularExpression("^(Entry-level|Mid-level|Senior|Lead)$")]string Level,
 [Range(0,2000000)]int? SalaryMin,[Range(0,2000000)]int? SalaryMax,[Required,RegularExpression("^(NZD|AUD|USD|GBP|EUR)$")]string Currency,
 [Required,StringLength(20000,MinimumLength=40)]string Description,[Required,StringLength(12000,MinimumLength=10)]string Requirements,
 [MaxLength(4000)]string Benefits,[MaxLength(1000)]string Skills,[Required,RegularExpression("^(Draft|Published|Closed)$")]string Status,
 DateTime ClosesAt,string? RowVersion);
public record ApplyRequest(Guid ResumeId,[Required,StringLength(8000,MinimumLength=20)]string CoverLetter);
public record StatusRequest([Required,RegularExpression("^(Reviewing|Shortlisted|Interview|Offer|Hired|Rejected)$")]string Status,[MaxLength(2000)]string Note,[Required]string RowVersion);
public record FeedbackRequest([Range(1,5)]int SkillsScore,[Range(1,5)]int ExperienceScore,[Range(1,5)]int CommunicationScore,[Required,StringLength(4000,MinimumLength=10)]string Strengths,[Required,StringLength(4000,MinimumLength=10)]string Improvements,[Required,StringLength(4000,MinimumLength=10)]string NextSteps);
public record MessageRequest([Required,StringLength(4000,MinimumLength=1)]string Body);
public record ReportRequest([Required,StringLength(2000,MinimumLength=10)]string Reason);
public record SearchRequest([Required,MaxLength(120)]string Name,[MaxLength(160)]string Keywords,[MaxLength(120)]string Location);
public record SuspendRequest(bool Suspended);
public record VerifyRequest(bool Verified);
