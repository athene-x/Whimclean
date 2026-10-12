using Athenex.Whimclean.Domain.Text;

namespace Athenex.Whimclean.Domain.Tests.Text;

public sealed class TextNormalizerTests
{
    [Theory]
    [InlineData("  Acme   Corp \t Ltd ", "Acme Corp Ltd")]
    [InlineData("single", "single")]
    [InlineData("   ", "")]
    public void NormalizeWhitespace_TrimsAndCollapses(string input, string expected)
    {
        Assert.Equal(expected, TextNormalizer.NormalizeWhitespace(input));
    }

    [Theory]
    [InlineData("  Acme   Corp ", "acme-corp")]
    [InlineData("my__slug--here", "my-slug-here")]
    [InlineData("-leading and trailing-", "leading-and-trailing")]
    [InlineData("Ünïcode Name", "ünïcode-name")]
    public void NormalizeSlug_LowercasesAndHyphenates(string input, string expected)
    {
        Assert.Equal(expected, TextNormalizer.NormalizeSlug(input));
    }

    [Fact]
    public void Normalize_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => TextNormalizer.NormalizeWhitespace(null!));
        Assert.Throws<ArgumentNullException>(() => TextNormalizer.NormalizeSlug(null!));
    }
}
