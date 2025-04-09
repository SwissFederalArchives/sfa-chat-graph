using AwosFramework.ApiClients.Jupyter.Utils;
using AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket
{
	public class ObservableSource<T> : ObservableBase<T>
	{
		private readonly SemaphoreSlim _subscriptionSemaphore = new(1);
		private readonly List<Subscription<T>> _subscriptions = new();
		public bool IsCompleted { get; private set; } = false;

		private void ThrowIfCompleted()
		{
			if (IsCompleted)
				throw new InvalidOperationException("Observable is completed");
		}

		public void NotifyCompleted()
		{
			IsCompleted = true;
			_subscriptions.ForEach(x => x.Observer.OnCompleted());
			_subscriptions.Clear();
			_subscriptionSemaphore.Dispose();
		}

		public void NotifyItem(T item)
		{
			ThrowIfCompleted();
			_subscriptionSemaphore.Wait();
			_subscriptions.ForEach(x => x.Observer.OnNext(item));
			_subscriptionSemaphore.Release();
		}

		private void Unsubscribe(Subscription<T> subscription)
		{
			if (IsCompleted == false)
			{
				_subscriptionSemaphore.Wait();
				_subscriptions.Remove(subscription);
				_subscriptionSemaphore.Release();
			}
		}

		protected override IDisposable SubscribeCore(IObserver<T> observer)
		{
			if (IsCompleted)
			{
				observer.OnCompleted();
				return new Subscription<T>(observer, null);
			}


			_subscriptionSemaphore.Wait();
			var subscription = new Subscription<T>(observer, Unsubscribe);
			_subscriptions.Add(subscription);
			_subscriptionSemaphore.Release();
			return subscription;
		}
	}
}
