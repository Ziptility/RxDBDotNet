using System;
using System.Collections.Generic;
using FluentAssertions;
using HotChocolate;
using RxDBDotNet.Documents;
using RxDBDotNet.Extensions;

namespace RxDBDotNet.Tests
{
    public class DocumentExtensionsTests
    {
        [Fact]
        public void GetGraphQLTypeName_WithGraphQLNameAttribute_ShouldReturnAttributeName()
        {
            // Arrange
            // Act
            var result = DocumentExtensions.GetGraphQLTypeName<DocumentWithAttribute>();

            // Assert
            result.Should().Be("CustomTypeName");
        }

        [Fact]
        public void GetGraphQLTypeName_WithoutGraphQLNameAttribute_ShouldReturnClassName()
        {
            // Arrange
            // Act
            var result = DocumentExtensions.GetGraphQLTypeName<DocumentWithoutAttribute>();

            // Assert
            result.Should().Be("DocumentWithoutAttribute");
        }

        [GraphQLName("CustomTypeName")]
        private class DocumentWithAttribute : IReplicatedDocument
        {
            public Guid Id { get; set; }
            public DateTimeOffset UpdatedAt { get; set; }
            public bool IsDeleted { get; set; }
            public List<string>? Topics { get; set; }
        }

        private class DocumentWithoutAttribute : IReplicatedDocument
        {
            public Guid Id { get; set; }
            public DateTimeOffset UpdatedAt { get; set; }
            public bool IsDeleted { get; set; }
            public List<string>? Topics { get; set; }
        }
    }
}
