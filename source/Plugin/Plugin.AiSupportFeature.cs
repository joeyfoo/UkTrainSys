using System;
using OpenBveApi.Runtime;

namespace Plugin {

	/// <summary>The interface to be implemented by the plugin.</summary>
	public partial class Plugin : IRuntime {

		/// <summary>Possible states of the AI driver's horn lever interaction.</summary>
		internal class AiSupportFeature {
			/// <summary>Whether plugin AI support is enabled.</summary>
			internal static bool Enabled = true;
			/// <summary>Whether or not openBVE's AI driver is currently enabled and related states should be shown.</summary>
			internal static bool AiDriverEnabled;
			/// <summary>A timer for use within the PerformAI() method, storing a value in milliseconds.</summary>
			internal static double Timer;
			/// <summary>Whether or not the timer used within the PerformAI() method is currently active.</summary>
			internal static bool TimerActive;
			/// <summary>A timer which keeps track of the elapsed time since the last call to the PerformAI() method, storing a value in milliseconds.</summary>
			/// <remarks>This can be used to check whether the AI driver is still enabled or not.</remarks>
			internal static double AiDriverEnabledTimer;
			/// <summary>Whether or not the AI support is currently operating the reverser.</summary>
			internal static bool OperatingReverser;
			/// <summary>The current state of the AI driver's horn lever interaction.</summary>
			internal static AiSoundHornStates SoundHornState;
			/// <summary>The location where the LastSignalData member was last updated.</summary>
			internal static double LastReportedSignalLocation;
			/// <summary>The maximum distance from the upcoming signal showing a red aspect, below which the AI Support will activate the DRA.</summary>
			internal static double MaximumDraActivationDistance = 150;
			/// <summary>Whether or not the next stop is meant to be the last (at a terminal station).</summary>
			internal static bool NextStopIsTheLast;
			/// <summary>Whether or not the AI Support should lower the pantograph when next stopping.</summary>
			internal static bool LowerPantographAtNextStop;
			/// <summary>Whether or not there is an upcoming neutral section.</summary>
			internal static bool UpcomingNeutralSection;
			/// <summary>The location of the next upcoming neutral section.</summary>
			internal static int LocationOfNextNeutralSection;
			
			// methods
			
			/// <summary>Re-initialises the Interlock Manager. Only call this method via the Plugin.Initialize() method.</summary>
			/// <param name="mode">The initialisation mode as set by the host application.</param>
			internal static void Reinitialise(InitializationModes mode) {
				NextStopIsTheLast = false;
				LowerPantographAtNextStop = false;
				UpcomingNeutralSection = false;
				LocationOfNextNeutralSection = 0;
				if (Plugin.DebugMode) {
					Plugin.ReportLogEntry(
						string.Format(
							"{0} AiSupportFeature: [INFORMATION] - Reinitialising",
							DateTime.Now.TimeOfDay.ToString()
						)
					);
				}
			}
			
			/// <summary>Calculates at what point to move the power handle to the off position, assuming it takes a specified period of time for power to run down to the off setting.</summary>
			/// <param name="seconds">The time in seconds which it will take before power will be off.</param>
			/// <returns>Whether or not to move the power handle to the off position yet.</returns>
			internal static bool CalculatePowerHandleOffLocation(int seconds) {
				if (UpcomingNeutralSection) {
					double location = (seconds * 0.000277777778) * TrainSpeed * 1000;
					if (TrainLocation > LocationOfNextNeutralSection - location) {
						return true;
					} else {
						return false;
					}
				} else {
					return false;
				}
			}
		}
	}
}