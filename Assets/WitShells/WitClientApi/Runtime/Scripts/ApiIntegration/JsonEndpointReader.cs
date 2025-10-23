using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace WitShells.WitClientApi
{
    public class JsonEndpointReader
    {
        private readonly Dictionary<string, EndpointDefinition> _map = new Dictionary<string, EndpointDefinition>();

        public string ResourcePath { get; }

        public JsonEndpointReader(string resourcePath = "Endpoints/endpoints")
        {
            ResourcePath = resourcePath;
            Load();
        }

        private void Load()
        {
            _map.Clear();
            var textAsset = Resources.Load<TextAsset>(ResourcePath);
            if (textAsset == null)
            {
                Debug.LogWarning($"JsonEndpointReader: could not find resource at '{ResourcePath}'");
                return;
            }

            JObject root;
            try
            {
                root = JObject.Parse(textAsset.text);
            }
            catch (JsonException je)
            {
                Debug.LogError($"JsonEndpointReader: failed to parse endpoints json: {je.Message}");
                return;
            }
            // If this looks like an OpenAPI/Swagger export (has 'paths'), convert it to our endpoint map
            if (root["paths"] != null && root["paths"].Type == JTokenType.Object)
            {
                var paths = (JObject)root["paths"];
                var components = root["components"] as JObject;
                var schemas = components != null && components["schemas"] is JObject ? (JObject)components["schemas"] : null;

                foreach (var pathProp in paths.Properties())
                {
                    var path = pathProp.Name; // includes leading '/'
                    var methods = pathProp.Value as JObject;
                    if (methods == null) continue;

                    foreach (var methodProp in methods.Properties())
                    {
                        var httpMethod = methodProp.Name.ToUpperInvariant();
                        var op = methodProp.Value as JObject;
                        if (op == null) continue;

                        // build endpoint definition
                        var def = new EndpointDefinition();
                        def.Method = httpMethod;
                        def.Path = path;
                        def.Key = op.Value<string>("operationId") ?? (path.TrimStart('/'));

                        // parameters (query)
                        var queryObj = new JObject();
                        // combine path-level and op-level parameters
                        var combinedParams = new List<JToken>();
                        if (paths[path]["parameters"] != null)
                        {
                            foreach (var p in paths[path]["parameters"])
                                combinedParams.Add(p);
                        }
                        if (op["parameters"] != null)
                        {
                            foreach (var p in op["parameters"]) combinedParams.Add(p);
                        }

                        foreach (var p in combinedParams)
                        {
                            var inType = p.Value<string>("in");
                            if (inType == "query")
                            {
                                var name = p.Value<string>("name");
                                var example = BuildExampleFromParameter(p, schemas);
                                queryObj[name] = example == null ? JValue.CreateString(string.Empty) : example;
                            }
                        }

                        def.Query = queryObj.HasValues ? queryObj : null;

                        // requestBody
                        if (op["requestBody"] != null)
                        {
                            var rb = op["requestBody"] as JObject;
                            var content = rb["content"] as JObject;
                            if (content != null)
                            {
                                // prefer application/json
                                JToken chosen = null;
                                if (content["application/json"] != null) chosen = content["application/json"];
                                else chosen = content.Properties().FirstOrDefault()?.Value;

                                if (chosen is JObject cj && cj["schema"] != null)
                                {
                                    var schema = cj["schema"];
                                    var example = BuildExampleFromSchema(schema, schemas);
                                    def.Body = example as JObject;
                                }
                            }
                        }

                        // responses - prefer 200 or first
                        JObject responseExample = null;
                        var responses = op["responses"] as JObject;
                        if (responses != null)
                        {
                            JProperty chosenResp = responses.Property("200") ?? responses.Properties().FirstOrDefault();
                            if (chosenResp != null)
                            {
                                var respObj = chosenResp.Value as JObject;
                                var content = respObj?["content"] as JObject;
                                if (content != null)
                                {
                                    JToken chosen = null;
                                    if (content["application/json"] != null) chosen = content["application/json"];
                                    else chosen = content.Properties().FirstOrDefault()?.Value;

                                    if (chosen is JObject ch && ch["schema"] != null)
                                    {
                                        var schema = ch["schema"];
                                        var ex = BuildExampleFromSchema(schema, schemas);
                                        responseExample = ex as JObject;
                                    }
                                }
                            }
                        }

                        def.Response = responseExample;

                        // basic stream detection
                        bool stream = false;
                        if (op["requestBody"] != null)
                        {
                            var rb = op["requestBody"] as JObject;
                            var content = rb?["content"] as JObject;
                            if (content != null && (content["application/octet-stream"] != null || content["multipart/form-data"] != null)) stream = true;
                        }
                        if (responses != null)
                        {
                            foreach (var r in responses.Properties())
                            {
                                var c = r.Value["content"] as JObject;
                                if (c != null && c["application/octet-stream"] != null) stream = true;
                            }
                        }
                        def.Stream = stream;

                        // register keys
                        var normalizedKey = def.Key ?? path.TrimStart('/');
                        if (normalizedKey.StartsWith("/")) normalizedKey = normalizedKey.TrimStart('/');
                        var shortKey = normalizedKey.Contains("/") ? normalizedKey.Substring(normalizedKey.LastIndexOf('/') + 1) : normalizedKey;
                        if (!_map.ContainsKey(normalizedKey)) _map[normalizedKey] = def;
                        if (!_map.ContainsKey(shortKey)) _map[shortKey] = def;
                    }
                }
                return;
            }

            // Otherwise assume simple endpoints JSON where each property is an endpoint definition
            foreach (var prop in root.Properties())
            {
                var key = prop.Name;
                var node = (JObject)prop.Value;

                var def = new EndpointDefinition
                {
                    Key = key,
                    Method = node.Value<string>("method") ?? "GET",
                    Path = node.Value<string>("path") ?? key,
                    Body = node.Value<JObject>("body"),
                    Query = node.Value<JObject>("query"),
                    Response = node.Value<JObject>("response"),
                    Stream = node.Value<bool?>("stream") ?? false
                };

                // store key normalized (remove leading slashes and common prefixes)
                var normalizedKey = key;
                if (normalizedKey.StartsWith("/")) normalizedKey = normalizedKey.TrimStart('/');
                // also allow using last path segment as short key: e.g. auth/signup -> signup
                var shortKey = normalizedKey.Contains("/") ? normalizedKey.Substring(normalizedKey.LastIndexOf('/') + 1) : normalizedKey;

                if (!_map.ContainsKey(normalizedKey)) _map[normalizedKey] = def;
                if (!_map.ContainsKey(shortKey)) _map[shortKey] = def;
            }
        }

        // Build an example JToken from an OpenAPI schema node. Supports $ref, object, array, primitives.
        private JToken BuildExampleFromSchema(JToken schemaNode, JObject componentsSchemas)
        {
            if (schemaNode == null) return null;

            if (schemaNode.Type == JTokenType.Object && schemaNode["$ref"] != null)
            {
                var refPath = schemaNode.Value<string>("$ref");
                // ref format: #/components/schemas/Name
                var parts = refPath.Split('/');
                var name = parts.Length > 0 ? parts[parts.Length - 1] : refPath;
                if (componentsSchemas != null && componentsSchemas[name] != null)
                {
                    return BuildExampleFromSchema(componentsSchemas[name], componentsSchemas);
                }
                return null;
            }

            var type = schemaNode.Value<string>("type");

            if (type == "object" || schemaNode["properties"] != null)
            {
                var obj = new JObject();
                var props = schemaNode["properties"] as JObject;
                if (props != null)
                {
                    foreach (var p in props.Properties())
                    {
                        var example = BuildExampleFromSchema(p.Value, componentsSchemas);
                        if (example == null)
                        {
                            var propType = p.Value.Value<string>("type");
                            example = SimpleValueForType(propType, p.Value as JObject);
                        }
                        obj[p.Name] = example ?? JValue.CreateNull();
                    }
                }
                return obj;
            }

            if (type == "array")
            {
                var items = schemaNode["items"];
                var ex = BuildExampleFromSchema(items, componentsSchemas) ?? JValue.CreateString(string.Empty);
                var arr = new JArray();
                arr.Add(ex);
                return arr;
            }

            // primitives
            return SimpleValueForType(type, schemaNode as JObject);
        }

        private JToken BuildExampleFromParameter(JToken param, JObject componentsSchemas)
        {
            if (param == null) return null;
            if (param["example"] != null) return param["example"];
            var schema = param["schema"];
            if (schema != null) return BuildExampleFromSchema(schema, componentsSchemas);
            return JValue.CreateString(string.Empty);
        }

        private JToken SimpleValueForType(string type, JObject schema)
        {
            if (type == null) type = "string";
            switch (type)
            {
                case "string":
                    var format = schema?.Value<string>("format");
                    if (format == "date-time" || format == "date") return JValue.CreateString("2020-01-01T00:00:00Z");
                    if (format == "binary") return JValue.CreateString("<binary>");
                    return JValue.CreateString("string");
                case "integer":
                    return new JValue(0);
                case "number":
                    return new JValue(0.0);
                case "boolean":
                    return new JValue(false);
                default:
                    return JValue.CreateString("string");
            }
        }

        public EndpointDefinition GetEndpoint(string key)
        {
            // normalize key by removing any leading '/'
            if (!string.IsNullOrEmpty(key))
            {
                key = key.TrimStart('/');
            }
            if (string.IsNullOrEmpty(key)) return null;
            if (_map.TryGetValue(key, out var val)) return val;
            return null;
        }

        public string[] AvailableKeys()
        {
            var set = new HashSet<string>();

            foreach (var def in _map.Values)
            {
                if (def == null) continue;
                // Prefer the explicit Path when available (it represents the full path). Fall back to Key.
                var candidate = !string.IsNullOrEmpty(def.Path) ? def.Path : def.Key;
                if (string.IsNullOrEmpty(candidate)) continue;

                var normalized = candidate.TrimStart('/');
                if (!string.IsNullOrEmpty(normalized)) set.Add(normalized);
            }

            var arr = new string[set.Count];
            set.CopyTo(arr);
            return arr;
        }
    }
}
