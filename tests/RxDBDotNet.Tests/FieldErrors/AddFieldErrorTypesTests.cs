using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using RxDBDotNet.Services;
using RxDBDotNet.Tests.Model;
using RxDBDotNet.Tests.Utils;

namespace RxDBDotNet.Tests.FieldErrors;

[Collection("DockerSetup")]
public class AddFieldErrorTypesTests : IAsyncLifetime
{
    private TestContext _testContext = null!;

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _testContext.DisposeAsync();
    }

    [Fact]
    public async Task PushWorkspace_WithCustomException_ShouldReturnCustomExceptionError()
    {
        // Arrange
        const string customTestExceptionMessage = "This is a custom test error";
        const string customTesExceptionValue = "Custom value";

        _testContext = TestSetupUtil.Setup(configureServices: services =>
        {
            // Remove the default replicated workspace document service
            services.RemoveAll(typeof(IDocumentService<ReplicatedWorkspace>));

            // Using Moq, register an instance that throws a CustomTestException when CreateDocumentAsync() is called
            services.AddSingleton(_ =>
            {
                var mockDocumentService = new Mock<IDocumentService<ReplicatedWorkspace>>();
                mockDocumentService.Setup(x => x.CreateDocumentAsync(It.IsAny<ReplicatedWorkspace>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new CustomTestException(customTestExceptionMessage, customTesExceptionValue));
                return mockDocumentService.Object;
            });
        },
            // Now register the CustomTestException as a field error type
            configureWorkspaceErrors: types => types.Add(typeof(CustomTestException)));

        var workspaceId = Provider.Sql.Create();
        var newWorkspace = new WorkspaceInputGql
        {
            Id = workspaceId,
            Name = Strings.CreateString(),
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
            Topics = new List<string> { workspaceId.ToString() },
        };

        var workspaceInputPushRowGql = new WorkspaceInputPushRowGql
        {
            AssumedMasterState = null,
            NewDocumentState = newWorkspace,
        };

        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?> { workspaceInputPushRowGql },
        };

        var pushWorkspaceMutation = new MutationQueryBuilderGql()
            .WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql()
                .WithAllFields()
                .WithErrors(new PushWorkspaceErrorQueryBuilderGql().WithCustomTestErrorFragment(new CustomTestErrorQueryBuilderGql().WithAllFields())), pushWorkspaceInputGql);

        // Act
        var response = await _testContext.HttpClient.PostGqlMutationAsync(pushWorkspaceMutation, _testContext.CancellationToken);

        // Assert
        response.Errors.Should().BeNullOrEmpty();
        response.Data.PushWorkspace.Should().NotBeNull();
        response.Data.PushWorkspace?.Workspace.Should().BeNullOrEmpty();
        response.Data.PushWorkspace?.Errors.Should().NotBeNull();
        response.Data.PushWorkspace?.Errors.Should().ContainSingle();
        response.Data.PushWorkspace?.Errors?.Single().Should().BeOfType<CustomTestErrorGql>();
        var customError = response.Data.PushWorkspace?.Errors?.Single() as CustomTestErrorGql;
        customError?.Message.Should().Be(customTestExceptionMessage);
        customError?.CustomField.Should().Be(customTesExceptionValue);
    }
}
