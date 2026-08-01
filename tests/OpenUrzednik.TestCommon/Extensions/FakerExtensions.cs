using System.Runtime.CompilerServices;

using Bogus;

namespace OpenUrzednik.TestCommon.Extensions;

public static class FakerExtensions
{
    public static Faker WithConstantSeed(this Faker faker, [CallerFilePath] string sourceFilePath = "",
        [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        faker.Random = new Randomizer(StableHash($"{Path.GetFileName(sourceFilePath)}:{memberName}:{sourceLineNumber}"));
        return faker;
    }
    
    public static Faker<T> WithConstantSeed<T>(this Faker<T> faker, [CallerFilePath] string sourceFilePath = "",
        [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) 
        where T : class
    {
        return faker.UseSeed(StableHash($"{Path.GetFileName(sourceFilePath)}:{memberName}:{sourceLineNumber}"));
    }

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
}