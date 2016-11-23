using System;
using System.Collections.Generic;

namespace Infrastructure
{
	public class ResourceOwner : IResourceOwner
	{
		List<IDisposable> disposables = new List<IDisposable>();
			
		public void Dispose()
		{
			foreach (var disposable in disposables)
				disposable.Dispose();
		}

		public void OwnResource(IDisposable disposable)
		{
			disposables.Add(disposable);
		}
	}
}

