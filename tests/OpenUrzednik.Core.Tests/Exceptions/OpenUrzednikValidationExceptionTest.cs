using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Exceptions;

public class OpenUrzednikValidationExceptionTest
{
    [Fact]
    public void Ctor_SeveralErrors_ListsThemInMessageAndErrors()
    {
        // Arrange
        ValidationError[] errors =
        [
            new("First.", "rule", "first", null),
            new("Second.", "rule", "second", null),
        ];

        // Act
        var exception = new OpenUrzednikValidationException(errors);

        // Assert
        exception.Message.ShouldBe("Validation failed with 2 errors: First.; Second.");
        exception.Error.ShouldBeSameAs(errors[0]);
        exception.Errors.ShouldBe(errors);
    }

    [Fact]
    public void Ctor_OneError_UsesItsMessage()
    {
        // Arrange
        var error = new ValidationError("First.", "rule", "first", null);

        // Act
        var exception = new OpenUrzednikValidationException([error]);

        // Assert
        exception.Message.ShouldBe("First.");
        exception.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public void Ctor_NoErrors_ThrowsArgumentException()
    {
        // Act && Assert
        Should.Throw<ArgumentException>(() => new OpenUrzednikValidationException(Array.Empty<ValidationError>()))
            .ParamName.ShouldBe("errors");
    }

    [Fact]
    public void Ctor_NullErrors_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new OpenUrzednikValidationException(null!));
    }

    [Fact]
    public void Ctor_MessageAndRule_HasNoError()
    {
        // Act
        var exception = new OpenUrzednikValidationException("message", "rule", "name", 1);

        // Assert
        exception.Error.ShouldBeNull();
        exception.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Ctor_NullElement_ThrowsArgumentException()
    {
        // Act && Assert
        Should.Throw<ArgumentException>(() => new OpenUrzednikValidationException([null!]))
            .ParamName.ShouldBe("errors");
    }

    [Fact]
    public void Ctor_NullElementAfterAnError_ThrowsArgumentException()
    {
        // Arrange
        ValidationError[] errors = [new("First.", "rule", "first", null), null!];

        // Act && Assert
        Should.Throw<ArgumentException>(() => new OpenUrzednikValidationException(errors))
            .ParamName.ShouldBe("errors");
    }

    [Fact]
    public void Ctor_MessageRuleNameAndValue_PutsThemInData()
    {
        // Act
        // The value as a validator reports an identifier: masked (ADR-0003).
        var exception = new OpenUrzednikValidationException("message", "nip", "nip", "******7890");

        // Assert
        exception.Data["ruleName"].ShouldBe("nip");
        exception.Data["name"].ShouldBe("nip");
        exception.Data["value"].ShouldBe("******7890");
    }

    [Fact]
    public void Ctor_NullNameAndValue_LeavesThemOutOfData()
    {
        // Act
        var exception = new OpenUrzednikValidationException("message", "rule", null, null);

        // Assert
        exception.Data.Keys.Cast<object>().ShouldBe(["ruleName"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Ctor_NullOrEmptyRuleName_ThrowsArgumentException(string? ruleName)
    {
        // Act && Assert
        Should.Throw<ArgumentException>(() => new OpenUrzednikValidationException("message", ruleName!, "name", 1))
            .ParamName.ShouldBe("ruleName");
    }
}
