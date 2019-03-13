namespace Plugin {

	/// <summary>Represents an Automatic Warning System.</summary>
	internal partial class AutomaticWarningSystem : ElectricalSystem {
		/// <summary>Possible acknowledgement states used in conjunction with the AWS Reset button.</summary>
		private enum ResetAcknowledgementStates {
			/// <summary>The default state where nothing needs to happen.</summary>
			None = 0,
			/// <summary>The AWS Reset button has been depressed, and its release is being awaited.</summary>
			AwaitingRelease = 1,
			/// <summary>The AWS Reset button has been depressed and released, so the AWS acknowledgement is completed.</summary>
			Released = 2
		}
	}
}