using System;
using System.Collections.Generic;

namespace ProjectMinkowski.Rendering;

public static class EntityManager {
    public static List<RenderableEntity> Entities = new();
    private static Dictionary<Type, HashSet<RenderableEntity>> _typeIndex = new();
    private static List<RenderableEntity> _spawnQueue = new();
    private static List<RenderableEntity> _despawnQueue = new();

    public static void Spawn(RenderableEntity entity) {
        _spawnQueue.Add(entity);
    }

    public static void Despawn(RenderableEntity entity) {
        _despawnQueue.Add(entity);
    }

    public static void ProcessQueues() {
        // Spawn
        foreach (var entity in _spawnQueue) {
            Entities.Add(entity);
            var type = entity.GetType();
            if (!_typeIndex.TryGetValue(type, out var set)) {
                set = new HashSet<RenderableEntity>();
                _typeIndex[type] = set;
            }
            set.Add(entity);
        }
        _spawnQueue.Clear();

        // Despawn
        foreach (var entity in _despawnQueue) {
            Entities.Remove(entity);
            var type = entity.GetType();
            if (_typeIndex.TryGetValue(type, out var set)) {
                set.Remove(entity);
            }
        }
        _despawnQueue.Clear();
    }

    //To iterate specific types (e.g., all BulletTracer), use RenderableEntity.Manager.GetAll<BulletTracer>().
    public static IEnumerable<T> GetAll<T>() where T : RenderableEntity { 
        if (_typeIndex.TryGetValue(typeof(T), out var set)) {
            foreach (var e in set) yield return (T)e;
        }
    }

    public static void ClearAll() {
        Entities.Clear();
        _typeIndex.Clear();
        _spawnQueue.Clear();
        _despawnQueue.Clear();
    }
}

