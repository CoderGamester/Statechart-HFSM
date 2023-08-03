using System;

// ReSharper disable CheckNamespace

namespace GameLovers.StatechartMachine.Internal
{
	internal struct StateFactoryData
	{
		public Action<IStatechartEvent> StateChartMoveNextCall;
		public IStatechart Statechart;
	}
}