using System;
using System.Collections.Generic;
using BattleTech.Serialization;
using HBS.Collections;
using HBS.Util;
using UnityEngine;

namespace MissionControl.AI {
	public class CustomAIOrder {
		public virtual string OrderType {
			get {
				return "UNSET";
			}
		}
	}
}
