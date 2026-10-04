# OpenUrzednik.Core

🇵🇱 [Wersja polska](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Core/README.md)

Shared abstractions for the whole **OpenUrzednik.NET** set: the Result pattern (`OpenUrzednikResult` / `OpenUrzednikResult<T>`), typed errors (`OpenUrzednikError`) and exceptions (`OpenUrzednikException`) for consumers who prefer the classic style.

> Part of [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET). See the [main README](https://github.com/milten89/OpenUrzednik.NET/blob/develop/README.en.md) for an overview of the project.

## Installation

```bash
dotnet add package OpenUrzednik.Core
```

## Example

```csharp
using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Extensions;

OpenUrzednikResult<int> result = OpenUrzednikResult.Success(42);
int value = result.EnsureSuccess();
```

## Working with a result

```csharp
var label = result
    .Map(v => v * 2)
    .Match(v => $"Value: {v}", errors => $"Error: {errors[0].Message}");

if (result.TryGetValue(out var current))
    Console.WriteLine(current);
```

`Map` changes the value of a successful result, `Bind` runs another operation that also returns a result, and `Match` handles both cases. If the result holds errors, they pass through unchanged and the functions you pass aren't called.

`EnsureSuccess()` throws an exception derived from `OpenUrzednikException`. Its `Error` property holds the error with its metadata (e.g. the HTTP status code), and `Errors` holds all of the result's errors. Several validation errors go into one `ValidationException`.

Every provider package (`OpenUrzednik.Gus`, `OpenUrzednik.Krs`, `OpenUrzednik.Mf`, `OpenUrzednik.Nbp`) depends on this package. You usually don't install it on its own, unless you are building your own client in the same style.
