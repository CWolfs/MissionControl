using System;
using System.Globalization;
using UnityEngine;
using Newtonsoft.Json;
using BTAssetReader.Core.Models;

namespace MissionControl.Data {
  public class PropLightDef {
    [JsonProperty("Name")]
    public string Name { get; set; } = "Light";

    [JsonProperty("Type")]
    public string Type { get; set; } = "Point"; // Point, Spot, Linear, Area

    [JsonProperty("Position")]
    public Vector3 Position { get; set; } = new Vector3(0f, 2f, 0f);

    [JsonProperty("Rotation")]
    public Vector3 Rotation { get; set; } = Vector3.zero;

    [JsonProperty("Color")]
    public string ColorHex { get; set; } = "#FFF2D9";

    [JsonProperty("Intensity")]
    public float Intensity { get; set; } = 1.0f;

    [JsonProperty("Radius")]
    public float Radius { get; set; } = 15.0f;

    [JsonProperty("InnerAngle")]
    public float InnerAngle { get; set; } = 30.0f;

    [JsonProperty("OuterAngle")]
    public float OuterAngle { get; set; } = 50.0f;

    [JsonProperty("Length")]
    public float Length { get; set; } = 2.0f;

    [JsonProperty("CastShadows")]
    public bool CastShadows { get; set; } = false;

    [JsonProperty("ContributeSpecular")]
    public bool ContributeSpecular { get; set; } = true;

    [JsonProperty("ContributeVolumetrics")]
    public bool ContributeVolumetrics { get; set; } = false;

    public Color GetUnityColor() {
      if (!string.IsNullOrEmpty(ColorHex) && ColorUtility.TryParseHtmlString(ColorHex, out Color parsed)) {
        return parsed;
      }
      return Color.white;
    }

    public static PropLightDef FromCore(PropLightData core) {
      Color c = new Color(core.Color.X, core.Color.Y, core.Color.Z, core.Color.W);
      return new PropLightDef {
        Name = core.Name,
        Type = core.Type,
        Position = new Vector3(core.Position.X, core.Position.Y, core.Position.Z),
        Rotation = new Vector3(core.Rotation.X, core.Rotation.Y, core.Rotation.Z),
        ColorHex = "#" + ColorUtility.ToHtmlStringRGB(c),
        Intensity = core.Intensity,
        Radius = core.Radius,
        InnerAngle = core.InnerAngle,
        OuterAngle = core.OuterAngle,
        Length = core.Length,
        CastShadows = core.CastShadows,
        ContributeSpecular = core.ContributeSpecular,
        ContributeVolumetrics = core.ContributeVolumetrics
      };
    }
  }
}
