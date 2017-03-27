using System;
using System.Collections.Generic;
using Gdk;
using Infrastructure;

namespace Wallet
{
	public class ImagesCache : Singleton<ImagesCache>
	{
		Dictionary<string, Gdk.Pixbuf> _IconsCache = new Dictionary<string, Pixbuf>();

		public Gdk.Pixbuf GetIcon(string image)
		{
			if (!_IconsCache.ContainsKey(image))
			{
				Pixbuf pixbuf = null;

				try
				{
					pixbuf = new Pixbuf(image);
				}
				catch
				{
					Console.WriteLine("missing image file: " + image);
					return null;
				}

				if (pixbuf != null)
				{
					_IconsCache[image] = pixbuf.ScaleSimple(32, 32, InterpType.Hyper);
				}
			}

			return _IconsCache[image];
		}
	}
}
