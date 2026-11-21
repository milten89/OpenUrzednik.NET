using System.Net;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests;

public sealed partial class WhiteListStatusClientTest
{
    // Fixture, status, error.type: the answers (every outcome of the service is a 200) and the API's HTTP error answers (ADR-0025).
    public static TheoryData<string, int, string?> MetricCases => new()
    {
        { "status-nip-active.json", 200, null },
        { "status-nip-not-on-list.json", 200, NotFoundError.ErrorCode },
        { "status-nip-invalid-nip.json", 200, BadRequestError.ErrorCode },
        { "status-nip-date-out-of-range.json", 200, BadRequestError.ErrorCode },
        { "status-nip-service-inactive.from-contract.json", 200, ServiceUnavailableError.ErrorCode },
        { "error-429-wl-191.from-error-table.json", 429, RateLimitExceededError.ErrorCode },
        { "error-400-wl-118.json", 400, BadRequestError.ErrorCode },
        { "error-400-wl-195.from-error-table.json", 400, ServiceUnavailableError.ErrorCode },
        { "error-500-wl-197.from-error-table.json", 500, UnknownError.ErrorCode },
    };

    // Status and body of every kind of answer; {id} is replaced with the NIP the test sends, as an API that echoes it would.
    public static TheoryData<int, string> LeakAnswers => new()
    {
        { 200, WhiteListFixtures.Load("status-nip-active.json") },
        { 200, WhiteListFixtures.Load("status-nip-not-on-list.json") },
        { 200, WhiteListFixtures.Load("status-nip-invalid-nip.json") },
        { 200, WhiteListFixtures.Load("status-nip-service-inactive.from-contract.json") },
        { 200, """{"result":{"subject":{"nip":"{id}","status":"Błąd dla {id}","statusCode":"I","requestedDate":"02-11-2026","responseDateTime":"21-11-2026 19:58:34","requestId":"Mohy5-99b3aea"}}}""" },
        { 200, """{"result":{"subject":{"nip":"{id}","status":"Błąd dla {id}","statusCode":"{id}","requestedDate":"02-11-2026","responseDateTime":"21-11-2026 19:58:34","requestId":"Mohy5-99b3aea"}}}""" },
        { 400, """{"message":"Nieprawidłowy identyfikator {id}.","code":"WL-115"}""" },
        { 429, """{"message":"Limit dla {id} wyczerpany.","code":"WL-191"}""" },
        { 500, """{"message":"Błąd dla {id}.","code":"WL-197"}""" },
        { 503, """{"message":"Błąd dla {id}.","code":"WL-100"}""" },
        { 404, "Not found: {id}" },
        { 200, "{" },
    };

    [Fact]
    public void GetVatStatusByNipAsync_NullNip_ThrowsBeforeReturningATask()
    {
        // Arrange
        var sut = CreateSut(Fixture("status-nip-active.json"), out var handler);

        // Act
        var exception = Should.Throw<ArgumentNullException>(() => { _ = sut.GetVatStatusByNipAsync(null!, Date, TestContext.Current.CancellationToken); });

        // Assert
        exception.ParamName.ShouldBe("nip");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_FormattedNipAndADate_SendsTheDigitsAndTheDate()
    {
        // Arrange
        var sut = CreateSut(Fixture("status-nip-active.json"), out var handler);

        // Act
        await sut.GetVatStatusByNipAsync("PL 736-812-54-16", Date.AddHours(23), TestContext.Current.CancellationToken);

        // Assert
        var request = handler.Request.ShouldNotBeNull();
        request.Method.ShouldBe(HttpMethod.Get);
        request.RequestUri.ShouldBe(new Uri("https://wykazpodatnikowvat.mf.gov.pl/api2/status/nip/7368125416?date=2026-11-02"));
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_NoDate_SendsNoDateParameterAndSkipsTheDateCheck()
    {
        // Arrange
        var sut = CreateSut(Fixture("status-nip-active-no-date.json"), out var handler);

        // Act
        var result = await sut.GetVatStatusByNipAsync(Nip, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Date.ShouldBe(new DateTime(2026, 11, 21));
        handler.Request.ShouldNotBeNull().RequestUri!.Query.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("status-nip-active.json", VatStatus.Active, "Oytfp-99b3aea")]
    [InlineData("status-nip-exempt.json", VatStatus.Exempt, "a5npN-99b3aea")]
    [InlineData("status-nip-not-registered.json", VatStatus.NotRegistered, "JBvoN-99b3aea")]
    public async Task GetVatStatusByNipAsync_Status_ReturnsItWithTheDayAndTheProof(string fixture, VatStatus status, string requestId)
    {
        // Arrange
        var sut = CreateSut(Fixture(fixture), out _);

        // Act
        var result = await sut.GetVatStatusByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new VatStatusResult(status, Date, requestId, Answered));
        result.Value.RequestDateTime.Offset.ShouldBe(TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_NotInTheRegister_ReturnsNotFoundWithTheProof()
    {
        // Arrange
        var sut = CreateSut(Fixture("status-nip-not-on-list.json"), out _);

        // Act
        var result = await sut.GetVatStatusByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListNotFoundError>();
        error.RequestId.ShouldBe("MdAhc-99b3aea");
        error.RequestDateTime.ShouldBe(Answered);
        error.ShouldBeAssignableTo<NotFoundError>();
    }

    [Theory]
    [InlineData("status-nip-invalid-nip.json", "I")]
    [InlineData("status-nip-date-out-of-range.json", "D")]
    public async Task GetVatStatusByNipAsync_Rejected_ReturnsQueryRejectedErrorWithTheCode(string fixture, string code)
    {
        // Arrange
        var sut = CreateSut(Fixture(fixture), out _);

        // Act
        var result = await sut.GetVatStatusByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListQueryRejectedError>().WhiteListCode.ShouldBe(code);
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_ServiceInactive_ReturnsServiceInactiveError()
    {
        // Arrange
        var sut = CreateSut(Fixture("status-nip-service-inactive.from-contract.json"), out _);

        // Act
        var result = await sut.GetVatStatusByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListServiceInactiveError>().WhiteListCode.ShouldBe("X");
    }

    [Theory]
    [InlineData("""{"result":{"subject":{"statusCode":"Q","requestedDate":"02-11-2026","responseDateTime":"21-11-2026 19:58:34","requestId":"r"}}}""")]
    [InlineData("""{"result":{"subject":{"statusCode":null,"requestedDate":"02-11-2026","responseDateTime":"21-11-2026 19:58:34","requestId":"r"}}}""")]
    [InlineData("""{"result":{"subject":{"statusCode":"C","requestedDate":"2026-11-02","responseDateTime":"21-11-2026 19:58:34","requestId":"r"}}}""")]
    [InlineData("""{"result":{"subject":null}}""")]
    [InlineData("""{"result":null}""")]
    public async Task GetVatStatusByNipAsync_UnusableAnswer_ReturnsSerializationError(string body)
    {
        // Arrange
        var sut = CreateSut(Json(HttpStatusCode.OK, body), out _);

        // Act
        var result = await sut.GetVatStatusByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
    }

    [Theory]
    [InlineData("7368125417")] // a wrong check digit
    [InlineData("736812541")] // too short
    [InlineData("DE7368125416")] // another country's prefix
    public async Task GetVatStatusByNipAsync_InvalidNip_ReturnsMaskedValidationErrorAndSendsNothing(string nip)
    {
        // Arrange
        var sut = CreateSut(Fixture("status-nip-active.json"), out var handler);

        // Act
        var result = await sut.GetVatStatusByNipAsync(nip, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<string>>();
        error.Name.ShouldBe("nip");
        error.Value.ShouldBe(OpenUrzednik.Privacy.SensitiveValue.Mask(nip));
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_InvalidNipAndNoDate_ReturnsOnlyTheNipError()
    {
        // Arrange
        var sut = CreateSut(Fixture("status-nip-active.json"), out var handler);

        // Act
        var result = await sut.GetVatStatusByNipAsync("7368125417", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<string>>();
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_InvalidNipAndDate_ReturnsBothErrorsTogether()
    {
        // Arrange
        var sut = CreateSut(Fixture("status-nip-active.json"), out var handler);

        // Act
        var result = await sut.GetVatStatusByNipAsync("7368125417", Today.AddDays(1), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.Select(e => e.GetType()).ShouldBe([typeof(ValidationError<string>), typeof(ValidationError<DateTime>)]);
        handler.Request.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(DatesOutOfRange))]
    public async Task GetVatStatusByNipAsync_DateOutOfRange_ReturnsValidationErrorAndSendsNothing(DateTime date)
    {
        // Arrange
        var sut = CreateSut(Fixture("status-nip-active.json"), out var handler);

        // Act
        var result = await sut.GetVatStatusByNipAsync(Nip, date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<DateTime>>();
        error.Name.ShouldBe("date");
        error.Value.ShouldBe(date.Date);
        handler.Request.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(DatesInRange))]
    public async Task GetVatStatusByNipAsync_DateAtTheBounds_SendsTheRequest(DateTime date)
    {
        // Arrange
        var sut = CreateSut(Fixture("status-nip-active.json"), out var handler);

        // Act
        var result = await sut.GetVatStatusByNipAsync(Nip, date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        handler.Request.ShouldNotBeNull().RequestUri!.Query.ShouldBe($"?date={date:yyyy-MM-dd}");
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_DailyLimit_ReturnsRateLimitErrorWithTheCodeAndTheDisplayPath()
    {
        // Arrange
        var sut = CreateSut(Fixture((HttpStatusCode)429, "error-429-wl-191.from-error-table.json"), out _);

        // Act
        var result = await sut.GetVatStatusByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListRateLimitExceededError>();
        error.WhiteListCode.ShouldBe("WL-191");
        error.RetryAfter.ShouldBe(TimeSpan.FromHours(12));
        error.Message.ShouldContain("status/nip/{nip}");
        error.Message.ShouldNotContain(Nip);
    }

    [Theory]
    [MemberData(nameof(MetricCases))]
    public async Task GetVatStatusByNipAsync_Completed_RecordsTheOperationWithItsErrorType(string fixture, int status, string? errorType)
    {
        // Arrange
        var metrics = new RecordingMetrics();
        var sut = CreateSut(Fixture((HttpStatusCode)status, fixture), out _, metrics: metrics);

        // Act
        await sut.GetVatStatusByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        var tags = metrics.Recorded.ShouldHaveSingleItem().Tags;
        tags["openurzednik.operation"].ShouldBe("mf_white_list.status.nip");
        tags.TryGetValue("error.type", out var recorded).ShouldBe(errorType is not null);
        recorded.ShouldBe(errorType);
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_InvalidDate_RecordsTheValidationErrorType()
    {
        // Arrange
        var metrics = new RecordingMetrics();
        var sut = CreateSut(Fixture("status-nip-active.json"), out _, metrics: metrics);

        // Act
        await sut.GetVatStatusByNipAsync(Nip, Today.AddDays(1), TestContext.Current.CancellationToken);

        // Assert
        metrics.Recorded.ShouldHaveSingleItem().Tags["error.type"].ShouldBe(ValidationError.ErrorCode);
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_WithADate_StartsTheSpanWithTheMaskedNipAndTheDate()
    {
        // Arrange
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var sut = CreateSut(Fixture("status-nip-active.json"), out _, traceSource: tracer);

        // Act
        await sut.GetVatStatusByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("mf_white_list.status.nip");
        span.Received(1).SetTag("mf_white_list.nip", "******5416");
        span.Received(1).SetTag("mf_white_list.date", "2026-11-02");
        span.Received(1).SetTag("url.path", "/api2/status/nip/{nip}");
        span.Received(2).Dispose(); // the operation span and its HTTP child (one substitute for both)
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_NoDate_SetsNoDateTag()
    {
        // Arrange
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var sut = CreateSut(Fixture("status-nip-active-no-date.json"), out _, traceSource: tracer);

        // Act
        await sut.GetVatStatusByNipAsync(Nip, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        span.Received(1).SetTag("mf_white_list.nip", "******5416");
        span.DidNotReceive().SetTag("mf_white_list.date", Arg.Any<string>());
        span.Received(1).SetTag("url.path", "/api2/status/nip/{nip}");
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_SpanNotRecording_DoesNotMaskForATag()
    {
        // Arrange: a span nobody records gets no masked values, so the call doesn't build them (#345)
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        span.IsRecording.Returns(false);
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.NotFound), out _, traceSource: tracer);

        // Act
        await sut.GetVatStatusByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("mf_white_list.status.nip");
        span.DidNotReceive().SetTag("mf_white_list.nip", Arg.Any<string>());
        span.DidNotReceive().SetTag(Arg.Any<string>(), "******5416");
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_NotInTheRegister_LogsNotOnListAtDebugAndRecordsIt()
    {
        // Arrange
        var (logger, span, tracer) = CreateTelemetrySubstitutes();
        var sut = CreateSut(Fixture("status-nip-not-on-list.json"), out _, logger, tracer);

        // Act
        await sut.GetVatStatusByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Debug, WhiteListEvents.NotOnList, null, "MF White List API has no entity on the list for {operation}",
            "operation", "GetVatStatusByNipAsync");
        logger.DidNotReceive().Log(OpenUrzednikLogLevel.Warning, WhiteListEvents.QueryRejected, Arg.Any<Exception?>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>());
        span.Received(1).SetTag("error.type", NotFoundError.ErrorCode);
    }

    [Theory]
    [InlineData("status-nip-invalid-nip.json", "I", "BadRequest")]
    [InlineData("status-nip-date-out-of-range.json", "D", "BadRequest")]
    [InlineData("status-nip-service-inactive.from-contract.json", "X", "ServiceUnavailable")]
    public async Task GetVatStatusByNipAsync_RejectedOrInactive_LogsQueryRejectedAtWarningWithTheCodeOnly(string fixture, string code, string errorType)
    {
        // Arrange
        var (logger, span, tracer) = CreateTelemetrySubstitutes();
        var sut = CreateSut(Fixture(fixture), out _, logger, tracer);

        // Act
        await sut.GetVatStatusByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Warning, WhiteListEvents.QueryRejected, null, "MF White List API rejected {operation} with code {code}",
            "operation", "GetVatStatusByNipAsync", "code", code);
        logger.DidNotReceive().Log(OpenUrzednikLogLevel.Debug, WhiteListEvents.NotOnList, Arg.Any<Exception?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        span.Received(1).SetTag("error.type", errorType == "BadRequest" ? BadRequestError.ErrorCode : ServiceUnavailableError.ErrorCode);
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_Status_LogsNothingAboutTheAnswer()
    {
        // Arrange
        var (logger, span, tracer) = CreateTelemetrySubstitutes();
        var sut = CreateSut(Fixture("status-nip-active.json"), out _, logger, tracer);

        // Act
        await sut.GetVatStatusByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        logger.DidNotReceive().Log(Arg.Any<OpenUrzednikLogLevel>(), WhiteListEvents.NotOnList, Arg.Any<Exception?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        span.DidNotReceive().SetTag("error.type", Arg.Any<string>());
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_InvalidDate_LogsValidationFailedWithTheMethodName()
    {
        // Arrange
        var (logger, span, tracer) = CreateTelemetrySubstitutes();
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.OK), out var handler, logger, tracer);

        // Act
        await sut.GetVatStatusByNipAsync(Nip, MinDate.AddDays(-1), TestContext.Current.CancellationToken);

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Debug, WhiteListEvents.ValidationFailed, null, "Validation failed for {operation}",
            "operation", "GetVatStatusByNipAsync");
        span.Received(1).SetTag("error.type", ValidationError.ErrorCode);
        handler.Request.ShouldBeNull();
        span.DidNotReceive().SetTag("url.path", Arg.Any<string>());
    }

    [Theory]
    [MemberData(nameof(LeakAnswers))]
    public async Task GetVatStatusByNipAsync_AnyAnswer_LeaksNoNip(int status, string body)
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var sut = CreateSut(Json((HttpStatusCode)status, body.Replace("{id}", Nip)), out _, telemetry);

        // Act
        var result = await sut.GetVatStatusByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotLeak(telemetry, Nip);
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_InvalidValues_LeaksNothing()
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var sut = CreateSut(Fixture("status-nip-active.json"), out _, telemetry);

        // Act
        var result = await sut.GetVatStatusByNipAsync("7368125417", Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.ShouldNotLeak(telemetry, "7368125417");
    }
}
