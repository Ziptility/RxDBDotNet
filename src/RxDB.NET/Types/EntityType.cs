using System.Reflection;

namespace RxDB.NET.Types
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
                    descriptor.Field(property.Name)
                        .Type(GetGraphQlType(property.PropertyType))
                        .Description($"Field for {property.Name}");
                }
            }
        }

        /// <summary>
        /// Maps a C# type to a corresponding GraphQL type.
        /// </summary>
        /// <param name="propertyType">The C# type to map.</param>
        /// <returns>The corresponding GraphQL type.</returns>
        private static IType GetGraphQlType(Type propertyType)
        {
            if (propertyType == typeof(string))
            {
                return new StringType();
            }

            if (propertyType == typeof(int))
            {
                return new IntType();
            }

            if (propertyType == typeof(float))
            {
                return new FloatType();
            }

            if (propertyType == typeof(bool))
            {
                return new BooleanType();
            }

            if (propertyType == typeof(DateTime))
            {
                return new DateTimeType();
            }

            if (propertyType == typeof(DateTimeOffset))
            {
                return new DateTimeType();
            }

            // Add more type mappings as needed
            return new StringType(); // Default to string if no match
        }
    }
}
