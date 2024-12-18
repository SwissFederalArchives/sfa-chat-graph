using VDS.RDF.Storage;
using VDS.RDF.Storage.Management;
using VDS.RDF.Storage.Management.Provisioning;

namespace sfa_chat_graph.Server.RDF
{

	class DummyTemplate : IStoreTemplate
	{
		public string ID { get; set; }

		public string TemplateName => ID;

		public string TemplateDescription => "Dummy Template";

		public IEnumerable<string> Validate()
		{
			return Enumerable.Empty<string>();
		}
	}

	public class OntoextStorageServer : IAsyncStorageServer
	{
		public string Endpoint { get; init; }
		private HttpClient _client;

		public IOBehaviour IOBehaviour => IOBehaviour.IsReadOnly;

		public OntoextStorageServer(string endpoint)
		{
			this.Endpoint = endpoint;
			_client = new HttpClient()
			{
				BaseAddress = new Uri(new Uri(Endpoint), "rest")
			};
		}

		public void ListStores(AsyncStorageCallback callback, object state)
		{
			ListStoresAsync(CancellationToken.None).ContinueWith(t =>
			{
				if (t.IsFaulted)
				{
					callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.ListStores, t.Exception), state);
				}
				else
				{
					callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.ListStores, t.Result), state);
				}
			});
		}

		public async Task<IEnumerable<string>> ListStoresAsync(CancellationToken cancellationToken)
		{
			var response = await _client.GetAsync("repositories");
			response.EnsureSuccessStatusCode();
			var repositories = await response.Content.ReadFromJsonAsync<OntotextRepositoryModel[]>();
			return repositories.Select(x => x.Id);
		}

		public void GetDefaultTemplate(string id, AsyncStorageCallback callback, object state)
		{
			callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.NewTemplate, id, new DummyTemplate() { ID = id }), state);
		}

		public Task<IStoreTemplate> GetDefaultTemplateAsync(string id, CancellationToken cancellationToken)
		{
			return Task.FromResult<IStoreTemplate>(new DummyTemplate() { ID = id });
		}

		public void GetAvailableTemplates(string id, AsyncStorageCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<IStoreTemplate>> GetAvailableTemplatesAsync(string id, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public void CreateStore(IStoreTemplate template, AsyncStorageCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		public Task<string> CreateStoreAsync(IStoreTemplate template, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public void DeleteStore(string storeID, AsyncStorageCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		public Task DeleteStoreAsync(string storeId, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public void GetStore(string storeId, AsyncStorageCallback callback, object state)
		{
			var store = new OntotextStorage(this, Endpoint, storeId);
			callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.GetStore, storeId, store), state);
		}

		public Task<IAsyncStorageProvider> GetStoreAsync(string storeId, CancellationToken cancellationToken)
		{
			return Task.FromResult<IAsyncStorageProvider>(new OntotextStorage(this, Endpoint, storeId));
		}

		public void Dispose()
		{
			_client.Dispose();
		}
	}
}
