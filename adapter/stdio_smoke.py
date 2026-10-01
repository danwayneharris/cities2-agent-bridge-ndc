"""Actual MCP stdio handshake and provider invocation against a synthetic mailbox."""
import asyncio,json,sys,tempfile,uuid
from pathlib import Path
from mcp import Client
from mcp.client.stdio import StdioServerParameters
from test_adapter import FakeMailbox,FakeClient

class ProviderMailbox(FakeMailbox):
    def run(self):
        import os
        while not self.stop.wait(.01):
            self.publish()
            for path in (self.root/'requests').glob('*.json'):
                packet=json.loads(path.read_text());self.calls.append(packet)
                reply={k:packet[k] for k in ('id','protocol','session','citySession')}
                if packet['command']=='list_providers':reply.update(FakeClient().call('list_providers'))
                else:reply.update(ok=True,result={'provider':packet['args']['provider'],'value':packet['args']['args']['value']})
                dest=self.root/'responses'/path.name;temp=dest.with_suffix('.tmp');temp.write_text(json.dumps(reply));os.replace(temp,dest);path.unlink()
async def run():
    with tempfile.TemporaryDirectory(prefix='bridge-mcp-stdio-') as folder:
        root=Path(folder);fake=ProviderMailbox(root/'mailbox')
        try:
            params=StdioServerParameters(command=sys.executable,args=[str(Path(__file__).with_name('server.py')),'--mailbox',str(root/'mailbox'),'--intents',str(root/'intents')])
            async with Client(params) as client:
                listing=await client.list_tools()
                tools=listing.tools if hasattr(listing,'tools') else listing
                tool=next(t for t in tools if t.title=='example.beta/echo')
                result=await client.call_tool(tool.name,{'value':3,'_bridge':{'session':'process-a','citySession':'city-a','intent':uuid.uuid4().hex}})
                assert not result.is_error and result.structured_content=={'provider':'example.beta','value':3},result
                print('PASS real stdio discovery and typed invocation of independent provider')
        finally:fake.close()
if __name__=='__main__':asyncio.run(run())
