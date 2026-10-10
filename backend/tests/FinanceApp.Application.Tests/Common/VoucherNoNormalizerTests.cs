using FluentAssertions;
using FinanceApp.Application.Common;

namespace FinanceApp.Application.Tests.Common;

public class VoucherNoNormalizerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_Empty_ReturnsNull(string? value)
    {
        VoucherNoNormalizer.Normalize(value).Should().BeNull();
    }

    [Fact]
    public void Normalize_TrimsSurroundingWhitespace()
    {
        VoucherNoNormalizer.Normalize("  PZ-2026-001  ").Should().Be("PZ-2026-001");
    }

    [Fact]
    public void Normalize_RejectsControlCharacters()
    {
        var act = () => VoucherNoNormalizer.Normalize("PZ\n001");
        act.Should().Throw<ValidationException>().WithMessage("*控制字符*");
    }

    [Fact]
    public void Normalize_RejectsValuesLongerThan64()
    {
        var act = () => VoucherNoNormalizer.Normalize(new string('A', 65));
        act.Should().Throw<ValidationException>().WithMessage("*64*");
    }

    [Fact]
    public void Normalize_AcceptsMaxLength()
    {
        var value = new string('B', 64);
        VoucherNoNormalizer.Normalize(value).Should().Be(value);
    }
}
