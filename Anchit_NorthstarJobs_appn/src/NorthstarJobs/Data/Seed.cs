using System.Text;
using Microsoft.EntityFrameworkCore;
using NorthstarJobs.Models;
using NorthstarJobs.Services;
namespace NorthstarJobs.Data;
public static class Seed {
 public static async Task Run(IServiceProvider services){
  var db=services.GetRequiredService<JobsDb>();var accounts=services.GetRequiredService<AccountService>();var config=services.GetRequiredService<IConfiguration>();
  if(await db.Accounts.AnyAsync())throw new InvalidOperationException("Demo seeding requires an empty database.");
  var password=config["Demo:Password"];if(string.IsNullOrWhiteSpace(password)||password.Length<12)throw new InvalidOperationException("Set Demo__Password to a unique password of 12 or more characters.");
  var seeker=Make("Alex Morgan","seeker@northstar.example",Roles.Seeker);seeker.Headline="Data analyst · Turning questions into useful answers";seeker.Location="Auckland, New Zealand";seeker.Skills="Python, SQL, Power BI, Communication";seeker.Bio="A fictional demo applicant who enjoys making data useful and working with thoughtful teams.";
  var employer=Make("Jamie Taylor","employer@northstar.example",Roles.Employer);
  var employer2=Make("Sam Lee","employer2@northstar.example",Roles.Employer);
  var employer3=Make("Casey Patel","employer3@northstar.example",Roles.Employer);
  var admin=Make("Demo Administrator","admin@northstar.example",Roles.Admin);
  var companies=new[]{new Company{OwnerId=employer.Id,Name="Forma Labs",Location="Auckland, New Zealand",Industry="Technology",Description="Fictional demonstration company. Forma Labs builds thoughtful software and data products for teams across Aotearoa.",Verified=true},new Company{OwnerId=employer2.Id,Name="Evergreen Studio",Location="Wellington, New Zealand",Industry="Design",Description="Fictional demonstration company. A small independent studio focused on useful, accessible digital experiences."},new Company{OwnerId=employer3.Id,Name="Harbour Collective",Location="Christchurch, New Zealand",Industry="Operations",Description="Fictional demonstration company. We help growing businesses build sustainable operations.",Verified=true}};
  db.Companies.AddRange(companies);
  var data=new[]{
   ("Senior Data Analyst",0,"Auckland, New Zealand","Technology","Full-time","Hybrid","Senior",110000,140000,"SQL, Python"),
   ("Full Stack Developer",0,"Auckland, New Zealand","Technology","Full-time","Remote","Mid-level",100000,135000,".NET, React"),
   ("Product Designer",1,"Wellington, New Zealand","Design","Full-time","Hybrid","Mid-level",95000,120000,"Figma, Research"),
   ("Data Science Intern",0,"Auckland, New Zealand","Technology","Internship","On-site","Entry-level",55000,65000,"Python, Statistics"),
   ("Digital Marketing Specialist",2,"Christchurch, New Zealand","Marketing","Full-time","Hybrid","Mid-level",75000,95000,"Analytics, Content"),
   ("Platform Engineer",0,"Wellington, New Zealand","Technology","Full-time","Remote","Senior",130000,165000,"Azure, DevOps"),
   ("Operations Coordinator",2,"Christchurch, New Zealand","Operations","Part-time","On-site","Entry-level",55000,70000,"Planning, Excel"),
   ("UX Researcher",1,"Wellington, New Zealand","Design","Contract","Hybrid","Senior",105000,130000,"Research, Design")
  };
  var jobs=new List<Job>();var index=0;
  foreach(var (title,c,location,category,type,mode,level,min,max,skills) in data){var j=new Job{CompanyId=companies[c].Id,Title=title,Location=location,Category=category,Type=type,WorkMode=mode,Level=level,SalaryMin=min,SalaryMax=max,Skills=skills,Status="Published",PublishedAt=DateTime.UtcNow.AddDays(-index++),ClosesAt=DateTime.UtcNow.AddDays(30),Description=$"DEMONSTRATION LISTING — this is not a real vacancy.\n\nJoin {companies[c].Name} as a {title.ToLowerInvariant()} and help turn ambitious ideas into useful outcomes. You will work in a supportive team, own meaningful projects, and collaborate with colleagues who care about quality.\n\nYou’ll have room to ask questions, develop your skills, and contribute to how we work.",Requirements=$"• Practical experience with {skills}.\n• A clear approach to solving problems and explaining your thinking.\n• Ability to work collaboratively and ask for feedback.\n• Commitment to inclusive and accessible work.",Benefits="• Flexible working arrangements suited to the role.\n• Time and support for professional development.\n• A collaborative team and clear expectations."};jobs.Add(j);db.Jobs.Add(j);}
  var resume=new Resume{OwnerId=seeker.Id,FileName="Alex_Morgan_Demo_Resume.pdf",Content=DemoPdf()};db.Resumes.Add(resume);
  var a=new Application{JobId=jobs[0].Id,SeekerId=seeker.Id,ResumeId=resume.Id,ApplicantName=seeker.Name,ApplicantSkills=seeker.Skills,ApplicantBio=seeker.Bio,CoverLetter="Demo application: I enjoy turning data into clear recommendations. My recent work has focused on SQL analysis, Python data preparation, and communicating findings to non-technical audiences.",Status="Interview",CreatedAt=DateTime.UtcNow.AddDays(-4)};db.Applications.Add(a);
  db.ApplicationEvents.AddRange(new ApplicationEvent{ApplicationId=a.Id,Status="Submitted",Note="Application received.",CreatedAt=DateTime.UtcNow.AddDays(-4)},new ApplicationEvent{ApplicationId=a.Id,Status="Reviewing",Note="The hiring team is reviewing your experience.",CreatedAt=DateTime.UtcNow.AddDays(-3)},new ApplicationEvent{ApplicationId=a.Id,Status="Interview",Note="We would like to discuss your recent analytics project. Please message us with your availability.",CreatedAt=DateTime.UtcNow.AddDays(-1)});
  db.Feedback.Add(new Feedback{ApplicationId=a.Id,SkillsScore=4,ExperienceScore=3,CommunicationScore=4,Strengths="Your SQL examples are clear, and your project summary connects analysis to business decisions.",Improvements="For a senior role, add a concrete example of designing reliable data pipelines and checking data quality.",NextSteps="Prepare a ten-minute walkthrough of one project, including limitations, validation, and the decisions it supported."});
  db.Messages.Add(new Message{ApplicationId=a.Id,SenderId=employer.Id,Body="Thanks for your application, Alex. Could you share your availability for an introductory conversation next week?"});
  db.Notifications.Add(new Notification{AccountId=seeker.Id,Text="New feedback on your Senior Data Analyst application.",Link=$"/applications/{a.Id}"});
  db.SavedJobs.Add(new SavedJob{AccountId=seeker.Id,JobId=jobs[1].Id});
  await db.SaveChangesAsync();Console.WriteLine("Demo data created. Accounts: seeker, employer, employer2, employer3, admin @northstar.example. Use your supplied Demo__Password.");
  Account Make(string name,string email,string role){var account=new Account{Name=name,Email=email,NormalizedEmail=AccountService.Normalize(email),Role=role,EmailConfirmed=true};accounts.SetPassword(account,password);db.Accounts.Add(account);return account;}
 }
 public static async Task CreateAdmin(IServiceProvider services,IConfiguration config){
  var email=config["Bootstrap:Email"];var password=config["Bootstrap:Password"];
  if(string.IsNullOrWhiteSpace(email)||!System.Net.Mail.MailAddress.TryCreate(email,out _)||email.Length>254||password is null||password.Length<12)throw new InvalidOperationException("Set Bootstrap__Email and Bootstrap__Password (at least 12 characters).");
  var db=services.GetRequiredService<JobsDb>();if(await db.Accounts.AnyAsync(x=>x.NormalizedEmail==AccountService.Normalize(email)))throw new InvalidOperationException("That account already exists; no role was changed.");
  var account=new Account{Name="Site administrator",Email=email.Trim(),NormalizedEmail=AccountService.Normalize(email),Role=Roles.Admin,EmailConfirmed=true};services.GetRequiredService<AccountService>().SetPassword(account,password);db.Accounts.Add(account);await db.SaveChangesAsync();Console.WriteLine("Administrator created. Remove Bootstrap credentials from the environment now.");
 }
 private static byte[] DemoPdf(){
  var content="BT /F1 20 Tf 50 750 Td (Alex Morgan - demonstration resume) Tj 0 -35 Td /F1 12 Tf (Fictional profile. Skills: Python, SQL, Power BI.) Tj ET";
  var objects=new[]{"<< /Type /Catalog /Pages 2 0 R >>","<< /Type /Pages /Kids [3 0 R] /Count 1 >>","<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>","<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",$"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream"};
  var text=new StringBuilder("%PDF-1.4\n");var offsets=new List<int>{0};for(var i=0;i<objects.Length;i++){offsets.Add(Encoding.ASCII.GetByteCount(text.ToString()));text.Append($"{i+1} 0 obj\n{objects[i]}\nendobj\n");}var xref=Encoding.ASCII.GetByteCount(text.ToString());text.Append($"xref\n0 {objects.Length+1}\n0000000000 65535 f \n");foreach(var offset in offsets.Skip(1))text.Append(offset.ToString("D10")+" 00000 n \n");text.Append($"trailer\n<< /Size {objects.Length+1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");return Encoding.ASCII.GetBytes(text.ToString());
 }
}
