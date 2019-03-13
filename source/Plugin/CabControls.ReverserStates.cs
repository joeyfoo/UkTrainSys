namespace Plugin {
	/// <summary>Represents the physical in-cab controls.</summary>
	internal static partial class CabControls {
		/// <summary>Possible states of the horn lever.</summary>
		internal enum ReverserStates {
			/// <summary>The reverser handle is in the backward position. The numerical value of this constant is 2.</summary>
			Backward = -1,
			/// <summary>The reverser handle is in the neutral position. The numerical value of this constant is 0.</summary>
			Neutral = 0,
			/// <summary>The reverser handle is in the forward position. The numerical value of this constant is 1.</summary>
			Forward = 1
		}
	}
}