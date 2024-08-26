using RxDBDotNet.Documents;

namespace RxDBDotNet.Security;

/// <summary>
/// Provides configuration options for setting up security policies in RxDBDotNet.
/// This class allows for fine-grained control over access to replicated documents.
/// </summary>
/// <typeparam name="TDocument">
/// The type of document that the security options apply to.
/// This type must implement the <see cref="IReplicatedDocument"/> interface.
/// </typeparam>
public sealed class SecurityOptions<TDocument> where TDocument : class, IReplicatedDocument
{
    internal List<PolicyRequirement> PolicyRequirements { get; } = [];

    /// <summary>
    /// Requires a specified policy to be met in order to create the replicated document.
    /// </summary>
    /// <param name="policy">The policy that must be met for create access.</param>
    /// <returns>The current <see cref="SecurityOptions{TDocument}"/> instance for method chaining.</returns>
    public SecurityOptions<TDocument> RequirePolicyToCreate(string policy)
    {
        return RequirePolicy(Operation.Create, policy);
    }

    /// <summary>
    /// Requires a specified policy to be met in order to read the replicated document.
    /// </summary>
    /// <param name="policy">The policy that must be met for read access.</param>
    /// <returns>The current <see cref="SecurityOptions{TDocument}"/> instance for method chaining.</returns>
    public SecurityOptions<TDocument> RequirePolicyToRead(string policy)
    {
        return RequirePolicy(Operation.Read, policy);
    }

    /// <summary>
    /// Requires a specified policy to be met in order to update the replicated document.
    /// </summary>
    /// <param name="policy">The policy that must be met for update access.</param>
    /// <returns>The current <see cref="SecurityOptions{TDocument}"/> instance for method chaining.</returns>
    public SecurityOptions<TDocument> RequirePolicyToUpdate(string policy)
    {
        return RequirePolicy(Operation.Update, policy);
    }

    /// <summary>
    /// Requires a specified policy to be met in order to delete the replicated document.
    /// </summary>
    /// <param name="policy">The policy that must be met for delete access.</param>
    /// <returns>The current <see cref="SecurityOptions{TDocument}"/> instance for method chaining.</returns>
    public SecurityOptions<TDocument> RequirePolicyToDelete(string policy)
    {
        return RequirePolicy(Operation.Delete, policy);
    }

    private SecurityOptions<TDocument> RequirePolicy(Operation operations, string policy)
    {
        PolicyRequirements.Add(new PolicyRequirement
        {
            DocumentOperation = new DocumentOperation
            {
                Operation = operations,
                DocumentType = typeof(TDocument),
            },
            Policy = policy,
        });

        return this;
    }
}
