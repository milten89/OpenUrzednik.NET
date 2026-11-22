---
status: proposed
date: 2026-11-22
confidence: medium
decision-makers: milten89
---

# 0027. MF White List flat file: packages, caches, store contracts and lookup

## Context and Problem Statement

The Ministry of Finance publishes the VAT White List every day as a flat file: a 7z archive with one JSON file of hashes ([page](https://www.gov.pl/web/kas/plik-plaski), [specification of 30.09.2026](https://www.gov.pl/attachment/768ee5d0-c177-4184-90dc-83b4a27353c6)). [ADR-0024](0024-dependency-policy-with-a-7z-reader.md) approved SharpCompress to read it in the planned package `OpenUrzednik.MF.WhiteList.FlatFile` and left storage and lookup to a record of their own. How does the library get each day's file reliably, keep the data for checks and the archives for later, look a NIP and an account number up, and let apps keep both somewhere else (a database, a cache, object storage) without the library knowing every store?

What the specification says (points of 30.09.2026):

* The file is published "codziennie po godzinie 00:01" (point 2; the ministry's page says about 07:00 at first, 00:01 later). Downloading "może być limitowane"; a block is lifted "około godziny 24:00 danego dnia" (point 7). Archives of the last 30 days can be downloaded (point 14).
* The file confirms "wystąpienie danej pary: numeru NIP oraz numeru rachunku … na Wykazie podatników VAT, aktualnych na dzień generowania pliku" (point 8). Neither the specification nor the page says what a taxpayer should keep as proof of a flat-file check; the only proof the ministry describes is the API's `requestId` ([ADR-0023](0023-mf-white-list-provider.md)).
* There is no test server: the page publishes 17 test pairs (NIP, account) only.

Measured on the archive of 20.11.2026 (`20261120.7z`):

* The archive is 248,926,546 bytes; the JSON inside, 501,913,574 bytes. It holds `naglowek` (the date, `liczbaTransformacji` = `"5000"` and the schema), `skrotyPodatnikowCzynnych` (3,341,992 hashes), `skrotyPodatnikowZwolnionych` (348,169 hashes) and `maski` (1,508 masks of virtual accounts, 944 banks, at most 23 per bank). Each list is sorted, without duplicates; each hash is 128 lowercase hex characters, 64 bytes, so the two lists are 236 MB in binary.
* A hash is `date (YYYYMMDD) + NIP (10 digits) + account (26 digits)`, or without the account for a NIP-only entry, put through `liczbaTransformacji` rounds of `s = lowercase_hex(SHA-512(UTF-8(s)))`. Verified against the 17 test pairs (whose header date is wrong: they match `20191121`).
* One chain of 5,000 rounds costs 3.5 ms on .NET 10 (`SHA512.HashData` over spans; 11 ms with a naive implementation) and 18 ms on .NET Framework 4.8 (naive). The date is part of every hash, so every day's data is new: a day is imported whole, never updated.

## Decision Drivers

* One line for the default case with DI (ADR-0026), and a few lines without it; no store to install for the default.
* No identifiers in stores, logs, spans or errors (ADR-0023, ADR-0025): a hash of `date + NIP + account` can be recomputed, so it is pseudonymous personal data too.
* Other places for the data (SQLite, Redis, …) and for the archives (S3, Azure Blob, …) as separate packages with their own dependencies, without a change to the base flat-file package.
* Public contracts that are frozen once released (ADR-0011) and can still grow.
* A reliable daily refresh: the download may be limited or fail, the file may not be published yet.
* No new dependency beyond ADR-0024 in the base flat-file package; netstandard2.0 support (ADR-0005); bounded memory (the 500 MB JSON is never loaded whole).

## Considered Options

* Package split: a base package with the built-in disk stores, plus one package per store with a dependency; or every store, the disk ones included, in a package of its own
* Store contracts: public interfaces that grow through new interfaces checked with `is`; or public abstract base classes
* What a store receives: a raw `Stream` of a binary format; or decoded, sorted hashes through independent readers
* The hash: `ReadOnlyMemory<byte>` of 64 bytes; or a 64-byte struct
* Store failures: thrown exceptions; or `OpenUrzednikResult`
* Retention: each store's own option; central limits of days, files and bytes that the refresher enforces through the contracts; or central limits of days and files, with a size limit in the options of each store that can have one
* Store configuration: typed options per store; the stores as properties of the flat-file options; or a universal provider name with a connection string
* Mask lookup: all candidates in one store call; or the exact hash first and the masks only after a miss
* Evidence: the matched hash in the result; or the file's date and the kept archive
* The cold-cache format: always the ministry's `.7z`; a transform inside each archive store; a one-way hook before saving; or a two-way archive format (encode before saving, decode on restore), the ministry's `.7z` by default
* The scheduled refresh: in the DI package; or the refresh logic in the base package and a thin hosting package
* The built-in hot store: an in-memory array; a sorted binary file with a prefix index, read through file reads, loaded into managed memory on request, or memory-mapped; a folder per hash prefix; SQLite in the base package

## Decision Outcome

Chosen options: the base package with the built-in disk stores, store packages only for stores with dependencies; interfaces that grow through new interfaces; decoded, sorted hashes through independent readers; a 64-byte struct; results, not exceptions; central limits of days and files, with size limits in the stores' options; typed options per store; the exact hash first; the file's date and the kept archive as evidence, no hash in the result; a two-way archive format, the ministry's `.7z` by default; the refresh logic in the base package with a thin hosting package; one memory-mapped file per day with a prefix index. Together they keep the default to one package and one line with DI, keep identifiers and file formats away from stores, and leave each store package free to bring its own dependency.

**Packages** (names by [ADR-0016](0016-next-providers-and-package-naming.md)'s variant rule; the DI and hosting packages each depend on the one above them, and a store package depends on the base flat-file package only):

| Package | Contents | Third-party dependencies |
|---|---|---|
| `OpenUrzednik.MF.WhiteList.FlatFile` | Download, checksum, unpack, decode, the hash chain and masks, the checks, the refresher, both store contracts and the archive-format contract, the disk hot store, the disk archive store and both built-in archive formats | SharpCompress (ADR-0024) |
| `OpenUrzednik.MF.WhiteList.FlatFile.DependencyInjection` | Registration, options bound from configuration, replacing either store or the archive format, the rejection handler of the download client ([#360](https://github.com/milten89/OpenUrzednik.NET/issues/360)) | `Microsoft.Extensions.Http`, `.Options` and `Polly.Core`, as ADR-0024 allows DI packages |
| `OpenUrzednik.MF.WhiteList.FlatFile.Hosting` | A hosted service that runs the refresher every day ([#361](https://github.com/milten89/OpenUrzednik.NET/issues/361)) | `Microsoft.Extensions.Hosting.Abstractions`, an abstraction at its lowest version, 8.0.0 (approved by the maintainer on 2026-11-22) |
| `OpenUrzednik.MF.WhiteList.FlatFile.Sqlite`, `.Redis`, `.S3`, … | One hot or archive store each, when an app needs it; no DI code (the DI package registers any store, below) | Each one's own, approved in its own ADR |

**Names:** the type prefix is `WhiteListFlatFile` (`WhiteListFlatFileTelemetry`, `WhiteListFlatFileLogEvents`, every public type below); the short name is `mf_white_list_flat_file` (ADR-0016), used in span names, tags, the `{provider}` log property and the `openurzednik.provider` metric tag.

**The base package's public API is enough.** A flat-file check returns the VAT status of the list that matched as the base package's `VatStatus` (root namespace, [ADR-0026](0026-client-api-with-one-public-namespace-per-provider.md)); everything else is new and lives in the flat-file packages. Nothing in `OpenUrzednik.MF.WhiteList` changes for the flat file, so its 1.1.0 API can be released as it is.

**The checks** (`IWhiteListFlatFileClient`; `WhiteListFlatFileClient` has the constructors `(IWhiteListFlatFileStore store)` and `(IWhiteListFlatFileStore store, WhiteListFlatFileOptions? options = null, TimeProvider? timeProvider = null, OpenUrzednikTelemetry? telemetry = null)`):

* `CheckNipBankAccountAsync(string nip, string bankAccount, DateTime date, …)` and `CheckNipAsync(string nip, DateTime date, …)` return `OpenUrzednikResult<WhiteListFlatFileCheckResult>`, a sealed record of `VatStatus? VatStatus` (`Active` or `Exempt` for the list that matched; `null` when the pair isn't in the file; never `NotRegistered`, which the file doesn't hold), `bool VirtualAccount` (matched through a mask) and `DateTime FileDate` (the day of the file that answered).
* The NIP and the account go through the shared validators (masked errors, ADR-0023). The client reads the day's info (masks, number of rounds) from the store, looks the exact hash up first (one chain, 3.5 ms), and only after a miss, when the account's bank has masks, builds the mask candidates as the specification describes and looks them up in a second call. The lowest matching candidate wins.
* A day the store doesn't have is `WhiteListFlatFileDayNotFoundError : NotFoundError`; the client knows only the hot store, so restoring the day is the refresher's job (`RestoreAsync`). Download, checksum and archive failures are the provider's error types (ADR-0021).

**Getting a day** (`WhiteListFlatFileSource`, the two constructors of ADR-0026 with an `HttpClient`):

* `DownloadAsync(DateTime date, …)` downloads the archive through a new streaming method of the shared HTTP layer (ADR-0019), to the work directory. The options' timeout covers the whole download (default 30 minutes); the HTTP request uses the same display-path and telemetry rules as the API clients. A 404 is "not published yet" for today before publication and "deleted" for a day older than 30 days: sealed provider subclasses of `HttpNotFoundError` tell the two apart (ADR-0021; names in #321). The download limit (point 7) is a provider subclass of `HttpRateLimitExceededError` whose `RetryAfter` is the server's `Retry-After` when present and otherwise the time until midnight in Warsaw, as ADR-0023 decides for the API. The DI package sets the download client's `HttpClient.Timeout` to the options' timeout; an app that adds a resilience handler to that client must raise the handler's total and attempt timeouts above it (the standard handler's defaults would cancel the download), and the README shows how. Resuming a broken download with HTTP `Range` is used if the server supports it, which #321 verifies.
* `OpenArchiveAsync(Stream archive, …)` opens an archive from anywhere (the archive store, a file); it is copied to the work directory first, because a 7z archive needs random access.
* `OpenBinaryAsync(Stream file, …)` opens a day kept in the library's binary day file (the binary archive format, below), copied to the work directory first; it checks the header, the SHA-256 of the content, the counts against the length and the sort order.
* `DownloadAsync` and `OpenArchiveAsync` check the JSON against the archive's `.sha512sum` entry, read it with `Utf8JsonReader` as a stream, and decode it once into a working file in the **binary day file** format: a header (a magic number, a version, the date, the number of rounds, the counts, the masks and a SHA-256 of the content), the 64K-entry prefix index and the two sorted lists, 64 bytes per hash. All three return a `WhiteListFlatFileDay` over that file, which also carries the day's masks and number of rounds.

**The refresher** (`WhiteListFlatFileRefresher`, base package; it takes the source, both stores, an optional archive format (default: the ministry's `.7z`), the options, a `TimeProvider` and telemetry):

* `RefreshAsync(DateTime? date = null, …)` (default: today in Warsaw) runs one attempt and returns a result: nothing to do when the hot store has the day; restore from the archive store when it has the archive; otherwise download, encode the archive with the archive format and save it to the archive store, import into the hot store, dispose the day. With `ColdCacheMaxFiles = 0` it skips the encoding and the save, and no archive store is needed. Then it enforces both caches' limits, oldest day first, never the day it just refreshed. A failure keeps everything it had.
* `RestoreAsync(DateTime date, …)` imports a day from the archive store into the hot store, through the archive format's decode. A check never does this by itself: unpacking 249 MB doesn't belong in a check.
* Retries over time (the file not published yet, a failed download, a limit lifted at midnight) belong to whoever schedules it: the hosting package, a cron job or a timer.

**The caches** (`WhiteListFlatFileOptions`, validated on start):

```csharp
public sealed class WhiteListFlatFileOptions
{
    public Uri? DownloadUrl { get; set; }          // default: the ministry's address from the specification
    public string? WorkDirectory { get; set; }     // default: a folder next to the disk store's directory, on the same volume
    public TimeSpan? Timeout { get; set; }         // the whole download; default 30 minutes
    public int HotCacheMaxDays { get; set; } = 2;  // imported days; at least 1
    public int ColdCacheMaxFiles { get; set; } = 30;   // archives; 0 switches the cold cache off
}
```

* The limits of days and files are central: the refresher enforces them through the stores' `ListDaysAsync`/`DeleteDayAsync` and `ListAsync`/`DeleteAsync`, so every store obeys the same rules.
* A size limit depends on the store (a disk quota, a bucket's cost, a database's own limits), so it is an option of the store that can have one, which the store enforces after each save, oldest first, never the day it just saved: the disk archive store has `MaxTotalBytes` (default none; below 1 GB, about four archives of today's size, it is a configuration error on start). Where a store keeps its data is its own typed option too.
* The disk stores' directories default to a folder under the user's local application data (`Environment.SpecialFolder.LocalApplicationData`), never the temp folder, where tmpfs, systemd-tmpfiles or Storage Sense may remove kept archives. The disk store takes a working file over by a rename only when both are on one volume; otherwise it copies it, and the day becomes visible only after the copy.
* The defaults keep today and yesterday hot (about 236 MB per day in the disk store) and the last 30 archives cold (about 7.5 GB), as many as the ministry keeps.

**The hot store contract:**

```csharp
public interface IWhiteListFlatFileStore
{
    Task<OpenUrzednikResult> ImportAsync(WhiteListFlatFileDay day, CancellationToken cancellationToken = default);
    Task<OpenUrzednikResult<WhiteListFlatFileDayInfo>> GetDayAsync(DateTime day, CancellationToken cancellationToken = default);
    Task<OpenUrzednikResult<WhiteListFlatFileMatch>> FindAsync(DateTime day, IReadOnlyList<WhiteListFlatFileHash> hashes, CancellationToken cancellationToken = default);
    Task<OpenUrzednikResult<IReadOnlyList<DateTime>>> ListDaysAsync(CancellationToken cancellationToken = default);
    Task<OpenUrzednikResult> DeleteDayAsync(DateTime day, CancellationToken cancellationToken = default);
}

public sealed class WhiteListFlatFileDay : IDisposable          // created only by WhiteListFlatFileSource
{
    public DateTime Date { get; }
    public int TransformationCount { get; }
    public IReadOnlyList<string> Masks { get; }                 // not personal data
    public long ActiveCount { get; }
    public long ExemptCount { get; }
    public WhiteListFlatFileHashReader OpenActive(long startIndex = 0);
    public WhiteListFlatFileHashReader OpenExempt(long startIndex = 0);
}

public sealed class WhiteListFlatFileHashReader : IDisposable
{
    public long Position { get; }
    public ValueTask<int> ReadAsync(Memory<WhiteListFlatFileHash> buffer, CancellationToken cancellationToken = default);
}

public readonly struct WhiteListFlatFileHash : IEquatable<WhiteListFlatFileHash>, IComparable<WhiteListFlatFileHash>
{
    public const int Size = 64;   // eight private ulong fields; FromBytes, CopyTo, Equals, CompareTo; ToString without the hash
}

public readonly struct WhiteListFlatFileMatch
{
    public static WhiteListFlatFileMatch None { get; }
    public static WhiteListFlatFileMatch Active(int candidateIndex);
    public static WhiteListFlatFileMatch Exempt(int candidateIndex);
    public WhiteListFlatFileList List { get; }                  // None, Active, Exempt
    public int CandidateIndex { get; }
}
```

* `WhiteListFlatFileDayInfo` is a sealed record of the day's date, number of rounds, masks and counts (with a custom `Equals` for its list, as the conventions require). A store keeps it with the day, so a check in another process reads it from the store.
* A store sees only `WhiteListFlatFileHash` values and the day's info: never a NIP, an account, the JSON or hex. The hashing and the checks stay in the base package, so a store can't get them wrong or hold identifiers.
* `ImportAsync` gets a verified, decoded day; importing a day again replaces it. Each `Open…` call returns an independent reader over the working file: a store may read a list again, read both in parallel, resume at a position after a failure, and choose its own batch size (the buffer is its own). The hashes come in the file's sorted order. Disposing the day deletes the working file, unless the built-in disk store took it over through an internal path of the base package; other stores copy what they need through the readers.
* `FindAsync` gets all candidates of one lookup in one call; it returns the lowest matching candidate and its list. A day the store doesn't have is a `NotFoundError` from `GetDayAsync` and `FindAsync`.
* Every method returns its failures as results (ADR-0002): a network store maps its own exceptions to the library's error types (a connection failure to `ConnectionFailedError`, a timeout to `RequestTimeoutError`, …); only caller cancellation and invalid arguments throw.
* `WhiteListFlatFileHash` is exactly 64 bytes, blittable on every target: the reader fills a buffer of them straight from the working file (`MemoryMarshal.Cast`), equality is a vectorized `SequenceEqual` on .NET and eight integer comparisons on netstandard2.0, and `CompareTo` compares the bytes in order (big-endian), the order of the ministry's lists. `ToString()` never returns the hash, so it can't reach a log by accident.

**The archive store contract** (the cold cache):

```csharp
public interface IWhiteListFlatFileArchiveStore
{
    Task<OpenUrzednikResult> SaveAsync(DateTime day, Stream archive, CancellationToken cancellationToken = default);
    Task<OpenUrzednikResult<Stream>> OpenAsync(DateTime day, CancellationToken cancellationToken = default);
    Task<OpenUrzednikResult<IReadOnlyList<WhiteListFlatFileArchiveInfo>>> ListAsync(CancellationToken cancellationToken = default);   // day and size in bytes
    Task<OpenUrzednikResult> DeleteAsync(DateTime day, CancellationToken cancellationToken = default);
}
```

* An archive store keeps the bytes the archive format gives it, unchanged; it never needs to support seeking (the source copies what `OpenAsync` returns). A missing day is a `NotFoundError`; failures are results, as for the hot store.

**The archive format** (what the cold cache keeps; independent of where an archive store keeps it):

```csharp
public interface IWhiteListFlatFileArchiveFormat
{
    // Before SaveAsync: the ministry's verified archive and the decoded day; returns the bytes to keep.
    Task<OpenUrzednikResult<Stream>> EncodeAsync(WhiteListFlatFileDay day, Stream ministryArchive, CancellationToken cancellationToken = default);
    // On restore: what OpenAsync returned, turned back into a content the source can open.
    Task<OpenUrzednikResult<WhiteListFlatFileArchiveContent>> DecodeAsync(DateTime day, Stream stored, CancellationToken cancellationToken = default);
}

public sealed class WhiteListFlatFileArchiveContent : IDisposable
{
    public static WhiteListFlatFileArchiveContent MinistryArchive(Stream archive);   // the ministry's .7z
    public static WhiteListFlatFileArchiveContent Binary(Stream file);               // the library's binary day file
    public WhiteListFlatFileArchiveKind Kind { get; }
    public Stream Stream { get; }
}
```

* The default, `WhiteListFlatFileMinistryArchiveFormat`, keeps the ministry's `.7z` as downloaded: it is the evidence (below), and it can be checked against its `.sha512sum` again at any time.
* The base package also has `WhiteListFlatFileBinaryArchiveFormat`: the binary day file, the format the source decodes into and the disk store keeps. A restore then skips unpacking and decoding the JSON. It is about 5% smaller (236 MB against 249 MB) than the `.7z`, no more: the hashes are random bytes and don't compress, so the format isn't compressed.
* An app's own format implements the interface (a different container, encryption, its own compression). Its decode returns one of the two contents, so a day is still built and verified only by `WhiteListFlatFileSource` (`OpenArchiveAsync` for the `.7z`, `OpenBinaryAsync` for the binary file, which checks the header, the checksum, the counts and the sort order); a third-party format never builds a `WhiteListFlatFileDay` itself.
* A format that changes later doesn't break the archives kept before: the built-in formats recognize their own bytes (the 7z signature, the binary file's magic number) and decode both, and a stored archive neither of them recognizes is a `SerializationError` from the restore.

**The contracts (both stores and the archive format) are frozen once released** (ADR-0011): a new capability is a new interface that the base package uses when `store is …`, as with the telemetry interfaces ([ADR-0025](0025-telemetry-abstractions-with-metrics.md)). The types the library creates (the day, the reader, the infos, the structs) are sealed classes, sealed records or structs, so they may gain members.

**The built-in stores:**

* `WhiteListFlatFileDiskStore` (options: directory): a day is one binary day file, with its 64K-entry index of hash prefixes (256 KB), read as a memory-mapped file (`System.IO.MemoryMappedFiles`, on every target). A lookup touches the index and one bucket of about 3.6 KB; the operating system's page cache keeps the pages in use in memory and shares them between processes on the machine, without 236 MB in each process's managed heap. Its import takes the working file over by a rename, without copying, when both are on one volume (above); a day becomes visible by an atomic rename. Checks running on a day being deleted finish on it: the store unmaps a day only after them, and a file that Windows won't delete yet because another process maps it is deleted at a later refresh.
* `WhiteListFlatFileDiskArchiveStore` (options: directory, default a folder under the local application data, as above; `MaxTotalBytes`): one file per day, as the archive format encoded it.

**Configuration:** each store has typed options of its own (a directory, a connection string, a bucket), never a property of `WhiteListFlatFileOptions`. In an app's configuration they still sit in one place, as subsections of the flat-file section (`OpenUrzednik:MF:WhiteList:FlatFile:DiskStore`, `…:S3ArchiveStore`). The DI package's `AddOpenUrzednikMFWhiteListFlatFile(section)` registers the client, the source, the refresher and the disk stores with options bound from configuration, and returns the download client's `IHttpClientBuilder` (ADR-0024). Its other extensions of `IHttpClientBuilder` carry the provider in their names, so they can't clash with another DI package's: `UseWhiteListFlatFileStore<TStore, TOptions>(section)`, `UseWhiteListFlatFileArchiveStore<TStore, TOptions>(section)`, `UseWhiteListFlatFileArchiveFormat<TFormat>()` and `AddOpenUrzednikMFWhiteListFlatFileRejectionHandler()`; a store package needs no DI code of its own. The hosting package adds the daily run (`AddWhiteListFlatFileDailyRefresh()`): after publication, with retries and backoff, and at once on start when today is missing.

**Evidence:** the result carries the file's date and the list that matched, not the hash: a hash can be recomputed by anyone from the date, NIP and account, so on its own it proves nothing, and the ministry names no proof for the flat file. What shows that a pair was in a day's file is that file, which the ministry keeps for 30 days and the cold cache can keep longer, as the ministry's `.7z` by default; an app that chooses another archive format keeps derived data, which can't be checked against the ministry's checksum. The README quotes point 8, says the ministry defines no proof for the flat file (unlike the API's `requestId`), and gives no legal advice (ADR-0023).

**Telemetry:** spans `mf_white_list_flat_file.download`, `.open_archive`, `.refresh`, `.restore`, `.import` (around a store's import, written by the refresher), `.check.nip_bank_account` and `.check.nip`; the operation-duration metric (ADR-0025). Tags and log properties carry days, counts, sizes and outcomes, never an identifier or a hash.

### Consequences

* Good, because the default case is one line with DI, the disk stores need no package of their own, and a store with a dependency is one more package and one more line.
* Good, because stores only keep opaque 64-byte values, the day's info and archives: no parsing, no identifiers, no hashing to get wrong, and one set of contract tests proves every store behaves the same.
* Good, because the limits of days and files are set in one place and every store obeys them, a size limit means what its store needs, and archives can be kept beyond the ministry's 30 days.
* Good, because the cold cache keeps the ministry's file by default, and an app that prefers fast restores or its own format changes one line, with any archive store.
* Bad, because an archive kept in another format than the ministry's `.7z` can't be checked against the ministry's checksum any more, so it is weaker evidence; the README says so next to the option.
* Good, because a memory-mapped day gives lookups at about memory speed without an option to choose and without a managed allocation per day.
* Good, because the refresh logic works without a host (a cron job, a timer), and memory stays bounded.
* Good, because the base package's 1.1.0 API needs no change.
* Bad, because the three contracts are frozen in their first release; a missed capability can only come as a new interface.
* Bad, because preparing a day needs disk space: the archive (249 MB) while it is unpacked and the working file (236 MB), which the disk store keeps; the defaults use about 0.5 GB hot and 7.5 GB cold.
* Bad, because a check costs 3.5 ms on .NET (18 ms on .NET Framework) for the exact hash, and up to 23 more chains, about 80 ms (about 400 ms on .NET Framework), when the account isn't found at a bank with masks; the cost is the ministry's design.

### Confirmation

* Known-answer tests of the hash chain with the ministry's 17 test pairs; small 7z fixtures in the real format, built from them, for the source, the decoder and the AOT smoke app (ADR-0024).
* Unit tests of the decoder, the hash struct, the readers, the checks and the refresher on streams, spans and `FakeTimeProvider`; the disk stores' tests on real temporary directories (rename, open files, a mapped day being deleted, retention and the size limit, a missing day).
* One contract test suite for each store contract that every store in the repository runs, and one for the archive format: a round trip of encode and decode to the same day, a restore of archives kept in the other built-in format, and a damaged or unknown archive as an error.
* `tests/OpenUrzednik.Architecture.Tests` covers the new packages' public API; leak tests show no identifier or hash in logs, spans and errors.
* The `reviewer` agent flags a change to either contract, hashing or identifiers outside the base flat-file package, and a store package's dependency without its ADR (its checklist gets these when this record is accepted).

## Pros and Cons of the Options

### Every store in a package of its own, the disk ones included

* Good, because the base package would hold no storage code.
* Bad, because every app would install more packages before its first check, for stores without dependencies. Testability isn't a reason either: the disk code is tested on real temporary directories and the rest on streams, without a file-system abstraction such as `System.IO.Abstractions` (a third-party dependency ADR-0024 would have to approve, whose mocks don't reproduce renames over open files).

### Public abstract base classes for stores

* Good, because a later version could add `virtual` members with a default body.
* Bad, because the repository already grows frozen contracts through new interfaces checked with `is` (ADR-0025), and a base class would take a store's only base type.

### A raw `Stream` of a binary format for the import

* Good, because the contract would be one stream.
* Bad, because every store would parse the format, could get the 64-byte framing wrong, and couldn't read a list twice, in parallel or from a position.

### `ReadOnlyMemory<byte>` for a hash

* Good, because it needs no new type.
* Bad, because its length isn't part of the type, so every store must check it, and comparisons and the sort order are left to each store.

### Thrown exceptions for store failures

* Good, because the contracts would be simpler.
* Bad, because a network store's failures are expected failures that ADR-0002 returns as results, and the client couldn't map unknown exceptions without `catch (Exception)`, which the code rules forbid.

### Retention as each store's own option

* Good, because the hot contract would have three methods instead of five.
* Bad, because each store would be configured separately and could disagree, and the archive limits would still need a central place.

### A central size limit

* Good, because one more limit would sit with the others.
* Bad, because what a size limit means depends on the store (a disk quota, a bucket's cost, a database that limits itself), and some stores already enforce one natively (an S3 lifecycle rule), so a central byte count would duplicate or contradict them. The archive sizes `ListAsync` reports are for telemetry and the stores' own limits.

### The stores as properties of the flat-file options

* Good, because the code would configure everything on one object.
* Bad, because an options class is data bound from configuration, and the binder creates only non-abstract types with a public parameterless constructor ([Options pattern in .NET](https://learn.microsoft.com/dotnet/core/extensions/options#bind-hierarchical-configuration)), so an abstract store property can't be bound. A store is a service with dependencies, a lifetime and disposal; DI registers it, and nested configuration sections already keep the settings in one place.

### A universal provider name and connection string

* Good, because an app could switch stores by configuration alone.
* Bad, because a string isn't enough for every store (a loose settings dictionary would follow), a typo shows only at runtime, and every store an app may switch to still has to be referenced and registered in code. It can be added to the DI package later without touching the contracts.

### All mask candidates in one store call

* Good, because a virtual account would take one store call.
* Bad, because the client can't tell a virtual account before the lookup, so every account at a bank with masks would cost up to 24 chains instead of one.

### Other cold-cache formats

* Always the ministry's `.7z`: good, because it is the strongest evidence and needs no extension point; bad, because an app that wants fast restores or its own container couldn't have them.
* A transform inside each archive store: bad, because where an archive is kept and what it contains would be tied together, so the binary format on S3 would need its own S3 store.
* A one-way hook before saving (a `Func<Stream, Stream>`): bad, because a restore couldn't turn the kept bytes back into something the source can open.

### The matched hash in the result

* Good, because an app could store it.
* Bad, because a hash can be recomputed from the date, NIP and account and proves nothing on its own; the archive does, and keeping it is the cold cache's job.

### The scheduled refresh in the DI package

* Good, because one package fewer.
* Bad, because every app with DI would get `Microsoft.Extensions.Hosting.Abstractions`, and apps without a host (a cron job, a function) would get no robust refresh; the logic belongs in the base package and the scheduling in a thin package.

### An in-memory array only, file reads, loading into managed memory, a folder per prefix, or SQLite in the base package

* An in-memory array only: bad, because 236 MB per day in every app's memory is too much as a default.
* File reads of the binary file: good, because nothing is mapped; bad, because each lookup is a system call and a copy, where a mapped file reads cached pages directly.
* Loading a day into managed memory on request: bad, because it puts 236 MB per day on the managed heap of every process and adds an option, where the page cache keeps the pages in use and shares them between processes.
* A folder per prefix: bad, because thousands of files per day are slow to write and delete.
* SQLite in the base package: bad, because it brings a native library per platform and a dependency every app would get; it is a store package, with its own ADR, when an app needs it.

## More Information

* Readiness checklist (ADR-0016) for the flat file, beyond what ADR-0023 covers for the API: transport and authentication (HTTPS GET of a public file, none); quota (a download limit, lifted about midnight, point 7); test environment (none, only the 17 test pairs, so fixtures are built from them); personal data (none in the file; the hashes are pseudonymous, so they are never logged); legal basis for automated access (art. 96b ust. 2 of the VAT act, as for the API, ADR-0023; the ministry points bulk checks to the flat file); terms and attribution (as for the API, ADR-0023); legal meaning (point 8; no proof defined, above); extra dependencies (SharpCompress, ADR-0024; `Microsoft.Extensions.Hosting.Abstractions` in the hosting package).
* Measurements: the archive of 20.11.2026; the hash chain measured with a stopwatch on .NET 10.0.15 and .NET Framework 4.8. The implementation measures it again, with the download, import and lookup times, on a real file.
* Implementation: [#320](https://github.com/milten89/OpenUrzednik.NET/issues/320) (package skeleton), [#321](https://github.com/milten89/OpenUrzednik.NET/issues/321) (download, integrity and the streaming method of the shared HTTP layer), [#322](https://github.com/milten89/OpenUrzednik.NET/issues/322) (the checks, the disk store), [#323](https://github.com/milten89/OpenUrzednik.NET/issues/323) (legal and usage documentation), [#360](https://github.com/milten89/OpenUrzednik.NET/issues/360) (DI package), [#361](https://github.com/milten89/OpenUrzednik.NET/issues/361) (the hosting package), all in milestone 1.2.0. Their bodies are updated to this record when it is accepted.
* [#362](https://github.com/milten89/OpenUrzednik.NET/issues/362) benchmarks the disk store's read method, a SQLite store and the restore from both built-in archive formats before #322; if its numbers contradict this record, the record is corrected before it is accepted.
* Merged after `release/1.1` is cut, so it ships with the flat-file code in 1.2.0 and can be corrected in place until `release/1.2` is cut (ADR-0022). The merging PR sets `status: accepted` here and in the index, so the record never sits on `develop` as `proposed`.
