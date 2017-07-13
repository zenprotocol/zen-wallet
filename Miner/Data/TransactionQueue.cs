using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Consensus;

namespace Miner.Data
{
    using T = TransactionValidation.PointedTransaction;

    public class TransactionQueue
    {
        List<T> _List = new List<T>();
        int _Index = 0;
        int _Counter = 0;

        public bool IsStuck
        {
            get
            {
                return _Counter >= _List.Count;
            }
        }

        public T Take()
        {
            return IsStuck || _List.Count == 0 ? null : _List[_Index];
        }

        public void Next()
        {
            if (IsStuck) return;
            _Index++;
            if (_Index >= _List.Count) _Index = 0;
            _Counter++;
        }

        //public void Reset()
        //{
        //    _Counter = 0;
        //}

        public void Clear()
        {
            _List.Clear();
            _Index = 0;
            _Counter = 0;
        }

        public void Push(T t)
        {
            if (_List.Count == 0)
            {
                _List.Insert(0,t);
                _Index = 0;
            } else {
                if (IsStuck) return;
                _List.Insert(_Index > 0 ? _Index - 1 : _List.Count - 1, t);
                if (_Index > 0) _Index++;
            }
            _Counter = 0;
        }

        public void Remove()
        {
            if (IsStuck) return;
            _List.RemoveAt(_Index);
            if (_Index == _List.Count) _Index = 0;
            _Counter = 0;
        }
    }
}