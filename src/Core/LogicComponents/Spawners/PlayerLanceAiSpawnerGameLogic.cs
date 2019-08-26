using System;

using System.Collections.Generic;

using BattleTech;
using BattleTech.Serialization;

using fastJSON;

using HBS.Util;

namespace MissionControl.LogicComponents.Spawners {
	public class PlayerLanceAiSpawnerGameLogic : LanceSpawnerGameLogic {
		public override void ContractInitialize() {
			base.ContractInitialize();
			UnitSpawnPointGameLogic[] unitSpawnPointGameLogicList = base.unitSpawnPointGameLogicList;
			SpawnableUnit[] lanceUnits = base.Combat.ActiveContract.Lances.GetLanceUnits(this.teamDefinitionGuid);

			int num = 0;
			while (num < lanceUnits.Length && num < unitSpawnPointGameLogicList.Length) {
				unitSpawnPointGameLogicList[num].OverrideSpawn(lanceUnits[num]);
				num++;
			}
		}

		protected override void OnTriggerSpawn(MessageCenterMessage message) {
			base.OnTriggerSpawn(message);
		}

		protected override void SpawnUnits(bool offScreen) {
			base.SpawnUnits(offScreen);
			// EncounterLayerParent.EnqueueLoadAwareMessage(new PlayerLanceSpawned(this.encounterObjectGuid));
			// EncounterLayerParent.EnqueueLoadAwareMessage(new RefreshPortraitsMessage());
		}

		public override void OnStarting() {
			base.OnStarting();
		}

		public override void OnEnterActive() {
			base.OnEnterActive();
		}

		public override List<EncounterObjectValidationEntry> Validate(Dictionary<string, EncounterObjectGameLogic> encounterObjectDict) {
			List<EncounterObjectValidationEntry> list = base.Validate(encounterObjectDict);
			/*
			if (!PlayerLanceSpawnerGameLogic.HACK_IsPlayerLanceGuid(this.encounterObjectGuid)) {
				string message = string.Format("Player lance spawner guids must be one of a particular list. They should be: Player1[{0}] Player2[{1}]", PlayerLanceSpawnerGameLogic.Player1LanceSpawnerGuid, PlayerLanceSpawnerGameLogic.Player2LanceSpawnerGuid);
				EncounterObjectValidationEntry item = EncounterObjectValidationEntry.GenericGameLogicError(this, base.DisplayName, message);
				list.Add(item);
			}

			if (this.aiOrderList.Count > 0) {
				list.Add(new EncounterObjectValidationEntry {
					encounterObject = this,
					validationError = string.Format("PlayerLanceChunk[{0}] should not have any AI Orders set.", base.name)
				});
			}
			*/

			if (base.unitSpawnPointGameLogicList == null || base.unitSpawnPointGameLogicList.Length < 4) {
				list.Add(new EncounterObjectValidationEntry {
					encounterObject = this,
					validationError = "Player lances need at least 4 spawn points"
				});
			}

			int num = 0;
			UnitSpawnPointGameLogic[] unitSpawnPointGameLogicList = base.unitSpawnPointGameLogicList;
			for (int i = 0; i < unitSpawnPointGameLogicList.Length; i++) {
				UnitSpawnPointGameLogic unitSpawnPointGameLogic = unitSpawnPointGameLogicList[i];

				if (unitSpawnPointGameLogic.HasUnitToSpawn) {
					if (num != i) {
						EncounterObjectValidationEntry item = EncounterObjectValidationEntry.GenericGameLogicError(unitSpawnPointGameLogic, base.DisplayName, "Forced player units must be sequential and start at PlayerLanceSpawner1.");
						list.Add(item);
					} else {
						num++;
					}

					if (unitSpawnPointGameLogic.HasDefaultPilot) {
						EncounterObjectValidationEntry item = EncounterObjectValidationEntry.GenericGameLogicError(unitSpawnPointGameLogic, base.DisplayName, "Forced player units must have a non default pilot.");
						list.Add(item);
					}
				}
			}
			return list;
		}

		/*
		public static bool HACK_IsPlayerLanceGuid(string guid) {
			return PlayerLanceSpawnerGameLogic.HACK_PlayerLanceGuidList.Contains(guid);
		}
		*/

		public override int Size() {
			return base.Size();
		}

		public override bool ShouldSave() {
			return true;
		}

		public override void Save(SerializationStream stream)	{
			base.Save(stream);
		}

		public override void Load(SerializationStream stream)	{
			base.Load(stream);
		}

		public override string ToJSON()	{
			return JSONSerializationUtility.ToJSON<PlayerLanceAiSpawnerGameLogic>(this);
		}

		public override void FromJSON(string json) {
			JSONSerializationUtility.FromJSON<PlayerLanceAiSpawnerGameLogic>(this, json);
		}

		public override string GenerateJSONTemplate() {
			return JSONSerializationUtility.ToJSON<PlayerLanceAiSpawnerGameLogic>(new PlayerLanceAiSpawnerGameLogic());
		}

		[JsonIgnore]
		public static readonly string Player1LanceSpawnerGuid = "76b654a6-4f2c-4a6f-86e6-d4cf868335fe";

		[JsonIgnore]
		public static readonly string Player2LanceSpawnerGuid = "5b08b8d4-ee50-4ae1-bf57-71bc4c0ee965";

		private static List<string> HACK_PlayerLanceGuidList = new List<string>(new string[]
		{
			PlayerLanceSpawnerGameLogic.Player1LanceSpawnerGuid,
			PlayerLanceSpawnerGameLogic.Player2LanceSpawnerGuid,
			"49c80ae9-df2d-4788-a56e-3b21cf381691",
			"1a670982-15d5-44cf-b76b-32433c27749e",
			"dafd0d98-c88a-4f90-b137-4fe5ed94ebbb",
			"eb440f61-efce-4d2b-91b5-97a0800e74c6",
			"577981ac-40a8-4a8e-9c27-16e5a96d0c7a",
			"723a18fd-385b-480c-8099-d0989293c9c7"
		});
	}
}
