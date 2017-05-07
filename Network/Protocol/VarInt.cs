using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public class CompactVarInt 
	{
		private ulong _Value = 0;
		private int _Size;
		public CompactVarInt(int size)
		{
			_Size = size;
		}
		public CompactVarInt(ulong value, int size)
		{
			_Value = value;
			_Size = size;
		}

		public ulong ToLong()
		{
			return _Value;
		}
	}


	//https://en.bitcoin.it/wiki/Protocol_specification#Variable_length_integer
	public class VarInt 
	{
		private byte _PrefixByte = 0;
		private ulong _Value = 0;

		public VarInt()
			: this(0)
		{

		}
		public VarInt(ulong value)
		{
			this._Value = value;
			if(_Value < 0xFD)
				_PrefixByte = (byte)(int)_Value;
			else if(_Value <= 0xffff)
				_PrefixByte = 0xFD;
			else if(_Value <= 0xffffffff)
				_PrefixByte = 0xFE;
			else
				_PrefixByte = 0xFF;
		}

		public ulong ToLong()
		{
			return _Value;
		}
	}
}
