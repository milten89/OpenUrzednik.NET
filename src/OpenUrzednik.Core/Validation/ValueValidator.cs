using System.Collections.ObjectModel;

using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Core.Validation;

public sealed class ValueValidator<T> : IValidationErrorsContainer
{
    public string? Name { get; }
    public T Value { get; }
    public bool IsValid => Errors.Count == 0;

    public ValueValidator(string name, T value)
    {
        Name = name;
        Value = value;
    }

    public ValueValidator(T value)
    {
        Value = value;
    }

    public IReadOnlyList<ValidationError> Errors => _errors?.AsReadOnly() ?? ReadOnlyCollection<ValidationError>.Empty;
    private List<ValidationError>? _errors;

    public void AddValidationError(ValidationError error)
        => (_errors ??= []).Add(error);

    public void AddValidationError(string message, string? name, object value)
            => AddValidationError(new ValidationError(message, name, value));

    public void AddValidationError(string message)
            => AddValidationError(new ValidationError(message, Name, Value!));
}
