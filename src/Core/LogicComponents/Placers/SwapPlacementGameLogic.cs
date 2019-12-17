using UnityEngine;

using BattleTech;
using HBS.Util;

using MissionControl.Data;
using MissionControl.Messages;

namespace MissionControl.LogicComponents.Placers {
  public class SwapPlacementGameLogic : EncounterObjectGameLogic {

    [SerializeField]
    public string swapTarget1Guid { get; set; } = "UNSET";

    [SerializeField]
    public string swapTarget2Guid { get; set; } = "UNSET";

    public override TaggedObjectType Type {
      get {
        return (TaggedObjectType)MCTaggedObjectType.SwapPlacement;
      }
    }

    public void Awake() {
      Main.LogDebug($"[SwapPlacementGameLogic.Awake] Awake");
      MessageCenter messageCenter = UnityGameInstance.BattleTechGame.MessageCenter;
      messageCenter.AddSubscriber((MessageCenterMessageType)MessageTypes.OnCustomContractTypeBuilt, new ReceiveMessageCenterMessage(this.OnCustomContractTypeBuilt));
    }

    public void OnDestroy() {
      Main.LogDebug($"[SwapPlacementGameLogic.OnDestroy] OnDestroy");
      MessageCenter messageCenter = UnityGameInstance.BattleTechGame.MessageCenter;
      messageCenter.RemoveSubscriber((MessageCenterMessageType)MessageTypes.OnCustomContractTypeBuilt, new ReceiveMessageCenterMessage(this.OnCustomContractTypeBuilt));
    }

    public override void AlwaysInit(CombatGameState combat) {
      base.AlwaysInit(combat);
      //  this.messageMemory.TrackMethod((MessageCenterMessageType)MessageTypes.OnCustomContractTypeBuilt, new ReceiveMessageCenterMessage(this.OnCustomContractTypeBuilt));
    }

    protected override void SubscribeToMessages(bool shouldAdd) {
      // this.messageMemory.Subscribe((MessageCenterMessageType)MessageTypes.OnCustomContractTypeBuilt, new ReceiveMessageCenterMessage(this.OnCustomContractTypeBuilt), shouldAdd);
      base.SubscribeToMessages(shouldAdd);
    }

    private void OnCustomContractTypeBuilt(MessageCenterMessage message) {
      Main.LogDebug($"[SwapPlacementGameLogic.OnCustomContractTypeBuilt] {message.ToString()}");
      SwapPlacement();
    }

    private void SwapPlacement() {
      EncounterObjectGameLogic targetGameLogic1 = MissionControl.Instance.EncounterLayerData.gameObject.GetEncounterObjectGameLogic(swapTarget1Guid);
      EncounterObjectGameLogic targetGameLogic2 = MissionControl.Instance.EncounterLayerData.gameObject.GetEncounterObjectGameLogic(swapTarget2Guid);

      GameObject target1Go = targetGameLogic1.gameObject;
      GameObject target2Go = targetGameLogic2.gameObject;

      Main.LogDebug($"[SwapPlacementGameLogic.SwapPlacement]) Swapping position and rotation between '{target1Go.name}' and '{target2Go.name}'");

      Vector3 target1Position = target1Go.transform.position;
      Vector3 target2Position = target2Go.transform.position;
      Quaternion target1Rotation = target1Go.transform.rotation;
      Quaternion target2Rotation = target2Go.transform.rotation;

      target1Go.transform.position = target2Position;
      target2Go.transform.position = target1Position;
      target1Go.transform.rotation = target2Rotation;
      target2Go.transform.rotation = target1Rotation;
    }

    public override void FromJSON(string json) {
      JSONSerializationUtility.FromJSON<SwapPlacementGameLogic>(this, json);
    }

    public override string GenerateJSONTemplate() {
      return JSONSerializationUtility.ToJSON<SwapPlacementGameLogic>(new SwapPlacementGameLogic());
    }

    public override string ToJSON() {
      return JSONSerializationUtility.ToJSON<SwapPlacementGameLogic>(this);
    }
  }
}
