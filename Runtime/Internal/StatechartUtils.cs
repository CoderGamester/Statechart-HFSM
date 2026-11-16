// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine.Internal
{
	/// <summary>
	/// Helper class for the statechart to work properly
	/// </summary>
	internal static class StatechartUtils
	{
		/// <summary>
		/// Removes the leading stack frames that belong to <see cref="System.Environment"/> and to this package, so the
		/// trace starts at the user code that declared the state or transition.
		/// </summary>
		public static string RemoveGarbageFromStackTrace(string stackTrace)
		{
			var lines = stackTrace.Split('\n');
			var first = 0;

			while (first < lines.Length && IsPackageFrame(lines[first]))
			{
				first++;
			}

			return string.Join("\n", lines, first, lines.Length - first) + "\n";
		}

		private static bool IsPackageFrame(string line)
		{
			return line.Contains("System.Environment") ||
			       line.Contains("GameLovers.StatechartMachine.Internal.") ||
			       line.Contains("GameLovers.StatechartMachine.Statechart");
		}
	}
}
