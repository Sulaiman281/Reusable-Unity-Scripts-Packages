using Newtonsoft.Json.Linq;

namespace WitShells.WitClientApi
{
    /// <summary>
    /// Lightweight representation of an endpoint defined in endpoints.json
    /// </summary>
    public class EndpointDefinition
    {
        public string Key;
        public string Method;
        public string Path;
        public JObject Body;
        public JObject Query;
        public JObject Response;
        public bool Stream = false;
    }
}
