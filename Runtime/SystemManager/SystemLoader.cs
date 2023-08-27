using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tetraizor.SystemManager.Base;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Tetraizor.SystemManager
{
    public class SystemLoader : MonoBehaviour
    {
        [SerializeField] private List<GameObject> _systems = new();

        [SerializeField] private int _bootSceneIndex = 0;
        [SerializeField] private int _systemSceneIndex = 1;
        [SerializeField] private int _mainMenuSceneIndex = 2;

        public UnityEvent SystemLoadingCompleteEvent;
        
        private void Start()
        {
            StartLoadingSystems();
        }

        private void StartLoadingSystems()
        {
            StartCoroutine(LoadSystemsAsync());
        }

        private IEnumerator LoadSystemsAsync()
        {
            Debug.Log("Starting to load systems...");
            SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(_systemSceneIndex));

            foreach (GameObject systemPrefab in _systems)
            {
                GameObject systemInstance = Instantiate(systemPrefab, Vector3.zero, Quaternion.identity);
                IPersistentSystem persistentSystem = systemInstance.GetComponent<IPersistentSystem>();

                if (persistentSystem == null)
                {
                    Debug.LogWarning($"{systemInstance.name} does not implement the interface 'IPersistentScene'. " +
                                     $"This system will be ignored.");
                    
                    continue;
                }

                yield return persistentSystem.LoadSystem();
                Debug.Log($"{persistentSystem.GetName()} finished loading.");
            }

            Debug.Log("Finished loading all the systems. Loading menu scene.");
        }
    }
}