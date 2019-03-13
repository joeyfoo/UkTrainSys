namespace Plugin {

	/// <summary>Represents the AI guard.</summary>
	internal static partial class AiGuard {
		/// <summary>Possible safety states of the Driver's Reminder Appliance.</summary>
		internal enum AiGuardStates {
			/// <summary>The guard is asleep. The numerical value of this constant is 0.</summary>
			None = 0,
			/// <summary>The AI guard will inform the driver upon stopping outside of the station limits. The numerical value of this constant is 1.</summary>
			MonitoringStoppingLocation = 1,
			/// <summary>The AI guard is issuing a ready to start buzzer signal to the driver. The numerical value of this constant is 2.</summary>
			DrawForward = 2,
			/// <summary>The AI guard is issuing a ready to start buzzer signal to the driver. The numerical value of this constant is 3.</summary>
			SetBack = 3,
			/// <summary>The AI guard is issuing a ready to start buzzer signal to the driver. The numerical value of this constant is 4.</summary>
			ReadyToStart = 4,
			/// <summary>The AI guard is waiting for the driver to reply with two buzzes. The numerical value of this constant is 5.</summary>
			AwaitingAcknowledgement = 5,
			/// <summary>The AI guard is has received acknowledgement from the driver. The numerical value of this constant is 6.</summary>
			AcknowledgementReceived = 6
		}
	}
}