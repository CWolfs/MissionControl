using UnityEngine;

using System;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using MissionControl.EncounterFactories;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class SetChunkStateAtRandomActivatorBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject activator;

    private GameObject parent;
    private List<string> chunkGuids;
    private string statusString;
    private EncounterObjectStatus statusType;

    public SetChunkStateAtRandomActivatorBuilder(ContractTypeBuilder contractTypeBuilder, GameObject parent, JObject activator) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.activator = activator;

      this.parent = parent;
      this.chunkGuids = ((JArray)activator["ChunkGuids"]).ToObject<List<string>>();
      this.statusString = activator.ContainsKey("Status") ? activator["Status"].ToString() : "Active";
      this.statusType = (EncounterObjectStatus)Enum.Parse(typeof(EncounterObjectStatus), statusString);
    }

    public override void Build() {
      ChunkFactory.CreateSetChunkStateAtRandomActivator(this.parent, this.chunkGuids, this.statusType);
    }
  }
}