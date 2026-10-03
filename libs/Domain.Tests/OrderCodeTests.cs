using System;
using Domain.Entity.Order;
using Exceptions;

namespace Domain.Tests;

public class OrderCodeTests
{
  [Fact]
  public void Generate_ReturnsCodeWithValidFormat()
  {
    var code = OrderCode.Generate();

    Assert.Equal(OrderCode.Length, code.Value.Length);
    Assert.True(OrderCode.TryParse(code.Value, out _));
  }

  [Fact]
  public void Generate_CalledManyTimes_ProducesDifferentCodes()
  {
    var codes = Enumerable.Range(0, 100)
        .Select(_ => OrderCode.Generate().Value)
        .ToList();

    Assert.Equal(codes.Count, codes.Distinct().Count());
  }

  [Theory]
  [InlineData("K7M4QX")]
  [InlineData("k7m4qx")]
  [InlineData("K7M-4QX")]
  [InlineData(" k7m - 4qx ")]
  public void TryParse_WithValidInput_ReturnsNormalizedCode(string input)
  {
    var result = OrderCode.TryParse(input, out var code);

    Assert.True(result);
    Assert.Equal("K7M4QX", code!.Value);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("K7M4Q")]    // too short
  [InlineData("K7M4QXA")]  // too long
  [InlineData("K7M4Q0")]   // zero
  [InlineData("K7M4QO")]   // letter O
  [InlineData("K7M4Q1")]   // one
  [InlineData("K7M4QI")]   // letter I
  [InlineData("K7M4QL")]   // letter L
  [InlineData("K7M4QU")]   // letter U
  [InlineData("K7M4Q!")]   // symbol
  public void TryParse_WithInvalidInput_ReturnsFalse(string? input)
  {
    var result = OrderCode.TryParse(input, out var code);

    Assert.False(result);
    Assert.Null(code);
  }

  [Fact]
  public void Parse_WithValidInput_ReturnsCode()
  {
    var code = OrderCode.Parse("k7m-4qx");

    Assert.Equal("K7M4QX", code.Value);
  }

  [Fact]
  public void Parse_WithInvalidInput_ThrowsDomainException()
  {
    Assert.Throws<DomainException>(() => OrderCode.Parse("INVALID"));
  }

  [Fact]
  public void ToString_ReturnsCodeWithHyphen()
  {
    var code = OrderCode.Parse("K7M4QX");

    Assert.Equal("K7M-4QX", code.ToString());
  }

  [Fact]
  public void Equals_WithSameValue_ReturnsTrue()
  {
    var first = OrderCode.Parse("K7M4QX");
    var second = OrderCode.Parse("k7m-4qx");

    Assert.Equal(first, second);
  }
}
