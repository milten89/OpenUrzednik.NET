using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using Bogus;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.TestCommon;

namespace OpenUrzednik.Http.Tests.Infrastructure;

// Shared fields and helpers; tests are in one file per method.
public partial class RestRequestExecutorTest
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };
    private static readonly JsonTypeInfo<TestDto> TypeInfo = (JsonTypeInfo<TestDto>)JsonOptions.GetTypeInfo(typeof(TestDto));

    private readonly TimeProvider _timeProvider = new FakeTimeProvider();
    private readonly OpenUrzednikTelemetry _telemetryProvider = new(NullOpenUrzednikLogger.Instance, NullOpenUrzednikTraceSource.Instance);

    private static readonly RestProviderProfile Profile = new("test", "Test API");

    // Providers resolve the base address themselves; the tests take it from the HttpClient.
    private static RestRequestExecutor CreateConnection(HttpClient httpClient, OpenUrzednikTelemetry telemetryProvider, TimeProvider timeProvider, TimeSpan? timeout = null)
        => new(httpClient, httpClient?.BaseAddress ?? new Uri("https://api.example.com/"), Profile, timeout, telemetryProvider, timeProvider);

    private static HttpClient CreateHttpClient(Faker faker, HttpResponseMessage response)
        => CreateHttpClient(faker, response, out _);

    private static HttpClient CreateHttpClient(Faker faker, HttpResponseMessage response, out StubHttpMessageHandler handler)
    {
        var baseAddress = faker.Internet.UrlWithPath("https").TrimEnd('/') + '/';
        handler = new StubHttpMessageHandler(response);
        return new HttpClient(handler) { BaseAddress = new Uri(baseAddress) };
    }

    private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, TestDto dto)
        => new(statusCode) { Content = JsonContent.Create(dto, TypeInfo) };

    private static (OpenUrzednikTelemetry Provider, IOpenUrzednikLogger Logger, IOpenUrzednikSpan Span) CreateTelemetrySubstitutes()
    {
        var logger = Substitute.For<IOpenUrzednikLogger>();
        var span = Substitute.For<IOpenUrzednikSpan>();
        var tracer = Substitute.For<IOpenUrzednikTraceSource>();
        tracer.StartSpan(Arg.Any<string>()).Returns(span);
        return (new OpenUrzednikTelemetry(logger, tracer), logger, span);
    }

    private sealed record TestDto(string Name, int Value);

    // A response body that never finishes arriving: reads complete only when they are cancelled.
#if !NET
    // Like NeverEndingStream, but its reads ignore the token, as .NET Framework's response stream does once a read has started.
    private sealed class TokenIgnoringStream : Stream
    {
        private readonly TaskCompletionSource<int> _never = new();

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _never.Task;

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
#endif

    private sealed class NeverEndingStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

#if NET
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
#else
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }
#endif

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class DisposeTrackingContent(string body) : StringContent(body, Encoding.UTF8, MediaTypeNames.Application.Json)
    {
        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    private static RestRequestExecutor CreateConnectionWithOverride(HttpClient httpClient, Func<ErrorResponseContext, Task<OpenUrzednikError?>> mapErrorAsync, OpenUrzednikTelemetry? telemetry = null)
        => new(httpClient, httpClient.BaseAddress!, new RestProviderProfile("test", "Test API", mapErrorAsync), telemetry: telemetry);
}
