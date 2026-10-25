"""Real HTTP smoke test. Uses an ephemeral password and synthetic local SQLite data."""
import http.cookiejar
import json
import os
from pathlib import Path
import secrets
import subprocess
import tempfile
import time
import urllib.error
import urllib.request

root = Path(__file__).resolve().parents[1]
dotnet = os.environ.get('DOTNET_EXE', 'dotnet')
password = secrets.token_urlsafe(32)
with tempfile.TemporaryDirectory(prefix='linkkeeper-http-') as temp:
    env = dict(os.environ, LINKKEEPER_PASSWORD=password, LINKKEEPER_DB=temp+'/links.db',
               ASPNETCORE_ENVIRONMENT='Development', ASPNETCORE_URLS='http://127.0.0.1:5017',
               Logging__LogLevel__Default='Warning',
               Categories__0__Name='pesquisa', Categories__0__DefaultDays='14',
               Categories__0__Aliases__0='research')
    process = subprocess.Popen([dotnet, str(root/'src/LinkKeeper.Web/bin/Release/net10.0/LinkKeeper.Web.dll')],
        cwd=root/'src/LinkKeeper.Web', env=env, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    client = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
    def call(path, method='GET', data=None, csrf=None):
        headers = {'Content-Type':'application/json'}
        if csrf: headers['X-CSRF-TOKEN'] = csrf
        request = urllib.request.Request('http://127.0.0.1:5017'+path,
            data=None if data is None else json.dumps(data).encode(), method=method, headers=headers)
        try:
            with client.open(request, timeout=5) as response:
                body=response.read(); return response.status, json.loads(body) if body and response.headers.get_content_type()=='application/json' else body
        except urllib.error.HTTPError as error:
            return error.code, None
    try:
        for attempt in range(50):
            try:
                if call('/health')[0] == 200: break
            except OSError: pass
            if process.poll() is not None: raise RuntimeError('Server exited before becoming ready')
            time.sleep(.1)
        assert call('/')[0] == 200
        assert call('/api/links/')[0] == 401
        token=call('/api/csrf')[1]['token']
        assert call('/api/login','POST',{'password':password})[0] == 400
        assert call('/api/login','POST',{'password':'incorrect'},token)[0] == 401
        assert call('/api/login','POST',{'password':password},token)[0] == 200
        token=call('/api/csrf')[1]['token']
        assert call('/api/links/','POST',{'command':'https://example.com --jobs /7dias'})[0] == 400
        code, created=call('/api/links/','POST',{'command':'https://example.com --jobs /7dias','description':'Smoke test'},token)
        assert code==201
        code, duplicate=call('/api/links/','POST',{'command':'https://example.com --jobs /1d'},token)
        assert code==200 and not duplicate['created'] and duplicate['item']['expiresAt']==created['item']['expiresAt']
        assert len(call('/api/links/?q=Smoke')[1])==1
        assert call('/api/links/?status=0')[0]==400
        assert call('/api/links/','POST',{'command':'javascript:alert(1) --fun'},token)[0]==400
        item_id=created['item']['id']
        assert call('/api/links/'+item_id+'/archive','POST',{},token)[0]==204
        assert call('/api/links/')[1]==[]
        assert len(call('/api/links/?status=Archived')[1])==1
        assert call('/api/links/'+item_id+'/renew','POST',{'days':7},token)[0]==409
        code, custom=call('/api/links/','POST',{'command':'https://example.org --research'},token)
        assert code==201 and custom['item']['category']=='pesquisa'
        assert call('/api/logout','POST',{},token)[0]==204
        assert call('/api/links/')[0]==401
        print('HTTP smoke passed: login, CSRF, create, duplicate, search, invalid URL/status, archive, renewal conflict, custom category, logout.')
    finally:
        process.terminate()
        process.wait(timeout=10)
