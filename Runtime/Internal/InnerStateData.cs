// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine.Internal
{
	/// <summary>
	/// This data objects contains the basic information about the current states managed by the statechart
	/// </summary>
	internal class InnerStateData
	{
		public IStateInternal InitialState;
		public IStateInternal CurrenState;
		public IStateFactoryInternal NestedFactory;
		public bool ExecuteExit;
		public bool ExecuteFinal;
	}
}