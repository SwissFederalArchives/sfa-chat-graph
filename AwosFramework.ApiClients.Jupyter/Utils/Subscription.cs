using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Utils
{
	public class Subscription<T> : IDisposable
	{
		private readonly IObserver<T> _observer;
		private readonly Action<Subscription<T>>? _unsubscribe;
		public IObserver<T> Observer => _observer;

		public Subscription(IObserver<T> observer, Action<Subscription<T>>? unsubscribe)
		{
			_observer=observer;
			_unsubscribe=unsubscribe;
		}

		public void Dispose()
		{
			_unsubscribe?.Invoke(this);
		}
	}
}
