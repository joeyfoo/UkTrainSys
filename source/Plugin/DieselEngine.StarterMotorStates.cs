namespace Plugin {

	/// <summary>Represents a diesel engine.</summary>
	internal partial class DieselEngine : PowerSupply {
		
		/// <summary>The possible states of the diesel engine starter motor.</summary>
		internal enum StarterMotorStates {
			/// <summary>The diesel engine is off. The numerical value of this constant is 0.</summary>
			Off = 0,
			/// <summary>The starter motor is spinning up. The numerical value of this constant is 1.</summary>
			Starting = 1,
			/// <summary>The starter motor is running. The numerical value of this constant is 2.</summary>
			Running = 2,
			/// <summary>The starter motor is running down. The numerical value of this constant is 3.</summary>
			RunningDown = 3,
			/// <summary>The starter motor has been interrupted because the engine start button was released. The numerical value of this constant is 4.</summary>
			StartupInterrupted = 4
		}
	}
}