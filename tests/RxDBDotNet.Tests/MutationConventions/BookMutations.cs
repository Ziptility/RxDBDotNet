﻿// tests\RxDBDotNet.Tests\MutationConventions\BookMutations.cs
#pragma warning disable CA1822
using System;
using System.Threading.Tasks;
using HotChocolate.Types;
using RxDBDotNet.Extensions;

namespace RxDBDotNet.Tests.MutationConventions;

[ExtendObjectType(typeof(Mutation))]
public class BookMutations
{
    public Task<bool> AddBook(Book newBook)
    {
        // no-op. only for schema validation.
        Console.WriteLine($"Received input of type {newBook.GetType()}");

        return Task.FromResult(true);
    }
}
