using System.Reflection;

namespace OpenUrzednik.TestCommon.Extensions;

public static class ObjectExtensions
{
    public static T GetPrivateField<T>(this object obj, string fieldName)
    {
        var field = obj.GetType()
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field is null)
            throw new InvalidOperationException(
                $"Unknown field {fieldName} of type {typeof(T)} in {obj.GetType()}");
        if (field.FieldType != typeof(T))
            throw new InvalidOperationException($"Field {fieldName} of type {field.FieldType.Name} does not match expected type {typeof(T)}");
        return  (T)field.GetValue(obj)!;
    }
}