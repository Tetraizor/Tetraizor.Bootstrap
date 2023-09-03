using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Tetraizor.Bootstrap.Base;
using Tetraizor.MonoSingleton;

namespace Tetraizor.Bootstrap
{
    public class Bootstrapper : MonoSingleton<Bootstrapper>
    {
        #region Properties

        [Header("System Properties")]
        [SerializeField] private List<GameObject> _systems = new(); // Must be assigned by hand to contain all System prefabs.

        [Header("Common Scene Indices")]
        [SerializeField] private int _bootSceneIndex = 0; // Scene that system loading happens.
        [SerializeField] private int _systemSceneIndex = 1; // Scene that all the systems will load in.

        private int _loadedSystemCount = 0;
        public int LoadedSystemCount => _loadedSystemCount;

        [Header("Events")]
        public readonly UnityEvent<IPersistentSystem, float> LoadProgressChangeEvent = new UnityEvent<IPersistentSystem, float>();
        public readonly UnityEvent<IPersistentSystem> SystemLoadFinishEvent = new UnityEvent<IPersistentSystem>();
        public readonly UnityEvent<string> MessageSendEvent = new UnityEvent<string>();
        public readonly UnityEvent BootCompleteEvent = new UnityEvent();

        #endregion

        #region Base Methods

        protected override void Init()
        {
            base.Init();
            StartLoadingSystems();
        }

        #endregion

        #region Load Methods

        private void StartLoadingSystems()
        {
            StartCoroutine(LoadSystemsAsync());
        }

        private IEnumerator LoadSystemsAsync()
        {
            MessageSendEvent?.Invoke("Starting to load systems...");

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

            MessageSendEvent?.Invoke("System container scene created and set active.");

            // Instantiate and load each system.
            foreach (GameObject systemPrefab in _systems)
            {
                int startTime = DateTime.Now.Millisecond;

                IPersistentSystem persistentSystem = LoadSystem(systemPrefab);

                if (persistentSystem == null)
                {
                    MessageSendEvent?.Invoke($"{systemPrefab.name} does not implement the interface 'IPersistentScene'. " +
                                                      $"This system will be ignored.");
                    continue;
                }

                yield return persistentSystem.LoadSystem();

                _loadedSystemCount++;

                int endTime = DateTime.Now.Millisecond;
                SystemLoadFinishEvent?.Invoke(persistentSystem);
                MessageSendEvent?.Invoke($"{persistentSystem.GetName()} finished loading in {(endTime - startTime)} milliseconds.");
            }

            // Reset active scene back to boot scene.
            SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(_bootSceneIndex));

            yield return new WaitForEndOfFrame();

            BootCompleteEvent?.Invoke();

            MessageSendEvent?.Invoke("Finished loading all the systems.");
        }

        public void UpdateLoadingState(IPersistentSystem system, float loadingPercentage)
        {
            float totalLoadingPercentage =
                ((float)_loadedSystemCount / _systems.Count) + loadingPercentage / _systems.Count;

            LoadProgressChangeEvent?.Invoke(system, totalLoadingPercentage);
        }

        // Get System from prefabs 
        private IPersistentSystem LoadSystem(GameObject systemPrefab)
        {
            GameObject systemInstance = Instantiate(systemPrefab, Vector3.zero, Quaternion.identity);

            systemInstance.name = systemPrefab.name;

            IPersistentSystem persistentSystem = systemInstance.GetComponent<IPersistentSystem>();

            return persistentSystem;
        }

        #endregion
    }
}