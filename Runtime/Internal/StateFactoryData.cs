using System;

// ReSharper disable CheckNamespace

namespace GameLovers.Statechart.Internal
{
	internal struct StateFactoryData
	{
		public Action<IStatechartEvent> StateChartMoveNextCall;
		public IStatechartDebug Statechart;
	}
}