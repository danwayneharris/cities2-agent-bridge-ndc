"""Protocol-1 client; no mod names, shell subprocesses, or implicit mutation retries."""
import ctypes
import datetime as dt
import json
import os
from pathlib import Path
import threading
import time
import uuid

class BridgeError(RuntimeError):
    pass

def read_shared(path):
    if os.name != 'nt':
        data = Path(path).read_bytes()
    else:
        # FILE_SHARE_READ|WRITE|DELETE is essential when Unity replaces heartbeats.
        from ctypes import wintypes
        kernel = ctypes.WinDLL('kernel32', use_last_error=True)
        create = kernel.CreateFileW
        create.argtypes = [wintypes.LPCWSTR, wintypes.DWORD, wintypes.DWORD, ctypes.c_void_p,
                           wintypes.DWORD, wintypes.DWORD, wintypes.HANDLE]
        create.restype = wintypes.HANDLE
        handle = create(str(path), 0x80000000, 7, None, 3, 0x80, None)
        if handle == ctypes.c_void_p(-1).value:
            raise ctypes.WinError(ctypes.get_last_error())
        kernel.ReadFile.argtypes = [wintypes.HANDLE, ctypes.c_void_p, wintypes.DWORD,
                                    ctypes.POINTER(wintypes.DWORD), ctypes.c_void_p]
        kernel.CloseHandle.argtypes = [wintypes.HANDLE]
        try:
            chunks=[];total=0
            while True:
                buffer=ctypes.create_string_buffer(65536);count=wintypes.DWORD()
                if not kernel.ReadFile(handle, buffer, len(buffer), ctypes.byref(count), None):
                    raise ctypes.WinError(ctypes.get_last_error())
                if not count.value: break
                total+=count.value
                if total>8*1024*1024: raise BridgeError('response_too_large')
                chunks.append(buffer.raw[:count.value])
            data=b''.join(chunks)
        finally: kernel.CloseHandle(handle)
    if len(data)>8*1024*1024: raise BridgeError('response_too_large')
    return json.loads(data.decode('utf-8-sig'))

class Client:
    def __init__(self, mailbox, intents):
        self.root=Path(mailbox).resolve();self.intents=Path(intents).resolve()
        if self.root == self.intents or self.root in self.intents.parents:
            raise ValueError('Intent records must be outside live mailbox')
        self.intents.mkdir(parents=True,exist_ok=True)
        self.lock=threading.Lock()
        self.last_request=None

    def status(self):
        value=None
        for attempt in range(10):
            try: value=read_shared(self.root/'session.json');break
            except (OSError,ValueError):
                if attempt==9: raise BridgeError('game_unavailable')
                time.sleep(.05)
        age=(dt.datetime.now(dt.timezone.utc)-dt.datetime.fromisoformat(value['heartbeatUtc'].replace('Z','+00:00'))).total_seconds()
        if value.get('protocol')!=1 or value.get('status')!='ready' or age< -2 or age>10:
            raise BridgeError('game_unavailable_or_stale')
        return value

    def call(self, command, args=None, *, expected=None, intent=None, timeout=15):
        with self.lock:
            return self._call(command,{} if args is None else args,expected,intent,timeout)

    def _call(self, command,args,expected,intent,timeout):
        if not 1<=timeout<=45: raise ValueError('timeout_out_of_range')
        if not isinstance(args,dict): raise ValueError('args_object_required')
        intent=uuid.UUID(intent).hex if intent else uuid.uuid4().hex
        self.last_request=intent
        record=self.intents/(intent+'.json')
        if record.exists():
            packet=json.loads(record.read_text(encoding='utf-8'))
            if packet['command']!=command or packet['args']!=args:
                raise BridgeError('intent_conflict')
            if expected and any(packet[k]!=expected[k] for k in ('session','citySession')):
                raise BridgeError('intent_session_conflict')
            # Recovery never re-publishes. A crash between recording and publication is unknown.
        else:
            state=self.status()
            if expected and any(state[k]!=expected[k] for k in ('session','citySession')):
                raise BridgeError('stale_session')
            packet=dict(protocol=1,id=intent,session=state['session'],citySession=state['citySession'],
                        command=command,args=args,expiresUtc=(dt.datetime.now(dt.timezone.utc)+dt.timedelta(seconds=timeout)).isoformat())
            data=json.dumps(packet,separators=(',',':'),allow_nan=False).encode('utf-8')
            if len(data)>16384: raise BridgeError('request_too_large')
            # Exclusive, flushed write before submission. Another adapter cannot reuse this intent.
            with record.open('x',encoding='utf-8') as f:
                json.dump(packet,f);f.flush();os.fsync(f.fileno())
            request=self.root/'requests'/(intent+'.json');temporary=request.with_suffix('.tmp')
            with temporary.open('xb') as f:
                f.write(data);f.flush();os.fsync(f.fileno())
            os.rename(temporary,request)
        deadline=time.monotonic()+timeout+2
        while time.monotonic()<deadline:
            try: response=read_shared(self.root/'responses'/(intent+'.json'))
            except OSError as e:
                if not isinstance(e,FileNotFoundError) and getattr(e,'winerror',None) not in (2,3,32,33): raise
                time.sleep(.05);continue
            if any(response.get(k)!=packet[k] for k in ('id','session','citySession','protocol')):
                raise BridgeError('response_identity_mismatch:'+intent)
            return response
        raise BridgeError('outcome_unknown:'+intent)
