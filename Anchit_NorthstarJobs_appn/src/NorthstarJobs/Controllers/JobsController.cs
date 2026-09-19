using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NorthstarJobs.Data;
using NorthstarJobs.Models;
using NorthstarJobs.Services;
namespace NorthstarJobs.Controllers;
[ApiController,Route("api/jobs")]
public class JobsController(JobsDb db):ControllerBase {
 [HttpGet] public async Task<IActionResult> List([FromQuery]string? q,[FromQuery]string? location,[FromQuery]string? category,[FromQuery]string? type,[FromQuery]string? mode,[FromQuery]string? level,[FromQuery]int? salary,[FromQuery]string sort="newest",[FromQuery]int page=1){
  var query=Live();
  if(!string.IsNullOrWhiteSpace(q)){q=q.Trim();query=query.Where(j=>j.Title.Contains(q)||j.Skills.Contains(q)||j.Company.Name.Contains(q));}
  if(!string.IsNullOrWhiteSpace(location))query=query.Where(j=>j.Location.Contains(location));
  if(!string.IsNullOrWhiteSpace(category))query=query.Where(j=>j.Category==category);
  if(!string.IsNullOrWhiteSpace(type))query=query.Where(j=>j.Type==type);
  if(!string.IsNullOrWhiteSpace(mode))query=query.Where(j=>j.WorkMode==mode);
  if(!string.IsNullOrWhiteSpace(level))query=query.Where(j=>j.Level==level);
  if(salary.HasValue)query=query.Where(j=>j.SalaryMax>=salary&&j.Currency=="NZD");
  var count=await query.CountAsync();page=Math.Clamp(page,1,10000);
  var ordered=sort=="salary"?query.OrderByDescending(j=>j.SalaryMax).ThenBy(j=>j.Id):query.OrderByDescending(j=>j.PublishedAt).ThenBy(j=>j.Id);
  var rows=await ordered.Skip((page-1)*12).Take(12).Select(j=>new{j.Id,j.Title,j.Location,j.Category,j.Type,j.WorkMode,j.Level,j.SalaryMin,j.SalaryMax,j.Currency,j.Skills,j.PublishedAt,j.ClosesAt,company=new{j.Company.Id,j.Company.Name,j.Company.Verified}}).ToListAsync();
  return Ok(new{items=rows,total=count,page,pageSize=12});
 }
 private IQueryable<Job> Live()=>db.Jobs.AsNoTracking().Where(j=>j.Status=="Published"&&j.ClosesAt>DateTime.UtcNow&&!j.Company.Owner.Suspended);
 [HttpGet("{id:guid}")]public async Task<IActionResult> Detail(Guid id){
  var j=await db.Jobs.AsNoTracking().Include(x=>x.Company).ThenInclude(x=>x.Owner).SingleOrDefaultAsync(x=>x.Id==id);
  if(j is null)return NotFound();
  var owner=User.Identity?.IsAuthenticated==true&&(j.Company.OwnerId==AccountService.Id(User)||User.IsInRole(Roles.Admin));
  if(!owner&&(j.Status!="Published"||j.ClosesAt<=DateTime.UtcNow||j.Company.Owner.Suspended))return NotFound(new{message="This job is no longer available."});
  return Ok(new{j.Id,j.Title,j.Location,j.Category,j.Type,j.WorkMode,j.Level,j.SalaryMin,j.SalaryMax,j.Currency,j.Description,j.Requirements,j.Benefits,j.Skills,j.Status,j.PublishedAt,j.ClosesAt,j.RowVersion,company=new{j.Company.Id,j.Company.Name,j.Company.Location,j.Company.Description,j.Company.Website,j.Company.Industry,j.Company.Verified}});
 }
 [Authorize(Roles=Roles.Employer),HttpGet("mine")]public async Task<IActionResult> Mine(){var uid=AccountService.Id(User);return Ok(await db.Jobs.Where(x=>x.Company.OwnerId==uid).OrderByDescending(x=>x.CreatedAt).Select(j=>new{j.Id,j.Title,j.Location,j.Status,j.ClosesAt,j.CreatedAt,j.RowVersion,applicationCount=db.Applications.Count(a=>a.JobId==j.Id),activeCount=db.Applications.Count(a=>a.JobId==j.Id&&a.Status!="Rejected"&&a.Status!="Withdrawn")}).ToListAsync());}
 [Authorize(Roles=Roles.Employer),HttpPost]public async Task<IActionResult> Create(JobRequest r){var company=await db.Companies.SingleAsync(x=>x.OwnerId==AccountService.Id(User));var error=Validate(r);if(error!=null)return BadRequest(new{message=error});var j=new Job{CompanyId=company.Id};Copy(r,j);db.Jobs.Add(j);await db.SaveChangesAsync();return Created($"/api/jobs/{j.Id}",new{j.Id});}
 [Authorize(Roles=Roles.Employer),HttpPut("{id:guid}")]public async Task<IActionResult> Edit(Guid id,JobRequest r){var j=await db.Jobs.Include(x=>x.Company).SingleOrDefaultAsync(x=>x.Id==id&&x.Company.OwnerId==AccountService.Id(User));if(j is null)return NotFound();if(j.Status=="Hidden")return Conflict(new{message="This listing is under administrator review."});if(Convert.ToBase64String(j.RowVersion)!=r.RowVersion)return Conflict(new{message="This listing changed. Reload before editing."});var error=Validate(r);if(error!=null)return BadRequest(new{message=error});Copy(r,j);await db.SaveChangesAsync();return Ok(new{j.Id});}
 private static string? Validate(JobRequest r){if(r.ClosesAt<=DateTime.UtcNow&&r.Status=="Published")return "Choose a future closing date.";if(r.SalaryMin.HasValue!=r.SalaryMax.HasValue||r.SalaryMin>r.SalaryMax)return "Enter both salary limits, with minimum no higher than maximum.";return null;}
 private static void Copy(JobRequest r,Job j){j.Title=r.Title.Trim();j.Location=r.Location.Trim();j.Category=r.Category;j.Type=r.Type;j.WorkMode=r.WorkMode;j.Level=r.Level;j.SalaryMin=r.SalaryMin;j.SalaryMax=r.SalaryMax;j.Currency=r.Currency;j.Description=r.Description;j.Requirements=r.Requirements;j.Benefits=r.Benefits;j.Skills=r.Skills;if(r.Status=="Published"&&j.PublishedAt==null)j.PublishedAt=DateTime.UtcNow;j.Status=r.Status;j.ClosesAt=r.ClosesAt.ToUniversalTime();}
 [Authorize(Roles=Roles.Seeker),HttpPut("{id:guid}/save")]public async Task<IActionResult> Save(Guid id){var uid=AccountService.Id(User);if(!await Live().AnyAsync(x=>x.Id==id))return NotFound();if(!await db.SavedJobs.AnyAsync(x=>x.JobId==id&&x.AccountId==uid)){db.SavedJobs.Add(new SavedJob{AccountId=uid,JobId=id});await db.SaveChangesAsync();}return NoContent();}
 [Authorize(Roles=Roles.Seeker),HttpDelete("{id:guid}/save")]public async Task<IActionResult> Unsave(Guid id){var row=await db.SavedJobs.SingleOrDefaultAsync(x=>x.JobId==id&&x.AccountId==AccountService.Id(User));if(row!=null){db.SavedJobs.Remove(row);await db.SaveChangesAsync();}return NoContent();}
 [Authorize(Roles=Roles.Seeker),HttpGet("saved")]public async Task<IActionResult> Saved(){var uid=AccountService.Id(User);return Ok(await db.SavedJobs.Where(x=>x.AccountId==uid).OrderByDescending(x=>x.CreatedAt).Select(x=>new{x.Job.Id,x.Job.Title,x.Job.Location,x.Job.Type,x.Job.WorkMode,x.Job.SalaryMin,x.Job.SalaryMax,x.Job.Currency,x.Job.Skills,x.Job.PublishedAt,x.Job.ClosesAt,x.Job.Status,available=x.Job.Status=="Published"&&x.Job.ClosesAt>DateTime.UtcNow&&!x.Job.Company.Owner.Suspended,company=new{x.Job.Company.Id,x.Job.Company.Name,x.Job.Company.Verified}}).ToListAsync());}
 [Authorize,HttpPost("{id:guid}/report")]public async Task<IActionResult> Report(Guid id,ReportRequest r){if(!await Live().AnyAsync(x=>x.Id==id))return NotFound();db.JobReports.Add(new JobReport{JobId=id,ReporterId=AccountService.Id(User),Reason=r.Reason});await db.SaveChangesAsync();return Ok(new{message="Report sent to the moderation team."});}
}
