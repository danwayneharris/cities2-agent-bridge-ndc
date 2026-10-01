using System;
using CitiesIIAgentBridge;
using Newtonsoft.Json.Linq;
class Program {
    static int checks;
    static void Check(bool ok, string label) { if (!ok) throw new Exception(label); checks++; Console.WriteLine("PASS " + label); }
    static void Reject(Action action, string error) { try { action(); } catch (Exception e) { Check(e.Message.Contains(error), error); return; } throw new Exception("Accepted " + error); }
    static void Main() {
        var registry = new ProviderRegistry(); registry.Register(typeof(First)); registry.Register(typeof(Second));
        var list = registry.List(); Check(((JArray)list["providers"]).Count == 2, "two independent providers");
        var desc = (JObject)list["providers"][0]; string revision = (string)desc["revision"];
        var request = new JObject { ["provider"] = "alpha", ["revision"] = revision, ["command"] = "read", ["args"] = new JObject() };
        int reads=0,writes=0; Action read=()=>reads++; Action write=()=>writes++;
        var result=registry.Invoke(request,new JObject { ["citySession"]="toy" },read,write);
        Check(reads==1 && writes==0 && (string)result["city"]=="toy", "read routing and context");
        request["command"]="write";registry.Invoke(request,new JObject(),read,write);
        Check(writes==1 && First.Calls==2,"mutation guard before invoke");
        Reject(()=>registry.Invoke(request,new JObject(),read,()=>throw new Exception("control_disabled")),"control_disabled");
        Check(First.Calls==2,"disabled controls never execute provider");
        request["revision"]="old";Reject(()=>registry.Invoke(request,new JObject(),read,write),"stale_provider_revision");
        request["revision"]=revision;request["command"]="unknown";Reject(()=>registry.Invoke(request,new JObject(),read,write),"provider_command_unavailable");
        request["command"]="read";request["args"]=new JArray();Reject(()=>registry.Invoke(request,new JObject(),read,write),"provider_args_object_required");
        registry.Register(typeof(First));request["args"]=new JObject();Reject(()=>registry.Invoke(request,new JObject(),read,write),"provider_unavailable");
        Check(!(bool)registry.List()["complete"],"duplicate identity reported and disabled");
        Reject(()=>new ProviderRegistry().Register(typeof(Missing)),"invalid_provider_signature");
        Reject(()=>new ProviderRegistry().Register(typeof(Invalid)),"invalid_provider_command");
        var discovered=new ProviderRegistry();discovered.Discover(new[]{typeof(Program).Assembly});discovered.Discover(new[]{typeof(Program).Assembly});
        Check(((JArray)discovered.List()["providers"]).Count==1,"generic opt-in discovery is idempotent");
        Console.WriteLine("Provider checks: "+checks);
    }
}
public class First {
    public static int Calls;
    public static string DescribeV1()=>Description("alpha");
    public static string Description(string id)=>new JObject { ["protocol"]=1,["id"]=id,["version"]="1",
        ["commands"]=new JArray(Command("read",true),Command("write",false)) }.ToString();
    public static JObject Command(string name,bool read)=>new JObject { ["name"]=name,["readOnly"]=read,["description"]="Toy provider",
        ["inputSchema"]=new JObject { ["type"]="object" },["outputSchema"]=new JObject { ["type"]="object" } };
    public static string InvokeV1(string c,string a,string context) { Calls++;return new JObject { ["city"]=JObject.Parse(context)["citySession"] }.ToString(); }
}
public class Second { public static string DescribeV1()=>First.Description("beta");public static string InvokeV1(string c,string a,string x)=>"{}"; }
public class Missing {}
public class Invalid { public static string DescribeV1()=>"{\"protocol\":1,\"id\":\"bad\",\"version\":\"1\",\"commands\":[{}]}";public static string InvokeV1(string c,string a,string x)=>"{}"; }
namespace CitiesBridge { public class ProviderV1 {public static string DescribeV1()=>First.Description("opt-in");public static string InvokeV1(string c,string a,string x)=>"{}";} }
