using OpenUrzednik.Core;

namespace OpenUrzednik.Nbp.UrlBuilder;

public interface INbpUrlBuilder
{
    OpenUrzednikResult<string> Latest();
    OpenUrzednikResult<string> ForTopCount(int topCount);
    OpenUrzednikResult<string> Today();
    OpenUrzednikResult<string> ForDate(DateOnly date);
    OpenUrzednikResult<string> ForDateRange(DateOnly from, DateOnly to);
}
