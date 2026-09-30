import asyncio
import datetime as dt
import json
import os
from pathlib import Path
import tempfile
import threading
import time
import unittest
import uuid
from bridge_client import Client,BridgeError,read_shared
from server import Adapter
import mcp.types as types

class FakeMailbox:
    def __init__(self,root,cadence=.005):
        self.root=Path(root);self.cadence=cadence;self.calls=[];self.stop=threading.Event();self.respond=True;self.publish_lock=threading.Lock();self.errors=[]
        self.state=dict(protocol=1,session='process-a',citySession='city-a',status='ready',controlEnabled=True)
        for name in ('requests','responses'): (self.root/name).mkdir(parents=True)
        self.publish();self.thread=threading.Thread(target=self.run,daemon=True);self.thread.start()
    def publish(self):
        value=self.state|{'heartbeatUtc':dt.datetime.now(dt.timezone.utc).isoformat()}
        with self.publish_lock:
            temp=self.root/'heartbeat.tmp';temp.write_text(json.dumps(value))
            for attempt in range(20):
                try:os.replace(temp,self.root/'session.json');break
                except PermissionError:
                    if attempt==19:raise
                    time.sleep(.002)
    def run(self):
        while not self.stop.wait(self.cadence):
            try:self.publish()
            except Exception as error:self.errors.append(error);return
            for path in (self.root/'requests').glob('*.json'):
                packet=json.loads(path.read_text());self.calls.append(packet)
                if self.respond:
                    reply={k:packet[k] for k in ('id','protocol','session','citySession')}
                    reply.update(ok=True,result={'echo':packet['args']})
                    dest=self.root/'responses'/path.name;temp=dest.with_suffix('.tmp')
                    temp.write_text(json.dumps(reply));os.replace(temp,dest)
                path.unlink()
    def close(self):
        self.stop.set();self.thread.join(2)
        if self.errors:raise self.errors[0]

class ClientTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(prefix='bridge-provider-tests-');self.base=Path(self.temp.name)
        self.fake=FakeMailbox(self.base/'mailbox');self.client=Client(self.base/'mailbox',self.base/'intents')
    def tearDown(self):self.fake.close();self.temp.cleanup()
    def test_shared_read_and_round_trip(self):
        self.assertEqual(self.client.call('ping')['result'],{'echo':{}})
    def test_stale_session_before_publication(self):
        with self.assertRaisesRegex(BridgeError,'stale_session'):self.client.call('write',expected={'session':'old','citySession':'city-a'})
        self.assertEqual(self.fake.calls,[])
    def test_recover_same_intent_after_client_restart(self):
        intent=uuid.uuid4().hex;first=self.client.call('write',{'x':1},intent=intent)
        other=Client(self.base/'mailbox',self.base/'intents')
        self.assertEqual(other.call('write',{'x':1},intent=intent),first)
        self.assertEqual(len(self.fake.calls),1)
        with self.assertRaisesRegex(BridgeError,'intent_conflict'):other.call('write',{'x':2},intent=intent)
    def test_recorded_unpublished_never_replayed(self):
        intent=uuid.uuid4().hex;packet=dict(id=intent,protocol=1,session='process-a',citySession='city-a',command='write',args={})
        (self.base/'intents'/(intent+'.json')).write_text(json.dumps(packet))
        with self.assertRaisesRegex(BridgeError,'outcome_unknown'):self.client.call('write',intent=intent,timeout=1)
        self.assertEqual(self.fake.calls,[])
    def test_response_identity_not_trusted(self):
        intent=uuid.uuid4().hex
        packet=dict(id=intent,protocol=1,session='process-a',citySession='city-a',command='write',args={})
        (self.base/'intents'/(intent+'.json')).write_text(json.dumps(packet))
        (self.base/'mailbox/responses'/(intent+'.json')).write_text(json.dumps(packet|{'session':'wrong'}))
        with self.assertRaisesRegex(BridgeError,'response_identity_mismatch'):self.client.call('write',intent=intent)
    def test_request_size_before_publication(self):
        with self.assertRaisesRegex(BridgeError,'request_too_large'):self.client.call('write',{'huge':'x'*18000})
        self.assertEqual(self.fake.calls,[])
    def test_no_game(self):
        self.fake.close();(self.base/'mailbox/session.json').unlink()
        with self.assertRaisesRegex(BridgeError,'game_unavailable'):self.client.status()
    def test_windows_delete_sharing(self):
        for _ in range(10):
            self.assertEqual(read_shared(self.base/'mailbox/session.json')['session'],'process-a')
            self.fake.publish()

class FakeClient:
    last_request=None
    def __init__(self):self.calls=[];self.available=True
    def status(self):
        if not self.available:raise BridgeError('game_unavailable')
        return dict(session='process-a',citySession='city-a')
    def call(self,name,args=None,**kw):
        self.calls.append((name,args,kw))
        if name=='list_providers':
            return {'ok':True,'result':{'complete':True,'errors':[],'providers':[
                {'id':p,'version':'1','revision':'hash-'+p,'commands':[{'name':'echo','readOnly':False,
                 'description':'Synthetic '+p,'inputSchema':{'type':'object','properties':{'value':{'type':'number','minimum':0}},'required':['value'],'additionalProperties':False},
                 'outputSchema':{'type':'object'}}]} for p in ('example.alpha','example.beta')]}}
        return {'ok':True,'result':{'provider':args['provider'],'value':args['args']['value']}}
class AdapterTests(unittest.IsolatedAsyncioTestCase):
    async def test_dynamic_tools_and_exact_routing(self):
        c=FakeClient();a=Adapter(c);listing=await a.list_tools(None,None)
        self.assertEqual(len(listing.tools),4)
        for tool in listing.tools[2:]:
            args={'value':2,'_bridge':{'session':'process-a','citySession':'city-a','intent':uuid.uuid4().hex}}
            result=await a.call_tool(None,types.CallToolRequestParams(name=tool.name,arguments=args))
            self.assertFalse(result.is_error);self.assertIn(result.structured_content['provider'],tool.title)
    async def test_invalid_schema_never_invokes(self):
        c=FakeClient();a=Adapter(c);listing=await a.list_tools(None,None);count=len(c.calls)
        result=await a.call_tool(None,types.CallToolRequestParams(name=listing.tools[2].name,arguments={'value':-1}))
        self.assertTrue(result.is_error);self.assertEqual(len(c.calls),count)
    async def test_no_game_keeps_status_tools(self):
        c=FakeClient();c.available=False;a=Adapter(c)
        self.assertEqual(len((await a.list_tools(None,None)).tools),2)
        result=await a.call_tool(None,types.CallToolRequestParams(name='bridge_status',arguments={}))
        self.assertTrue(result.is_error)
    async def test_schema_failure_does_not_report_previous_request(self):
        c=FakeClient();a=Adapter(c);listing=await a.list_tools(None,None)
        c.last_request='previous-operation'
        result=await a.call_tool(None,types.CallToolRequestParams(name=listing.tools[2].name,arguments={}))
        self.assertTrue(result.is_error)
        self.assertIsNone(result.structured_content['requestId'])

    async def test_cancelled_call_holds_lock_until_worker_finishes(self):
        started=threading.Event();release=threading.Event()
        class BlockingClient(FakeClient):
            def status(self):
                started.set()
                if not release.wait(3):raise RuntimeError('test_worker_timeout')
                return super().status()
        c=BlockingClient();a=Adapter(c)
        first=asyncio.create_task(a.call_tool(None,types.CallToolRequestParams(name='bridge_status',arguments={})))
        try:
            for _ in range(100):
                if started.is_set():break
                await asyncio.sleep(.01)
            self.assertTrue(started.is_set())
            first.cancel()
            await asyncio.sleep(.03)
            self.assertTrue(a.operation_lock.locked())
            self.assertFalse(first.done())
            first.cancel()
            await asyncio.sleep(.03)
            self.assertTrue(a.operation_lock.locked())
        finally:release.set()
        with self.assertRaises(asyncio.CancelledError):await first
        self.assertFalse(a.operation_lock.locked())

    async def test_invalid_output_schema_rejects_discovery(self):
        class InvalidSchemaClient(FakeClient):
            def call(self,*args,**kwargs):
                response=super().call(*args,**kwargs)
                response['result']['providers'][0]['commands'][0]['outputSchema']={'type':'not-a-json-type'}
                return response
        a=Adapter(InvalidSchemaClient())
        result=await a.call_tool(None,types.CallToolRequestParams(name='bridge_discover',arguments={}))
        self.assertTrue(result.is_error)
        self.assertEqual(a.tools,{})

if __name__=='__main__':unittest.main()
