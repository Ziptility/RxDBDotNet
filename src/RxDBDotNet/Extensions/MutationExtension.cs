using Microsoft.Extensions.DependencyInjection;
using RxDBDotNet.Documents;
using RxDBDotNet.Models;
using RxDBDotNet.Resolvers;
using RxDBDotNet.Security;
using RxDBDotNet.Services;
using static RxDBDotNet.Extensions.DocumentExtensions;

namespace RxDBDotNet.Extensions;

#pragma warning disable CA1812
internal sealed class MutationExtension<TDocument> : ObjectTypeExtension
#pragma warning restore CA1812
    where TDocument : class, IReplicatedDocument
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        var graphQLTypeName = GetGraphQLTypeName<TDocument>();
        var pushDocumentsName = $"push{graphQLTypeName}";
        var pushRowArgName = $"{char.ToLowerInvariant(graphQLTypeName[0])}{graphQLTypeName[1..]}PushRow";

        descriptor.Name("Mutation")
            .Field(pushDocumentsName)
            .UseMutationConvention(new MutationFieldOptions
            {
                Disable = true,
            })
            //.Authorize()
            .Type<NonNullType<ListType<NonNullType<ObjectType<TDocument>>>>>()
            .Argument(pushRowArgName, a => a.Type<ListType<InputObjectType<DocumentPushRow<TDocument>>>>()
                .Description($"The list of {graphQLTypeName} documents to push to the server."))
            .Description($"Pushes {graphQLTypeName} documents to the server and detects any conflicts.")
            .Resolve(context =>
            {
                var mutation = context.Resolver<MutationResolver<TDocument>>();
                var documentService = context.Service<IDocumentService<TDocument>>();
                var documents = context.ArgumentValue<List<DocumentPushRow<TDocument>?>?>(pushRowArgName);
                var cancellationToken = context.RequestAborted;
                var authorizationHelper = context.Services.GetRequiredService<AuthorizationHelper>();
                var currentUser = context.GetUser();
                var securityOptions = context.Services.GetService<SecurityOptions<TDocument>>();

                return mutation.PushDocumentsAsync(
                    documents,
                    documentService,
                    currentUser,
                    securityOptions,
                    authorizationHelper,
                    cancellationToken);
            });
    }
}
