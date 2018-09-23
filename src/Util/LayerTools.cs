using System;
using UnityEngine;

namespace ContractCommand.Utils {
  public class LayerTools {
    public static void SetLayerRecursively(GameObject go, int layerNumber) {
      foreach (Transform trans in go.GetComponentsInChildren<Transform>(true)) {
        trans.gameObject.layer = layerNumber;
      }
    }

    public static int OnlyIncluding( params int[] layers ){
      return MakeMask( layers );
    }
    
    public static int EverythingBut( params int[] layers ){
      return ~MakeMask( layers );
    }
    
    public static bool ContainsLayer( LayerMask layerMask, int layer ){
      return ( layerMask.value & 1 << layer ) != 0 ;	
    }
    
    static int MakeMask( params int[] layers ){
      int mask = 0;
      foreach ( int item in layers ) {
        mask |= 1 << item;
      }
      return mask;	
    }
  }
}