using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using OpenUrzednik.Http.Infrastructure;

using Shouldly;

namespace OpenUrzednik.Http.Tests.Infrastructure;

public partial class HttpContentJsonTest
{
    private static readonly JsonTypeInfo<int[]> TypeInfo =
        (JsonTypeInfo<int[]>)new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() }.GetTypeInfo(typeof(int[]));

    [Fact]
    public async Task ReadJsonAsync_JsonContent_ReturnsValue()
    {
        // Arrange
        using var content = JsonContent.Create(new[] { 1, 2, 3 }, TypeInfo);

        // Act
        var value = await content.ReadJsonAsync(TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        value.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task ReadJsonAsync_NullContent_ReturnsDefaultOnNetStandardAndThrowsOnDotNet()
    {
        // Arrange
        // .NET Framework leaves HttpResponseMessage.Content null when there is no body; .NET always sets one. Calling the method
        // directly reaches the netstandard2.0 branch's null check on any runtime (the http-netstandard shard, ADR-0020).
        HttpContent content = null!;
        int[]? value = [0];

        // Act
        var exception = await Record.ExceptionAsync(async () => value = await content.ReadJsonAsync(TypeInfo, TestContext.Current.CancellationToken));

        // Assert
#if NET
        exception.ShouldBeOfType<ArgumentNullException>();
#else
        exception.ShouldBeNull();
        value.ShouldBeNull();
#endif
    }
}
