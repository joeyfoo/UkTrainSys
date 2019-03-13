namespace Plugin {
	/// <summary>Represents the physical in-cab controls.</summary>
	internal static partial class CabControls {
		/// <summary>Possible states of the horn lever.</summary>
		internal enum HornStates {
			/// <summary>The horn lever is pushed forward. The numerical value of this constant is 0.</summary>
			Forward = 0,
			/// <summary>The horn lever is centred. The numerical value of this constant is 1.</summary>
			Centre = 1,
			/// <summary>The horn lever is pulled backward. The numerical value of this constant is 2.</summary>
			Backward = 2,
			/// <summary>The horn lever is automatically being pushed forward followed by being pulled backward. The numerical value of this constant is 3.</summary>
			AutoForwardFirst = 3,
			/// <summary>The horn lever is automatically being pulled backward followed by being pushed forward. The numerical value of this constant is 4.</summary>
			AutoBackwardFirst = 4
		}
	}
}