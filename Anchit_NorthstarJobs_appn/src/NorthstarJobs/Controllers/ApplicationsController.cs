using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NorthstarJobs.Data;
using NorthstarJobs.Models;
using NorthstarJobs.Services;
namespace NorthstarJobs.Controllers;
[ApiController,Authorize,Route("api/applications")]
public class ApplicationsController(JobsDb db):ControllerBase {
 private Guid Uid=>AccountService.Id(User);
 private IQueryable<Application> Owned()=>db.Applications.Where(x=>x.SeekerId==Uid||x.Job.Company.OwnerId==Uid);
 [HttpGet]public async Task<object> List([FromQuery]Guid? jobId,[FromQuery]string? status){var q=Owned();if(jobId.HasValue)q=q.Where(x=>x.JobId==jobId);if(!string.IsNullOrWhiteSpace(status))q=q.Where(x=>x.Status==status);return await q.OrderByDescending(x=>x.UpdatedAt).Take(500).Select(x=>new{x.Id,x.JobId,x.Status,x.CreatedAt,x.UpdatedAt,x.ApplicantName,x.ApplicantSkills,x.RowVersion,jobTitle=x.Job.Title,companyName=x.Job.Company.Name,feedbackCount=db.Feedback.Count(f=>f.ApplicationId==x.Id)}).ToListAsync();}
 [HttpGet("{id:guid}")]public async Task<IActionResult> Detail(Guid id){
  var a=await Owned().Include(x=>x.Job).ThenInclude(x=>x.Company).SingleOrDefaultAsync(x=>x.Id==id);if(a is null)return NotFound();
  var events=await db.ApplicationEvents.Where(x=>x.ApplicationId==id).OrderBy(x=>x.CreatedAt).Select(x=>new{x.Status,x.Note,x.CreatedAt}).ToListAsync();
  var feedback=await db.Feedback.Where(x=>x.ApplicationId==id).OrderByDescending(x=>x.CreatedAt).Select(x=>new{x.SkillsScore,x.ExperienceScore,x.CommunicationScore,x.Strengths,x.Improvements,x.NextSteps,x.CreatedAt}).ToListAsync();
  var messages=await db.Messages.Where(x=>x.ApplicationId==id).OrderBy(x=>x.CreatedAt).Select(x=>new{x.Id,x.Body,x.CreatedAt,senderName=x.Sender.Name,mine=x.SenderId==Uid}).ToListAsync();
  return Ok(new{a.Id,a.JobId,a.Status,a.CreatedAt,a.UpdatedAt,a.CoverLetter,a.ApplicantName,a.ApplicantSkills,a.ApplicantBio,a.ResumeId,a.RowVersion,jobTitle=a.Job.Title,companyName=a.Job.Company.Name,isEmployer=a.Job.Company.OwnerId==Uid,events,feedback,messages});
 }
 [Authorize(Roles=Roles.Seeker),HttpPost("job/{jobId:guid}")]public async Task<IActionResult> Apply(Guid jobId,ApplyRequest r){
  await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
  var j=await db.Jobs.Include(x=>x.Company).ThenInclude(x=>x.Owner).SingleOrDefaultAsync(x=>x.Id==jobId);
  if(j is null||j.Status!="Published"||j.ClosesAt<=DateTime.UtcNow||j.Company.Owner.Suspended)return BadRequest(new{message="This job is no longer accepting applications."});
  if(await db.Applications.AnyAsync(x=>x.JobId==jobId&&x.SeekerId==Uid))return Conflict(new{message="You have already applied for this job."});
  if(!await db.Resumes.AnyAsync(x=>x.Id==r.ResumeId&&x.OwnerId==Uid))return BadRequest(new{message="Choose one of your uploaded resumes."});
  var user=await db.Accounts.SingleAsync(x=>x.Id==Uid);
  var a=new Application{JobId=jobId,SeekerId=Uid,ResumeId=r.ResumeId,CoverLetter=r.CoverLetter,ApplicantName=user.Name,ApplicantBio=user.Bio,ApplicantSkills=user.Skills};
  db.Applications.Add(a);db.ApplicationEvents.Add(new ApplicationEvent{ApplicationId=a.Id,Status="Submitted",Note="Application received by the employer."});
  Notify(j.Company.OwnerId,$"New application for {j.Title}.",a.Id);Notify(Uid,$"Your application for {j.Title} was submitted.",a.Id);
  await db.SaveChangesAsync();await tx.CommitAsync();return Created($"/api/applications/{a.Id}",new{a.Id});
 }
 [Authorize(Roles=Roles.Employer),HttpPost("{id:guid}/status")]public async Task<IActionResult> Status(Guid id,StatusRequest r){
  var a=await db.Applications.Include(x=>x.Job).SingleOrDefaultAsync(x=>x.Id==id&&x.Job.Company.OwnerId==Uid);if(a is null)return NotFound();
  if(Convert.ToBase64String(a.RowVersion)!=r.RowVersion)return Conflict(new{message="Application changed. Reload it first."});
  if(!Workflow.CanMove(a.Status,r.Status))return BadRequest(new{message=$"Cannot move from {a.Status} to {r.Status}."});
  a.Status=r.Status;a.UpdatedAt=DateTime.UtcNow;db.ApplicationEvents.Add(new ApplicationEvent{ApplicationId=id,Status=r.Status,Note=r.Note});Notify(a.SeekerId,$"{a.Job.Title}: application moved to {r.Status}.",id);await db.SaveChangesAsync();return NoContent();
 }
 [Authorize(Roles=Roles.Seeker),HttpPost("{id:guid}/withdraw")]public async Task<IActionResult> Withdraw(Guid id){var a=await db.Applications.Include(x=>x.Job).ThenInclude(x=>x.Company).SingleOrDefaultAsync(x=>x.Id==id&&x.SeekerId==Uid);if(a is null)return NotFound();if(a.Status is "Withdrawn" or "Hired" or "Rejected")return Conflict(new{message="This application is already closed."});a.Status="Withdrawn";a.UpdatedAt=DateTime.UtcNow;db.ApplicationEvents.Add(new ApplicationEvent{ApplicationId=id,Status="Withdrawn",Note="Withdrawn by the applicant."});Notify(a.Job.Company.OwnerId,$"An application for {a.Job.Title} was withdrawn.",id);await db.SaveChangesAsync();return NoContent();}
 [Authorize(Roles=Roles.Employer),HttpPost("{id:guid}/feedback")]public async Task<IActionResult> Feedback(Guid id,FeedbackRequest r){var a=await db.Applications.Include(x=>x.Job).SingleOrDefaultAsync(x=>x.Id==id&&x.Job.Company.OwnerId==Uid);if(a is null)return NotFound();if(a.Status=="Withdrawn")return Conflict(new{message="The applicant withdrew this application."});db.Feedback.Add(new Feedback{ApplicationId=id,SkillsScore=r.SkillsScore,ExperienceScore=r.ExperienceScore,CommunicationScore=r.CommunicationScore,Strengths=r.Strengths,Improvements=r.Improvements,NextSteps=r.NextSteps});a.UpdatedAt=DateTime.UtcNow;Notify(a.SeekerId,$"New employer feedback for {a.Job.Title}.",id);await db.SaveChangesAsync();return NoContent();}
 [HttpPost("{id:guid}/messages")]public async Task<IActionResult> Message(Guid id,MessageRequest r){var a=await Owned().Include(x=>x.Job).ThenInclude(x=>x.Company).SingleOrDefaultAsync(x=>x.Id==id);if(a is null)return NotFound();if(a.Status=="Withdrawn")return Conflict(new{message="Messaging is closed for withdrawn applications."});if(string.IsNullOrWhiteSpace(r.Body))return BadRequest(new{message="Enter a message."});db.Messages.Add(new Message{ApplicationId=id,SenderId=Uid,Body=r.Body.Trim()});a.UpdatedAt=DateTime.UtcNow;Notify(a.SeekerId==Uid?a.Job.Company.OwnerId:a.SeekerId,$"New message about {a.Job.Title}.",id);await db.SaveChangesAsync();return NoContent();}
 private void Notify(Guid accountId,string text,Guid applicationId)=>db.Notifications.Add(new Notification{AccountId=accountId,Text=text,Link=$"/applications/{applicationId}"});
}
