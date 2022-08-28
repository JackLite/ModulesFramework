using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Core
{
    public partial class DataWorld
    {
        private readonly Dictionary<Type, EcsTable> _data = new Dictionary<Type, EcsTable>();
        private readonly EcsTable<Entity> _entitiesTable = new EcsTable<Entity>();
        private int _entityCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity NewEntity()
        {
            ++_entityCount;
            var id = _entityCount;
            var entity = new Entity
            {
                Id = id,
                World = this
            };
            _entitiesTable.AddData(id, entity);
            return entity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent<T>(int eid, T component) where T : struct
        {
            GetEscTable<T>().AddData(eid, component);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent<T>(int id) where T : struct
        {
            GetEscTable<T>().Remove(id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(int id) where T : struct
        {
            return ref GetEscTable<T>().GetData(id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int[] GetLinearData<T>() where T : struct
        {
            return GetEscTable<T>().GetEntitiesId();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsTable<T> GetEscTable<T>() where T : struct
        {
            CreateTableIfNeed<T>();
            return (EcsTable<T>) _data[typeof(T)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Query<T> Select<T>() where T : struct
        {
            var table = GetEscTable<T>();
            var query = new Query<T>(this, table);
            return query;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsTable<T> CreateTableIfNeed<T>() where T : struct
        {
            var type = typeof(T);
            if (!_data.ContainsKey(type))
                _data[type] = new EcsTable<T>();
            return (EcsTable<T>) _data[type];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Entity GetEntity(int id)
        {
            return _entitiesTable.GetData(id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DestroyEntity(int id)
        {
            foreach (var table in _data.Values)
            {
                table.Remove(id);
            }
            _entitiesTable.Remove(id);
        }
    }
}