using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using SpawnVariation;

public static class ComponentExtensions {
  public static Component CopyComponent(this GameObject destination, Component original) {  
      System.Type type = original.GetType();
      Component copy = destination.AddComponent(type);
      System.Reflection.FieldInfo[] fields = type.GetFields(
        System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Instance |
        System.Reflection.BindingFlags.Public
      ); 

      for (int x = 0; x<fields.Length; x++) {
        System.Reflection.FieldInfo field = fields [x];

        if (field.IsDefined (typeof(SerializeField), false)) {
          field.SetValue (copy, field.GetValue (original));
        }
      }

      return copy;
  }
}