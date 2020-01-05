// ReSharper disable CheckNamespace

namespace GameLovers.Statechart.Internal
{
	internal static class StatechartUtils
	{
		public static string RemoveGarbageFromStackTrace(string stackTrace)
		{
			var lineIdx = stackTrace.IndexOf('\n') + 1;
			do
			{
				stackTrace = stackTrace.Remove(0, lineIdx);
				lineIdx = stackTrace.IndexOf('\n') + 1;
			}
			while (stackTrace.Substring(0, lineIdx).Contains("StateMachine.Internal"));

			return stackTrace + "\n";
		}
	}
}
