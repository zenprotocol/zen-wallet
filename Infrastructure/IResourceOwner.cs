using System;

namespace Infrastructure
{
	public interface IResourceOwner
	{
		void OwnResource(IDisposable disposable);
	}
}

