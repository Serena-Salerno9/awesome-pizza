using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Exceptions;

namespace Domain.Entity.Order;

public sealed record OrderCode
{
  public const int Length = 6;

  // Esclusi caratteri ambigui: 0/O, 1/I/L, U
  private const string Alphabet = "23456789ABCDEFGHJKMNPQRSTVWXYZ";

  public string Value { get; }

  private OrderCode(string value) => Value = value;

  public static OrderCode Generate() =>
      new(RandomNumberGenerator.GetString(Alphabet, Length));

  public static bool TryParse(string? input, [NotNullWhen(true)] out OrderCode? code)
  {
    code = null;
    if (string.IsNullOrWhiteSpace(input))
      return false;

    var normalized = new string([.. input
        .Where(c => c != '-' && !char.IsWhiteSpace(c))
        .Select(char.ToUpperInvariant)]);

    if (normalized.Length != Length || normalized.Any(c => !Alphabet.Contains(c)))
      return false;

    code = new OrderCode(normalized);
    return true;
  }

  public static OrderCode Parse(string input) =>
      TryParse(input, out var code)
          ? code
          : throw new DomainException($"'{input}' is not a valid order code.");

  public override string ToString() => $"{Value[..3]}-{Value[3..]}";
}
