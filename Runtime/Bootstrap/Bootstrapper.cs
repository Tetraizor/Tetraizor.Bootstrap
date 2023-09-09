using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Tetraizor.Bootstrap.Base;
using Tetraizor.MonoSingleton;
using Tetraizor.DebugUtils;

namespace Tetraizor.Bootstrap
{
    public class Bootstrapper : MonoSingleton<Bootstrapper>
    {
        #region Properties

        [Header("System Properties")]
        [SerializeField] private List<GameObject> _systems = new(); // Must be assigned by hand to contain all System prefabs.
        [SerializeField] private List<GameObject> _subsystems = new(); // Must be assigned by hand to contain all Subsystem prefabs.


        [Header("Common Scene Indices")]
        [SerializeField] private int _bootSceneIndex = 0; // Scene that system loading happens.
        [SerializeField] private int _systemSceneIndex = 1; // Scene that all the systems will load in.

        private int _loadedSystemCount = 0;
        public int LoadedSystemCount => _loadedSystemCount;

        [Header("Events")]
        public readonly UnityEvent<IPersistentSystem, float> LoadProgressChangeEvent = new UnityEvent<IPersistentSystem, float>();
        public readonly UnityEvent<IPersistentSystem> SystemLoadFinishEvent = new UnityEvent<IPersistentSystem>();
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
            DebugBus.LogPrint("Starting to load systems...");

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

            DebugBus.LogPrint("System container scene created and set active.");

            // Instantiate and load each system.
            foreach (GameObject systemPrefab in _systems)
            {
                double startTime = Time.realtimeSinceStartupAsDouble;

                GameObject persistentSystemInstance = InstantiateSystem(systemPrefab);
                IPersistentSystem persistentSystem = persistentSystemInstance.GetComponent<IPersistentSystem>();

                if (persistentSystem == null)
                {
                    DebugBus.LogError($"{systemPrefab.name} does not implement the interface 'IPersistentScene'. " +
                                                      $"This system will be ignored.");
                    continue;
                }

                // Load subsystems.
                foreach (GameObject subsystemPrefab in _subsystems)
                {
                    IPersistentSubsystem subsystem = subsystemPrefab.GetComponent<IPersistentSubsystem>();

                    if (subsystem == null)
                    {
                        DebugBus.LogError($"Subsystem '{subsystemPrefab.name}' does not contain a IPersistentSubsystem, prefab will be ignored.");
                        continue;
                    }

                    // If subsystem target name matches, init it. 
                    if (subsystem.GetSystemName().CompareTo(persistentSystem.GetName()) == 0)
                    {
                        GameObject subsystemGameObject = Instantiate(subsystemPrefab, persistentSystemInstance.transform);
                        subsystemGameObject.name = subsystemPrefab.name + "(Plugin)";
                    }
                }

                yield return persistentSystem.LoadSystem();

                _loadedSystemCount++;

                double endTime = Time.realtimeSinceStartupAsDouble;

                SystemLoadFinishEvent?.Invoke(persistentSystem);
                DebugBus.LogPrint($"{persistentSystem.GetName()} finished loading in {(int)((endTime - startTime) * 1000)} milliseconds.");
            }

            // Reset active scene back to boot scene.
            SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(_bootSceneIndex));

            yield return new WaitForEndOfFrame();

            BootCompleteEvent?.Invoke();

            DebugBus.LogSuccess("Finished loading all systems.");
        }

        public void UpdateLoadingState(IPersistentSystem system, float loadingPercentage)
        {
            float totalLoadingPercentage =
                ((float)_loadedSystemCount / _systems.Count) + loadingPercentage / _systems.Count;

            LoadProgressChangeEvent?.Invoke(system, totalLoadingPercentage);
        }

        // Get System from prefabs 
        private GameObject InstantiateSystem(GameObject systemPrefab)
        {
            GameObject systemInstance = Instantiate(systemPrefab, Vector3.zero, Quaternion.identity);

            systemInstance.name = systemPrefab.name;

            return systemInstance;
        }

        #endregion
    }
}