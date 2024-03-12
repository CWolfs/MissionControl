using BattleTech;

/**
This result will shake the camera
*/
namespace MissionControl.Result {
  public class CustomCameraShakeResult : EncounterResult {
    public float ShakeStrength { get; set; }
    public float ShakeDuration { get; set; }
    public string AudioEventDefID { get; set; }
    public string AudioEventEntry { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug($"[CustomCameraShakeResult] Triggering for ShakeStrength '{ShakeStrength}' ShakeDuration '{ShakeDuration}' AudioEventDefID '{AudioEventDefID}' AudioEventEntry '{AudioEventEntry}'");
      AudioEventManager.PlayAudioEvent(AudioEventDefID, AudioEventEntry);
      CameraControl.Instance.AddCameraShake(ShakeStrength, ShakeDuration, CameraControl.Instance.transform.position);
    }
  }
}