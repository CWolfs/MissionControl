using UnityEngine;

using Newtonsoft.Json.Linq;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using System;
using System.Collections.Generic;

using MissionControl.Result;

namespace MissionControl.ContractTypeBuilders {
  public class PropsBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JArray propsArray;

    public PropsBuilder(ContractTypeBuilder contractTypeBuilder, JArray resultsArray) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.propsArray = resultsArray;
    }

    public void Build() {
      // Build Buildings

      // Build Structure
      foreach (JObject prop in propsArray.Children<JObject>()) {
        BuildProp(prop);
      }
    }

    private void BuildProp(JObject result) {
      string type = result["Type"].ToString();

    }
  }
}