using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NorthstarJobs.Data;
using NorthstarJobs.Models;
using NorthstarJobs.Services;
using Xunit;
namespace NorthstarJobs.Tests;
public class DomainTests {
 [Theory]
 [InlineData("Submitted","Reviewing",true)]
 [InlineData("Submitted","Hired",false)]
 [InlineData("Shortlisted","Offer",true)]
 [InlineData("Interview","Offer",true)]
 [InlineData("Offer","Hired",true)]
 [InlineData("Rejected","Interview",false)]
 [InlineData("Withdrawn","Reviewing",false)]
 [InlineData("Hired","Rejected",false)]
 [InlineData("Reviewing","Submitted",false)]
 public void HiringStagesDoNotReopenClosedApplications(string from,string to,bool expected)=>Assert.Equal(expected,Workflow.CanMove(from,to));
 [Fact]public void PasswordHashesAreSaltedAndVerify(){var hasher=new PasswordHasher<Account>();var a=new Account();var x=hasher.HashPassword(a,"Sample-Test-Password-123");var y=hasher.HashPassword(a,"Sample-Test-Password-123");Assert.NotEqual(x,y);Assert.NotEqual(PasswordVerificationResult.Failed,hasher.VerifyHashedPassword(a,x,"Sample-Test-Password-123"));Assert.Equal(PasswordVerificationResult.Failed,hasher.VerifyHashedPassword(a,x,"Wrong-password"));}
 [Fact]public void RegistrationCannotChooseAdmin(){var r=new RegisterRequest("Test User","person@example.com","Valid-test-password","Admin",null);Assert.False(Validator.TryValidateObject(r,new ValidationContext(r),new List<ValidationResult>(),true));}
 [Theory][InlineData(0)][InlineData(6)]public void FeedbackRejectsScoresOutsideScale(int score){var r=new FeedbackRequest(score,3,3,"Clear and useful strengths","Specific areas to improve","Practical recommended next steps");Assert.False(Validator.TryValidateObject(r,new ValidationContext(r),new List<ValidationResult>(),true));}
 [Fact]public void SchemaHasApplicationUniquenessAndConcurrency(){using var db=new JobsDb(new DbContextOptionsBuilder<JobsDb>().UseSqlServer("Server=localhost;Database=Unused;Integrated Security=True;TrustServerCertificate=True").Options);var entity=db.Model.FindEntityType(typeof(Application))!;Assert.Contains(entity.GetIndexes(),i=>i.IsUnique&&i.Properties.Select(p=>p.Name).SequenceEqual(new[]{"JobId","SeekerId"}));Assert.True(entity.FindProperty("RowVersion")!.IsConcurrencyToken);Assert.All(db.Model.GetEntityTypes().SelectMany(e=>e.GetForeignKeys()),fk=>Assert.Equal(DeleteBehavior.Restrict,fk.DeleteBehavior));var sql=db.Database.GenerateCreateScript();Assert.Contains("CREATE TABLE [Accounts]",sql);Assert.Contains("rowversion",sql);Assert.Contains("CK_Feedback_Scores",sql);}
 [Fact]public void EmailNormalizationIsConsistent()=>Assert.Equal("NAME@EXAMPLE.COM",AccountService.Normalize(" Name@example.com "));
}
