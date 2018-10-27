using System;
using System.Collections.Generic;
using BattleTech;

namespace MissionControl.AI {
	public class CustomAIOrder {
		public virtual string CustomOrderType {
			get {
				return "UNSET";
			}
		}
	}
}
