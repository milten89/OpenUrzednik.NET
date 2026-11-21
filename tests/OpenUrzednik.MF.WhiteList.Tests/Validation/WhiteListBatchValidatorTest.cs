using OpenUrzednik.Core.Errors;
using OpenUrzednik.Identifiers;
using OpenUrzednik.MF.WhiteList.Validation;
using OpenUrzednik.Privacy;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Validation;

public sealed class WhiteListBatchValidatorTest
{
    private const string Nip = "7368125416";
    private const string Nip2 = "3169973097";
    private const string InvalidNip = "7368125417";

    private static readonly string[] Names = WhiteListBatchValidator.ElementNames("nips");

    [Fact]
    public void ElementNames_Parameter_NamesTheFirst30Positions()
    {
        // Act
        var result = WhiteListBatchValidator.ElementNames("regons");

        // Assert
        result.Length.ShouldBe(30);
        result[0].ShouldBe("regons[0]");
        result[29].ShouldBe("regons[29]");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(30)]
    public void Validate_1To30ValidIdentifiers_ReturnsSuccessAndTheDigits(int count)
    {
        // Arrange
        var identifiers = Enumerable.Range(0, count).Select(i => i % 2 == 0 ? "PL 736-812-54-16" : Nip2).ToArray();
        var sut = Create(identifiers);

        // Act
        var result = sut.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        sut.Normalized.ShouldBe(Enumerable.Range(0, count).Select(i => i % 2 == 0 ? Nip : Nip2));
        sut.Joined().ShouldBe(string.Join(",", sut.Normalized));
    }

    [Fact]
    public void Joined_Duplicates_KeepsThemInOrder()
    {
        // Arrange
        var sut = Create([Nip2, Nip, Nip2]);
        sut.Validate();

        // Act
        var result = sut.Joined();

        // Assert
        result.ShouldBe($"{Nip2},{Nip},{Nip2}");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void Validate_CountOutOfRange_ReturnsCountError(int count)
    {
        // Arrange
        var sut = Create(Enumerable.Repeat(Nip, count).ToArray());

        // Act
        var result = sut.Validate();

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<int>>();
        error.Name.ShouldBe("nips");
        error.RuleName.ShouldBe("whiteListBatchCount");
        error.Value.ShouldBe(count);
        error.Message.ShouldBe($"'nips' should hold from 1 to 30 identifiers, not {count}.");
    }

    [Fact]
    public void Validate_InvalidIdentifiers_ReturnsAMaskedErrorPerPositionInOrder()
    {
        // Arrange
        var sut = Create([InvalidNip, Nip, "", InvalidNip]);

        // Act
        var result = sut.Validate();

        // Assert
        var errors = result.Errors.Select(e => e.ShouldBeOfType<ValidationError<string>>()).ToList();
        errors.Select(e => e.Name).ShouldBe(["nips[0]", "nips[2]", "nips[3]"]);
        errors.Select(e => e.RuleName).ShouldAllBe(name => name == "nip");
        errors[0].Value.ShouldBe(SensitiveValue.Mask(InvalidNip));
        errors[0].Message.ShouldBe("'nips[0]' has an invalid check digit.");
    }

    [Fact]
    public void Validate_InvalidIdentifierAtANamedPosition_UsesTheCachedName()
    {
        // Arrange: the names are built once per parameter, so a call allocates none for them
        var sut = Create([Nip, InvalidNip]);

        // Act
        var result = sut.Validate();

        // Assert
        ((ValidationError)result.Errors.ShouldHaveSingleItem()).Name.ShouldBeSameAs(Names[1]);
    }

    [Fact]
    public void Validate_TooManyWithInvalidOnesPastTheNamedPositions_NamesThemToo()
    {
        // Arrange
        var identifiers = Enumerable.Repeat(Nip, 32).ToArray();
        identifiers[30] = InvalidNip;
        identifiers[31] = InvalidNip;
        var sut = Create(identifiers);

        // Act
        var result = sut.Validate();

        // Assert
        result.Errors.Select(e => ((ValidationError)e).Name).ShouldBe(["nips", "nips[30]", "nips[31]"]);
    }

    [Fact]
    public void ThrowIfAnyNull_NoNull_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => WhiteListBatchValidator.ThrowIfAnyNull([Nip, "", Nip2], "nips"));
    }

    [Fact]
    public void ThrowIfAnyNull_NullList_ThrowsWithTheParameterName()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => WhiteListBatchValidator.ThrowIfAnyNull(null!, "nips")).ParamName.ShouldBe("nips");
    }

    [Fact]
    public void ThrowIfAnyNull_NullElement_ThrowsNamingItsPosition()
    {
        // Act
        var exception = Should.Throw<ArgumentNullException>(() => WhiteListBatchValidator.ThrowIfAnyNull([Nip, Nip2, null!, Nip], "nips"));

        // Assert
        exception.ParamName.ShouldBe("nips");
        exception.Message.ShouldStartWith("'nips[2]' is null.");
    }

    private static WhiteListBatchValidator Create(string[] identifiers)
        => new("nips", identifiers, Names, static (name, value) => new NipValidator(name, value));
}
