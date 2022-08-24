using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unity.Multiplayer.Samples.Utilities
{
    /// <summary>
    /// Custom spawning component to be added to a scene GameObject. This component collects NetworkObjects in a scene
    /// marked by a special tag, collects their Transform data, destroys their prefab instance, and performs the dynamic
    /// spawning of said objects during Netcode for GameObject's (Netcode) OnNetworkSpawn() callback.
    /// </summary>
    [RequireComponent(typeof(NetcodeHooks))]
    public class NetworkObjectSpawner : MonoBehaviour
    {
        [SerializeField]
        NetcodeHooks m_NetcodeHooks;

        [SerializeField]
        SpawnObjectData m_SpawnObjectDataPrefab;

        [SerializeField]
        List<SpawnObjectData> m_SpawnObjectData;

        const string k_NetworkObjectSpawnerCollectableTag = "NetworkObjectSpawnerCollectable";

        void Awake()
        {
            m_NetcodeHooks.OnNetworkSpawnHook += OnNetworkSpawn;
        }

        void OnDestroy()
        {
            if (m_NetcodeHooks)
            {
                m_NetcodeHooks.OnNetworkSpawnHook -= OnNetworkSpawn;
            }
        }

        void OnNetworkSpawn()
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                enabled = false;
                return;
            }

            StartCoroutine(WaitToSpawnNetworkObjects());
        }

        IEnumerator WaitToSpawnNetworkObjects()
        {
            // must wait for Netcode's OnNetworkSpawn() sweep before dynamically spawning
            yield return new WaitForEndOfFrame();
            SpawnNetworkObjects();
        }

        void SpawnNetworkObjects()
        {
            for (int i = m_SpawnObjectData.Count - 1; i >= 0; i--)
            {
                var spawnedGameObject = Instantiate(m_SpawnObjectData[i].prefabReference,
                    m_SpawnObjectData[i].transform.position,
                    m_SpawnObjectData[i].transform.rotation,
                    null);

                spawnedGameObject.transform.localScale = m_SpawnObjectData[i].transform.lossyScale;
                var spawnedNetworkObject = spawnedGameObject.GetComponent<NetworkObject>();
                spawnedNetworkObject.Spawn();

                Destroy(m_SpawnObjectData[i].gameObject);
            }
        }

#if UNITY_EDITOR
        public void CollectTaggedPrefabInstances()
        {
            var prefabStage = PrefabStageUtility.GetPrefabStage(gameObject);

            var root = prefabStage.prefabContentsRoot;

            var networkObjects = root.GetComponentsInChildren<NetworkObject>();
            var taggedNetworkObjects = networkObjects.Where(obj => obj.CompareTag(k_NetworkObjectSpawnerCollectableTag));

            foreach (var editorOnlyObject in taggedNetworkObjects)
            {
                var pathToPrefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(editorOnlyObject);
                var original =
                    PrefabUtility.GetCorrespondingObjectFromSourceAtPath(editorOnlyObject, pathToPrefab);

                var instantiated = PrefabUtility.InstantiatePrefab(m_SpawnObjectDataPrefab.gameObject);
                var instantiatedPrefab = instantiated as GameObject;

                if (instantiatedPrefab)
                {
                    instantiatedPrefab.transform.SetPositionAndRotation(editorOnlyObject.transform.position,
                        editorOnlyObject.transform.rotation);

                    instantiatedPrefab.transform.localScale = editorOnlyObject.transform.lossyScale;
                    instantiatedPrefab.transform.SetParent(root.gameObject.transform);

                    var spawnedObjectData = instantiatedPrefab.GetComponent<SpawnObjectData>();
                    spawnedObjectData.prefabReference = original.gameObject;
                    instantiatedPrefab.name += $"({original.name})";

                    m_SpawnObjectData.Add(spawnedObjectData);

                    // destroy scene prefab instance
                    DestroyImmediate(editorOnlyObject.gameObject, true);

                    PrefabUtility.SaveAsPrefabAsset(root, prefabStage.assetPath, out var success);
                }
            }
        }
    }

    [CustomEditor(typeof(NetworkObjectSpawner))]
    public class NetworkObjectSpawnerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var networkObjectSpawner = (NetworkObjectSpawner)target;
            if (PrefabStageUtility.GetCurrentPrefabStage() &&
                GUILayout.Button("Collect tagged prefab instances"))
            {
                networkObjectSpawner.CollectTaggedPrefabInstances();
            }
        }
    }
#endif
}
