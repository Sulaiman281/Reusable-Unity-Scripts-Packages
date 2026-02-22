## Shipping REST Integrations Faster in Unity with WitClientApi

Most Unity teams know they “should” wrap their REST APIs, but still end up sprinkling raw `UnityWebRequest` calls and half-finished DTOs across scenes. A few sprints later, nobody remembers which endpoint expects what, changing a backend field breaks three features, and adding a new endpoint feels like surgery instead of a simple config change.

WitClientApi is designed to break that cycle.

In this guide, you’ll learn how to turn your REST backend into a single JSON manifest that Unity can call dynamically (or with typed DTOs when you want them), how to plug in authentication and token storage without rewriting your game, and how to keep API changes from becoming a production fire drill.

By the end, you’ll be able to wire up a real REST backend to Unity with a small, testable setup that scales – without generating thousands of lines of boilerplate.

---

## Why REST integrations in Unity hurt more than they should

If you’ve integrated a non-trivial backend into Unity before, you’ve probably hit at least one of these pain points:

- Every new endpoint means another MonoBehaviour that builds URLs by hand.
- You duplicate request/response DTOs, then forget to update all call sites when the API moves.
- Authentication logic (sign-in, token refresh, secure storage) leaks across scenes and systems.
- Different teammates call the same endpoint with slightly different JSON payloads.

On small prototypes this is annoying. On a live game or simulation with real users, it becomes dangerous:

- A backend envelope change (for example, wrapping everything in `{ success, data, error }`) suddenly breaks existing JSON parsing.
- A forgotten `PlayerPrefs` write on a background thread randomly fails in production.
- A 401 response path is handled in three different places, each in its own way.

The net result: too much time spent maintaining glue code, not enough time building actual gameplay, UI, or learning scenarios.

WitClientApi exists to centralize that glue, so your “API surface” lives in one place – a JSON manifest – while authentication, token storage, and response parsing are handled by focused, overrideable services.

---

## What this guide will help you ship

After following this article, you’ll be able to:

- Define your entire REST surface in one JSON file (or reuse your OpenAPI/Swagger export).
- Call endpoints dynamically from Unity scripts without hand-written DTOs.
- Optionally add typed DTOs where they help (and nowhere else).
- Plug in a clean authentication flow with automatic token refresh and safe token storage.
- Swap in custom HTTP handlers or token stores without touching your gameplay code.

This is a practical, tool-focused guide. We’ll stay close to real Unity workflows so you can drop WitClientApi into an existing project in less than an hour.

---

## Core idea #1: Your Swagger spec becomes your Unity manifest

The first shift WitClientApi introduces is simple but powerful:

> You don’t hand-author a giant endpoint map – you reuse your existing OpenAPI/Swagger JSON and let WitClientApi do the translation.

In a typical setup, your backend team already maintains a Swagger/OpenAPI document (for example `swagger.json`). With WitClientApi you:

1. Export that JSON from your backend.
2. Copy the raw JSON into `Resources/Endpoints/endpoints.json` inside your Unity project.
3. Point your `ApiClientManager` at this Resources location (the default path expects that folder).

At runtime, `JsonEndpointReader` loads the Swagger JSON, reads its `paths` and `components.schemas`, and converts them into the compact internal manifest that WitClientApi uses for calls. That instantly turns “documented endpoints” into “callable endpoints” without hand-writing a custom manifest.

You still get the same benefits as a manual manifest:

- **Single source of truth** for methods, paths, and payload shapes – your Swagger file.
- **Backend-driven changes**: when the API evolves, you just drop in an updated Swagger export.
- **Safer refactors**: existing Unity code keeps calling the same logical keys while the underlying paths/methods are updated by the reader.

If you ever need to prototype a brand‑new endpoint before your backend team updates Swagger, you can temporarily edit `endpoints.json` by hand – but the default, preferred workflow is Swagger‑first.

---

## Core idea #2: Dynamic first, typed where it matters

One of the biggest traps in API integration work is generating or hand-writing DTOs for every request and response **before** you know which ones actually need to be strongly typed.

WitClientApi pushes back against that by being dynamic-first:

- You can call endpoints with anonymous objects and receive dynamic results.
- You can inspect those results directly in the Editor or logs.

Then, **only where you need compile-time guarantees**, you switch to typed DTOs:

```csharp
client.CallEndpoint<LoginRequest, TokenResponse>(
	"auth/login",
	new LoginRequest { Email = "a@b.com", Password = "p" },
	resp => Debug.Log(resp.AccessToken),
	err  => Debug.LogError(err));
```

This hybrid approach gives you:

- **Speed** in early integration – everything works with simple anonymous objects.
- **Safety** on critical contracts – token flows, inventory payloads, user profiles, and anything you rely on heavily.

Under the hood, `ResponseParser` helps bridge dynamic and typed worlds. It can take JSON strings, JToken-like objects, or plain POCOs and extract a `T` from common envelope shapes such as:

```json
{
  "success": true,
  "data": { "accessToken": "..." },
  "error": null
}
```

That means when your backend changes how it wraps responses, you update parsing logic in **one place** instead of every call site.

---

## Core idea #3: Authentication, tokens, and threads are handled for you

Authentication is where many Unity projects drift from “quick prototype” into “hard to reason about.” Tokens live in static fields, `PlayerPrefs` calls happen from worker threads, and refresh flows are duplicated.

WitClientApi centralizes this flow into three concepts:

1. **AuthService** – handles sign-in, sign-out, refresh, and token parsing.
2. **ITokenStorage** / **PlayerPrefsTokenStorage** – handles where tokens live.
3. **IHttpHandler** / **DefaultHttpHandler** – handles actual HTTP traffic.

Out of the box, `AuthService` understands both direct token responses and wrapped envelopes, `PlayerPrefsTokenStorage` keeps an in-memory cache for fast, thread-safe reads on worker threads and flushes writes on the Unity main thread, and `ApiClientManager` automatically attaches `Authorization: Bearer <token>` headers when tokens exist.

That means your API layer is **thread-aware** by design: you can schedule work on background threads (for example via the WitShells ThreadingJob package) without worrying about unsafe `PlayerPrefs` calls or inconsistent token state.

On a 401, the manager triggers `AuthService.RefreshTokenAsync` once, retries the original request, and only then surfaces an error if refresh fails. Your gameplay code doesn’t have to know any of this – it simply calls endpoints.

When you need custom behavior, you inherit and plug in your own implementations:

- Implement `ITokenStorage` if you want encrypted or server-side token storage.
- Inherit `AuthService` to support non-standard sign-in flows or exotic envelopes.
- Implement `IHttpHandler` if you need specialized networking or diagnostics.

Because these are all explicit services, you can unit test them in isolation and swap them out without touching UI or scene logic.

---

## A concrete setup example: Git packages, Swagger JSON, and a custom ApiClientManager

Let’s walk through a realistic scenario that matches how many WitShells users prefer to integrate: you install everything via Git URLs, keep your config in Resources, feed Swagger JSON straight into WitClientApi, and expose a strongly-typed API manager for the rest of your game.

**1. Install packages via Git URLs**

Using Unity’s Package Manager (**Window → Package Manager → + → Add package from Git URL**):

- Add WitClientApi:
  - `https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/WitClientApi`
- Add Threading Job (for background work support):
  - `https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/ThreadingJob`
- Add Design Patterns (dependency of Threading Job and useful utilities):
  - `https://github.com/Sulaiman281/Reusable-Unity-Scripts-Packages.git?path=Assets/WitShells/DesignPatterns`

Unity will pull these directly from Git; no manual folder copying required.

**2. Create an ApiConfig in Resources**

- In the Project window, create an `ApiConfig` asset.
- Set your base URL (for example, `https://api.yourgame.com`).
- Place this asset in a `Resources` directory so that WitClientApi can load it easily at runtime.

Here you can also define additional options and paths relevant to your backend.

**3. Copy your Swagger/OpenAPI JSON into Resources**

- Export your backend’s Swagger JSON (commonly `swagger.json` or `openapi.json`).
- Create a folder structure under Resources such as `Resources/Endpoints/`.
- Paste the raw Swagger JSON into `Resources/Endpoints/endpoints.json`.
- Ensure your `ApiClientManager` is configured to look at this Resources location (this is the default convention).

From here, `JsonEndpointReader` will read the Swagger spec and surface endpoints by key – no hand-written manifest required.

**4. Subclass ApiClientManager and expose a domain-specific API**

Rather than sprinkling calls to `ApiClientManager` all over your project, a clean pattern is to subclass it and expose intent‑level methods. For example, a chat‑driven AI API might look like this:

```csharp
using Newtonsoft.Json;
using RescuedVR.ChatbotAPI;
using UnityEngine;
using UnityEngine.Events;
using WitShells.DesignPatterns;
using WitShells.WitClientApi;

public class ChatAiApi : ApiClientManager
{
	[Header("Runtime")]
	[SerializeField] private string lastPersonalityMessage;
	[SerializeField] private string[] lastOptions;

	public static ChatAiApi Api => Instance as ChatAiApi;

	public void Initialize(string personality, UnityAction<InitializeResponse> onSuccess = null, UnityAction<string> onFail = null)
	{
		var request = new PersonalityRequest { personality = personality };
		Instance.CallEndpoint<PersonalityRequest, InitializeResponse>("initialize_conversation", request, onSuccess: success =>
		{
			WitLogger.Log($"Chat AI API initialized with personality: {success.personality}");
			lastPersonalityMessage = success.initial_message;
			lastOptions = success.initial_options;

			onSuccess?.Invoke(success);
		}, onFail: error =>
		{
			WitLogger.LogError($"Failed to initialize Chat AI API: {error}");
			onFail?.Invoke(error);
		});
	}

	public void SendMessage(int choiceIndex, UnityAction<MessageResponse> onSuccess = null, UnityAction<string> onFail = null)
	{
		var request = new MessageRequest
		{
			choice_index = choiceIndex,
			current_options = lastOptions,
			personality_message = lastPersonalityMessage
		};

		Instance.CallEndpoint<MessageRequest, MessageResponse>("send_message", request, onSuccess: success =>
		{
			WitLogger.Log($"Received message: {JsonUtility.ToJson(success)}");
			lastPersonalityMessage = success.personality_response;
			lastOptions = success.new_options;
			onSuccess?.Invoke(success);
		}, onFail: error =>
		{
			WitLogger.LogError($"Failed to send message to Chat AI API: {error}");
			onFail?.Invoke(error);
		});
	}

	public void GetStatus(UnityAction<StatusResponse> onSuccess = null, UnityAction<string> onFail = null)
	{
		Instance.CallEndpoint("get_status", null, onSuccess: success =>
		{
			WitLogger.Log($"Chat AI API Status: {JsonUtility.ToJson(success)}");
			onSuccess?.Invoke(JsonConvert.DeserializeObject<StatusResponse>(success.ToString()));

		}, onFail: error =>
		{
			WitLogger.LogError($"Failed to get status from Chat AI API: {error}");
			onFail?.Invoke(error);
		});
	}
}
```

This pattern keeps all your endpoint keys (`initialize_conversation`, `send_message`, `get_status`) and DTOs in one place, while the underlying WitClientApi handles HTTP, authentication, token refresh, and threading concerns.

---

## Implementation checklist for your project

Use this short checklist to integrate WitClientApi into a new or existing Unity project:

1. **Install the package**
   - Add `Assets/WitShells/WitClientApi` to your project.
   - Confirm the Newtonsoft JSON dependency is present.

2. **Configure your API**
   - Create an `ApiConfig` asset and set the base URL.
   - Decide whether to author `endpoints.json` by hand or import an OpenAPI export.

3. **Set up the manager**
   - Add `ApiClientManager` to a bootstrap scene GameObject.
   - Assign the `ApiConfig` reference.
   - Optionally, plug in custom `IHttpHandler`, `ITokenStorage`, or `AuthService`.

4. **Call endpoints dynamically**
   - Start with anonymous objects and dynamic responses for fast iteration.
   - Use logging to inspect responses and validate envelopes.

5. **Introduce typed DTOs where needed**
   - For critical flows (tokens, currency, user profile), define simple request/response classes.
   - Switch those calls to `CallEndpoint<TReq, TRes>()` and leverage `ResponseParser` for safety.

6. **Harden authentication**
   - Verify that sign-in, sign-out, and refresh behavior match your backend.
   - If you need encrypted storage or platform-specific behavior, implement a custom `ITokenStorage`.

7. **Monitor and evolve**
   - As the backend changes, update `endpoints.json` (or your OpenAPI export) as the single source of truth.
   - Keep gameplay and UI scripts free of low-level HTTP details.

This flow keeps the “API layer” thin, explicit, and testable while freeing your scenes from brittle networking logic.

---

## Wrapping up: Turn your backend into a Unity asset

The most successful simulation, game-based learning, and connected game projects treat their backend as a **first-class asset** in Unity, not an afterthought wired through scattered HTTP calls.

WitClientApi gives you a practical path to do exactly that:

- An endpoint manifest that lives in JSON instead of hard-coded URLs.
- A dynamic-first API with optional typing where it counts.
- A focused authentication and token storage layer that can evolve over time.

If you’re currently maintaining hand-rolled API code across multiple scenes, consider this your invitation to experiment: pick one feature – a login flow, a user profile fetch, or a leaderboard – and migrate it to WitClientApi.

Spend 30–60 minutes following the checklist above, then measure how easy it is to change an endpoint path, wrap responses in an envelope, or add a refresh token flow compared to your old setup.

If the pain drops and your iteration speed goes up, you’ve just found a better default for every REST-backed Unity project you ship.

Your backend already knows how to talk. WitClientApi’s job is to make sure your game can listen – cleanly, safely, and fast.
