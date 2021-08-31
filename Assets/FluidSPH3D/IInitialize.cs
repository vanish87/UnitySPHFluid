namespace UnityTools.Common
{
	public interface IInitialize
	{
		bool Inited { get; }
		void Init();
		void Deinit();
	}
}