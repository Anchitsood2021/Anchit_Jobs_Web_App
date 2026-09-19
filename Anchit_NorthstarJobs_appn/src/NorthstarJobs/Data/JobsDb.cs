using Microsoft.EntityFrameworkCore;
using NorthstarJobs.Models;
namespace NorthstarJobs.Data;
public class JobsDb(DbContextOptions<JobsDb> options) : DbContext(options) {
 public DbSet<Account> Accounts => Set<Account>(); public DbSet<Company> Companies => Set<Company>();
 public DbSet<Job> Jobs => Set<Job>(); public DbSet<Resume> Resumes => Set<Resume>();
 public DbSet<Application> Applications => Set<Application>(); public DbSet<ApplicationEvent> ApplicationEvents => Set<ApplicationEvent>();
 public DbSet<Feedback> Feedback => Set<Feedback>(); public DbSet<Message> Messages => Set<Message>();
 public DbSet<SavedJob> SavedJobs => Set<SavedJob>(); public DbSet<SavedSearch> SavedSearches => Set<SavedSearch>();
 public DbSet<Notification> Notifications => Set<Notification>(); public DbSet<JobReport> JobReports => Set<JobReport>();
 public DbSet<AccountToken> AccountTokens => Set<AccountToken>(); public DbSet<MailItem> MailItems => Set<MailItem>();
 protected override void OnModelCreating(ModelBuilder b) {
  b.Entity<Account>().HasIndex(x=>x.NormalizedEmail).IsUnique();
  b.Entity<Company>().HasIndex(x=>x.OwnerId).IsUnique();
  b.Entity<Application>().HasIndex(x=>new{x.JobId,x.SeekerId}).IsUnique();
  b.Entity<SavedJob>().HasKey(x=>new{x.AccountId,x.JobId});
  b.Entity<AccountToken>().HasIndex(x=>x.Hash).IsUnique();
  b.Entity<Job>().HasIndex(x=>new{x.Status,x.ClosesAt});
  b.Entity<Notification>().HasIndex(x=>new{x.AccountId,x.Read,x.CreatedAt});
  b.Entity<MailItem>().HasIndex(x=>new{x.SentAt,x.Attempts});
  b.Entity<Feedback>().HasIndex(x=>new{x.ApplicationId,x.CreatedAt});
  b.Entity<Job>().ToTable(t=>t.HasCheckConstraint("CK_Job_Salary","([SalaryMin] IS NULL AND [SalaryMax] IS NULL) OR ([SalaryMin] IS NOT NULL AND [SalaryMax] IS NOT NULL AND [SalaryMin] >= 0 AND [SalaryMax] >= [SalaryMin])"));
  b.Entity<Feedback>().ToTable(t=>t.HasCheckConstraint("CK_Feedback_Scores","[SkillsScore] BETWEEN 1 AND 5 AND [ExperienceScore] BETWEEN 1 AND 5 AND [CommunicationScore] BETWEEN 1 AND 5"));
  foreach(var fk in b.Model.GetEntityTypes().SelectMany(e=>e.GetForeignKeys())) fk.DeleteBehavior=DeleteBehavior.Restrict;
 }
}
