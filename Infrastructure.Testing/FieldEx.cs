using System;
using System.Runtime.CompilerServices;

namespace Infrastructure.Testing
{
	public class FieldEx<T, FieldType> where T : class
	{
		private static readonly ConditionalWeakTable<T, StrongBox<FieldType>> _values = new ConditionalWeakTable<T, StrongBox<FieldType>>();

		public static FieldType Get(T obj)
		{
			StrongBox<FieldType> box;
			if (!_values.TryGetValue(obj, out box))
				return default(FieldType);

			return box.Value;
		}

		public static void Set(T obj, FieldType value)
		{
			StrongBox<FieldType> box = _values.GetOrCreateValue(obj);
			box.Value = value;
		}
	}

	//public class HashFieldEx<T> : FieldEx<T, byte[]>
	//{
	//}

	//public class TxHashEx : HashFieldEx<Types.Transaction>
	//{
	//	public byte[] GetHash(Types.Transaction tx)
	//	{
	//		var hash = FieldEx<Types.Transaction, byte[]>.Get(tx);

	//		if (hash == default(byte[]))
	//		{
	//			hash = Merkle.transactionHasher.Invoke(tx);
	//			FieldEx<Types.Transaction, byte[]>.Set(tx, hash);
	//		}

	//		return hash;
	//	}
	//}

	//public class BkHashEx : HashFieldEx<Types.Block>
	//{
	//	public byte[] GetHash(Types.Block bk)
	//	{
	//		var hash = FieldEx<Types.Block, byte[]>.Get(bk);

	//		if (hash == default(byte[]))
	//		{
	//			hash = Merkle.blockHeaderHasher.Invoke(bk.header);
	//			FieldEx<Types.Block, byte[]>.Set(bk, hash);
	//		}

	//		return hash;
	//	}
	//}
}
