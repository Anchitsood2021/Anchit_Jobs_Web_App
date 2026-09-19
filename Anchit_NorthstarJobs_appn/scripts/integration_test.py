#!/usr/bin/env python3
"""Black-box API tests against a running Development instance with SQL Server.
Creates uniquely named test users/listings and retains them for inspection.
Run only against a disposable local test database, not production.
No third-party Python dependencies.
"""
import http.cookiejar, json, os, secrets, urllib.request, urllib.error, uuid
from datetime import datetime, timedelta, timezone
BASE=os.environ.get('TEST_BASE_URL','http://localhost:5080').rstrip('/')
if os.environ.get('ALLOW_TEST_WRITES')!='yes':
    raise SystemExit('Set ALLOW_TEST_WRITES=yes to acknowledge writes to a disposable test database.')
RUN=uuid.uuid4().hex[:12]
PASSWORD=secrets.token_urlsafe(24)
passed=[]
class Client:
    def __init__(self):
        self.opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
        self.csrf=None
    def call(self,path,method='GET',body=None,expected=200,csrf=True,raw=None,content_type=None):
        headers={}
        if method!='GET' and csrf:
            if self.csrf is None: self.csrf=self.call('/auth/csrf')['token']
            headers['X-CSRF-TOKEN']=self.csrf
        data=raw
        if body is not None:
            data=json.dumps(body).encode();headers['Content-Type']='application/json'
        if content_type:headers['Content-Type']=content_type
        req=urllib.request.Request(BASE+'/api'+path,data=data,headers=headers,method=method)
        try:
            response=self.opener.open(req,timeout=25);code=response.status;payload=response.read();ctype=response.headers.get('Content-Type','')
        except urllib.error.HTTPError as e:
            code=e.code;payload=e.read();ctype=e.headers.get('Content-Type','')
        assert code==expected,f'{method} {path}: expected {expected}, got {code}: {payload[:600]!r}'
        if not payload:return None
        return json.loads(payload) if 'json' in ctype else payload
    def register(self,role,suffix):
        self.email=f'test-{RUN}-{suffix}@example.invalid'
        self.call('/auth/register','POST',{'name':'Test '+suffix,'email':self.email,'password':PASSWORD,'role':role,'companyName':'Test Company '+suffix if role=='Employer' else None})
        self.csrf=None
        me=self.call('/auth/me')['user'];assert me is not None,'Run in Development with Site:RequireConfirmedEmail=false.'
        return me
    def upload(self,content=b'%PDF-1.4\n% test PDF signature only\n%%EOF',expected=200):
        boundary='Northstar'+uuid.uuid4().hex
        data=(f'--{boundary}\r\nContent-Disposition: form-data; name="file"; filename="test.pdf"\r\nContent-Type: application/pdf\r\n\r\n'.encode()+content+f'\r\n--{boundary}--\r\n'.encode())
        return self.call('/profile/resumes','POST',raw=data,content_type='multipart/form-data; boundary='+boundary,expected=expected)
def done(label):passed.append(label);print('PASS:',label)
anon=Client();employer=Client();other=Client();seeker=Client();stranger=Client()
anon.call('/jobs/mine',expected=401);done('Anonymous users cannot access hiring dashboard API')
anon.call('/auth/register','POST',{'name':'Bad admin','email':f'{RUN}@example.invalid','password':PASSWORD,'role':'Admin'},expected=400);done('Self-registration cannot create an administrator')
employer.register('Employer','employer');other.register('Employer','other');seeker.register('Seeker','seeker');stranger.register('Seeker','stranger')
seeker.call('/jobs/mine',expected=403);done('Job seekers cannot use employer endpoints')
employer.call('/profile','PUT',{'name':'Test','headline':'','location':'','bio':'','skills':''},csrf=False,expected=400);done('Mutating requests require an anti-forgery token')
job={'title':'Integration Data Engineer '+RUN,'location':'Auckland','category':'Technology','type':'Full-time','workMode':'Hybrid','level':'Mid-level','salaryMin':90000,'salaryMax':120000,'currency':'NZD','description':'A test-only role for verifying real application workflows and access controls.','requirements':'Experience with SQL, Python, and clear communication.','benefits':'Test only','skills':'SQL, Python','status':'Published','closesAt':(datetime.now(timezone.utc)+timedelta(days=5)).isoformat(),'rowVersion':None}
jid=employer.call('/jobs','POST',job,expected=201)['id'];stored=employer.call('/jobs/'+jid);job['rowVersion']=stored['rowVersion']
other.call('/jobs/'+jid,'PUT',job,expected=404);done('Employers cannot edit another employer’s job')
seeker.upload(b'not a PDF',expected=400);resume=seeker.upload();stranger.call('/profile/resumes/'+resume['id'],expected=404);done('PDF signature and resume ownership checks')
letter={'resumeId':resume['id'],'coverLetter':'I bring useful SQL and Python experience to this test role.'}
aid=seeker.call('/applications/job/'+jid,'POST',letter,expected=201)['id'];seeker.call('/applications/job/'+jid,'POST',letter,expected=409);done('Applications persist and duplicates are blocked')
other.call('/applications/'+aid,expected=404);stranger.call('/applications/'+aid,expected=404);employer.call('/profile/resumes/'+resume['id']);done('Only the applicant and owning employer can access an application and CV')
a=employer.call('/applications/'+aid)
employer.call('/applications/'+aid+'/status','POST',{'status':'Hired','note':'Invalid skip','rowVersion':a['rowVersion']},expected=400)
employer.call('/applications/'+aid+'/status','POST',{'status':'Interview','note':'Please share availability.','rowVersion':a['rowVersion']},expected=204)
employer.call('/applications/'+aid+'/status','POST',{'status':'Rejected','note':'Stale edit','rowVersion':a['rowVersion']},expected=409);done('Hiring workflow and optimistic concurrency checks')
employer.call('/applications/'+aid+'/feedback','POST',{'skillsScore':4,'experienceScore':3,'communicationScore':4,'strengths':'Clear practical SQL examples.','improvements':'Provide more production monitoring examples.','nextSteps':'Prepare a short project walkthrough.'},expected=204)
a=seeker.call('/applications/'+aid);assert len(a['feedback'])==1 and a['status']=='Interview';done('Employer feedback and status are visible to applicant')
seeker.call('/applications/'+aid+'/messages','POST',{'body':'Available next Tuesday.'},expected=204)
assert employer.call('/applications/'+aid)['messages'][-1]['body']=='Available next Tuesday.';done('Application conversation persists')
seeker.call('/applications/'+aid+'/withdraw','POST',expected=204)
employer.call('/profile/resumes/'+resume['id'],expected=404)
employer.call('/applications/'+aid+'/messages','POST',{'body':'Not permitted after withdrawal'},expected=409);done('Withdrawal ends employer CV access and closes messaging')
seeker.call('/profile/resumes/'+resume['id'],'DELETE',expected=409);done('Application resume snapshots cannot be deleted independently')
employer.call('/jobs/'+jid,'PUT',dict(job,status='Closed'),expected=200)
stranger.call('/applications/job/'+jid,'POST',letter,expected=400);done('Closed listings reject new applications')
print(json.dumps({'passed':len(passed),'checks':passed,'run':RUN},indent=2))
