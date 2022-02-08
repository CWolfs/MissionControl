using System.Collections.Generic;

namespace MissionControl.Logic {
  public class LogicState {
    public Dictionary<string, bool> boolState = new Dictionary<string, bool>();
    public Dictionary<string, object> objectState = new Dictionary<string, object>();
    public Dictionary<string, string> stringState = new Dictionary<string, string>();
    public Dictionary<string, int> intState = new Dictionary<string, int>();

    public void Set(string key, bool flag) {
      boolState[key] = flag;
    }

    public void Set(string key, string val) {
      stringState[key] = val;
    }

    public void Set(string key, int val) {
      intState[key] = val;
    }

    public void Set(string key, List<string> keys) {
      objectState[key] = keys;
    }

    public void Set(string key, List<string[]> keys) {
      objectState[key] = keys;
    }

    public bool GetBool(string key) {
      if (boolState.ContainsKey(key)) return boolState[key];
      return false;
    }

    public string GetString(string key) {
      if (stringState.ContainsKey(key)) return stringState[key];
      return null;
    }

    public int GetInt(string key) {
      if (intState.ContainsKey(key)) return intState[key];
      return -1;
    }

    public object GetObject(string key) {
      if (objectState.ContainsKey(key)) return objectState[key];
      return null;
    }
  }
}