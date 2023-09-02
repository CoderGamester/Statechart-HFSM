// ReSharper disable CheckNamespace

namespace GameLoversEditor.StatechartMachine.Tests
{
	/// <summary>
	/// Mocking interface to check method calls received
	/// </summary>
	public interface IMockCaller
	{
		void InitialOnExitCall(int id);
		void FinalOnEnterCall(int id);
		void StateOnEnterCall(int id);
		void StateOnExitCall(int id);
		void OnTransitionCall(int id);
	}
}
