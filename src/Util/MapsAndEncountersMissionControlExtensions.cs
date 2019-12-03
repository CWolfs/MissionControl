using System.Linq;
using System.Collections.Generic;

using BattleTech.Data;

public static class MetaDatabaseExtensions {
  public static List<Mood_MDD> GetMoods(this MetadataDatabase mdd) {
    return mdd.Query<Mood_MDD>("SELECT * FROM Mood").ToList<Mood_MDD>();
  }

  public static List<ContractType_MDD> GetCustomContractTypes(this MetadataDatabase mdd) {
    return mdd.Query<ContractType_MDD>("SELECT * FROM Contract WHERE ContractTypeID >= 1000").ToList<ContractType_MDD>();
  }
}