using UnityEngine;

using System.Collections.Generic;

using BattleTech;

namespace MissionControl.Result {
  internal class ArtilleryVfxBaseline : MonoBehaviour {
    private Vector3 originalLocalScale;
    private ParticleSystem[] particleSystems;
    private float[] originalStartSizeMultipliers;
    private float[] originalStartSpeedMultipliers;
    private float[] originalGravityModifierMultipliers;
    private Vector3[] originalShapeScales;
    private bool captured;

    public void CaptureIfNeeded() {
      if (captured) return;
      captured = true;

      originalLocalScale = transform.localScale;
      particleSystems = GetComponentsInChildren<ParticleSystem>(true);
      int count = particleSystems.Length;
      originalStartSizeMultipliers = new float[count];
      originalStartSpeedMultipliers = new float[count];
      originalGravityModifierMultipliers = new float[count];
      originalShapeScales = new Vector3[count];

      for (int i = 0; i < count; i++) {
        ParticleSystem ps = particleSystems[i];
        var main = ps.main;
        originalStartSizeMultipliers[i] = main.startSizeMultiplier;
        originalStartSpeedMultipliers[i] = main.startSpeedMultiplier;
        originalGravityModifierMultipliers[i] = main.gravityModifierMultiplier;

        var shape = ps.shape;
        originalShapeScales[i] = shape.scale;
      }
    }

    public void ApplyScale(float scale) {
      CaptureIfNeeded();

      transform.localScale = originalLocalScale * scale;

      for (int i = 0; i < particleSystems.Length; i++) {
        ParticleSystem ps = particleSystems[i];
        if (ps == null) continue;

        var main = ps.main;
        main.startSizeMultiplier = originalStartSizeMultipliers[i] * scale;
        main.startSpeedMultiplier = originalStartSpeedMultipliers[i] * scale;
        main.gravityModifierMultiplier = originalGravityModifierMultipliers[i] * scale;

        var shape = ps.shape;
        shape.scale = originalShapeScales[i] * scale;
      }
    }

    private void OnDisable() {
      if (captured) {
        ApplyScale(1.0f);
      }
    }
  }

  public class ScalableArtilleryObjectiveSequence : ArtilleryObjectiveSequence {
    private readonly float vfxScale;
    private readonly List<ICombatant> splashTargets;
    private readonly int splashDamage;
    private readonly int splashHeatDamage;
    private readonly int splashStabilityDamage;
    private bool splashApplied;

    public ScalableArtilleryObjectiveSequence(
      CombatGameState combat,
      List<Vector3> targetPositions,
      ArtilleryVFXType artilleryVFXType,
      List<ICombatant> targets,
      float damageEachLocation,
      int heatDamage,
      int stabilityDamage,
      TerrainMaskFlags applyDesignMaskOnExplosion,
      float vfxScale = 1.0f,
      List<ICombatant> splashTargets = null,
      int splashDamage = 0,
      int splashHeatDamage = 0,
      int splashStabilityDamage = 0)
      : base(combat, targetPositions, artilleryVFXType, targets, damageEachLocation, heatDamage, stabilityDamage, applyDesignMaskOnExplosion) {
      this.vfxScale = vfxScale;
      this.splashTargets = splashTargets;
      this.splashDamage = splashDamage;
      this.splashHeatDamage = splashHeatDamage;
      this.splashStabilityDamage = splashStabilityDamage;
    }

    public override void OnUpdate() {
      base.OnUpdate();

      if (!splashApplied && splashTargets != null && splashTargets.Count > 0 &&
          (state == SequenceState.ShowingEffects || (state == SequenceState.Shelling && AllTargets.Count == 0))) {
        splashApplied = true;
        ApplySplashDamage();
      }
    }

    private void ApplySplashDamage() {
      Main.LogDebug($"[ScalableArtilleryObjectiveSequence] Applying splash damage to {splashTargets.Count} targets: Damage={splashDamage}, Heat={splashHeatDamage}, Stability={splashStabilityDamage}");

      foreach (ICombatant splashTarget in splashTargets) {
        if (splashTarget == null || splashTarget.IsDead) continue;

        Main.LogDebug($"[ScalableArtilleryObjectiveSequence] Applying splash damage to '{splashTarget.DisplayName}'");

        if (splashDamage > 0) {
          DamageOrderUtility.ApplyDamageToAllLocations("ArtillerySplash", base.SequenceGUID, base.RootSequenceGUID, splashTarget, splashDamage, splashDamage, AttackDirection.FromArtillery, DamageType.Artillery);
        }

        if (splashHeatDamage != 0) {
          DamageOrderUtility.ApplyHeatDamage(base.SequenceGUID, splashTarget, splashHeatDamage);
        }

        if (splashStabilityDamage > 0) {
          DamageOrderUtility.ApplyStabilityDamage(base.SequenceGUID, splashTarget, splashStabilityDamage);
        }
      }
    }

    // PlayScaledFX is called by the Harmony patch to add scaling support
    // Note: This requires the assembly to be publicized
    public void PlayScaledFX(Vector3 targetPosition) {
      Main.LogDebug($"[ScalableArtilleryObjectiveSequence] Playing artillery VFX at position {targetPosition} with scale {vfxScale}");

      this.targetPosition = targetPosition;

      switch (artilleryVFXType) {
        case ArtilleryVFXType.ArtilleryShellSingle:
          SpawnAndScaleVFX(Combat.Constants.VFXNames.artillery_projectile_single, targetPosition);
          SpawnAndScaleVFX(Combat.Constants.VFXNames.artillery_impact_barrage, targetPosition);
          break;

        case ArtilleryVFXType.ArtilleryShellBarrage:
          SpawnAndScaleVFX(Combat.Constants.VFXNames.artillery_projectile_barrage, targetPosition);
          SpawnAndScaleVFX(Combat.Constants.VFXNames.artillery_impact_barrage, targetPosition);
          break;

        case ArtilleryVFXType.Explosion:
          SpawnAndScaleVFX(Combat.Constants.VFXNames.artillery_explosion, targetPosition);
          break;

        case ArtilleryVFXType.OrbitalPPC:
          SpawnAndScaleVFX(Combat.Constants.VFXNames.artillery_orbital_ppc, targetPosition);
          break;

        case ArtilleryVFXType.ElectricTransformerExplosion:
          SpawnAndScaleVFX(Combat.Constants.VFXNames.artillery_electric_transformer_explosion, targetPosition);
          break;

        case ArtilleryVFXType.CoolantExplosion:
          SpawnAndScaleVFX(Combat.Constants.VFXNames.artillery_coolant_explosion, targetPosition);
          break;
      }
    }

    private void SpawnAndScaleVFX(string prefabName, Vector3 position) {
      ObjectSpawnData osd = new ObjectSpawnData(prefabName, position, playFX: true, autoPoolObject: true);
      osd.Spawn(Combat);
      ScaleVFX(osd.spawnedObject);
    }

    private void ScaleVFX(GameObject vfxObject) {
      if (vfxObject == null) return;

      ArtilleryVfxBaseline baseline = vfxObject.GetComponent<ArtilleryVfxBaseline>();
      if (baseline == null) {
        if (vfxScale == 1.0f) return;
        baseline = vfxObject.AddComponent<ArtilleryVfxBaseline>();
        baseline.CaptureIfNeeded();
      }

      Main.LogDebug($"[ScalableArtilleryObjectiveSequence] Scaling VFX object '{vfxObject.name}' to {vfxScale}");
      baseline.ApplyScale(vfxScale);

      if (vfxScale != 1.0f) {
        ParticleSystem rootPs = vfxObject.GetComponent<ParticleSystem>();
        if (rootPs != null) {
          rootPs.Stop(withChildren: true);
          rootPs.Clear(withChildren: true);
          rootPs.Play(withChildren: true);
        }
      }
    }
  }
}
