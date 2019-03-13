namespace Plugin {

	/// <summary>Represents a Vigilance Device.</summary>
	internal partial class VigilanceDevice : ElectricalSystem {
		/// <summary>Possible states of the Vigilance Device</summary>
		internal enum SafetyStates {
			/// <summary>The Vigilance Device has been isolated. The numerical value of this constant is 0.</summary>
			Isolated = 0,
			/// <summary>The Vigilance Device inactivity timer is counting down. The numerical value of this constant is 1.</summary>
			InactivityTimerActive = 1,
			/// <summary>The Vigilance Device inactivity timer expired, the acknowledgement timer has been triggered, and is counting down.
			/// The numerical value of this constant is 2.</summary>
			CancelTimerActive = 2,
			/// <summary>The acknowledgement timer expired and the safety system is intervening. The numerical value of this constant is 3.</summary>
			CancelTimerExpired = 3,
			/// <summary>The Vigilance Device has issued a Brake Demand instruction to the Interlock Manager. The numerical value of this constant is 4.</summary>
			BrakeDemandIssued = 4
		}
	}
}