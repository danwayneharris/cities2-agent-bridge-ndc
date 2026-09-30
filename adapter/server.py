"""Generic stdio MCP adapter. No individual mod integrations or game auto-launch."""
import argparse
import asyncio
import hashlib
import json
from pathlib import Path
import jsonschema
from mcp.server.lowlevel import Server
from mcp.server.stdio import stdio_server
import mcp.types as types
from bridge_client import Client

class Adapter:
    def __init__(self,client):
        self.client=client;self.tools={};self.operation_lock=asyncio.Lock()
    def discover(self):
        state=self.client.status()
        response=self.client.call('list_providers',expected=state)
        if not response['ok']: raise RuntimeError(response.get('error','discovery_failed'))
        catalog=response['result']
        if not catalog['complete']: raise RuntimeError('incomplete_discovery:'+json.dumps(catalog['errors']))
        tools={}
        for provider in catalog['providers']:
            for command in provider['commands']:
                # Fixed-length hash avoids truncation collisions for arbitrary valid provider IDs.
                key=provider['id']+'/'+command['name']
                name='mod_'+hashlib.sha256(key.encode()).hexdigest()[:24]
                schema=json.loads(json.dumps(command['inputSchema']))
                if '_bridge' in schema.get('properties',{}): raise RuntimeError('reserved_argument:_bridge')
                schema.setdefault('properties',{})['_bridge']={'type':'object','properties':{
                    'session':{'type':'string'},'citySession':{'type':'string'},
                    'intent':{'type':'string','pattern':'^[0-9a-f]{32}$'}},
                    'required':['session','citySession','intent'],'additionalProperties':False}
                schema.setdefault('required',[]).append('_bridge')
                jsonschema.Draft202012Validator.check_schema(schema)
                if name in tools: raise RuntimeError('tool_name_collision')
                tool=types.Tool(name=name,title=key,description=command['description'],inputSchema=schema,
                    outputSchema=command['outputSchema'],annotations=types.ToolAnnotations(readOnlyHint=command['readOnly'],
                    destructiveHint=not command['readOnly'],idempotentHint=False,openWorldHint=False))
                tools[name]=(tool,provider['id'],provider['revision'],command['name'])
        self.tools=tools
        return catalog
    async def list_tools(self,context,params):
        async with self.operation_lock:
            try: await asyncio.to_thread(self.discover)
            except Exception: self.tools={}
            return types.ListToolsResult(tools=[types.Tool(name='bridge_status',description='Read heartbeat only. Returns current session identities; no game launch or pause.',inputSchema={'type':'object','additionalProperties':False}),
                types.Tool(name='bridge_discover',description='Discover opt-in providers; returns explicit errors if unavailable. Does not pause.',inputSchema={'type':'object','additionalProperties':False})]+[x[0] for x in self.tools.values()])
    async def call_tool(self,context,params):
        async with self.operation_lock:
            try:
                args=dict(params.arguments or {})
                if params.name=='bridge_status':
                    if args: raise ValueError('unexpected_arguments')
                    result=await asyncio.to_thread(self.client.status)
                elif params.name=='bridge_discover':
                    if args: raise ValueError('unexpected_arguments')
                    result=await asyncio.to_thread(self.discover)
                else:
                    if params.name not in self.tools: raise ValueError('tool_unavailable_rediscover')
                    tool,provider,revision,command=self.tools[params.name]
                    jsonschema.Draft202012Validator(tool.input_schema).validate(args)
                    token=args.pop('_bridge')
                    response=await asyncio.to_thread(self.client.call,'invoke_provider',
                        {'provider':provider,'revision':revision,'command':command,'args':args},
                        expected=token,intent=token['intent'])
                    if not response['ok']: raise RuntimeError(response.get('error','provider_failed'))
                    result=response['result']
                    jsonschema.Draft202012Validator(tool.output_schema).validate(result)
                return types.CallToolResult(content=[types.TextContent(type='text',text=json.dumps(result))],structuredContent=result)
            except Exception as error:
                detail={'error':str(error),'requestId':self.client.last_request,'retryPolicy':'Inspect/recover the original intent; never blindly submit a new mutation.'}
                return types.CallToolResult(content=[types.TextContent(type='text',text=json.dumps(detail))],structuredContent=detail,isError=True)

async def main():
    parser=argparse.ArgumentParser();parser.add_argument('--mailbox',required=True);parser.add_argument('--intents',required=True)
    args=parser.parse_args();adapter=Adapter(Client(args.mailbox,args.intents))
    server=Server('Cities Bridge',version='0.1.0',on_list_tools=adapter.list_tools,on_call_tool=adapter.call_tool)
    async with stdio_server() as (read,write):
        await server.run(read,write,server.create_initialization_options())
if __name__=='__main__':asyncio.run(main())
