using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using NorthstarJobs.Data;
using NorthstarJobs.Models;
using NorthstarJobs.Services;
var startupArgs=args.Where(x=>x is not "--init-db" and not "--seed-demo" and not "--export-sql" and not "--create-admin").ToArray();
var builder=WebApplication.CreateBuilder(startupArgs);
var connection=builder.Configuration.GetConnectionString("Jobs");
if(string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("Set ConnectionStrings__Jobs or use dotnet user-secrets. See README.md.");
builder.Services.AddDbContext<JobsDb>(o=>o.UseSqlServer(connection));
builder.Services.AddScoped<IPasswordHasher<Account>,PasswordHasher<Account>>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddHostedService<MailWorker>();
builder.Services.AddControllersWithViews(o=>o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAntiforgery(o=> {o.HeaderName="X-CSRF-TOKEN"; o.Cookie.SameSite=SameSiteMode.Strict; o.Cookie.SecurePolicy=builder.Environment.IsDevelopment()?CookieSecurePolicy.SameAsRequest:CookieSecurePolicy.Always;});
var keyPath=Path.GetFullPath(builder.Configuration["Site:KeyPath"]??"App_Data/keys"); Directory.CreateDirectory(keyPath);
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keyPath)).SetApplicationName("NorthstarJobs");
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o=>{
 o.Cookie.Name="Northstar.Session"; o.Cookie.HttpOnly=true; o.Cookie.SameSite=SameSiteMode.Strict;
 o.Cookie.SecurePolicy=builder.Environment.IsDevelopment()?CookieSecurePolicy.SameAsRequest:CookieSecurePolicy.Always;
 o.ExpireTimeSpan=TimeSpan.FromHours(8); o.SlidingExpiration=false;
 o.Events.OnRedirectToLogin=c=>{c.Response.StatusCode=401;return Task.CompletedTask;};
 o.Events.OnRedirectToAccessDenied=c=>{c.Response.StatusCode=403;return Task.CompletedTask;};
 o.Events.OnValidatePrincipal=async c=>{
  if(!Guid.TryParse(c.Principal?.FindFirstValue(ClaimTypes.NameIdentifier),out var id)){c.RejectPrincipal();return;}
  var db=c.HttpContext.RequestServices.GetRequiredService<JobsDb>();
  var a=await db.Accounts.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);
  if(a is null||a.Suspended||a.SecurityStamp.ToString()!=c.Principal?.FindFirstValue("stamp")) {c.RejectPrincipal(); await c.HttpContext.SignOutAsync();}
 };
});
builder.Services.AddAuthorization();
builder.Services.Configure<ForwardedHeadersOptions>(o=>{
 o.ForwardedHeaders=ForwardedHeaders.XForwardedFor|ForwardedHeaders.XForwardedProto;
 foreach(var proxy in builder.Configuration.GetSection("Site:TrustedProxies").Get<string[]>()??[])
  o.KnownProxies.Add(System.Net.IPAddress.Parse(proxy));
});
builder.Services.AddRateLimiter(o=>{
 o.RejectionStatusCode=429;
 o.AddPolicy("auth",ctx=>RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString()??"unknown",_=>new FixedWindowRateLimiterOptions{PermitLimit=12,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));
 o.GlobalLimiter=PartitionedRateLimiter.Create<HttpContext,string>(ctx=>RateLimitPartition.GetFixedWindowLimiter(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)??ctx.Connection.RemoteIpAddress?.ToString()??"unknown",_=>new FixedWindowRateLimiterOptions{PermitLimit=240,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));
});
builder.WebHost.ConfigureKestrel(o=>o.Limits.MaxRequestBodySize=6*1024*1024);
var app=builder.Build();
if(args.Contains("--export-sql")) {
 using var scope=app.Services.CreateScope(); var db=scope.ServiceProvider.GetRequiredService<JobsDb>();
 Console.WriteLine(db.Database.GenerateCreateScript()); return;
}
if(args.Contains("--init-db")) {
 using var scope=app.Services.CreateScope(); var db=scope.ServiceProvider.GetRequiredService<JobsDb>();
 await db.Database.EnsureCreatedAsync(); Console.WriteLine("Database initialized. Existing schemas are not modified.");
 if(args.Contains("--seed-demo")) {if(!app.Environment.IsDevelopment()) throw new InvalidOperationException("Demo seeding is Development only."); await Seed.Run(scope.ServiceProvider);}
 return;
}
if(args.Contains("--create-admin")) {
 using var scope=app.Services.CreateScope(); await Seed.CreateAdmin(scope.ServiceProvider,builder.Configuration); return;
}
app.UseForwardedHeaders();
app.Use(async(ctx,next)=>{
 ctx.Response.Headers["X-Content-Type-Options"]="nosniff";
 ctx.Response.Headers["Referrer-Policy"]="same-origin";
 ctx.Response.Headers["Content-Security-Policy"]="default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'; object-src 'none'";
 if(ctx.Request.Path.StartsWithSegments("/api")) ctx.Response.Headers.CacheControl="no-store";
 try { await next(); }
 catch(DbUpdateConcurrencyException){ctx.Response.StatusCode=409;await ctx.Response.WriteAsJsonAsync(new{message="This record changed. Reload it and try again."});}
 catch(DbUpdateException ex) when(ex.InnerException is Microsoft.Data.SqlClient.SqlException sql && (sql.Number==2601||sql.Number==2627)) {ctx.Response.StatusCode=409;await ctx.Response.WriteAsJsonAsync(new{message="This record already exists. Refresh and try again."});}
 catch(Exception ex){app.Logger.LogError(ex,"Request failed: {TraceId}",ctx.TraceIdentifier); if(!ctx.Response.HasStarted){ctx.Response.StatusCode=500;await ctx.Response.WriteAsJsonAsync(new{message="The request could not be completed. Please try again.",traceId=ctx.TraceIdentifier});}}
});
if(!app.Environment.IsDevelopment()){app.UseHsts();app.UseHttpsRedirection();}
app.UseDefaultFiles(); app.UseStaticFiles(); app.UseRouting(); app.UseAuthentication(); app.UseAuthorization(); app.UseRateLimiter();
app.MapControllers();
app.MapGet("/health",()=>Results.Ok(new{status="running"}));
app.MapGet("/health/ready",async (JobsDb db)=>await db.Database.CanConnectAsync()?Results.Ok(new{status="ready"}):Results.StatusCode(503));
app.Map("/api/{**path}",()=>Results.NotFound(new{message="API endpoint not found."}));
app.MapFallbackToFile("index.html"); app.Run();
public partial class Program { }
