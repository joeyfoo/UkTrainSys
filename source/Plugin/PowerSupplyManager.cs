using System;
using System.Reflection;
using OpenBveApi.Runtime;

namespace Plugin {
	
	/// <summary>The power supply manager connects and manages electrical systems and power supplies.</summary>
	/// <remarks>This class provides a single system which interacts with a power supply, and which manages the operative electrical states of connected systems.</remarks>
	internal static class PowerSupplyManager {
		
		// members
		
		/// <summary>The voltage required by the system.</summary>
		internal static double RequiredVoltage;
		/// <summary>The current required by the system. If a power supply is not able to deliver this amount of current, the device will become inoperative.</summary>
		internal static double RequiredCurrent;
		/// <summary>An array holding the electrical systems currently connected to this power supply.</summary>
		internal static ElectricalSystem[] ConnectedSystems = new ElectricalSystem[1];
		/// <summary>The number of electrical systems currently being managed.</summary>
		internal static int ConnectedSystemCount;
		/// <summary>The currently selected power supply.</summary>
		internal static PowerSupply SelectedPowerSupply;
		
		// methods
		
		/// <summary>Connects electrical systems to the power supply manager.</summary>
		/// <param name="electricalSystems">The electrical systems to connect to the power supply manager.</param>
		/// <remarks>Electrical systems cannot be disconnected again, but each system can have its circuit breaker opened or closed, or an alternative power supply can be selected.</remarks>
		internal static void ConnectSystems(params ElectricalSystem[] electricalSystems) {
			if (electricalSystems != null) {
				/* Resize the array if necessary */
				while (ConnectedSystems.Length < electricalSystems.Length) {
					Array.Resize<ElectricalSystem>(ref ConnectedSystems, electricalSystems.Length * 2);
				}
				for (int i = 0; i < electricalSystems.Length; i++) {
					ConnectedSystems[i] = electricalSystems[i];
					ConnectedSystemCount++;
					if (Plugin.DebugMode) {
						Plugin.ReportLogEntry(
							string.Format(
								"{0} PowerSupplyManager: [INFORMATION] {1} now connected",
								DateTime.Now.TimeOfDay.ToString(),
								ConnectedSystems[i].ToString()
							)
						);
					}
				}
			}
		}
		
		/// <summary>Connects a power supply to the power supply manager, selecting it for use.</summary>
		/// <param name="electricalSystems">The power supply to connect and use.</param>
		internal static void ConnectPowerSupply(PowerSupply powerSupply) {
			if (powerSupply != null) {
				SelectedPowerSupply = powerSupply;
				if (Plugin.DebugMode) {
					Plugin.ReportLogEntry(
						string.Format(
							"{0} PowerSupplyManager: [INFORMATION] {1} now connected and selected as the active power supply",
							DateTime.Now.TimeOfDay.ToString(),
							SelectedPowerSupply.ToString()
						)
					);
				}
			}
		}
		
		/// <summary>Calculates the total number of amperes being drawn from the battery.</summary>
		/// <remarks>If the ampere load exceeds the maximum current rating of the power supply, the power supply circuit breaker will trip open.</remarks>
		internal static void CalculateCurrentLoad() {
			if (SelectedPowerSupply != null) {
				/* Calculate the load of all systems connected to this power supply */
				RequiredCurrent = 0;
				double maxVoltage = 0;
				for (int i = 0; i < ConnectedSystemCount; i++) {
					if (ConnectedSystems[i].BreakerState == SystemBreakerStates.Closed && ConnectedSystems[i].OperativeState != OperativeStates.Failed) {
						RequiredCurrent += ConnectedSystems[i].RequiredCurrent;
						if (ConnectedSystems[i].RequiredVoltage > maxVoltage) {
							maxVoltage = ConnectedSystems[i].RequiredVoltage;
						}
					}
					RequiredVoltage = maxVoltage;
				}
				/* Check the power supply to see if it is able to supply enough current, and set the
				 * power state of connected systems accordingly */
				if (!SelectedPowerSupply.Enabled || SelectedPowerSupply.PowerState == PowerStates.Overloaded || SelectedPowerSupply.BreakerState == SystemBreakerStates.Open) {
					for (int i = 0; i < ConnectedSystemCount; i++) {
						if (ConnectedSystems[i].PowerState != PowerStates.InsufficientPower) {
							ConnectedSystems[i].PowerState = PowerStates.InsufficientPower;
							if (Plugin.DebugMode) {
								Plugin.ReportLogEntry(
									string.Format(
										"{0} PowerSupplyManager: [CONDITION] {1} - Insufficent power",
										DateTime.Now.TimeOfDay.ToString(),
										ConnectedSystems[i].ToString()
									)
								);
							}
						}
					}
				} else {
					for (int i = 0; i < ConnectedSystemCount; i++) {
						if (ConnectedSystems[i].PowerState != PowerStates.Nominal) {
							ConnectedSystems[i].PowerState = PowerStates.Nominal;
						}
					}
				}
			}
		}
		
		/// <summary>This should be called once during each Elapse() call, and defines the behaviour of the system.
		/// This class should issue demands and requests to the Interlock Manager, rather than directly altering brake or power handle positions.</summary>
		/// <param name="elapsedTime">The elapsed time since the last call to this method in milliseconds.</param>
		/// <param name="debugBuilder">A string builder object, to which debug text can be appended.</param>
		internal static void Update(double elapsedTime, System.Text.StringBuilder debugBuilder) {
			CalculateCurrentLoad();
			
			/* Add any information to display via openBVE's in-game debug interface mode below */
			if (Plugin.DebugMode) {
				if (SelectedPowerSupply != null) {
					debugBuilder.AppendFormat("[PM:{0}]",
					                          SelectedPowerSupply.ToString());
				}
			}
		}
	}
}
