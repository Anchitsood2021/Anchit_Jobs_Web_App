using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NorthstarJobs.Data;
using NorthstarJobs.Models;
using NorthstarJobs.Services;
namespace NorthstarJobs.Controllers;
[ApiController,Authorize,Route("api/profile")]
public class ProfileController(JobsDb db):ControllerBase {
 private Guid Uid=>AccountService.Id(User);
 [HttpPut]public async Task<IActionResult> Edit(ProfileRequest r){var a=await db.Accounts.SingleAsync(x=>x.Id==Uid);a.Name=r.Name.Trim();a.Headline=r.Headline;a.Location=r.Location;a.Bio=r.Bio;a.Skills=r.Skills;await db.SaveChangesAsync();return Ok(AccountService.Public(a));}
 [Authorize(Roles=Roles.Employer),HttpGet("company")]public async Task<object> Company()=>await db.Companies.Where(x=>x.OwnerId==Uid).Select(x=>new{x.Name,x.Description,x.Website,x.Location,x.Industry,x.Verified}).SingleAsync();
 [Authorize(Roles=Roles.Employer),HttpPut("company")]public async Task<IActionResult> CompanyEdit(CompanyRequest r){if(!string.IsNullOrWhiteSpace(r.Website)&&(!Uri.TryCreate(r.Website,UriKind.Absolute,out var u)||u.Scheme!="https"))return BadRequest(new{message="Company website must be a complete https:// address."});var c=await db.Companies.SingleAsync(x=>x.OwnerId==Uid);if(c.Name!=r.Name||c.Website!=r.Website)c.Verified=false;c.Name=r.Name.Trim();c.Description=r.Description;c.Location=r.Location;c.Industry=r.Industry;c.Website=r.Website;await db.SaveChangesAsync();return NoContent();}
 [Authorize(Roles=Roles.Seeker),HttpGet("resumes")]public async Task<object> Resumes()=>await db.Resumes.Where(x=>x.OwnerId==Uid).OrderByDescending(x=>x.CreatedAt).Select(x=>new{x.Id,x.FileName,x.CreatedAt}).ToListAsync();
 [Authorize(Roles=Roles.Seeker),HttpPost("resumes"),RequestSizeLimit(6*1024*1024)]public async Task<IActionResult> Upload(IFormFile file){
  if(file.Length<5||file.Length>5*1024*1024||!Path.GetExtension(file.FileName).Equals(".pdf",StringComparison.OrdinalIgnoreCase))return BadRequest(new{message="Choose a PDF resume up to 5 MB."});
  if(await db.Resumes.CountAsync(x=>x.OwnerId==Uid)>=10)return BadRequest(new{message="You can store up to 10 resumes. Delete an unused resume first."});
  using var stream=new MemoryStream();await file.CopyToAsync(stream);var bytes=stream.ToArray();if(Encoding.ASCII.GetString(bytes,0,5)!="%PDF-")return BadRequest(new{message="The file is not a PDF."});
  // Signature validation is not malware scanning. Add a scanning/quarantine service before public launch.
  var name=Path.GetFileName(file.FileName);if(name.Length>200)name=name[..196]+".pdf";
  var resume=new Resume{OwnerId=Uid,FileName=name,Content=bytes};db.Resumes.Add(resume);await db.SaveChangesAsync();return Ok(new{resume.Id,resume.FileName,resume.CreatedAt});
 }
 [HttpGet("resumes/{id:guid}")]public async Task<IActionResult> Download(Guid id){var resume=await db.Resumes.SingleOrDefaultAsync(x=>x.Id==id);if(resume is null)return NotFound();var allowed=resume.OwnerId==Uid||await db.Applications.AnyAsync(x=>x.ResumeId==id&&x.Job.Company.OwnerId==Uid&&x.Status!="Withdrawn");if(!allowed)return NotFound();return File(resume.Content,"application/pdf",resume.FileName);}
 [Authorize(Roles=Roles.Seeker),HttpDelete("resumes/{id:guid}")]public async Task<IActionResult> Delete(Guid id){var r=await db.Resumes.SingleOrDefaultAsync(x=>x.Id==id&&x.OwnerId==Uid);if(r is null)return NotFound();if(await db.Applications.AnyAsync(x=>x.ResumeId==id))return Conflict(new{message="This resume is attached to an application and must be retained with it."});db.Resumes.Remove(r);await db.SaveChangesAsync();return NoContent();}
 [Authorize(Roles=Roles.Seeker),HttpGet("searches")]public async Task<object> Searches()=>await db.SavedSearches.Where(x=>x.AccountId==Uid).OrderByDescending(x=>x.CreatedAt).Select(x=>new{x.Id,x.Name,x.Keywords,x.Location}).ToListAsync();
 [Authorize(Roles=Roles.Seeker),HttpPost("searches")]public async Task<IActionResult> Search(SearchRequest r){if(await db.SavedSearches.CountAsync(x=>x.AccountId==Uid)>=20)return BadRequest(new{message="You can save up to 20 searches."});var s=new SavedSearch{AccountId=Uid,Name=r.Name,Keywords=r.Keywords,Location=r.Location};db.SavedSearches.Add(s);await db.SaveChangesAsync();return Ok(new{s.Id});}
 [Authorize(Roles=Roles.Seeker),HttpDelete("searches/{id:guid}")]public async Task<IActionResult> DeleteSearch(Guid id){var s=await db.SavedSearches.SingleOrDefaultAsync(x=>x.Id==id&&x.AccountId==Uid);if(s is null)return NotFound();db.SavedSearches.Remove(s);await db.SaveChangesAsync();return NoContent();}
 [HttpGet("notifications")]public async Task<object> Notifications()=>await db.Notifications.Where(x=>x.AccountId==Uid).OrderByDescending(x=>x.CreatedAt).Take(100).Select(x=>new{x.Id,x.Text,x.Link,x.Read,x.CreatedAt}).ToListAsync();
 [HttpPost("notifications/read")]public async Task<IActionResult> Read(){await db.Notifications.Where(x=>x.AccountId==Uid&&!x.Read).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Read,true));return NoContent();}
}
