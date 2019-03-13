using System;
using OpenBveApi.Runtime;

namespace Plugin {

	/// <summary>Represents a Driver Reminder Appliance.</summary>
	internal partial class DriverReminderAppliance : ElectricalSystem {
		/// <summary>Possible safety states of the Driver's Reminder Appliance.</summary>
		internal enum SafetyStates {
			/// <summary>Drivers's Reminder Appliance is deactivated. The numerical value of this constant is 0.</summary>
			Deactivated = 0,
			/// <summary>Drivers's Reminder Appliance is activated. The numerical value of this constant is 1.</summary>
			Activated = 1,
		}
	}
}