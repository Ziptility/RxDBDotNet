namespace RxDB.NET.Types;

/// <summary>
/// Defines the GraphQL input type for a Checkpoint.
/// </summary>
public class CheckpointInputType : InputObjectType<Checkpoint>
{
    protected override void Configure(IInputObjectTypeDescriptor<Checkpoint> descriptor)
    {
        descriptor.Field(f => f.Id).Type<NonNullType<IdType>>();
        descriptor.Field(f => f.UpdatedAt).Type<NonNullType<DateTimeType>>();
    }
}