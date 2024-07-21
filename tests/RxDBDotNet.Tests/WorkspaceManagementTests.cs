using System.Net.Http.Json;
using LiveDocs.GraphQLApi.Models;
using Newtonsoft.Json.Linq;

namespace RxDBDotNet.Tests
{
    public class WorkspaceManagementTests
    {
        [Fact]
        public async Task TestCase1_1_CreateNewWorkspace_ShouldSucceed()
        {
            // Arrange
            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.LiveDocs_AppHost>();
            await using var app = await appHost.BuildAsync();
            await app.StartAsync();

            using var client = app.CreateHttpClient("replicationApi");

            var newWorkspace = new Workspace
            {
                Id = Guid.NewGuid(),
                Name = "Test Workspace",
                UpdatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false,
            };

            const string query = """
                                 mutation CreateWorkspace($workspace: WorkspaceInputPushRow!) {
                                     pushWorkspace(workspacePushRow: [$workspace]) {
                                         id
                                         name
                                         updatedAt
                                         isDeleted
                                     }
                                 }
                                 """;

            var variables = new
            {
                workspace = new
                {
                    newDocumentState = newWorkspace,
                },
            };

            var request = new { query, variables };

            // Act
            var response = await client.PostAsJsonAsync("/graphql", request);

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var jObject = JObject.Parse(content);

            var createdWorkspace = jObject["data"]!["pushWorkspace"]!.First!;
            Assert.Equal(newWorkspace.Id.ToString(), createdWorkspace["id"]!.ToString());
            Assert.Equal(newWorkspace.Name, createdWorkspace["name"]!.ToString());
            Assert.False(createdWorkspace["isDeleted"]!.Value<bool>());

            // Verify the workspace exists in the database
            const string verificationQuery = """
                                             query GetWorkspace($id: UUID!) {
                                                 workspace(id: $id) {
                                                     id
                                                     name
                                                     updatedAt
                                                     isDeleted
                                                 }
                                             }
                                             """;

            var verificationVariables = new { id = newWorkspace.Id };
            var verificationRequest = new { query = verificationQuery, variables = verificationVariables };

            var verificationResponse = await client.PostAsJsonAsync("/graphql", verificationRequest);
            verificationResponse.EnsureSuccessStatusCode();

            var verificationContent = await verificationResponse.Content.ReadAsStringAsync();
            var verificationJObject = JObject.Parse(verificationContent);

            var verifiedWorkspace = verificationJObject["data"]!["workspace"]!;
            Assert.Equal(newWorkspace.Id.ToString(), verifiedWorkspace["id"]!.ToString());
            Assert.Equal(newWorkspace.Name, verifiedWorkspace["name"]!.ToString());
            Assert.False(verifiedWorkspace["isDeleted"]!.Value<bool>());
        }

        [Fact]
        public async Task TestCase1_2_CreateWorkspaceWithDuplicateName_ShouldFail()
        {
            // Arrange
            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.LiveDocs_AppHost>();
            await using var app = await appHost.BuildAsync();
            await app.StartAsync();

            using var client = app.CreateHttpClient("replicationApi");

            const string existingWorkspaceName = "Existing Workspace";

            // First, create a workspace
            await CreateWorkspace(client, existingWorkspaceName);

            // Now, try to create another workspace with the same name
            var duplicateWorkspace = new Workspace
            {
                Id = Guid.NewGuid(),
                Name = existingWorkspaceName,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false,
            };

            const string query = """
                                 mutation CreateWorkspace($workspace: WorkspaceInputPushRow!) {
                                     pushWorkspace(workspacePushRow: [$workspace]) {
                                         id
                                         name
                                     }
                                 }
                                 """;

            var variables = new
            {
                workspace = new
                {
                    newDocumentState = duplicateWorkspace,
                },
            };

            var request = new { query, variables };

            // Act
            var response = await client.PostAsJsonAsync("/graphql", request);

            // Assert
            var content = await response.Content.ReadAsStringAsync();
            var jObject = JObject.Parse(content);

            Assert.True(jObject["errors"] != null, "Expected an error response");
            Assert.Contains("workspace name is already in use", jObject["errors"]![0]!["message"]!.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        private static async Task CreateWorkspace(HttpClient client, string name)
        {
            var workspace = new Workspace
            {
                Id = Guid.NewGuid(),
                Name = name,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false,
            };

            const string query = """
                                 mutation CreateWorkspace($workspace: WorkspaceInputPushRow!) {
                                     pushWorkspace(workspacePushRow: [$workspace]) {
                                         id
                                         name
                                     }
                                 }
                                 """;

            var variables = new
            {
                workspace = new
                {
                    newDocumentState = workspace,
                },
            };

            var request = new { query, variables };

            var response = await client.PostAsJsonAsync("/graphql", request);
            response.EnsureSuccessStatusCode();
        }
    }
}
