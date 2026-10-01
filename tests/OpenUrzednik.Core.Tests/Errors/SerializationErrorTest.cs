using System.Text.Json;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Errors;

public class SerializationErrorTest
{
    [Fact]
    public void ToException_WithException_ReturnsSerializationExceptionWrappingOriginal()
    {
        // Arrange
        var original = new JsonException("Unexpected token.");
        var error = new SerializationError("message", original);

        // Act
        var exception = error.ToException();

        // Assert
        var serializationException = exception.ShouldBeOfType<SerializationException>();
        serializationException.ShouldBeAssignableTo<OpenUrzednikException>();
        serializationException.Message.ShouldBe("message");
        serializationException.Code.ShouldBe(SerializationError.ErrorCode);
        serializationException.InnerException.ShouldBeSameAs(original);
    }

    [Fact]
    public void ToException_WithoutException_ReturnsSerializationExceptionWithoutInnerException()
    {
        // Arrange
        var error = new SerializationError("message");

        // Act
        var exception = error.ToException();

        // Assert
        var serializationException = exception.ShouldBeOfType<SerializationException>();
        serializationException.InnerException.ShouldBeNull();
        error.Exception.ShouldBeNull();
    }
}
