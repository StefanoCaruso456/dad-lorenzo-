using System.Collections.Generic;
using UnityEngine;

namespace CrossHop.Core
{
    /// <summary>
    /// Lightweight, allocation-free object pool for a single prefab.
    /// Mobile-critical: we never Instantiate/Destroy during a run — everything
    /// spawned (lanes, obstacles, coins) is recycled through a pool.
    /// </summary>
    public sealed class ObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly Stack<GameObject> _idle = new();

        public ObjectPool(GameObject prefab, Transform parent = null, int prewarm = 0)
        {
            _prefab = prefab != null
                ? prefab
                : throw new System.ArgumentNullException(nameof(prefab));
            _parent = parent;

            for (int i = 0; i < prewarm; i++)
            {
                GameObject go = CreateInstance();
                go.SetActive(false);
                _idle.Push(go);
            }
        }

        /// <summary>Fetch an instance, activating it at the given pose.</summary>
        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject go = _idle.Count > 0 ? _idle.Pop() : CreateInstance();
            Transform t = go.transform;
            t.SetPositionAndRotation(position, rotation);
            go.SetActive(true);
            return go;
        }

        /// <summary>Return an instance to the pool for reuse.</summary>
        public void Release(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            if (_parent != null) go.transform.SetParent(_parent, false);
            _idle.Push(go);
        }

        private GameObject CreateInstance()
            => Object.Instantiate(_prefab, _parent);
    }
}
