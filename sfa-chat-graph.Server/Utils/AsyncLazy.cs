namespace sfa_chat_graph.Server.Utils
{
	public class AsyncLazy<T> : IDisposable
	{
		private Task<T>? _initTask = null;
		private Func<Task<T>> _valueFactory;

		
		public Task<T> ValueAsync()
		{	
			if (_initTask == null)
				_initTask = _valueFactory();

			return _initTask;
		}

		public void Dispose()
		{
			if(IsValueCreated && Value is IDisposable disposable)
				disposable.Dispose();
		}

		public T Value => ValueAsync().GetAwaiter().GetResult();
		public bool IsValueCreated => _initTask != null && _initTask.Status == TaskStatus.RanToCompletion;

		public AsyncLazy(Func<Task<T>> valueFactory) : base()
		{
			_valueFactory = valueFactory;
			_initTask = new Task<T>(() => default(T));
		}
	}
}
