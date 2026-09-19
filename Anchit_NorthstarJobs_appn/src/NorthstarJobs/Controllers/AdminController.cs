using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NorthstarJobs.Data;
using NorthstarJobs.Models;
using NorthstarJobs.Services;
namespace NorthstarJobs.Controllers;
[ApiController,Authorize(Roles=Roles.Admin),Route("api/admin")]
public class AdminController(JobsDb db):ControllerBase {
 [HttpGet]public async Task<object> Dashboard()=>new{
  accounts=await db.Accounts.OrderByDescending(x=>x.CreatedAt).Take(200).Select(x=>new{x.Id,x.Name,x.Email,x.Role,x.Suspended,x.CreatedAt}).ToListAsync(),
  companies=await db.Companies.OrderBy(x=>x.Name).Select(x=>new{x.Id,x.Name,x.Verified,x.Website}).ToListAsync(),
  jobs=await db.Jobs.OrderByDescending(x=>x.CreatedAt).Take(200).Select(x=>new{x.Id,x.Title,x.Status,company=x.Company.Name}).ToListAsync(),
  reports=await db.JobReports.Where(x=>!x.Resolved).OrderBy(x=>x.CreatedAt).Select(x=>new{x.Id,x.JobId,x.Reason,x.CreatedAt,jobTitle=x.Job.Title}).ToListAsync(),
  failedEmails=await db.MailItems.CountAsync(x=>x.SentAt==null&&x.Attempts>=5)};
 [HttpPost("accounts/{id:guid}/suspend")]public async Task<IActionResult> Suspend(Guid id,SuspendRequest r){var a=await db.Accounts.SingleOrDefaultAsync(x=>x.Id==id);if(a is null)return NotFound();if(a.Role==Roles.Admin)return BadRequest(new{message="Administrator accounts cannot be suspended here."});a.Suspended=r.Suspended;a.SecurityStamp=Guid.NewGuid();await db.SaveChangesAsync();return NoContent();}
 [HttpPost("companies/{id:guid}/verify")]public async Task<IActionResult> Verify(Guid id,VerifyRequest r){var c=await db.Companies.SingleOrDefaultAsync(x=>x.Id==id);if(c is null)return NotFound();c.Verified=r.Verified;await db.SaveChangesAsync();return NoContent();}
 [HttpPost("jobs/{id:guid}/hide")]public async Task<IActionResult> Hide(Guid id){var j=await db.Jobs.SingleOrDefaultAsync(x=>x.Id==id);if(j is null)return NotFound();j.Status="Hidden";await db.SaveChangesAsync();return NoContent();}
 [HttpPost("jobs/{id:guid}/restore")]public async Task<IActionResult> Restore(Guid id){var j=await db.Jobs.SingleOrDefaultAsync(x=>x.Id==id);if(j is null)return NotFound();if(j.Status!="Hidden")return BadRequest();j.Status="Draft";await db.SaveChangesAsync();return NoContent();}
 [HttpPost("reports/{id:guid}/resolve")]public async Task<IActionResult> Resolve(Guid id){var r=await db.JobReports.SingleOrDefaultAsync(x=>x.Id==id);if(r is null)return NotFound();r.Resolved=true;await db.SaveChangesAsync();return NoContent();}
 [HttpPost("mail/retry")]public async Task<IActionResult> RetryMail(){await db.MailItems.Where(x=>x.SentAt==null&&x.Attempts>=5).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Attempts,0).SetProperty(x=>x.LastAttemptAt,(DateTime?)null));return NoContent();}
}
