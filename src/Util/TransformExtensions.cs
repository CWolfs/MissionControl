using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using MissionControl;

public static class TransformExtensions {
  public static List<T> FindObjectsWithinProximity<T>(this Transform transform, float proximity) where T : MonoBehaviour{  
    List<T> objects = new List<T> ();

    T[] foundObjects = GameObject.FindObjectsOfType<T> ();
    for(int x = 0; x<foundObjects.Length; x++){
      T obj = foundObjects [x];
      if ((obj.transform.position - transform.position).magnitude <= proximity) {
        objects.Add (obj);
      }
    }

    return objects;
  }
}