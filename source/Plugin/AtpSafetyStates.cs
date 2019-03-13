namespace Plugin {

	/// <summary>Represents an Automatic Train Protection System.</summary>
	internal partial class AutomaticTrainProtection : ElectricalSystem {
		/// <summary>Possible non-visual warning states of the Automatic Train Protection System.</summary>
		internal enum SafetyStates {
			/// <summary>Set this state when no processing or action is to be taken. The numerical value of this constant is 0.</summary>
			None = 0
		}
	}
}