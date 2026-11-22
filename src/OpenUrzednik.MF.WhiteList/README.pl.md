# OpenUrzednik.MF.WhiteList

🇬🇧 Pełna dokumentacja jest po angielsku: [README.md](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.MF.WhiteList/README.md). Ta wersja zawiera tylko część informacyjną. Jeśli wersje się różnią, obowiązuje angielska.

> ⚠️ **Projekt nieoficjalny:** OpenUrzednik.NET to projekt społecznościowy. Nie jest powiązany z Ministerstwem Finansów, Krajową Administracją Skarbową (KAS) ani z żadną inną polską instytucją publiczną i nie działa z ich upoważnienia.

Klient **Białej Listy VAT** (Wykaz podatników VAT Ministerstwa Finansów) dla OpenUrzednik.NET: wyszukiwanie po NIP, REGON lub numerze rachunku bankowego, sprawdzenie, czy rachunek należy do podmiotu o danym NIP lub REGON w wybranym dniu, oraz status VAT z usługi WL2. Dane pochodzą z [API ministerstwa](https://www.gov.pl/web/kas/api-wykazu-podatnikow-vat); biblioteka niczego nie udostępnia dalej.

> Część zestawu [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET). Przegląd projektu po polsku znajdziesz w [głównym README](https://github.com/milten89/OpenUrzednik.NET/blob/main/README.pl.md).

## Instalacja

```bash
dotnet add package OpenUrzednik.MF.WhiteList
```

## Użycie

Pakiet ma dwa klienty: `WhiteListClient` (wyszukiwanie i sprawdzanie) oraz `WhiteListStatusClient` (status VAT z WL2).

```csharp
using OpenUrzednik.MF.WhiteList;

using var httpClient = new HttpClient();
var client = new WhiteListClient(httpClient);
var date = new DateTime(2026, 11, 20);

// Search by NIP, with the proof of the check.
var search = await client.SearchByNipAsync("736-812-54-16", date);
if (search.IsSuccess)
{
    var subject = search.Value.Subject;
    Console.WriteLine($"{subject.Name}: {subject.VatStatus}");
    Console.WriteLine($"Proof: {search.Value.RequestId}, {search.Value.RequestDateTime:O}");
}
else if (search.Errors[0] is WhiteListNotFoundError notOnList)
    Console.WriteLine($"Not on the list. Proof: {notOnList.RequestId}, {notOnList.RequestDateTime:O}");
else
    Console.WriteLine(search.Errors[0].Message);

// Check whether an account belongs to a NIP on that date.
var check = await client.CheckNipBankAccountAsync("7368125416", "61 1090 1014 0000 0712 1981 2874", date);
if (check.IsSuccess)
    Console.WriteLine(check.Value.AccountAssigned ? $"On the list, proof {check.Value.RequestId}" : "Not assigned");

// The VAT status from WL2; without a date the API answers for its today.
var statusClient = new WhiteListStatusClient(httpClient);
var status = await statusClient.GetVatStatusByNipAsync("7368125416");
if (status.IsSuccess)
    Console.WriteLine($"{status.Value.VatStatus} on {status.Value.Date:yyyy-MM-dd}");
```

Z kontenerem DI użyj pakietu [OpenUrzednik.MF.WhiteList.DependencyInjection](https://www.nuget.org/packages/OpenUrzednik.MF.WhiteList.DependencyInjection): `builder.Services.AddOpenUrzednikMFWhiteList()` rejestruje oba klienty.

Błędy, takie jak wyczerpany dzienny limit, podmiot spoza listy, upływ limitu czasu albo błędny NIP, wracają w wyniku, a nie jako wyjątki. Jeśli wolisz wyjątki, wywołaj na wyniku `EnsureSuccess()`.

## Więcej po angielsku

- [Limity API i limit dzienny](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.MF.WhiteList/README.md#api-limits-and-quota)
- [Zakres dat](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.MF.WhiteList/README.md#dates)
- [Dowód sprawdzenia (`requestId`) i jego znaczenie prawne](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.MF.WhiteList/README.md#the-proof-of-a-check-requestid)
- [Nowy adres API](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.MF.WhiteList/README.md#the-host-changed)
- [Błędy](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.MF.WhiteList/README.md#errors)
- [Dane osobowe](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.MF.WhiteList/README.md#personal-data)
- [Ponawianie zapytań](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.MF.WhiteList/README.md#resilience)
- [Logowanie i śledzenie](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.MF.WhiteList/README.md#logging-and-tracing)
- [Telemetria: spany, tagi, metryki i identyfikatory zdarzeń logów](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.MF.WhiteList/README.md#telemetry-reference)
- [Zgodność między wersjami](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.MF.WhiteList/README.md#compatibility)
