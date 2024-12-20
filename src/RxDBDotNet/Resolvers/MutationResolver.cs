﻿// src\RxDBDotNet\Resolvers\MutationResolver.cs

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using RxDBDotNet.Configuration;
using RxDBDotNet.Documents;
using RxDBDotNet.Models;
using RxDBDotNet.Security;
using RxDBDotNet.Services;

namespace RxDBDotNet.Resolvers;

/// <summary>
///     Represents a GraphQL mutation resolver for pushing documents.
///     This class implements the server-side logic for the 'push' operation in the RxDB replication protocol.
/// </summary>
/// <typeparam name="TDocument">The type of document being replicated, which must implement IReplicatedDocument.</typeparam>
/// <remarks>
///     Note that this class must not use constructor injection per:
///     https://chillicream.com/docs/hotchocolate/v13/server/dependency-injection#constructor-injection
/// </remarks>
public sealed class MutationResolver<TDocument> where TDocument : class, IReplicatedDocument
{
    /// <summary>
    ///     Pushes a set of documents to the server and detects any conflicts.
    /// </summary>
    /// <param name="documents">The list of documents to push, including their assumed master state.</param>
    /// <param name="documentService">The document service to be used for data access.</param>
    /// <param name="currentUser">Provides access to the current user.</param>
    /// <param name="securityOptions">A collection of authorization requirements to be checked.</param>
    /// <param name="authorizationHelper">The service used for authorization checks.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
    /// <returns>A task representing the asynchronous operation, with a result of any conflicting documents.</returns>
#pragma warning disable CA1822 // disable Mark members as static since this is a class instantiated by DI
    internal async Task<List<TDocument>> PushDocumentsAsync(
        List<DocumentPushRow<TDocument>?>? documents,
        IDocumentService<TDocument> documentService,
        ClaimsPrincipal? currentUser,
        DocumentSecurityOptions<TDocument>? securityOptions,
        AuthorizationHelper? authorizationHelper,
        CancellationToken cancellationToken)
    {
        // Early return if no documents are provided.
        // This is an optimization to avoid unnecessary processing.
        if (documents?.Count == 0)
        {
            return [];
        }

        // Step 1: Categorize documents and detect conflicts
        // This aligns with the RxDB protocol's requirement to detect conflicts before applying changes
        var (conflicts, updates, creates) = await CategorizeDocumentsAsync(documents, documentService, cancellationToken)
            .ConfigureAwait(false);

        // Step 2: Apply changes only if there are no initial conflicts
        // This ensures atomicity of operations as per the RxDB protocol
        if (conflicts.Count == 0)
        {
            var applyConflicts = await ApplyChangesAsync(creates, updates, documentService, authorizationHelper,
                    currentUser, securityOptions, cancellationToken)
                .ConfigureAwait(false);

            conflicts.AddRange(applyConflicts);
        }

        // Step 3: Return all conflicts
        // This allows the client to handle conflicts according to the RxDB protocol
        return conflicts;
    }

    /// <summary>
    ///     Categorizes the incoming documents into conflicts, updates, and new creates.
    ///     This method aligns with the RxDB protocol's requirement to detect conflicts before applying changes.
    /// </summary>
    /// <param name="documents">The list of documents to categorize.</param>
    /// <param name="service">The document service to be used for data access.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
    /// <returns>A tuple containing lists of conflicting, updated, and new documents.</returns>
    private static async Task<(List<TDocument> Conflicts, List<TDocument> Updates, List<TDocument> Creates)> CategorizeDocumentsAsync(
        List<DocumentPushRow<TDocument>?>? documents,
        IDocumentService<TDocument> service,
        CancellationToken cancellationToken)
    {
        var conflicts = new List<TDocument>();
        var updates = new List<TDocument>();
        var creates = new List<TDocument>();

        // If no documents are provided, return empty lists
        if (documents == null)
        {
            return (conflicts, updates, creates);
        }

        // Iterate through each document to categorize it
        foreach (var document in documents)
        {
            if (document == null)
            {
                continue;
            }

            // Fetch the current state of the document from the service
            // This is crucial for detecting conflicts as per the RxDB protocol
            var existing = await service.GetDocumentByIdAsync(document.NewDocumentState.Id, cancellationToken)
                .ConfigureAwait(false);

            if (existing != null)
            {
                // Document exists in the service, handle potential conflicts
                HandleExistingDocument(document, existing, conflicts, updates,
                    service);
            }
            else
            {
                // Document doesn't exist in the service, handle as a new document
                HandleNewDocument(document, conflicts, creates);
            }
        }

        return (conflicts, updates, creates);
    }

    /// <summary>
    ///     Handles the categorization of an existing document.
    ///     This method implements the conflict detection mechanism as per the RxDB protocol.
    /// </summary>
    /// <param name="document">The document push row containing the new state and assumed master state.</param>
    /// <param name="existing">The existing document in the service.</param>
    /// <param name="conflicts">The list to add conflicting documents to.</param>
    /// <param name="updates">The list to add documents that need updating to.</param>
    /// <param name="service">The document service to be used for data access.</param>
    private static void HandleExistingDocument(
        DocumentPushRow<TDocument> document,
        TDocument existing,
        List<TDocument> conflicts,
        List<TDocument> updates,
        IDocumentService<TDocument> service)
    {
        // Check if the assumed master state matches the current state in the service
        // This is a key part of the RxDB conflict detection mechanism
        if (document.AssumedMasterState == null || !service.AreDocumentsEqual(existing, document.AssumedMasterState))
        {
            // Conflict detected: The document has been modified since the client's last sync
            conflicts.Add(existing);
        }
        else
        {
            // No conflict: The document can be updated
            updates.Add(document.NewDocumentState);
        }
    }

    /// <summary>
    ///     Handles the categorization of a new document.
    ///     This method deals with edge cases where the client might be out of sync.
    /// </summary>
    /// <param name="document">The document push row containing the new state and assumed master state.</param>
    /// <param name="conflicts">The list to add conflicting documents to.</param>
    /// <param name="creates">The list to add new documents to.</param>
    private static void HandleNewDocument(
        DocumentPushRow<TDocument> document,
        List<TDocument> conflicts,
        List<TDocument> creates)
    {
        if (document.AssumedMasterState == null)
        {
            // Document doesn't exist and client doesn't assume it does: This is a new document
            creates.Add(document.NewDocumentState);
        }
        else
        {
            // Conflict: Client assumes a state for a non-existent document
            // This handles edge cases where the client might be out of sync
            conflicts.Add(document.AssumedMasterState);
        }
    }

    /// <summary>
    ///     Applies the changes to the service, including creating new documents and updating existing ones.
    ///     This method ensures atomicity of operations as per the RxDB protocol.
    /// </summary>
    /// <param name="creates">The list of new documents to create.</param>
    /// <param name="updates">The list of existing documents to update.</param>
    /// <param name="documentService">The document service to be used for data access.</param>
    /// <param name="authorizationHelper">The service used for authorization checks.</param>
    /// <param name="currentUser">Provides access to the current user.</param>
    /// <param name="securityOptions">A collection of authorization requirements to be checked.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
    /// <returns>A list of documents that are considered conflicting if an error occurs during the process.</returns>
    private static async Task<List<TDocument>> ApplyChangesAsync(
        List<TDocument> creates,
        List<TDocument> updates,
        IDocumentService<TDocument> documentService,
        AuthorizationHelper? authorizationHelper,
        ClaimsPrincipal? currentUser,
        DocumentSecurityOptions<TDocument>? securityOptions,
        CancellationToken cancellationToken)
    {
        // Create new documents
        foreach (var create in creates)
        {
            await AuthorizeOperationAsync(authorizationHelper, currentUser, securityOptions, Operation.Create)
                .ConfigureAwait(false);

            // Set the server timestamp to ensure data integrity and security
            // This overrides any client-provided timestamp, as client-side clocks cannot be trusted
            // It's crucial for maintaining a reliable timeline of document changes and preventing potential exploits
            create.UpdatedAt = DateTimeOffset.UtcNow;
            await documentService.CreateDocumentAsync(create, cancellationToken)
                .ConfigureAwait(false);
        }

        // Update existing documents
        foreach (var update in updates)
        {
            await HandleDocumentUpdateAsync(update, documentService, authorizationHelper, currentUser,
                    securityOptions, cancellationToken)
                .ConfigureAwait(false);
        }

        // Commit all changes in a single transaction
        // This ensures atomicity of the entire operation, a key requirement of the RxDB protocol
        await documentService.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        // If we reach here, all changes were applied successfully
        return [];
    }

    /// <summary>
    ///     Handles the update of a single document, including soft deletes as per RxDB protocol.
    /// </summary>
    /// <param name="update">The document to update.</param>
    /// <param name="service">The document service to be used for data access.</param>
    /// <param name="authorizationHelper">The service used for authorization checks.</param>
    /// <param name="currentUser">Provides access to the current user.</param>
    /// <param name="securityOptions">A collection of authorization requirements to be checked.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
    private static async Task HandleDocumentUpdateAsync(
        TDocument update,
        IDocumentService<TDocument> service,
        AuthorizationHelper? authorizationHelper,
        ClaimsPrincipal? currentUser,
        DocumentSecurityOptions<TDocument>? securityOptions,
        CancellationToken cancellationToken)
    {
        // Set the server timestamp for updates
        // This is a critical security measure that ensures:
        // 1. The integrity of the document's timeline is maintained
        // 2. Potential time-based attacks or inconsistencies due to client clock discrepancies are mitigated
        // 3. The server has the authoritative timestamp for all document changes
        update.UpdatedAt = DateTimeOffset.UtcNow;

        if (update.IsDeleted)
        {
            await AuthorizeOperationAsync(authorizationHelper, currentUser, securityOptions, Operation.Delete)
                .ConfigureAwait(false);

            // Handle soft deletes as per RxDB protocol
            // Documents are never physically deleted, only marked as deleted
            await service.MarkAsDeletedAsync(update, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            await AuthorizeOperationAsync(authorizationHelper, currentUser, securityOptions, Operation.Update)
                .ConfigureAwait(false);

            // Update the existing document
            await service.UpdateDocumentAsync(update, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static Task AuthorizeOperationAsync(
        AuthorizationHelper? authorizationHelper,
        ClaimsPrincipal? currentUser,
        DocumentSecurityOptions<TDocument>? securityOptions,
        Operation operation)
    {
        if (authorizationHelper == null)
        {
            return Task.CompletedTask;
        }

        var documentOperation = new DocumentOperation
        {
            Operation = operation,
            DocumentType = typeof(TDocument),
        };

        return authorizationHelper.AuthorizeAsync(currentUser, documentOperation, securityOptions);
    }
}
