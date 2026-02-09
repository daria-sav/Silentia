using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuControl : MonoBehaviour
{
    [SerializeField] private GameObject continueButton;
    private void Start()
    {
        string loadPath = Path.Combine(Application.persistentDataPath, SaveLoadManager.instance.folderName, SaveLoadManager.instance.fileName);
        if (File.Exists(loadPath))
        {
            continueButton.SetActive(true);
            EventSystem.current.firstSelectedGameObject = continueButton;
        }
        else
        {
            continueButton.SetActive(false);
        }
    }
    public void NewGame()
    {
        SaveLoadManager.instance.DeleteFolder(SaveLoadManager.instance.folderName);
        LevelManager.instance.LoadLevelString("Level1");
    }

    public void ContinueGame()
    {
        string loadPath = Path.Combine(Application.persistentDataPath, SaveLoadManager.instance.folderName, SaveLoadManager.instance.fileName);

        if (File.Exists(loadPath))
        {
            SpawnData spawnData = new SpawnData();

            SaveLoadManager.instance.Load(spawnData, SaveLoadManager.instance.folderName, SaveLoadManager.instance.fileName);
            string sceneToLoad = string.IsNullOrEmpty(spawnData.sceneName) ? "Level1" : spawnData.sceneName;

            LevelManager.instance.LoadLevelString(sceneToLoad);
        }
        else
        {
            SpawnData spawnData = new SpawnData();

            SaveLoadManager.instance.Save(spawnData, SaveLoadManager.instance.folderName, SaveLoadManager.instance.fileName);
            string sceneToLoad = string.IsNullOrEmpty(spawnData.sceneName) ? "Level1" : spawnData.sceneName;

            LevelManager.instance.LoadLevelString(sceneToLoad);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}