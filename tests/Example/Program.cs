using System;
using CitiesBridge;
using CitiesIIAgentBridge;
using Newtonsoft.Json.Linq;

var registry = new ProviderRegistry();
registry.Discover(new[] { typeof(ProviderV1).Assembly });
var catalog = registry.List();
if (!(bool)catalog["complete"] || ((JArray)catalog["providers"]).Count != 1) throw new Exception("Discovery failed");
var request = new JObject { ["provider"] = "example.hello", ["revision"] = catalog["providers"][0]["revision"],
    ["command"] = "greet", ["args"] = new JObject { ["name"] = "Dan" } };
void Reject(Action action) { try { action(); } catch (InvalidOperationException) { return; } throw new Exception("Expected rejection"); }
Reject(() => registry.Invoke(request, new JObject(), () => {}, () => throw new Exception("Unexpected write")));
ProviderV1.Loaded = true; // Model lifecycle; this is not an in-game load test.
var result = registry.Invoke(request, new JObject(), () => {}, () => throw new Exception("Unexpected write"));
if ((string)result["message"] != "Hello, Dan!") throw new Exception("Greeting mismatch");
request["args"] = new JObject { ["name"] = new string('x', 41) };
Reject(() => registry.Invoke(request, new JObject(), () => {}, () => {}));
ProviderV1.Loaded = false;
Reject(() => registry.Invoke(request, new JObject(), () => {}, () => {}));
Console.WriteLine("PASS example discovery, unloaded guard, read-only greeting, invalid input and disposal guard");
