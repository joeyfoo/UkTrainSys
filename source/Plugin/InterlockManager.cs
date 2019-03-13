using System;
using OpenBveApi.Runtime;

namespace Plugin {
	
	/// <summary>This class acts as an interface between a safety system and the train's controls.</summary>
	/// <remarks>Safety systems should not attempt to alter the power and brake handle's themselves. Rather, they should
	/// issue a request to the interlock manager. The interlock manager will then decide whether power or brake handle positions
	/// can be changed, based upon conditions, such as interlock status and train speed.</remarks>
	internal static class InterlockManager {
		
		// members
		
		/// <summary>Whether or not the train brakes are currently applied and being held on.</summary>
		private static bool MyBrakesHeldOn;
		/// <summary>Whether or not the traction power is currently cut off, and being held off.</summary>
		private static bool MyTractionPowerHeldOff;
		/// <summary>Whether or not a request to release the brakes is pending.</summary>
		private static bool BrakeReleaseRequested;
		/// <summary>Whether or not a request to enable traction power is pending.</summary>
		private static bool TractionPowerRequested;
		/// <summary>Whether or not the system has had its functionality disabled.</summary>
		/// <remarks>Set this to true if a train is not to be equipped with a particular system.</remarks>
		internal static bool Enabled;
		
		// properties
		
		/// <summary>Gets whether or not the brakes are being held on.</summary>
		internal static bool BrakesHeldOn {
			get { return MyBrakesHeldOn; }
		}
		
		/// <summary>Gets whether or not traction power is being held off.</summary>
		internal static bool TractionPowerHeldOff {
			get { return MyTractionPowerHeldOff; }
		}
		
		// methods
		
		/// <summary>Re-initialises the Interlock Manager. Only call this method via the Plugin.Initialize() method.</summary>
		/// <param name="mode">The initialisation mode as set by the host application.</param>
		internal static void Reinitialise(InitializationModes mode) {
			MyBrakesHeldOn = true;
			MyTractionPowerHeldOff = true;
			BrakeReleaseRequested = true;
			TractionPowerRequested = true;
			if (Plugin.DebugMode) {
				Plugin.ReportLogEntry(
					string.Format(
						"{0} InterlockManager: [INFORMATION] - Reinitialising",
						DateTime.Now.TimeOfDay.ToString()
					)
				);
			}
		}
		
		/// <summary>Called by a safety system when it indicates that a reset or acknowledgement command has been registered.</summary>
		/// <remarks>The reset will only be performed when appropriate conditions are met.</remarks>
		internal static void RequestBrakeReset() {
			if (MyBrakesHeldOn && !BrakeReleaseRequested) {
				/* Only set the reset requests if necessary */
				BrakeReleaseRequested = true;
				if (Plugin.DebugMode) {
					Plugin.ReportLogEntry(
						string.Format(
							"{0} InterlockManager: [INFORMATION] - Interlock brake reset and release request",
							DateTime.Now.TimeOfDay.ToString()
						)
					);
				}
			}
		}
		
		/// <summary>Called by a safety system when it indicates that a reset or acknowledgement command has been registered.</summary>
		/// <remarks>The reset will only be performed when appropriate conditions are met.</remarks>
		internal static void RequestTractionPowerReset() {
			if (MyTractionPowerHeldOff && !TractionPowerRequested) {
				/* Only set the reset requests if necessary */
				TractionPowerRequested = true;
				if (Plugin.DebugMode) {
					Plugin.ReportLogEntry(
						string.Format(
							"{0} InterlockManager: [INFORMATION] - Interlock traction power reset and release request",
							DateTime.Now.TimeOfDay.ToString()
						)
					);
				}
			}
		}
		
		/// <summary>Issues ONLY a traction power cut-off demand with immediate effect; brakes are unaffected.</summary>
		internal static void DemandTractionPowerCutoff() {
			if (!MyTractionPowerHeldOff) {
				MyTractionPowerHeldOff = true;
				if (Plugin.Diesel.Enabled) {
					Plugin.Diesel.CutRevs();
				}
				if (Plugin.DebugMode) {
					Plugin.ReportLogEntry(
						string.Format(
							"{0} InterlockManager: [INFORMATION] - Traction power cutoff demand - (location {1} metres)",
							DateTime.Now.TimeOfDay.ToString(),
							Plugin.TrainLocation.ToString()
						)
					);
				}
			}
		}
		
		/// <summary>Issues a brake demand (traction power is automatically cut as well), with immediate effect.</summary>
		internal static void DemandBrakeApplication() {
			if (!MyBrakesHeldOn) {
				MyBrakesHeldOn = true;
				DemandTractionPowerCutoff();
				if (Plugin.DebugMode) {
					Plugin.ReportLogEntry(
						string.Format(
							"{0} InterlockManager: [INFORMATION] - Brake demand - (location {1} metres)",
							DateTime.Now.TimeOfDay.ToString(),
							Plugin.TrainLocation.ToString()
						)
					);
				}
			}
		}
		
		/// <summary>This method should be called if the configuration file indicates that the interlock manager is to be disabled.</summary>
		internal static void Disable() {
			MyBrakesHeldOn = false;
			MyTractionPowerHeldOff = false;
			BrakeReleaseRequested = false;
			TractionPowerRequested = false;
			if (Enabled) {
				Enabled = false;
			}
		}
		
		/// <summary>This method should be called if the interlock manager is to be enabled.</summary>
		internal static void Enable() {
			if (!Enabled) {
				Enabled = true;
			}
		}
		
		/// <summary>This should be called during each Elapse() call.</summary>
		/// <param name="elapsedTime">The elapsed time since the last call to this method in milliseconds.</param>
		/// <param name="handles">The handles of the cab.</param>
		/// <param name="panel">The array of panel variables the plugin initialized in the Load call.</param>
		/// <param name="debugBuilder">A string builder object, to which debug text can be appended.</param>
		/// <remarks>Set members of the Handles argument to overwrite the driver's settings.</remarks>
		internal static void Update(double elapsedTime, Handles handles, int[] panel, System.Text.StringBuilder debugBuilder) {
			if (Enabled) {
				/* General behaviour */
				if (!CabControls.MasterSwitchOn) {
					/* If the master switch is off, interlocks are always on */
					MyBrakesHeldOn = true;
					MyTractionPowerHeldOff = true;
				} else {
					/* The master switch is on, so handle interlock release requests - general behaviour
					 * 
					 * Brake interlock
					 */
					if (BrakeReleaseRequested) {
						if (Plugin.TrainSpeed == 0 &&
						    CabControls.PowerPosition == 0 &&
						    Plugin.Dra.SafetyState == DriverReminderAppliance.SafetyStates.Deactivated &&
						    Plugin.Tpws.SafetyState != TrainProtectionWarningSystem.SafetyStates.BrakesAppliedCountingDown &&
						    Plugin.Vig.SafetyState != VigilanceDevice.SafetyStates.BrakeDemandIssued &&
						    Plugin.Vig.SafetyState != VigilanceDevice.SafetyStates.CancelTimerExpired &&
						    Plugin.Aws.SafetyState != AutomaticWarningSystem.SafetyStates.CancelTimerExpired &&
						    Plugin.Aws.SafetyState != AutomaticWarningSystem.SafetyStates.TpwsAwsBrakeDemandIssued &&
						    Plugin.Aws.SafetyState != AutomaticWarningSystem.SafetyStates.TpwsTssBrakeDemandIssued) {
							if (MyBrakesHeldOn) {
								if (Plugin.DebugMode) {
									Plugin.ReportLogEntry(
										string.Format(
											"{0} InterlockManager: [INFORMATION] - Brake interlock release successful",
											DateTime.Now.TimeOfDay.ToString()
										)
									);
								}
							}
							MyBrakesHeldOn = false;
							BrakeReleaseRequested = false;
						}
					}
					
					/* Traction power interlock */
					if (TractionPowerRequested) {
						if (CabControls.PowerPosition == 0 &&
						    PowerSupplyManager.SelectedPowerSupply is OverheadPowerSupply ||
						    PowerSupplyManager.SelectedPowerSupply is DieselEngine &&
						    Plugin.Dra.SafetyState == DriverReminderAppliance.SafetyStates.Deactivated &&
						    Plugin.Tpws.SafetyState != TrainProtectionWarningSystem.SafetyStates.BrakesAppliedCountingDown &&
						    Plugin.Vig.SafetyState != VigilanceDevice.SafetyStates.BrakeDemandIssued &&
						    Plugin.Vig.SafetyState != VigilanceDevice.SafetyStates.CancelTimerExpired &&
						    Plugin.Aws.SafetyState != AutomaticWarningSystem.SafetyStates.CancelTimerExpired &&
						    Plugin.Aws.SafetyState != AutomaticWarningSystem.SafetyStates.TpwsAwsBrakeDemandIssued &&
						    Plugin.Aws.SafetyState != AutomaticWarningSystem.SafetyStates.TpwsTssBrakeDemandIssued) {
							if (MyTractionPowerHeldOff) {
								if (Plugin.DebugMode) {
									Plugin.ReportLogEntry(
										string.Format(
											"{0} InterlockManager: [INFORMATION] - Traction interlock release successful",
											DateTime.Now.TimeOfDay.ToString()
										)
									);
								}
							}
							MyTractionPowerHeldOff = false;
							TractionPowerRequested = false;
						}
					}
					
					/* Specific circumstances which override generic behaviour */
					if (PowerSupplyManager.SelectedPowerSupply is Battery && Plugin.TrainSpeed == 0) {
						/* Handle brake release when train has been brought to a stand after lowering the pantograph */
						if (Plugin.Dra.SafetyState == DriverReminderAppliance.SafetyStates.Deactivated &&
						    Plugin.Tpws.SafetyState != TrainProtectionWarningSystem.SafetyStates.BrakesAppliedCountingDown &&
						    Plugin.Vig.SafetyState != VigilanceDevice.SafetyStates.BrakeDemandIssued &&
						    Plugin.Vig.SafetyState != VigilanceDevice.SafetyStates.CancelTimerExpired &&
						    Plugin.Aws.SafetyState != AutomaticWarningSystem.SafetyStates.CancelTimerExpired &&
						    Plugin.Aws.SafetyState == AutomaticWarningSystem.SafetyStates.TpwsAwsBrakeDemandIssued &&
						    Plugin.Aws.SafetyState == AutomaticWarningSystem.SafetyStates.TpwsTssBrakeDemandIssued) {
							MyBrakesHeldOn = false;
						}
					}
					
					/* Door interlock */
					if (CabControls.DoorState != DoorStates.None && CabControls.ReverserPosition == CabControls.ReverserStates.Neutral) {
						/* Handle brake release if doors are open, and reverser is neutral */
						MyBrakesHeldOn = false;
					} else if (CabControls.DoorState != DoorStates.None) {
						MyBrakesHeldOn = true;
						MyTractionPowerHeldOff = true;
					}
				}
				
				/* Set the train brake and power handle positions */
				if (MyBrakesHeldOn) {
					handles.BrakeNotch = Plugin.TrainSpecifications.BrakeNotches + 1;
				}
				if (MyTractionPowerHeldOff) {
					handles.PowerNotch = 0;
					handles.Reverser = (int)CabControls.ReverserStates.Neutral;
				}
				
				/* Add any information to display via openBVE's in-game debug interface mode below */
				if (Plugin.DebugMode) {
					debugBuilder.AppendFormat("[IM:Hb:{0} Tpho:{1} Rb:{2} Tpr:{3}]",
					                          BrakesHeldOn.ToString(),
					                          TractionPowerHeldOff.ToString(),
					                          BrakeReleaseRequested.ToString(),
					                          TractionPowerRequested.ToString());
				}
			}
		}
	}
}
