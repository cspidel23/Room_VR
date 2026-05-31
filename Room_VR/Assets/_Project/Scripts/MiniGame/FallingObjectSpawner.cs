using System.Collections;
using UnityEngine;

namespace RoomVR.MiniGame
{
    public class FallingObjectSpawner : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Prefab with FallingTarget component.")]
        GameObject m_TargetPrefab;

        [SerializeField]
        [Tooltip("Seconds between each spawn.")]
        float m_SpawnInterval = 1.5f;

        [SerializeField]
        [Tooltip("Spawn area half-size.")]
        float m_SpawnHalfSize = 3.5f;

        [SerializeField]
        MiniGameController m_Controller;

        Coroutine m_SpawnRoutine;

        public void StartSpawning(int totalTargets)
        {
            StopSpawning();
            m_SpawnRoutine = StartCoroutine(SpawnRoutine(totalTargets));
        }

        public void StopSpawning()
        {
            if (m_SpawnRoutine != null)
            {
                StopCoroutine(m_SpawnRoutine);
                m_SpawnRoutine = null;
            }
        }

        IEnumerator SpawnRoutine(int totalTargets)
        {
            for (var i = 0; i < totalTargets; i++)
            {
                SpawnTarget();
                yield return new WaitForSeconds(m_SpawnInterval);
            }
        }

        void SpawnTarget()
        {
            if (m_TargetPrefab == null)
                return;

            var x = Random.Range(-m_SpawnHalfSize, m_SpawnHalfSize);
            var z = Random.Range(-m_SpawnHalfSize, m_SpawnHalfSize);
            var spawnPos = new Vector3(x, 0f, z);

            var instance = Instantiate(m_TargetPrefab, transform);
            instance.transform.localPosition = spawnPos;

            var target = instance.GetComponent<FallingTarget>();
            target?.Initialize(m_Controller);
        }
    }
}
