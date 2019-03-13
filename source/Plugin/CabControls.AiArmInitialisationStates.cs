namespace Plugin {
	/// <summary>Represents the physical in-cab controls.</summary>
	internal static partial class CabControls {
		/// <summary>Possible states of the BR 8x series AC electric power handle.</summary>
		internal enum AiArmInitialisationStates {
			/// <summary>The AI arm initialisation has not taken placed yet. The numerical value of this constant is 0.</summary>
			Pending = 0,
			/// <summary>The AI arm initialisation is currently taking place. The numerical value of this constant is 1.</summary>
			Initialising = 1,
			/// <summary>The AI arm initialisation has taken place. The numerical value of this constant is 2.</summary>
			Initialised = 2
		}
	}
}