# Samples

🇵🇱 [Wersja polska](README.md)

Two programs that call the real NBP API. Both use the projects in `src/`, so they show the current code, not the version on NuGet.

| Project | What it shows |
| :--- | :--- |
| [`OpenUrzednik.Samples.Console`](OpenUrzednik.Samples.Console/Program.cs) | The NBP clients without a DI container: checking the result, `Match`, `TryGetValue`, a validation error and `EnsureSuccess()` with an exception |
| [`OpenUrzednik.Samples.WebApi`](OpenUrzednik.Samples.WebApi/Program.cs) | A minimal API with `AddOpenUrzednikNbp()` and `AddStandardResilienceHandler()`; it turns the result's errors into HTTP statuses and problem details |

## Running

```bash
dotnet run --project samples/OpenUrzednik.Samples.Console -f net10.0
dotnet run --project samples/OpenUrzednik.Samples.WebApi -f net10.0
```

The Web API answers `/gold/latest`, `/rates/USD?last=5` and `/tables/c/latest`, among others. `/rates/USD?last=0` returns 400 with a validation error, and missing data returns 404.
