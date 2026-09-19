using System.ComponentModel.DataAnnotations;
namespace NorthstarJobs.Models;
public abstract class Entity { public Guid Id { get; set; } = Guid.NewGuid(); }
public static class Roles { public const string Seeker="Seeker", Employer="Employer", Admin="Admin"; }
public class Account : Entity {
 [MaxLength(254)] public string Email { get; set; } = "";
 [MaxLength(254)] public string NormalizedEmail { get; set; } = "";
 [MaxLength(150)] public string Name { get; set; } = "";
 [MaxLength(20)] public string Role { get; set; } = Roles.Seeker;
 [MaxLength(512)] public string PasswordHash { get; set; } = "";
 public Guid SecurityStamp { get; set; } = Guid.NewGuid();
 public bool EmailConfirmed { get; set; }
 public bool Suspended { get; set; }
 public int FailedLogins { get; set; }
 public DateTime? LockoutUntil { get; set; }
 [MaxLength(150)] public string Headline { get; set; } = "";
 [MaxLength(120)] public string Location { get; set; } = "";
 [MaxLength(2000)] public string Bio { get; set; } = "";
 [MaxLength(1000)] public string Skills { get; set; } = "";
 public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class Company : Entity {
 public Guid OwnerId { get; set; } public Account Owner { get; set; } = null!;
 [MaxLength(150)] public string Name { get; set; } = "";
 [MaxLength(300)] public string Website { get; set; } = "";
 [MaxLength(120)] public string Location { get; set; } = "";
 [MaxLength(3000)] public string Description { get; set; } = "";
 [MaxLength(80)] public string Industry { get; set; } = "";
 public bool Verified { get; set; }
}
public class Job : Entity {
 public Guid CompanyId { get; set; } public Company Company { get; set; } = null!;
 [MaxLength(160)] public string Title { get; set; } = "";
 [MaxLength(120)] public string Location { get; set; } = "";
 [MaxLength(80)] public string Category { get; set; } = "Technology";
 [MaxLength(30)] public string Type { get; set; } = "Full-time";
 [MaxLength(30)] public string WorkMode { get; set; } = "Hybrid";
 [MaxLength(30)] public string Level { get; set; } = "Mid-level";
 public int? SalaryMin { get; set; } public int? SalaryMax { get; set; }
 [MaxLength(3)] public string Currency { get; set; } = "NZD";
 [MaxLength(20000)] public string Description { get; set; } = "";
 [MaxLength(12000)] public string Requirements { get; set; } = "";
 [MaxLength(4000)] public string Benefits { get; set; } = "";
 [MaxLength(1000)] public string Skills { get; set; } = "";
 [MaxLength(20)] public string Status { get; set; } = "Draft";
 public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
 public DateTime? PublishedAt { get; set; }
 public DateTime ClosesAt { get; set; } = DateTime.UtcNow.AddDays(30);
 [Timestamp] public byte[] RowVersion { get; set; } = [];
}
public class Resume : Entity {
 public Guid OwnerId { get; set; } public Account Owner { get; set; } = null!;
 [MaxLength(200)] public string FileName { get; set; } = "resume.pdf";
 public byte[] Content { get; set; } = [];
 public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class Application : Entity {
 public Guid JobId { get; set; } public Job Job { get; set; } = null!;
 public Guid SeekerId { get; set; } public Account Seeker { get; set; } = null!;
 public Guid ResumeId { get; set; } public Resume Resume { get; set; } = null!;
 [MaxLength(8000)] public string CoverLetter { get; set; } = "";
 [MaxLength(150)] public string ApplicantName { get; set; } = "";
 [MaxLength(1000)] public string ApplicantSkills { get; set; } = "";
 [MaxLength(2000)] public string ApplicantBio { get; set; } = "";
 [MaxLength(30)] public string Status { get; set; } = "Submitted";
 public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
 public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
 [Timestamp] public byte[] RowVersion { get; set; } = [];
}
public class ApplicationEvent : Entity {
 public Guid ApplicationId { get; set; } public Application Application { get; set; } = null!;
 [MaxLength(30)] public string Status { get; set; } = "";
 [MaxLength(2000)] public string Note { get; set; } = "";
 public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class Feedback : Entity {
 public Guid ApplicationId { get; set; } public Application Application { get; set; } = null!;
 public int SkillsScore { get; set; } public int ExperienceScore { get; set; } public int CommunicationScore { get; set; }
 [MaxLength(4000)] public string Strengths { get; set; } = "";
 [MaxLength(4000)] public string Improvements { get; set; } = "";
 [MaxLength(4000)] public string NextSteps { get; set; } = "";
 public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class Message : Entity {
 public Guid ApplicationId { get; set; } public Application Application { get; set; } = null!;
 public Guid SenderId { get; set; } public Account Sender { get; set; } = null!;
 [MaxLength(4000)] public string Body { get; set; } = "";
 public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class SavedJob {
 public Guid AccountId { get; set; } public Account Account { get; set; } = null!;
 public Guid JobId { get; set; } public Job Job { get; set; } = null!;
 public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class SavedSearch : Entity {
 public Guid AccountId { get; set; } public Account Account { get; set; } = null!;
 [MaxLength(120)] public string Name { get; set; } = "";
 [MaxLength(160)] public string Keywords { get; set; } = "";
 [MaxLength(120)] public string Location { get; set; } = "";
 public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class Notification : Entity {
 public Guid AccountId { get; set; } public Account Account { get; set; } = null!;
 [MaxLength(400)] public string Text { get; set; } = "";
 [MaxLength(200)] public string Link { get; set; } = "/applications";
 public bool Read { get; set; }
 public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class JobReport : Entity {
 public Guid JobId { get; set; } public Job Job { get; set; } = null!;
 public Guid ReporterId { get; set; } public Account Reporter { get; set; } = null!;
 [MaxLength(2000)] public string Reason { get; set; } = "";
 public bool Resolved { get; set; }
 public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class AccountToken : Entity {
 public Guid AccountId { get; set; } public Account Account { get; set; } = null!;
 [MaxLength(64)] public string Hash { get; set; } = "";
 [MaxLength(20)] public string Purpose { get; set; } = "";
 public DateTime ExpiresAt { get; set; }
}
public class MailItem : Entity {
 [MaxLength(254)] public string Recipient { get; set; } = "";
 [MaxLength(200)] public string Subject { get; set; } = "";
 [MaxLength(16000)] public string Body { get; set; } = "";
 public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
 public DateTime? SentAt { get; set; }
 public int Attempts { get; set; }
 public DateTime? LastAttemptAt { get; set; }
}
