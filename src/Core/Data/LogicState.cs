using System.Collections.Generic;

namespace MissionControl.Logic {
  public class LogicState {
    public Dictionary<string, bool> state = new Dictionary<string, bool>();

    public void Set(string key, bool flag) {
      state[key] = flag;
    }

    public bool GetBool(string key) {
      if (state.ContainsKey(key)) return state[key];
      return false;
    }
  }
}