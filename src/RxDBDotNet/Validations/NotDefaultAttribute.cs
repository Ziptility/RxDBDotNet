using System.ComponentModel.DataAnnotations;

namespace RxDBDotNet.Validations;

[AttributeUsage(AttributeTargets.Property)]
public sealed class NotDefaultAttribute : ValidationAttribute
{
    private const string DefaultErrorMessage = "The {0} field must not have the default value";

    public NotDefaultAttribute() : base(DefaultErrorMessage)
    {
    }

    public override bool IsValid(object? value)
    {
        // NotDefault doesn't necessarily mean required
        if (value is null)
        {
            return true;
        }

        var type = value.GetType();
        if (type.IsValueType)
        {
            var defaultValue = Activator.CreateInstance(type);
            return !value.Equals(defaultValue);
        }

        // non-null ref type
        return true;
    }
}
