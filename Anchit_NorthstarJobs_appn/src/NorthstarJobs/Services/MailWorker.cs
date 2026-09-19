using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using NorthstarJobs.Data;
namespace NorthstarJobs.Services;
// Single worker deployment. For multiple replicas, replace polling with a leased queue.
public class MailWorker(IServiceScopeFactory scopes,IConfiguration config,IHostEnvironment env,ILogger<MailWorker> log):BackgroundService {
 protected override async Task ExecuteAsync(CancellationToken stop){
  while(!stop.IsCancellationRequested){
   try {
    using var scope=scopes.CreateScope();var db=scope.ServiceProvider.GetRequiredService<JobsDb>();
    var cutoff=DateTime.UtcNow.AddMinutes(-5);
    var batch=await db.MailItems.Where(x=>x.SentAt==null&&x.Attempts<5&&(x.LastAttemptAt==null||x.LastAttemptAt<cutoff)).OrderBy(x=>x.CreatedAt).Take(20).ToListAsync(stop);
    foreach(var item in batch){
     item.Attempts++;item.LastAttemptAt=DateTime.UtcNow;
     try {
      using var message=new MailMessage(config["Mail:From"] is {Length:>0} from?from:"local@northstar.invalid",item.Recipient,item.Subject,item.Body);
      using var smtp=new SmtpClient();
      if(env.IsDevelopment()&&config.GetValue<bool>("Mail:DevelopmentPickup")) {
       var dir=Path.GetFullPath("App_Data/mail");Directory.CreateDirectory(dir);smtp.DeliveryMethod=SmtpDeliveryMethod.SpecifiedPickupDirectory;smtp.PickupDirectoryLocation=dir;
      } else {
       if(string.IsNullOrWhiteSpace(config["Mail:Host"])) throw new InvalidOperationException("Configure Mail:Host to send email.");
       smtp.Host=config["Mail:Host"]!;smtp.Port=config.GetValue("Mail:Port",587);smtp.EnableSsl=config.GetValue("Mail:UseSsl",true);
       smtp.Credentials=new NetworkCredential(config["Mail:Username"],config["Mail:Password"]);
      }
      await smtp.SendMailAsync(message,stop); item.SentAt=DateTime.UtcNow;item.Body="Delivered (content removed).";
     }catch(Exception ex) when(!stop.IsCancellationRequested){log.LogWarning(ex,"Mail delivery failed for item {Id}",item.Id);}
     await db.SaveChangesAsync(stop);
    }
   }catch(Exception ex) when(!stop.IsCancellationRequested){log.LogWarning(ex,"Mail queue unavailable; retrying.");}
   await Task.Delay(TimeSpan.FromSeconds(30),stop);
  }
 }
}
