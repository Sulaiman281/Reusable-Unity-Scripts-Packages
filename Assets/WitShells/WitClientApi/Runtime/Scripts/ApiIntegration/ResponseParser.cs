using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WitShells.WitClientApi
{
    public class ApiEnvelope
    {
        public bool Success { get; set; }
        public string Data { get; set; }
        public string Error { get; set; }
    }

    public class ResponseParser
    {
        /// <summary>
        /// Parse different kinds of response payloads into T.
        /// Accepts JToken, string (raw JSON or JSON-encoded string), or POCOs.
        /// If the response uses an envelope { success, data, error } the .data node is used.
        /// </summary>
        public T ParseResponse<T>(object json)
        {
            if (json == null) return default;

            JToken token = null;

            // If it's already a JToken, use it
            if (json is JToken jt)
            {
                token = jt;
            }
            else if (json is string s)
            {
                // Try to parse the string as JSON
                // It may be:
                //  - a JSON object string: {"success":true,...}
                //  - a JSON-encoded string: "\"{...}\"" (double-encoded)
                //  - plain text (not JSON)
                var parsed = TryParseStringToJToken(s);
                if (parsed != null)
                {
                    token = parsed;
                }
                else
                {
                    // Not JSON parseable; attempt to deserialize directly to T
                    try { return Json.Deserialize<T>(s); }
                    catch { return default; }
                }
            }
            else
            {
                // Plain object -> convert to JToken
                try { token = JToken.FromObject(json); } catch { token = null; }
            }

            if (token == null) return default;

            // If the token is a string token that contains JSON, try to parse inner text
            if (token.Type == JTokenType.String)
            {
                var inner = token.ToString();
                var innerParsed = TryParseStringToJToken(inner);
                if (innerParsed != null) token = innerParsed;
            }

            // If this looks like an envelope { success, data, error } -> use .data
            if (token.Type == JTokenType.Object && token["data"] != null)
            {
                var data = token["data"];
                try
                {
                    return data.ToObject<T>();
                }
                catch
                {
                    try { return Json.Deserialize<T>(data.ToString()); } catch { return default; }
                }
            }

            // Otherwise try to convert token directly to T
            try
            {
                return token.ToObject<T>();
            }
            catch
            {
                try { return Json.Deserialize<T>(token.ToString()); } catch { return default; }
            }
        }

        private JToken TryParseStringToJToken(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            // Trim surrounding quotes that commonly appear when a JSON object was serialized into a string
            var candidate = s.Trim();
            // If the candidate starts and ends with a quote, try to unescape it first
            if (candidate.Length >= 2 && candidate[0] == '"' && candidate[candidate.Length - 1] == '"')
            {
                try
                {
                    // Deserialize the outer string to get the inner string (unescape)
                    var unescaped = JsonConvert.DeserializeObject<string>(candidate);
                    if (!string.IsNullOrWhiteSpace(unescaped))
                    {
                        candidate = unescaped;
                    }
                }
                catch { /* ignore, fall back to parsing candidate directly */ }
            }

            try
            {
                return JToken.Parse(candidate);
            }
            catch
            {
                return null;
            }
        }
    }
}