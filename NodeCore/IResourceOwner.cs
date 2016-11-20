using System;

namespace NodeCore
{
	public interface IResourceOwner
	{
		void OwnResource(IDisposable disposable);
	}
}

