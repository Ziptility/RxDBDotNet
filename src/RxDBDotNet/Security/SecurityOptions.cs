// src\RxDBDotNet\Security\SecurityOptions.cs
using RxDBDotNet.Documents;

namespace RxDBDotNet.Security;

/// <summary>
/// Provides configuration options for setting up security policies in RxDBDotNet.
/// This class allows for fine-grained control over access to documents.
/// </summary>
/// <typeparam name="TDocument">
/// The type of document that the security options apply to.
/// This type must implement the <see cref="IReplicatedDocument"/> interface.
/// </typeparam>
public sealed class SecurityOptions<TDocument> where TDocument : IReplicatedDocument
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

    /// <summary>
    /// Requires a specified policy to be met for the given operations on the replicated document.
    /// </summary>
    /// <param name="operations">The operations to which the policy applies. This can be a combination of Operation flags.</param>
    /// <param name="policy">The policy that must be met for the specified operations.</param>
    /// <returns>The current <see cref="SecurityOptions{TDocument}"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when no operations are specified or when the policy is null or whitespace.
    /// </exception>
    public SecurityOptions<TDocument> RequirePolicy(Operation operations, string policy)
    {
        if (operations == Operation.None)
        {
            throw new ArgumentException("At least one operation must be specified.", nameof(operations));
        }

        if (string.IsNullOrWhiteSpace(policy))
        {
            throw new ArgumentException("Policy cannot be null or whitespace.", nameof(policy));
        }

        foreach (Operation operation in Enum.GetValues(typeof(Operation)))
        {
            if (operation != Operation.None && operations.HasFlag(operation))
            {
                PolicyRequirements.Add(new PolicyRequirement
                {
                    DocumentOperation = new DocumentOperation
                    {
                        Operation = operation,
                        DocumentType = typeof(TDocument),
                    },
                    Policy = policy,
                });
            }
        }

        return this;
    }
}
