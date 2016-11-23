using System;

namespace Infrastructure
{
	public interface IResourceOwner : IDisposable
	{
		void OwnResource(IDisposable disposable);
	}
}

