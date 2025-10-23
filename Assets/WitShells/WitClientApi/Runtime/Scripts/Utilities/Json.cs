using Newtonsoft.Json;

namespace WitShells.WitClientApi
{
    public static class Json
    {
        public static readonly JsonSerializerSettings DefaultOptions = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
        };

        public static string Serialize<T>(T obj)
        {
            if (obj == null) return string.Empty;
            return JsonConvert.SerializeObject(obj, DefaultOptions);
        }

        public static T Deserialize<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return default;
            return JsonConvert.DeserializeObject<T>(json, DefaultOptions);
        }
    }
}