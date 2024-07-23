using System.Net.Http.Json;
using LiveDocs.GraphQLApi.Models;
using Newtonsoft.Json.Linq;
using RT.Comb;
using RxDBDotNet.Tests.Setup;
using Xunit.Abstractions;

namespace RxDBDotNet.Tests;

public class BasicDocumentOperationsTests(ITestOutputHelper output) : TestBase(output)
{
    [Fact]
    public async Task TestCase1_1_CreateSingleDocument_ShouldSucceed()
    {
        // Arrange
        var newWorkspace = new WorkspaceGql
        {
            Id = Provider.Sql.Create(),
            Name = "Test Workspace",
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
        };

        const string query = @"
                 mutation CreateWorkspace($workspace: WorkspaceInputPushRow!) {
                     pushWorkspace(workspacePushRow: [$workspace]) {
                         id
                         name
                         updatedAt
                         isDeleted
                     }
                 }";

        var variables = new
        {
            workspace = new
            {
                assumedMasterState = (object?)null,
                newDocumentState = newWorkspace,
            },
        };

        var request = new
        {
            query,
            variables,
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/graphql", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var jObject = JObject.Parse(content);

        var pushResult = jObject["data"]!["pushWorkspace"];
        Assert.NotNull(pushResult);
        Assert.Empty(pushResult);

        // Verify the workspace exists in the database
        await VerifyWorkspaceExists(newWorkspace.Id);
    }

    [Fact(Skip = "Work in progress")]
    public async Task TestCase1_2_CreateDocumentsWithDuplicateName_ShouldFail()
    {
        // Arrange
        const string existingWorkspaceName = "Existing Workspace";

        // First, create a workspace
        await CreateWorkspace(existingWorkspaceName);

        // Now, try to create another workspace with the same name
        var duplicateWorkspace = new Workspace
        {
            Id = Provider.Sql.Create(),
            Name = existingWorkspaceName,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
        };

        const string query = @"
            mutation CreateWorkspace($workspace: WorkspaceInputPushRow!) {
                pushWorkspace(workspacePushRow: [$workspace]) {
                    id
                    name
                    updatedAt
                    isDeleted
                }
            }";

        var variables = new
        {
            workspace = new
            {
                assumedMasterState = (object?)null,
                newDocumentState = duplicateWorkspace,
            },
        };

        var request = new
        {
            query,
            variables,
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/graphql", request);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        var jObject = JObject.Parse(content);

        Assert.NotNull(jObject["errors"]);
        Assert.Contains("workspace name is already in use", jObject["errors"]![0]!["message"]!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact(Skip = "Work in progress")]
    public async Task TestCase1_3_PullDocument_ShouldReturnCreatedDocuments()
    {
        // Arrange
        await CreateWorkspace("Workspace 1");
        await CreateWorkspace("Workspace 2");

        const string query = @"
            query PullWorkspaces($checkpoint: WorkspaceInputCheckpoint, $limit: Int!) {
                pullWorkspace(checkpoint: $checkpoint, limit: $limit) {
                    documents {
                        id
                        name
                        updatedAt
                        isDeleted
                    }
                    checkpoint {
                        lastDocumentId
                        updatedAt
                    }
                }
            }";

        var variables = new
        {
            checkpoint = (object?)null,
            limit = 10,
        };

        var request = new
        {
            query,
            variables,
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/graphql", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var jObject = JObject.Parse(content);

        var documents = jObject["data"]!["pullWorkspace"]!["documents"]!;
        Assert.True(documents.Count() >= 2);
        Assert.Contains(documents, d => string.Equals(d["name"]!.ToString(), "Workspace 1", StringComparison.Ordinal));
        Assert.Contains(documents, d => string.Equals(d["name"]!.ToString(), "Workspace 2", StringComparison.Ordinal));

        Assert.NotNull(jObject["data"]!["pullWorkspace"]!["checkpoint"]);
    }

    [Fact(Skip = "Work in progress")]
    public async Task TestCase1_4_StreamDocument_ShouldReceiveUpdates()
    {
        // Note: This is a basic test for the subscription setup.
        // Testing real-time updates would require a more complex test setup.

        const string query = @"
            subscription StreamWorkspaces($headers: WorkspaceInputHeaders!) {
                streamWorkspace(headers: $headers) {
                    documents {
                        id
                        name
                        updatedAt
                        isDeleted
                    }
                    checkpoint {
                        lastDocumentId
                        updatedAt
                    }
                }
            }";

        var variables = new
        {
            headers = new
            {
                Authorization = "Bearer test-token", // Replace with actual auth token if needed
            },
        };

        var request = new
        {
            query,
            variables,
        };

        // Act & Assert
        // For now, we're just checking if the subscription query is accepted
        var response = await HttpClient.PostAsJsonAsync("/graphql", request);
        response.EnsureSuccessStatusCode();

        // In a real scenario, you'd set up a WebSocket connection and listen for updates
    }

    private async Task CreateWorkspace(string name)
    {
        var workspace = new Workspace
        {
            Id = Provider.Sql.Create(),
            Name = name,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
        };

        const string query = @"
            mutation CreateWorkspace($workspace: WorkspaceInputPushRow!) {
                pushWorkspace(workspacePushRow: [$workspace]) {
                    id
                    name
                    updatedAt
                    isDeleted
                }
            }";

        var variables = new
        {
            workspace = new
            {
                assumedMasterState = (object?)null,
                newDocumentState = workspace,
            },
        };

        var request = new
        {
            query,
            variables,
        };

        var response = await HttpClient.PostAsJsonAsync("/graphql", request);
        response.EnsureSuccessStatusCode();
    }

    private async Task VerifyWorkspaceExists(Guid workspaceId)
    {
        const string query = @"
                query PullWorkspace($checkpoint: WorkspaceInputCheckpoint, $limit: Int!) {
                    pullWorkspace(checkpoint: $checkpoint, limit: $limit) {
                        documents {
                            id
                            name
                            updatedAt
                            isDeleted
                        }
                    }
                }";

        var variables = new
        {
            checkpoint = new
            {
                updatedAt = DateTimeOffset.MinValue,
                lastDocumentId = (Guid?)null,
            },
            limit = 100,
        };

        var request = new
        {
            query,
            variables,
        };

        var response = await HttpClient.PostAsJsonAsync("/graphql", request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var jObject = JObject.Parse(content);

        var documents = jObject["data"]!["pullWorkspace"]!["documents"]!;
        Assert.Contains(documents, d => string.Equals(d["id"]!.ToString(), workspaceId.ToString(), StringComparison.Ordinal));
    }
}
