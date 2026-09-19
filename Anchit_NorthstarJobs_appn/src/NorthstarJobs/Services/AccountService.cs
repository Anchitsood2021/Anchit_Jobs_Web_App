using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NorthstarJobs.Data;
using NorthstarJobs.Models;
namespace NorthstarJobs.Services;
public class AccountService(JobsDb db,IPasswordHasher<Account> hasher,IConfiguration config) {
 public static Guid Id(ClaimsPrincipal p)=>Guid.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!);
 public static string Normalize(string email)=>email.Trim().ToUpperInvariant();
 public static object Public(Account a)=>new{a.Id,a.Email,a.Name,a.Role,a.EmailConfirmed,a.Headline,a.Location,a.Bio,a.Skills};
 public async Task SignIn(HttpContext context,Account a) {
  var claims=new[]{new Claim(ClaimTypes.NameIdentifier,a.Id.ToString()),new Claim(ClaimTypes.Name,a.Name),new Claim(ClaimTypes.Role,a.Role),new Claim("stamp",a.SecurityStamp.ToString())};
  await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)),new AuthenticationProperties{IsPersistent=false});
 }
 public bool Check(Account a,string password)=>hasher.VerifyHashedPassword(a,a.PasswordHash,password)!=PasswordVerificationResult.Failed;
 public void SetPassword(Account a,string password){a.PasswordHash=hasher.HashPassword(a,password);a.SecurityStamp=Guid.NewGuid();a.FailedLogins=0;a.LockoutUntil=null;}
 public async Task QueueToken(Account a,string purpose) {
  var old=await db.AccountTokens.Where(x=>x.AccountId==a.Id&&x.Purpose==purpose).ToListAsync(); db.AccountTokens.RemoveRange(old);
  var token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
  db.AccountTokens.Add(new AccountToken{AccountId=a.Id,Hash=Hash(token),Purpose=purpose,ExpiresAt=DateTime.UtcNow.AddHours(purpose=="Reset"?1:24)});
  var route=purpose=="Reset"?"reset-password":"verify-email";
  var link=$"{config["Site:BaseUrl"]?.TrimEnd('/')}/{route}#token={token}";
  db.MailItems.Add(new MailItem{Recipient=a.Email,Subject=purpose=="Reset"?"Reset your Northstar password":"Confirm your Northstar email",Body=$"Hello {a.Name},\n\nOpen this link to {(purpose=="Reset"?"reset your password":"confirm your email")}:\n{link}\n\nIf you did not request this, ignore this email. This link expires in {(purpose=="Reset"?"one hour":"24 hours")}."});
 }
 public static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
