using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using MissionControl;

public static class DirectoryUtils {
  public static IEnumerable<string> GetAllDirectories(string path) {
    try {
      return Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories);
    } catch (UnauthorizedAccessException e) {
      // In case we don't have access to the folder.
      Main.Logger.LogError(e.Message);
      return Enumerable.Empty<string>();
    }
  }
}
