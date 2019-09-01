using System.Linq;

using System.Collections.Generic;

using BattleTech;

using MissionControl.Rules;

using HBS.Util;

namespace MissionControl.LogicComponents.Spawners {
	public class CustomPlayerLanceSpawnerGameLogic : LanceSpawnerGameLogic {
		public override void ContractInitialize() {
			base.ContractInitialize();
			UnitSpawnPointGameLogic[] unitSpawnPointGameLogicList = base.unitSpawnPointGameLogicList;
			SpawnableUnit[] lanceUnits = base.Combat.ActiveContract.Lances.GetLanceUnits(this.teamDefinitionGuid);

			if (this.teamDefinitionGuid == EncounterRules.PLAYER_TEAM_ID) {
 				lanceUnits = lanceUnits.Skip(4).ToArray();
			}

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

			if (this.teamDefinitionGuid == EncounterRules.PLAYER_TEAM_ID) {
				EncounterLayerParent.EnqueueLoadAwareMessage(new RefreshPortraitsMessage());
			}
		}

		public override void OnStarting() {
			base.OnStarting();
		}

		public override void OnEnterActive() {
			base.OnEnterActive();
		}

		public override List<EncounterObjectValidationEntry> Validate(Dictionary<string, EncounterObjectGameLogic> encounterObjectDict) {
			List<EncounterObjectValidationEntry> list = base.Validate(encounterObjectDict);

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
			return JSONSerializationUtility.ToJSON<CustomPlayerLanceSpawnerGameLogic>(this);
		}

		public override void FromJSON(string json) {
			JSONSerializationUtility.FromJSON<CustomPlayerLanceSpawnerGameLogic>(this, json);
		}

		public override string GenerateJSONTemplate() {
			return JSONSerializationUtility.ToJSON<CustomPlayerLanceSpawnerGameLogic>(new CustomPlayerLanceSpawnerGameLogic());
		}
	}
}
