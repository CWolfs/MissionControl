using UnityEngine;

using System.Collections.Generic;

using BattleTech;

namespace MissionControl.Result {
  public class ScalableArtilleryObjectiveSequence : ArtilleryObjectiveSequence {
    private float vfxScale;

    public ScalableArtilleryObjectiveSequence(CombatGameState combat, List<Vector3> targetPositions, ArtilleryVFXType artilleryVFXType, List<ICombatant> targets, float damageEachLocation, int heatDamage, int stabilityDamage, TerrainMaskFlags applyDesignMaskOnExplosion, float vfxScale = 1.0f)
      : base(combat, targetPositions, artilleryVFXType, targets, damageEachLocation, heatDamage, stabilityDamage, applyDesignMaskOnExplosion) {
      this.vfxScale = vfxScale;
    }

    // PlayScaledFX is called by the Harmony patch to add scaling support
    // Note: This requires the assembly to be publicized
    public void PlayScaledFX(Vector3 targetPosition) {
      Main.LogDebug($"[ScalableArtilleryObjectiveSequence] Playing artillery VFX at position {targetPosition} with scale {vfxScale}");

      // Get the artilleryVFXType from the base class (need to use reflection or make it accessible)
      ArtilleryVFXType artilleryVFXType = (ArtilleryVFXType)GetType().BaseType.GetField("artilleryVFXType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(this);

      ObjectSpawnData osd;

      switch (artilleryVFXType) {
        case ArtilleryVFXType.ArtilleryShellSingle:
          osd = new ObjectSpawnData(Combat.Constants.VFXNames.artillery_projectile_single, targetPosition, playFX: true, autoPoolObject: true);
          osd.Spawn(Combat);
          ScaleVFX(osd.spawnedObject);

          osd = new ObjectSpawnData(Combat.Constants.VFXNames.artillery_impact_barrage, targetPosition, playFX: true, autoPoolObject: true);
          osd.Spawn(Combat);
          ScaleVFX(osd.spawnedObject);
          break;

        case ArtilleryVFXType.ArtilleryShellBarrage:
          osd = new ObjectSpawnData(Combat.Constants.VFXNames.artillery_projectile_barrage, targetPosition, playFX: true, autoPoolObject: true);
          osd.Spawn(Combat);
          ScaleVFX(osd.spawnedObject);

          osd = new ObjectSpawnData(Combat.Constants.VFXNames.artillery_impact_barrage, targetPosition, playFX: true, autoPoolObject: true);
          osd.Spawn(Combat);
          ScaleVFX(osd.spawnedObject);
          break;

        case ArtilleryVFXType.Explosion:
          osd = new ObjectSpawnData(Combat.Constants.VFXNames.artillery_explosion, targetPosition, playFX: true, autoPoolObject: true);
          osd.Spawn(Combat);
          ScaleVFX(osd.spawnedObject);
          break;

        case ArtilleryVFXType.OrbitalPPC:
          osd = new ObjectSpawnData(Combat.Constants.VFXNames.artillery_orbital_ppc, targetPosition, playFX: true, autoPoolObject: true);
          osd.Spawn(Combat);
          ScaleVFX(osd.spawnedObject);
          break;

        case ArtilleryVFXType.ElectricTransformerExplosion:
          osd = new ObjectSpawnData(Combat.Constants.VFXNames.artillery_electric_transformer_explosion, targetPosition, playFX: true, autoPoolObject: true);
          osd.Spawn(Combat);
          ScaleVFX(osd.spawnedObject);
          break;

        case ArtilleryVFXType.CoolantExplosion:
          osd = new ObjectSpawnData(Combat.Constants.VFXNames.artillery_coolant_explosion, targetPosition, playFX: true, autoPoolObject: true);
          osd.Spawn(Combat);
          ScaleVFX(osd.spawnedObject);
          break;
      }
    }

    private void ScaleVFX(GameObject vfxObject) {
      if (vfxObject == null || vfxScale == 1.0f) return;

      Main.LogDebug($"[ScalableArtilleryObjectiveSequence] Attempting to scale VFX object '{vfxObject.name}' to {vfxScale}");

      // Scale the transform
      vfxObject.transform.localScale = Vector3.one * vfxScale;

      // Also scale particle systems - they have their own scaling properties
      ParticleSystem[] particleSystems = vfxObject.GetComponentsInChildren<ParticleSystem>(true);
      Main.LogDebug($"[ScalableArtilleryObjectiveSequence] Found {particleSystems.Length} particle systems in '{vfxObject.name}'");

      foreach (ParticleSystem ps in particleSystems) {
        var main = ps.main;
        main.startSizeMultiplier *= vfxScale;
        main.startSpeedMultiplier *= vfxScale;
        main.gravityModifierMultiplier *= vfxScale;

        var shape = ps.shape;
        shape.scale *= vfxScale;

        //  Main.LogDebug($"[ScalableArtilleryObjectiveSequence] Scaled particle system '{ps.name}' in '{vfxObject.name}'");
      }

      Main.LogDebug($"[ScalableArtilleryObjectiveSequence] Successfully scaled VFX object '{vfxObject.name}' and its {particleSystems.Length} particle systems to {vfxScale}");
    }
  }
}
