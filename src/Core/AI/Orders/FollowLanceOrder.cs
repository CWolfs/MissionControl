using System;
using System.Collections.Generic;
using BattleTech.Serialization;
using HBS.Collections;
using HBS.Util;
using UnityEngine;

namespace MissionControl.AI {
	public class FollowLanceOrder : CustomAIOrder {
		public override string OrderType {
			get {
				return "FOLLOW_LANCE";
			}
		}
	}
}
