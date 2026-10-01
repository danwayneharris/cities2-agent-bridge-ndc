using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Keep this exact type name: it is the bridge's discovery convention.
namespace CitiesBridge {
    public static class ProviderV1 {
        internal static bool Loaded;

        // Discovery must work before a city or even this mod has loaded.
        public static string DescribeV1() => @"{
  ""protocol"": 1,
  ""id"": ""example.hello"",
  ""version"": ""1.0.0"",
  ""commands"": [
    {
      ""name"": ""greet"",
      ""description"": ""Return a greeting. Does not change the city."",
      ""readOnly"": true,
      ""inputSchema"": {
        ""type"": ""object"",
        ""properties"": {
          ""name"": {
            ""type"": ""string"",
            ""minLength"": 1,
            ""maxLength"": 40
          }
        },
        ""required"": [
          ""name""
        ],
        ""additionalProperties"": false
      },
      ""outputSchema"": {
        ""type"": ""object"",
        ""properties"": {
          ""message"": {
            ""type"": ""string""
          }
        },
        ""required"": [
          ""message""
        ],
        ""additionalProperties"": false
      }
    }
  ]
}";

        public static string InvokeV1(string command, string argumentsJson, string contextJson) {
            if (!Loaded) throw new InvalidOperationException("example_mod_not_loaded");
            if (command != "greet") throw new ArgumentException("unknown_command");
            var args = JObject.Parse(argumentsJson);
            // Descriptors help clients; handlers must enforce their own contract.
            if (args.Count != 1 || args["name"]?.Type != JTokenType.String)
                throw new ArgumentException("name_string_required");
            var name = (string)args["name"];
            if (name.Length < 1 || name.Length > 40)
                throw new ArgumentException("name_length_must_be_1_to_40");
            // No game state or entities retained; this command needs no context.
            return new JObject { ["message"] = "Hello, " + name + "!" }.ToString(Formatting.None);
        }
    }
}
