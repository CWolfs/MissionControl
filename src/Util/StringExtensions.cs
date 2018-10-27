using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using MissionControl;

public static class StringExtensions {
  public static string Capitalise(this string originalString) {
    if (string.IsNullOrEmpty(originalString)) return originalString;
    return Char.ToUpper(originalString[0]) + originalString.Substring(1);
  }
}