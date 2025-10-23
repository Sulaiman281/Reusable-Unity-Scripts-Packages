Wit Client API (WitClientApi)

Overview

WitClientApi is a small Unity package that makes integrating REST APIs fast and flexible. Define your backend surface in a single JSON manifest (Resources/Endpoints/endpoints.json) and call endpoints dynamically from runtime code or with typed DTOs — no hand-written request/response DTOs are required. The package also provides a small, overrideable authentication flow, thread-safe token storage, and a response parser that tolerates common API envelope shapes.

Quick Start

1. Drop the package folder into your project (Assets/WitShells/WitClientApi).
2. Create an `ApiConfig` ScriptableObject (or provide one in Resources) with your API base URL.
3. Place your endpoints manifest at `Assets/WitShells/WitClientApi/Runtime/Resources/Endpoints/endpoints.json` (sample included).
4. Add the `ApiClientManager` (or `WitClientManager` alias) to a GameObject in your scene.

Basic usage (dynamic):

var client = FindObjectOfType<ApiClientManager>();
client.CallEndpoint("auth/login", new { email = "me@domain.com", password = "p" },
(result) => Debug.Log(Json.Serialize(result)),
(error) => Debug.LogError(error));

Typed DTO usage:

client.CallEndpoint<LoginRequest, TokenResponse>("auth/login", new LoginRequest { Email = "a@b.com", Password = "p" },
(resp) => Debug.Log(resp.AccessToken),
(err) => Debug.LogError(err));

What the package provides

- Dynamic endpoint invocation via a JSON manifest (no DTOs required).
- Optional typed calls using generics (CallEndpoint<TReq, TRes>).
- `ApiClientManager` (MonoSingleton) that manages endpoints, the HTTP handler, token storage and auth flows.
- `AuthService` (overrideable) with SignIn/SignOut/Refresh implementations and envelope-aware parsing.
- `PlayerPrefsTokenStorage` (thread-safe): keeps an in-memory cache for background threads and flushes writes on the main thread.
- `ResponseParser` to safely extract payloads from common envelope shapes like { success, data, error }.

Endpoint manifest (endpoints.json)

Place a JSON file under `Runtime/Resources/Endpoints/endpoints.json`. Keys are identifiers (we recommend path-like keys such as "auth/login"). Each entry should include:

- method: HTTP method (GET, POST, PUT, PATCH, DELETE)
- path: relative path on your API (e.g. "/api/auth/login")
- query: example query object (optional)
- body: example request body (optional)
- response: example response (optional)
- stream: true/false (optional)

Example:

{
"auth/login": {
"method": "POST",
"path": "/api/auth/login",
"body": { "email": "user@example.com", "password": "string" },
"response": { "accessToken": "string", "refreshToken": "string" }
}
}

Swagger / OpenAPI support

If your backend already exposes an OpenAPI/Swagger JSON, you don't need to hand-author the endpoints manifest. The package's `JsonEndpointReader` recognizes a full OpenAPI JSON export (the typical `swagger.json` / `openapi.json`) and will convert its `paths` and `components.schemas` into the compact endpoint manifest at runtime.

Simple workflow:

1. Export your API's final OpenAPI/Swagger JSON from your backend (for example `swagger.json`).
2. Copy the JSON file contents and save it as `Assets//Resources/Endpoints/endpoints.json` (overwrite the sample if present).
3. Start the Editor or play mode — the reader will load the OpenAPI content and expose endpoints using normalized keys (e.g., `GET:/api/users` or a path-like key derived from the OpenAPI operationId if available).

Notes:

- The converter extracts operation methods, paths and example schemas when available and builds example request/response payloads. It doesn't attempt to be a full OpenAPI client generator; instead it produces a compact, examples-first manifest suitable for runtime calls and optional DTO generation.
- If your OpenAPI export contains example values in `components.schemas` or `requestBody`/`responses`, those will be used to build request/response examples. If examples are missing, the reader will synthesize simple sample values from types.
- If you prefer automated DTO generation from OpenAPI schemas, ask and I can add a generator that emits simple POCOs.

Authentication and token handling

Default behavior

The package provides an `AuthService` and `PlayerPrefsTokenStorage` out of the box. Sign-in responses are parsed by `AuthService.ParseTokenResponse` which supports both direct token responses and enveloped responses like:

{
"success": true,
"data": { "accessToken": "...", "refreshToken": "..." },
"error": null
}

Access tokens are attached to outgoing requests automatically (Authorization: Bearer <token>) by `ApiClientManager` when available. On 401 responses, the manager will trigger `AuthService.RefreshTokenAsync` once and retry the original request with the new token when refresh succeeds.

Thread-safety

`PlayerPrefsTokenStorage` keeps tokens in an in-memory cache for fast thread-safe reads on background threads and queues PlayerPrefs writes to be flushed on the Unity main thread during Update(). This avoids calling PlayerPrefs from worker threads.

Overriding authentication/token behavior

The package is designed to be extended. Recommended extension points:

1. Custom token storage (persist tokens elsewhere):

- Implement `ITokenStorage` (see Runtime/Scripts/Token/ITokenStorage.cs) and set your implementation on `ApiClientManager` during InitializeServices or at runtime.

2. Custom auth flows or envelope parsing:

- Inherit `AuthService` and override `SignInAsync`, `SignOutAsync`, and/or `RefreshTokenAsync`. You can also override `ParseTokenResponse` to handle non-standard envelopes.

3. Swap HTTP handler:

- Replace `DefaultHttpHandler` by implementing `IHttpHandler` if you need custom networking or advanced HttpClient configuration.

Example: custom auth service

public class MyAuthService : AuthService
{
public MyAuthService(ApiConfig cfg, IHttpHandler http, ITokenStorage store) : base(cfg, http, store) { }

    public override async Task<TokenResponse> SignInAsync(object credentials, CancellationToken ct)
    {
        // custom parsing, validation, or backend-specific envelope handling
        var tr = await base.SignInAsync(credentials, ct);
        // augment or transform TokenResponse
        return tr;
    }

}

Then initialize in code:

\_authService = new MyAuthService(ApiConfig, \_httpHandler, \_tokenStorage);

Response parsing

Use `ResponseParser.ParseResponse<T>(object)` to convert dynamic or enveloped responses into a typed object safely. It accepts:

- a JToken
- a JSON string (raw or double-encoded)
- a plain POCO object

Best practices

- Prefer passing the original object/JToken returned by `CallEndpoint` directly into `ResponseParser` instead of serializing it to a string first.
- Keep private data (refresh tokens) secure — replace `PlayerPrefsTokenStorage` if you require encrypted storage.
- For streaming endpoints, mark `stream: true` and extend the manager to use a specialized upload/download path.

Developer notes

- The package ships with `com.unity.nuget.newtonsoft-json` dependency. Ensure your project resolves that package.
- If you want automatic DTO generation from OpenAPI, I can add a generator that emits simple POCOs from `components.schemas`.

Contributing

Contributions welcome. Open a PR with a focused change and tests where appropriate.

License

MIT
