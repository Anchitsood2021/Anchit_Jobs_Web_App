namespace NorthstarJobs.Services;
public static class Workflow {
 public static bool CanMove(string from,string to)=>from switch {
  "Submitted"=>to is "Reviewing" or "Shortlisted" or "Interview" or "Rejected",
  "Reviewing"=>to is "Shortlisted" or "Interview" or "Rejected",
  "Shortlisted"=>to is "Interview" or "Offer" or "Rejected",
  "Interview"=>to is "Offer" or "Rejected",
  "Offer"=>to is "Hired" or "Rejected",
  _=>false
 };
}
