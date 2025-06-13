# WitShells API Integration Guide

## Introduction

WitShells API Integration provides an easy and flexible way to connect and interact with APIs directly inside Unity. With this system, you can:

- **Quickly integrate any REST API** into your Unity project.
- **Test API endpoints directly in the Unity Editor** without writing extra code.
- **Register UnityEvents** for success, failure, and progress callbacks, making it simple to connect API responses to your game logic or UI.
- **Monitor API progress and completion** globally for loading feedback or user notifications.

This guide will walk you through the setup and usage of the WitShells API Integration system, so you can connect your Unity project to any web service with minimal effort.

## Quick Start

At the top bar, navigate to **WitShells/API/RestApiConfig** to open the RestApiConfig window. Here you can:

- Set your API URLs for different environments (Local, Test, Production).
- Add default headers (such as API keys or content types).
- Enable logging for debugging.
- Set and cache your access token and refresh token for authorization.

Next, go to **WitShells/API/Create Api Manager** in the top bar. This will automatically create an API Manager GameObject in your scene.

This GameObject contains an **ApiExecutor** component, where you can:

- List and configure your API endpoints visually.
- Set HTTP method, path, content type, and response type for each endpoint.
- Add body fields and custom headers as needed.
- Test endpoints directly in Play Mode to see how they function.

With these steps, you can quickly start working with API integration in your Unity project—no code required!

---

## Advanced Usage: Integrate by Code

If you want more control, you can create and call endpoints directly from your scripts and register for UnityEvents.

**Example: Creating a custom endpoint and registering events**

```csharp
using UnityEngine;
using WitShells.ApiIntegration;

public class MyApiExample : MonoBehaviour
{
    public ApiExecutor apiExecutor;

    void Start()
    {
        // Create a new endpoint request
        var endpointRequest = new ApiEndpointRequest
        {
            endpointName = "GetUserData",
            endpoint = new ApiEndpoint
            {
                Method = HttpMethod.GET,
                ContentType = ContentType.JSON,
                Path = "/user/data",
                IsSecure = true,
                responseType = ResponseType.Json
            },
            includeDefaultHeaders = true
        };

        // Register success and fail events
        endpointRequest.onSuccess.onJsonData.AddListener(OnApiSuccess);
        endpointRequest.onFail.AddListener(OnApiFail);

        apiExecutor.Execute(endpointRequest);
        Debug.Log("API request sent: " + endpointRequest.endpointName);
    }

    void OnApiSuccess(string json)
    {
        Debug.Log("API Success: " + json);
    }

    void OnApiFail(ApiException ex)
    {
        Debug.LogError($"API Error: {ex.error} - {ex.details}");
    }
}
```