﻿// example/LiveDocs.GraphQLApi/Security/CustomClaimTypes.cs

namespace LiveDocs.GraphQLApi.Security;

/// <summary>
/// Provides custom claim types used within the LiveDocs example app.
/// </summary>
public static class CustomClaimTypes
{
    /// <summary>
    /// Represents the custom claim type for the user's workspace.
    /// </summary>
    public const string WorkspaceId = "http://livedocs.example.org/claims/workspace";
}
