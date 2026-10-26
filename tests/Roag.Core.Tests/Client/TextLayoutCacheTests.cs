using Roag.Graphics;
using Xunit;

namespace Roag.Core.Tests.Client;

public sealed class TextLayoutCacheTests
{
    public static IEnumerable<object[]> Examples =>
    [
        new object[] { "", 8, new[] { "" } },
        new object[] { "one two", 7, new[] { "one two" } },
        new object[] { " one  two \n\nthree", 4, new[] { "one", "two", "", "thre", "e", "" } },
        new object[] { "abcdef g", 3, new[] { "abc", "def", "g" } },
        new object[] { "abcdef", 3, new[] { "abc", "def", "" } },
        new object[] { "a\n", 2, new[] { "a", "" } },
        new object[] { "abcdef", 6, new[] { "abcdef" } },
        new object[] { "x y", 1, new[] { "x", "y" } }
    ];

    [Theory]
    [MemberData(nameof(Examples))]
    public void WrappingPreservesWordsLongTokensAndBlankParagraphSpacing(string text, int columns, string[] expected)
        => Assert.Equal(expected, new TextLayoutCache().Get(text, columns));

    [Fact]
    public void EqualTextReusesImmutableLayoutAndWidthChangesReflow()
    {
        var cache = new TextLayoutCache(); var first = cache.Get("one two three", 7);
        Assert.Same(first, cache.Get(new string("one two three".ToCharArray()), 7));
        Assert.Equal(new[] { "one two", "three" }, first);
        Assert.Equal(new[] { "one", "two", "thr", "ee", "" }, cache.Get("one two three", 3));
        Assert.Same(first, cache.Get("one two three", 7));
        var list = Assert.IsAssignableFrom<IList<string>>(first);
        Assert.Throws<NotSupportedException>(() => list[0] = "corrupted");
    }

    [Fact]
    public void LeastRecentlyUsedLayoutsAreEvictedWithoutGrowingTheCache()
    {
        var cache = new TextLayoutCache(); var zero = cache.Get("entry 0", 12); var one = cache.Get("entry 1", 12);
        for (int i = 2; i < TextLayoutCache.Capacity; i++) cache.Get("entry " + i, 12);
        Assert.Same(zero, cache.Get("entry 0", 12)); cache.Get("entry 128", 12);
        Assert.Same(zero, cache.Get("entry 0", 12)); Assert.NotSame(one, cache.Get("entry 1", 12));
        Assert.Equal(TextLayoutCache.Capacity, cache.Count);
    }

    [Fact]
    public void CharacterBudgetBoundsLargeChangingDescriptionsAndOversizedTextIsNotRetained()
    {
        var cache = new TextLayoutCache();
        for (int i = 0; i < 20; i++)
        {
            cache.Get(new string('x', 4000) + i, 80);
            Assert.InRange(cache.Characters, 0, TextLayoutCache.CharacterBudget);
        }
        Assert.InRange(cache.Count, 1, 10);
        int count = cache.Count, characters = cache.Characters;
        var oversized = new string('y', TextLayoutCache.MaxTextLength + 1);
        Assert.NotEmpty(cache.Get(oversized, 80)); Assert.Equal(count, cache.Count); Assert.Equal(characters, cache.Characters);
    }

    [Fact]
    public void WarmedLookupDoesNotAllocateWhileDynamicTextGetsANewLayout()
    {
        var cache = new TextLayoutCache(); var expected = cache.Get("Food 8 / Gold 25", 10);
        IReadOnlyList<string>? actual = null;
        long bytes = WarmAllocationProbe.Measure(() => actual = cache.Get("Food 8 / Gold 25", 10));
        Assert.Same(expected, actual); Assert.InRange(bytes, 0, 1024);
        Assert.NotSame(expected, cache.Get("Food 9 / Gold 25", 10));
    }
}
