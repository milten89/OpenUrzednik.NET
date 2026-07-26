namespace OpenUrzednik.Core.Validation;

public static class ValidationExtensions
{
    public static ValueValidator<TEnum> EnsureDefinedEnum<TEnum>(this ValueValidator<TEnum> validator)
        where TEnum : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(validator, nameof(validator));

        if (!Enum.IsDefined(validator.Value))
        {
            if (validator.Name is not null)
                validator.AddValidationError($"{validator.Name} of type {typeof(TEnum).Name} has invalid value.");
            else
                validator.AddValidationError($"Value {validator.Value} is invalid for type {typeof(TEnum).Name}");
        }

        return validator;
    }
}
