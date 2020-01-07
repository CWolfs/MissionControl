using UnityEngine;

namespace MissionControl.Ui {
  public class UiContainerFactory {
    public static GameObject CreatePanel(GameObject parent, string name = null) {
      return new GameObject(name);
    }
  }
}