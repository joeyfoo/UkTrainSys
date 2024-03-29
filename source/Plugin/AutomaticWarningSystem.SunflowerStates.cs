﻿namespace Plugin {

	/// <summary>Represents an Automatic Warning System.</summary>
	internal partial class AutomaticWarningSystem : ElectricalSystem {
		/// <summary>Possible visual warning states of the Automatic Warning System (sunflower instrument).</summary>
		private enum SunflowerStates {
			/// <summary>Sunflower is dark/not shown. The numerical value of this constant is 0.</summary>
			Clear = 0,
			/// <summary>Sunflower is shown. The numerical value of this constant is 1.</summary>
			Warn = 1
		}
	}
}