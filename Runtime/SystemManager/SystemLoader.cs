using System;
using System.Collections;
using System.Collections.Generic;
using Tetraizor.MonoSingleton;
using UnityEngine;
using Tetraizor.SystemManager.Base;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Tetraizor.SystemManager
{
    public class SystemLoader : MonoSingleton<SystemLoader>
    {
        #region Properties

        [Header("System Properties")] 
        [SerializeField] private List<GameObject> _systems = new();

        [Header("Common Scene Indices")]
        [SerializeField] private int _bootSceneIndex = 0;
        [SerializeField] private int _systemSceneIndex = 1;

        private int _loadedSystemCount = 0;

        public readonly UnityEvent SystemLoadingCompleteEvent = new UnityEvent();
        public readonly UnityEvent<string> SendLoadStateMessageEvent = new UnityEvent<string>();
        public readonly UnityEvent<IPersistentSystem, float> SystemLoadingStateChangeEvent = new UnityEvent<IPersistentSystem, float>();

        #endregion

        protected override void Init()
        {
            base.Init();
            StartLoadingSystems();
        }

        private void StartLoadingSystems()
        {
            StartCoroutine(LoadSystemsAsync());
        }

        private IEnumerator LoadSystemsAsync()
        {
            SendLoadStateMessageEvent?.Invoke("System loading started.");

            // Load empty scene to insert systems into.
            AsyncOperation systemSceneLoadingOperation =
                SceneManager.LoadSceneAsync(_systemSceneIndex, LoadSceneMode.Additive);
            systemSceneLoadingOperation.allowSceneActivation = true;

            while (!systemSceneLoadingOperation.isDone)
            {
                yield return null;
            }

            // Make system scene active to create System prefabs in.
            SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(_systemSceneIndex));

            SendLoadStateMessageEvent?.Invoke("Systems scene created and set active.");
            
            // Instantiate and load each system.
            foreach (GameObject systemPrefab in _systems)
            {
                IPersistentSystem persistentSystem = LoadSystem(systemPrefab);

                if (persistentSystem == null)
                {
                    SendLoadStateMessageEvent?.Invoke($"{systemPrefab.name} does not implement the interface 'IPersistentScene'. " +
                                                      $"This system will be ignored.");
                    continue;
                }

                yield return persistentSystem.LoadSystem();

                _loadedSystemCount++;

                SendLoadStateMessageEvent?.Invoke($"{persistentSystem.GetName()} finished loading.");
            }

            // Reset active scene back to boot scene.
            SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(_bootSceneIndex));

            print(SceneManager.GetActiveScene().name);

            yield return new WaitForEndOfFrame();

            SystemLoadingCompleteEvent?.Invoke();

            SendLoadStateMessageEvent?.Invoke("Finished loading all the systems. Loading menu scene.");
        }

        public void UpdateLoadingState(IPersistentSystem system, float loadingPercentage)
        {
            float totalLoadingPercentage =
                ((float)_loadedSystemCount / _systems.Count) + loadingPercentage / _systems.Count;

            SystemLoadingStateChangeEvent?.Invoke(system, totalLoadingPercentage);
        }

        private IPersistentSystem LoadSystem(GameObject systemPrefab)
        {
            GameObject systemInstance = Instantiate(systemPrefab, Vector3.zero, Quaternion.identity);

            systemInstance.name = systemPrefab.name;

            IPersistentSystem persistentSystem = systemInstance.GetComponent<IPersistentSystem>();

            return persistentSystem;
        }
    }
}