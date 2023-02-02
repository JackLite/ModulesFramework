using System;
using System.Runtime.CompilerServices;
using ModulesFramework.Exceptions;

namespace ModulesFramework.Data
{
    public abstract class EcsTable
    {
        public abstract EntityData[] EntitiesData { get; }
        public abstract object GetDataObject(int eid);
        public abstract bool Contains(int eid);
        public abstract void Remove(int eid);
    }

    public class EcsTable<T> : EcsTable where T : struct
    {
        private T[] _table;
        private int _index;
        private int[] _tableMap;
        private EntityData[] _entityData;

        public override EntityData[] EntitiesData => _entityData;

        public EcsTable()
        {
            _table = new T[64];
            _index = 0;
            _tableMap = new int[64];
            _entityData = new EntityData[64];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddData(int eid, in T data)
        {
            if (_index >= _table.Length)
            {
                Array.Resize(ref _table, _table.Length * 2);
            }
            while (eid >= _tableMap.Length)
            {
                Array.Resize(ref _tableMap, _tableMap.Length * 2);
                Array.Resize(ref _entityData, _tableMap.Length);
            }
            
            _table[_index] = data;
            _tableMap[eid] = _index;
            _entityData[eid] = new EntityData { eid = eid, isActive = true};
            ++_index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetData(int eid)
        {
            if (!Contains(eid))
                throw new DataNotExistsInTableException<T>(eid);
            return ref _table[_tableMap[eid]];
        }

        /// <summary>
        /// Only for internal usage!
        /// This method is for debugging. You should never use it in production code.
        /// </summary>
        /// <param name="eid">Id of Entity</param>
        /// <returns>Boxed struct T</returns>
        /// <seealso cref="GetData"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override object GetDataObject(int eid)
        {
            return _table[_tableMap[eid]];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Remove(int eid)
        {
            if (eid >= _tableMap.Length)
                return;
            _tableMap[eid] = int.MaxValue;
            ref var ed = ref _entityData[eid];
            ed.isActive = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Contains(int eid)
        {
            if (eid >= _tableMap.Length)
                return false;
            return _entityData[eid].isActive;
        }
    }
}