using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Core.Validation;

public abstract class ValueValidator<T>
{
    public abstract string Name { get; }
    public string PropertyName { get; }
    public T Value { get; }

    protected ValueValidator(string propertyName, T value)
    {
        ArgumentNullException.ThrowIfNull(propertyName, nameof(propertyName));

        PropertyName = propertyName;
        Value = value;
    }

    public abstract OpenUrzednikResult Validate();

    protected OpenUrzednikResult GetValidationErrorResult(string message, string? propertyName, object value)
            => OpenUrzednikResult.Failure(new ValidationError(message, Name, propertyName, value));

    protected OpenUrzednikResult GetValidationErrorResult(string message)
            => GetValidationErrorResult(message, PropertyName, Value!);
}
