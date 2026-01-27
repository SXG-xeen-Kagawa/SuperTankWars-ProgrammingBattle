using UnityEngine;
using UnityEngine.Pool;

namespace SXG2025
{

    public class CannonShellPool
    {
        const int POOL_SIZE = 64;

        private ObjectPool<CannonShell> m_pool;

        private Transform m_objectRootTr = null;


        public CannonShellPool()
        {
            m_pool = new ObjectPool<CannonShell>(
                // 生成処理 
                () => Object.Instantiate(PrefabHolder.Instance.CannonShellPrefab, m_objectRootTr),
                // Get時 
                bullet => bullet.gameObject.SetActive(true),
                // Release時 
                bullet => bullet.gameObject.SetActive(false),
                // Destroy時 
                bullet => Object.Destroy(bullet.gameObject),
                collectionCheck: true,
                defaultCapacity: POOL_SIZE,
                maxSize: POOL_SIZE
                );
        }

        public void SetObjectRootTr(Transform rootTr)
        {
            m_objectRootTr = rootTr;
        }

        public CannonShell GetData()
        {
            return m_pool.Get();
        }

        public void ReleaseData(CannonShell shell)
        {
            m_pool.Release(shell);
        }


    }


}

