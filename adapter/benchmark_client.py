"""Synthetic only: identical ping effects, 250ms mailbox cadence, no Unity claims."""
import argparse,json,statistics,subprocess,tempfile,time
from pathlib import Path
from bridge_client import Client
from test_adapter import FakeMailbox
parser=argparse.ArgumentParser();parser.add_argument('--output',required=True);parser.add_argument('--count',type=int,default=12);args=parser.parse_args()
if not 2<=args.count<=100:parser.error('count must be 2..100')
rows=[]
with tempfile.TemporaryDirectory(prefix='bridge-client-benchmark-') as directory:
 root=Path(directory);fake=FakeMailbox(root/'mailbox',cadence=.25);client=Client(root/'mailbox',root/'intents')
 try:
  for i in range(args.count):
   for mode in (['powershell','persistent'] if i%2==0 else ['persistent','powershell']):
    begin=time.perf_counter()
    if mode=='powershell':
     process=subprocess.run(['powershell.exe','-NoProfile','-File',str(Path(__file__).resolve().parents[1]/'bridge.ps1'),'-Command','ping','-MailboxPath',str(root/'mailbox')],capture_output=True,text=True,timeout=20)
     if process.returncode:raise RuntimeError(process.stderr)
     response=json.loads(process.stdout)
    else:response=client.call('ping')
    assert response['ok'] and response['result']=={'echo':{}}
    rows.append(dict(mode=mode,seconds=time.perf_counter()-begin))
 finally:fake.close()
summary={m:{'count':args.count,'median':statistics.median(r['seconds'] for r in rows if r['mode']==m),'max':max(r['seconds'] for r in rows if r['mode']==m)} for m in ('powershell','persistent')}
record=dict(scope='synthetic ping; journal inactive; 250ms fake-server cadence; not game/agent performance; small sample',rows=rows,summary=summary)
Path(args.output).write_text(json.dumps(record,indent=2));print(json.dumps(summary,indent=2))
