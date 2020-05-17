using System;
using System.Linq;

public static class ArrayExtensions {
  public static T[] Shuffle<T>(this T[] array) {
    Random rnd = new Random();
    return array.OrderBy(x => rnd.Next()).ToArray();
  }
}