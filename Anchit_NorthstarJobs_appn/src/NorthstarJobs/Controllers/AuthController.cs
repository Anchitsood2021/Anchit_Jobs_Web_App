using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using NorthstarJobs.Data;
using NorthstarJobs.Models;
using NorthstarJobs.Services;
namespace NorthstarJobs.Controllers;
[ApiController,Route("api/auth")]
public class AuthController(JobsDb db,AccountService accounts,IConfiguration config):ControllerBase {
 [HttpGet("csrf")] public object Csrf([FromServices]IAntiforgery antiforgery)=>new{token=antiforgery.GetAndStoreTokens(HttpContext).RequestToken};
 [HttpGet("me")] public async Task<object> Me(){if(User.Identity?.IsAuthenticated!=true)return new{user=(object?)null};return new{user=AccountService.Public(await db.Accounts.SingleAsync(x=>x.Id==AccountService.Id(User)))};}
 [HttpPost("register"),EnableRateLimiting("auth")]
 public async Task<IActionResult> Register(RegisterRequest r){
  if(r.Role==Roles.Employer&&string.IsNullOrWhiteSpace(r.CompanyName))return BadRequest(new{message="Enter your company name."});
  var normalized=AccountService.Normalize(r.Email);
  if(await db.Accounts.AnyAsync(x=>x.NormalizedEmail==normalized))return Conflict(new{message="An account with this email already exists. Sign in or reset your password."});
  var a=new Account{Name=r.Name.Trim(),Email=r.Email.Trim(),NormalizedEmail=normalized,Role=r.Role};accounts.SetPassword(a,r.Password);db.Accounts.Add(a);
  if(r.Role==Roles.Employer)db.Companies.Add(new Company{OwnerId=a.Id,Name=r.CompanyName!.Trim()});
  await accounts.QueueToken(a,"Verify");await db.SaveChangesAsync();
  if(!config.GetValue<bool>("Site:RequireConfirmedEmail")){await accounts.SignIn(HttpContext,a);return Ok(new{user=AccountService.Public(a)});}
  return Ok(new{requiresConfirmation=true,message="Account created. Check your email to confirm your address before signing in."});
 }
 [HttpPost("login"),EnableRateLimiting("auth")]
 public async Task<IActionResult> Login(LoginRequest r){
  var a=await db.Accounts.SingleOrDefaultAsync(x=>x.NormalizedEmail==AccountService.Normalize(r.Email));
  if(a is null||a.Suspended||a.LockoutUntil>DateTime.UtcNow)return Unauthorized(new{message="Unable to sign in. Check your details or try again later."});
  if(!accounts.Check(a,r.Password)){a.FailedLogins++;if(a.FailedLogins>=5){a.LockoutUntil=DateTime.UtcNow.AddMinutes(15);a.FailedLogins=0;}await db.SaveChangesAsync();return Unauthorized(new{message="Unable to sign in. Check your details or try again later."});}
  if(config.GetValue<bool>("Site:RequireConfirmedEmail")&&!a.EmailConfirmed)return StatusCode(403,new{message="Confirm your email before signing in. You can request another confirmation email."});
  a.FailedLogins=0;a.LockoutUntil=null;await db.SaveChangesAsync();await accounts.SignIn(HttpContext,a);return Ok(new{user=AccountService.Public(a)});
 }
 [Authorize,HttpPost("logout")]public async Task<IActionResult> Logout(){await HttpContext.SignOutAsync();return NoContent();}
 [HttpPost("forgot-password"),EnableRateLimiting("auth")]public Task<IActionResult> Forgot(EmailRequest r)=>SendToken(r,"Reset");
 [HttpPost("resend-confirmation"),EnableRateLimiting("auth")]public Task<IActionResult> Resend(EmailRequest r)=>SendToken(r,"Verify");
 private async Task<IActionResult> SendToken(EmailRequest r,string purpose){var a=await db.Accounts.SingleOrDefaultAsync(x=>x.NormalizedEmail==AccountService.Normalize(r.Email));if(a is not null&&!a.Suspended&&(purpose!="Verify"||!a.EmailConfirmed)){await accounts.QueueToken(a,purpose);await db.SaveChangesAsync();}return Ok(new{message="If the account is eligible, an email will arrive shortly."});}
 [HttpPost("verify-email"),EnableRateLimiting("auth")]public async Task<IActionResult> Verify(TokenRequest r){
  await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
  var t=await db.AccountTokens.Include(x=>x.Account).SingleOrDefaultAsync(x=>x.Hash==AccountService.Hash(r.Token)&&x.Purpose=="Verify"&&x.ExpiresAt>DateTime.UtcNow);
  if(t is null)return BadRequest(new{message="The link has expired or was already used."});t.Account.EmailConfirmed=true;db.AccountTokens.Remove(t);await db.SaveChangesAsync();await tx.CommitAsync();return Ok(new{message="Email confirmed. You can now sign in."});
 }
 [HttpPost("reset-password"),EnableRateLimiting("auth")]public async Task<IActionResult> Reset(ResetRequest r){
  await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
  var t=await db.AccountTokens.Include(x=>x.Account).SingleOrDefaultAsync(x=>x.Hash==AccountService.Hash(r.Token)&&x.Purpose=="Reset"&&x.ExpiresAt>DateTime.UtcNow);
  if(t is null||t.Account.Suspended)return BadRequest(new{message="The link has expired or was already used."});accounts.SetPassword(t.Account,r.Password);db.AccountTokens.Remove(t);await db.SaveChangesAsync();await tx.CommitAsync();return Ok(new{message="Password reset. Sign in with your new password."});
 }
 [Authorize,HttpPost("change-password")]public async Task<IActionResult> Change(PasswordRequest r){var a=await db.Accounts.SingleAsync(x=>x.Id==AccountService.Id(User));if(!accounts.Check(a,r.CurrentPassword))return BadRequest(new{message="Current password is incorrect."});accounts.SetPassword(a,r.NewPassword);await db.SaveChangesAsync();await accounts.SignIn(HttpContext,a);return NoContent();}
}
