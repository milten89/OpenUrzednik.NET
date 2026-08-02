using System.Runtime.CompilerServices;

using Bogus;
using Bogus.DataSets;

namespace OpenUrzednik.TestCommon.Extensions;

public static class FakerExtensions
{
    public static readonly DateTime BetweenStart = new DateTime(2000, 01, 01, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime BetweenEnd = new DateTime(2030, 12, 31, 23, 59, 59, DateTimeKind.Utc);
    
    public static Faker WithConstantSeed(this Faker faker, DateTime? refDate = null,
        [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        faker.Random = new Randomizer(GetSeed(sourceFilePath, memberName, sourceLineNumber));
        faker.DateTimeReference = refDate ?? faker.Date.Between(BetweenStart, BetweenEnd);
        return faker;
    }
    
    public static Faker<T> WithConstantSeed<T>(this Faker<T> faker, DateTime? refDate = null,
        [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) 
        where T : class
    {
        return faker.UseSeed(GetSeed(sourceFilePath, memberName, sourceLineNumber))
                    .UseDateTimeReference(refDate ?? ((IFakerTInternal)faker).FakerHub.Date.Between(BetweenStart, BetweenEnd));
    }

    private static int GetSeed(string sourceFilePath, string memberName, int sourceLineNumber)
        => StableHash($"{Path.GetFileName(sourceFilePath)}:{memberName}:{sourceLineNumber}");
    
    private static int StableHash(string s)
    {
        unchecked
        {
            var hash = 17;
            foreach (var c in s)
                hash = hash * 31 + c;
            return hash;
        }
    }
    
    public static Faker<T> LinkRandomizerTo<T>(this Faker<T> faker, Faker source) where T : class
    {
        var hub = ((IFakerTInternal)faker).FakerHub;
        if (!ReferenceEquals(hub.Random, source.Random))
            hub.Random = source.Random;
        if (hub.DateTimeReference != source.DateTimeReference)
            faker.UseDateTimeReference(source.DateTimeReference);
        return faker;
    }
    
    public static Faker<T> LinkRandomizerTo<T, TSource>(this Faker<T> faker, Faker<TSource> source)
        where T : class
        where TSource : class
    {
        return faker.LinkRandomizerTo(((IFakerTInternal)source).FakerHub);
    }
}