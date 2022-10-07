using System;
using System.Linq;

public static class StringExtensions {
  public static string Capitalise(this string originalString) {
    if (string.IsNullOrEmpty(originalString)) return originalString;
    return Char.ToUpper(originalString[0]) + originalString.Substring(1);
  }

  public static bool In(this string originalString, params string[] comparisonStrings) {
    return comparisonStrings.Contains(originalString);
  }

  public static string ToUpperFirst(this string originalString) {
    if (originalString == "" || originalString == null) return originalString;
    if (originalString.Length < 2) return originalString[0].ToString().ToUpper();

    originalString = originalString.ToLower();
    return originalString[0].ToString().ToUpper() + originalString.Substring(1);
  }
}