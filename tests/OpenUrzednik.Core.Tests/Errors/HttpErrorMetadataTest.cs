using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Core.Tests.Errors;

// Shared data; tests are in one file per method. The accessors on each error type are covered in StatusCodeMetadataTest;
// these cover errors that never carry a status.
public partial class HttpErrorMetadataTest
{
    public static TheoryData<OpenUrzednikError> ErrorsThatNeverCarryAStatus => new()
    {
        new ValidationError("message", "rule", "name", "value"),
        new SerializationError("message"),
        new ConnectionFailedError("message"),
    };
}
