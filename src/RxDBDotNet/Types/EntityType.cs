using System.Reflection;

namespace RxDBDotNet.Types
{
    /// <summary>
    /// Defines the GraphQL object type for an entity.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity being replicated.</typeparam>
    public class EntityType<TEntity> : ObjectType<TEntity> where TEntity : class, IReplicatedEntity
    {
        protected override void Configure(IObjectTypeDescriptor<TEntity> descriptor)
        {
            // Configure fields for the entity
            descriptor.Field(f => f.Id)
                .Type<NonNullType<IdType>>()
                .Description("The unique identifier for the entity.");

            descriptor.Field(f => f.UpdatedAt)
                .Type<NonNullType<DateTimeType>>()
                .Description("The timestamp of the last update to the entity.");

            descriptor.Field(f => f.IsDeleted)
                .Type<NonNullType<BooleanType>>()
                .Description("Indicates whether the entity has been marked as deleted.");

            // Dynamically add other fields based on the TEntity properties
            foreach (var property in typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.Name != nameof(IReplicatedEntity.Id) &&
                    property.Name != nameof(IReplicatedEntity.UpdatedAt) &&
                    property.Name != nameof(IReplicatedEntity.IsDeleted))
                {
                    var fieldType = GetGraphQlType(property.PropertyType);
                    descriptor.Field(property.Name)
                        .Type(fieldType)
                        .Description($"Field for {property.Name}");
                }
            }
        }

        /// <summary>
        /// Maps a C# type to a corresponding GraphQL type.
        /// </summary>
        /// <param name="propertyType">The C# type to map.</param>
        /// <returns>The corresponding GraphQL type.</returns>
        private static Type GetGraphQlType(Type propertyType)
        {
            if (propertyType == typeof(string))
            {
                return typeof(StringType);
            }

            if (propertyType == typeof(int))
            {
                return typeof(IntType);
            }

            if (propertyType == typeof(float))
            {
                return typeof(FloatType);
            }

            if (propertyType == typeof(bool))
            {
                return typeof(BooleanType);
            }

            if (propertyType == typeof(DateTime))
            {
                return typeof(DateTimeType);
            }

            if (propertyType == typeof(DateTimeOffset))
            {
                return typeof(DateTimeType);
            }

            // Add more type mappings as needed
            return typeof(StringType); // Default to string if no match
        }
    }
}
