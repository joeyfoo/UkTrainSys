using System;
using OpenBveApi.Runtime;

namespace Plugin {
	/// <summary>The interface to be implemented by the plugin.</summary>
	public partial class Plugin : IRuntime {
		/// <summary>Writes a string of text to the debug log file.</summary>
		/// <param name="reportEntry">The string to write to the debug log file.</param>
		internal static void ReportLogEntry(string reportEntry) {
			if (Plugin.DebugMode) {
				try {
					if (!System.IO.File.Exists(DebugLogFile)) {
						using (System.IO.StreamWriter streamWriter = System.IO.File.CreateText(DebugLogFile)) {
							streamWriter.WriteLine(
								string.Format(
									"{2}{0}New Debug Log: Created {1}{0}{2}",
									Environment.NewLine,
									System.DateTime.Now.ToString(),
									"=========================================="
								)
							);
						}
					}
					using (System.IO.StreamWriter streamWriter = System.IO.File.AppendText(DebugLogFile)) {
						streamWriter.WriteLine(reportEntry);
					}
				} catch (Exception) {
				}
			}
		}
	}
}