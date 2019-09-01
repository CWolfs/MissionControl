using System.Collections.Generic;

namespace MissionControl.Logic {
  public class LogicState {
    public Dictionary<string, bool> boolState = new Dictionary<string, bool>();
    public Dictionary<string, object> objectState = new Dictionary<string, object>();

    public void Set(string key, bool flag) {
      boolState[key] = flag;
    }

    public void Set(string key, List<string> keys) {
      objectState.Add(key, keys);
    }

    public void Set(string key, List<string[]> keys) {
      objectState.Add(key, keys);
    }

    public bool GetBool(string key) {
      if (boolState.ContainsKey(key)) return boolState[key];
      return false;
    }

    public object GetObject(string key) {
      if (objectState.ContainsKey(key)) return objectState[key];
      return null;
    }
  }
}