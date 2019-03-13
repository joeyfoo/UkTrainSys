using System;
using System.Text;
using System.Globalization;
using OpenBveApi.Runtime;

namespace Plugin {

	/// <summary>The interface to be implemented by the plugin.</summary>
	public partial class Plugin : IRuntime {
		/// <summary>Loads key-value pairs from the configuration file, or if the file does not exist, creates a new configuration file with default values.</summary>
		/// <param name="exceptionRaised">A string containing any exception which is raised.</param>
		/// <returns>True if the configuration file was loaded or created successfully.</returns>
		private bool LoadConfigFile(out string exceptionRaised) {
			bool success;
			System.Reflection.AssemblyName assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
			ConfigFile = System.IO.Path.Combine(TrainPath, assemblyName.Name + ".cfg");
			CultureInfo culture = CultureInfo.InvariantCulture;
			try {
				if (!System.IO.File.Exists(ConfigFile)) {
					/* Write out a new configuration file with default settings
					 * (from class members which should be intialised with default values already) */
					StringBuilder builder = new StringBuilder();
					builder.AppendFormat(
						"; Configuration file for {0} (automatically generated at {1}){2}{2}",
						assemblyName.Name,
						System.DateTime.Now.ToString(),
						Environment.NewLine
					);
					builder.AppendFormat(
						"; Intended for plugin version: {0}{1}{1}",
						assemblyName.Version.ToString(),
						Environment.NewLine
					);
					builder.AppendFormat(
						"[Options]{0}DebugMode = {1}{0}{0}",
						Environment.NewLine,
						(DebugMode ? "True" : "False")
					);
					builder.AppendFormat(
						"[AiSupport]{0}Enabled = {1}{0}MaximumDraActivationDistance = {2}{0}{0}",
						Environment.NewLine,
						(AiSupportFeature.Enabled ? "True" : "False"),
						AiSupportFeature.MaximumDraActivationDistance.ToString()
					);
					builder.AppendFormat(
						"[InterlockManager]{0}Enabled = {1}{0}{0}",
						Environment.NewLine,
						(InterlockManager.Enabled ? "True" : "False")
					);
					builder.AppendFormat(
						"[Battery]{0}Enabled = {1}{0}MaximumOutputVoltage = {2}{0}MaximumOutputCurrent = {3}{0}DischargeRate = {4}{0}{0}",
						Environment.NewLine,
						MainBattery.Enabled.ToString(culture),
						MainBattery.MaximumOutputVoltage.ToString(culture),
						MainBattery.MaximumOutputCurrent.ToString(culture),
						MainBattery.DischargeRate.ToString(culture)
					);
					builder.AppendFormat(
						"[Overhead]{0}Enabled = {1}{0}MaximumOutputVoltage = {2}{0}MaximumOutputCurrent = {3}{0}MaximumInputVoltage = {4}{0}MinimumInputVoltage = {5}{0}{0}",
						Environment.NewLine,
						OverheadSupply.Enabled.ToString(culture),
						OverheadSupply.MaximumOutputVoltage.ToString(culture),
						OverheadSupply.MaximumOutputCurrent.ToString(culture),
						OverheadSupply.MaximumInputVoltage.ToString(culture),
						OverheadSupply.MinimumInputVoltage.ToString(culture)
					);
					builder.AppendFormat(
						"[Diesel]{0}Enabled = {1}{0}MaximumOutputVoltage = {2}{0}MaximumOutputCurrent = {3}{0}MotorStartDelay = {4}{0}EngineStartDelay = {5}{0}EngineRunDownDelay = {6}{0}EngineStallDelay = {7}{0}StallProbability = {8}{0}{0}",
						Environment.NewLine,
						Diesel.Enabled.ToString(culture),
						Diesel.MaximumOutputVoltage.ToString(culture),
						Diesel.MaximumOutputCurrent.ToString(culture),
						Diesel.MotorStartDelay.ToString(culture),
						Diesel.EngineStartDelay.ToString(culture),
						Diesel.EngineRunDownDelay.ToString(culture),
						Diesel.EngineStallDelay.ToString(culture),
						Diesel.StallProbability.ToString(culture)
					);
					builder.AppendFormat(
						"[Aws]{0}Enabled = {1}{0}RequiredVoltage = {2}{0}RequiredCurrent = {3}{0}CancelTimeout = {4}{0}{0}",
						Environment.NewLine,
						Aws.Enabled.ToString(culture),
						Aws.RequiredVoltage.ToString(culture),
						Aws.RequiredCurrent.ToString(culture),
						Aws.CancelTimeout.ToString(culture)
					);
					builder.AppendFormat(
						"[Vigilance]{0}Enabled = {1}{0}RequiredVoltage = {2}{0}RequiredCurrent = {3}{0}InactivityTimeout = {4}{0}CancelTimeout = {5}{0}ReducedCycleTime = {6}{0}{0}",
						Environment.NewLine,
						Vig.Enabled.ToString(culture),
						Vig.RequiredVoltage.ToString(culture),
						Vig.RequiredCurrent.ToString(culture),
						Vig.InactivityTimeout.ToString(culture),
						Vig.CancelTimeout.ToString(culture),
						Vig.ReducedCycleTime.ToString(culture)
					);
					builder.AppendFormat(
						"[Tpws]{0}Enabled = {1}{0}RequiredVoltage = {2}{0}RequiredCurrent = {3}{0}BrakesAppliedTimeout = {4}{0}TssOverrideTimeout = {5}{0}OssTimeout = {6}{0}IndicatorBlinkRate = {7}{0}{0}",
						Environment.NewLine,
						Tpws.Enabled.ToString(culture),
						Tpws.RequiredVoltage.ToString(culture),
						Tpws.RequiredCurrent.ToString(culture),
						Tpws.BrakesAppliedTimeout.ToString(culture),
						Tpws.TssOverrideTimeout.ToString(culture),
						Tpws.OssTimeout.ToString(culture),
						Tpws.IndicatorBlinkRate.ToString(culture)
					);
					builder.AppendFormat(
						"[Dra]{0}Enabled = {1}{0}RequiredVoltage = {2}{0}RequiredCurrent = {3}{0}{0}",
						Environment.NewLine,
						Dra.Enabled.ToString(culture),
						Dra.RequiredVoltage.ToString(culture),
						Dra.RequiredCurrent.ToString(culture)
					);
					builder.AppendFormat(
						"[Headlights]{0}Enabled = {1}{0}RequiredVoltage = {2}{0}RequiredCurrent = {3}{0}{0}",
						Environment.NewLine,
						Head.Enabled.ToString(culture),
						Head.RequiredVoltage.ToString(culture),
						Head.RequiredCurrent.ToString(culture)
					);
					builder.AppendFormat(
						"[Taillights]{0}Enabled = {1}{0}RequiredVoltage = {2}{0}RequiredCurrent = {3}{0}{0}",
						Environment.NewLine,
						Tail.Enabled.ToString(culture),
						Tail.RequiredVoltage.ToString(culture),
						Tail.RequiredCurrent.ToString(culture)
					);
					builder.AppendFormat(
						"[Blower]{0}Enabled = {1}{0}RequiredVoltage = {2}{0}RequiredCurrent = {3}{0}Delay = {4}{0}{0}",
						Environment.NewLine,
						Fan.Enabled.ToString(culture),
						Fan.RequiredVoltage.ToString(culture),
						Fan.RequiredCurrent.ToString(culture),
						Fan.Delay.ToString(culture)
					);
					builder.AppendFormat(
						"[Pantograph]{0}Enabled = {1}{0}UpResetTimeout = {2}{0}PantographLocation = {3}{0}{0}",
						Environment.NewLine,
						Pan.Enabled.ToString(culture),
						Pan.UpResetTimeout.ToString(culture),
						Pan.PantographLocation.ToString(culture)
					);
					builder.AppendFormat(
						"[TapChanger]{0}Enabled = {1}{0}{0}",
						Environment.NewLine,
						Tap.Enabled.ToString(culture)
					);
					builder.AppendFormat(
						"[Apc]{0}Enabled = {1}{0}RequiredVoltage = {2}{0}RequiredCurrent = {3}{0}ApcReceiverLocation = {4}{0}{0}",
						Environment.NewLine,
						Apc.Enabled.ToString(culture),
						Apc.RequiredVoltage.ToString(culture),
						Apc.RequiredCurrent.ToString(culture),
						Apc.ReceiverLocation.ToString(culture)
					);
					builder.AppendFormat(
						"[Horn]{0}CentreTimeout = {1}{0}",
						Environment.NewLine,
						CabControls.HornCentreTimeout.ToString(culture)
					);
					
					/* Create the new configurarion file and write all the lines to it */
					System.IO.File.WriteAllText(ConfigFile, builder.ToString(), new System.Text.UTF8Encoding(true));
				} else {
					/* Read the existing configuration file and parse its contents */
					string[] lines = System.IO.File.ReadAllLines(ConfigFile, new System.Text.UTF8Encoding());
					string section = "";
					string keyName;
					string valueData;
					for (int i = 0; i < lines.Length; i++) {
						lines[i].Trim();
						if (!lines[i].StartsWith(";", StringComparison.OrdinalIgnoreCase)) {
							/* This line isn't definitely a comment, so process it */
							if (lines[i].StartsWith("[", StringComparison.Ordinal) && lines[i].EndsWith("]", StringComparison.Ordinal)) {
								/* This is a section identifier */
								section = lines[i].Substring(1, lines[i].Length - 2).ToLowerInvariant();
								section = section.Trim();
							} else {
								if (lines[i].Contains("=")) {
									/* This is likely a key-value pair, so process the line */
									if (lines[i].Contains(";")) {
										/* Remove any comments at the end of the line */
										int j = lines[i].IndexOf(";");
										lines[i] = lines[i].Substring(0, j);
									}
									int k = lines[i].IndexOf("=");
									keyName = lines[i].Substring(0, k).ToLowerInvariant();
									keyName = keyName.Trim();
									valueData = lines[i].Substring(k + 1, lines[i].Length - k - 1).ToLowerInvariant();
									valueData = valueData.Trim();
									/* Switch to each section and the key-value pairs therein */
									switch (section) {
										case "options":
											switch (keyName) {
												case "debugmode":
													DebugMode = valueData == "false" ? false : true;
													break;
											}
											break;
										case "aisupport":
											switch (keyName) {
												case "enabled":
													AiSupportFeature.Enabled = valueData == "false" ? false : true;
													break;
												case "maximumdraactivationdistance":
													double val;
													if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
														AiSupportFeature.MaximumDraActivationDistance = val;
													}
													break;
											}
											break;
										case "interlockmanager":
											switch (keyName) {
												case "enabled":
													InterlockManager.Enabled = valueData == "false" ? false : true;
													break;
											}
											break;
										case "battery":
											switch (keyName) {
												case "enabled":
													{
														if (valueData == "true") {
															MainBattery.Enable();
														}
														break;
													}
												case "maximumoutputvoltage":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															MainBattery.MaximumOutputVoltage = val;
														}
														break;
													}
												case "maximumoutputcurrent":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															MainBattery.MaximumOutputCurrent = val;
														}
														break;
													}
												case "dischargerate":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															MainBattery.DischargeRate = val;
														}
														break;
													}
											}
											break;
										case "overhead":
											switch (keyName) {
												case "enabled":
													{
														if (valueData == "true") {
															OverheadSupply.Enable();
														}
														break;
													}
												case "maximumoutputvoltage":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															OverheadSupply.MaximumOutputVoltage = val;
														}
														break;
													}
												case "maximumoutputcurrent":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															OverheadSupply.MaximumOutputCurrent = val;
														}
														break;
													}
												case "maximuminputvoltage":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															OverheadSupply.MaximumInputVoltage = val;
														}
														break;
													}
												case "minimuminputvoltage":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															OverheadSupply.MinimumInputVoltage = val;
														}
														break;
													}
											}
											break;
										case "diesel":
											switch (keyName) {
												case "enabled":
													{
														if (valueData == "true") {
															Diesel.Enable();
														}
														break;
													}
												case "maximumoutputvoltage":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Diesel.MaximumOutputVoltage = val;
														}
														break;
													}
												case "maximumoutputcurrent":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Diesel.MaximumOutputCurrent = val;
														}
														break;
													}
												case "motorstartdelay":
													{
														int val;
														if (int.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Diesel.MotorStartDelay = val;
														}
														break;
													}
												case "enginestartdelay":
													{
														int val;
														if (int.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Diesel.EngineStartDelay = val;
														}
														break;
													}
												case "enginerundowndelay":
													{
														int val;
														if (int.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Diesel.EngineRunDownDelay = val;
														}
														break;
													}
												case "enginestalldelay":
													{
														int val;
														if (int.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Diesel.EngineStallDelay = val;
														}
														break;
													}
												case "stallprobability":
													{
														int val;
														if (int.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Diesel.StallProbability = val;
														}
														break;
													}
											}
											break;
										case "aws":
											switch (keyName) {
												case "enabled":
													{
														if (valueData == "true") {
															Aws.Enable();
														}
														break;
													}
												case "requiredvoltage":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Aws.RequiredVoltage = val;
														}
														break;
													}
												case "requiredcurrent":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Aws.RequiredCurrent = val;
														}
														break;
													}
												case "canceltimeout":
													{
														int val;
														if (int.TryParse(valueData, NumberStyles.Integer, culture, out val)) {
															Aws.CancelTimeout = val;
														}
														break;
													}
											}
											break;
										case "vigilance":
											switch (keyName) {
												case "enabled":
													{
														if (valueData == "true") {
															Vig.Enable();
														}
														break;
													}
												case "requiredvoltage":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Vig.RequiredVoltage = val;
														}
														break;
													}
												case "requiredcurrent":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Vig.RequiredCurrent = val;
														}
														break;
													}
												case "inactivitytimeout":
													{
														int val;
														if (int.TryParse(valueData, NumberStyles.Integer, culture, out val)) {
															Vig.InactivityTimeout = val;
														}
														break;
													}
												case "canceltimeout":
													{
														int val;
														if (int.TryParse(valueData, NumberStyles.Integer, culture, out val)) {
															Vig.CancelTimeout = val;
														}
														break;
													}
												case "reducedcycletime":
													{
														if (valueData == "true") {
															Vig.ReducedCycleTime = valueData == "false" ? false : true;
														}
														break;
													}
											}
											break;
										case "tpws":
											switch (keyName) {
												case "enabled":
													{
														if (valueData == "true") {
															Tpws.Enable();
														}
														break;
													}
												case "requiredvoltage":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Tpws.RequiredVoltage = val;
														}
														break;
													}
												case "requiredcurrent":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Tpws.RequiredCurrent = val;
														}
														break;
													}
												case "brakesappliedtimeout":
													{
														int val;
														if (int.TryParse(valueData, NumberStyles.Integer, culture, out val)) {
															Tpws.BrakesAppliedTimeout = val;
														}
														break;
													}
												case "tssoverridetimeout":
													{
														int val;
														if (int.TryParse(valueData, NumberStyles.Integer, culture, out val)) {
															Tpws.TssOverrideTimeout = val;
														}
														break;
													}
												case "osstimeout":
													{
														int val;
														if (int.TryParse(valueData, NumberStyles.Integer, culture, out val)) {
															Tpws.OssTimeout = val;
														}
														break;
													}
												case "indicatorblinkrate":
													{
														int val;
														if (int.TryParse(valueData, NumberStyles.Integer, culture, out val)) {
															Tpws.IndicatorBlinkRate = val;
														}
														break;
													}
											}
											break;
										case "dra":
											switch (keyName) {
												case "enabled":
													{
														if (valueData == "true") {
															Dra.Enable();
														}
														break;
													}
												case "requiredvoltage":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Dra.RequiredVoltage = val;
														}
														break;
													}
												case "requiredcurrent":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Dra.RequiredCurrent = val;
														}
														break;
													}
											}
											break;
										case "headlights":
											switch (keyName) {
												case "enabled":
													{
														if (valueData == "true") {
															Head.Enable();
														}
														break;
													}
												case "requiredvoltage":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Head.RequiredVoltage = val;
														}
														break;
													}
												case "requiredcurrent":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Head.RequiredCurrent = val;
														}
														break;
													}
											}
											break;
										case "taillights":
											switch (keyName) {
												case "enabled":
													{
														if (valueData == "true") {
															Tail.Enable();
														}
														break;
													}
												case "requiredvoltage":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Tail.RequiredVoltage = val;
														}
														break;
													}
												case "requiredcurrent":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Tail.RequiredCurrent = val;
														}
														break;
													}
											}
											break;
										case "blower":
											switch (keyName) {
												case "enabled":
													{
														if (valueData == "true") {
															Fan.Enable();
														}
														break;
													}
												case "requiredvoltage":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Fan.RequiredVoltage = val;
														}
														break;
													}
												case "requiredcurrent":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Fan.RequiredCurrent = val;
														}
														break;
													}
												case "delay":
													{
														int val;
														if (int.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Fan.Delay = val;
														}
														break;
													}
											}
											break;
										case "pantograph":
											switch (keyName) {
												case "enabled":
													{
														if (valueData == "true") {
															Pan.Enable();
														}
														break;
													}
												case "upresettimeout":
													{
														int val;
														if (int.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Pan.UpResetTimeout = val;
														}
														break;
													}
												case "pantographlocation":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Pan.PantographLocation = val;
														}
														break;
													}
											}
											break;
										case "tapchanger":
											switch (keyName) {
												case "enabled":
													{
														if (valueData == "true") {
															Tap.Enable();
														}
														break;
													}
											}
											break;
										case "apc":
											switch (keyName) {
												case "enabled":
													{
														if (valueData == "true") {
															Apc.Enable();
														}
														break;
													}
												case "requiredvoltage":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Apc.RequiredVoltage = val;
														}
														break;
													}
												case "requiredcurrent":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Apc.RequiredCurrent = val;
														}
														break;
													}
												case "apcreceiverlocation":
													{
														double val;
														if (double.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															Apc.ReceiverLocation = val;
														}
														break;
													}
											}
											break;
										case "horn":
											switch (keyName) {
												case "centretimeout":
													{
														int val;
														if (int.TryParse(valueData, NumberStyles.Float, culture, out val)) {
															CabControls.HornCentreTimeout = val;
														}
														break;
													}
											}
											break;
									}
								}
							}
						}
					}
				}
				success = true;
				exceptionRaised = null;
			} catch (Exception ex) {
				exceptionRaised = ex.Message.ToString();
				success = false;
			}
			return success;
		}
	}
}