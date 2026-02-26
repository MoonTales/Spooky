using System;
using System.Collections.Generic;
using System.Linq;
using Managers;
using UnityEngine;

namespace System
{
    [System.Serializable]
    public struct SaveData
    {
        public List<SavableObject> saveData;
    }

    [System.Serializable]
    public struct SavableObject
    {
        public string Id;
        public string Data;
    }

    public class SaveSystem : Singleton<SaveSystem>
    {
        private SaveData _currentSaveData;

        protected override void Awake()
        {
            base.Awake();
            _currentSaveData = new SaveData { saveData = new List<SavableObject>() };
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.G)){
                SaveGame();
            }
            if (Input.GetKeyDown(KeyCode.H)){
                LoadGame();
            }
        }

        // ------------------------
        // Public API functions
        // ------------------------
        public void SaveData(SavableObject data)
        {
            // check if the data.id is already present in our save data
            int index = _currentSaveData.saveData.FindIndex(s => s.Id == data.Id);
            if (index >= 0)
            {
                // if it is, we want to update the existing data
                _currentSaveData.saveData[index] = data;
            }
            else
            {
                // if it is not, we want to add it to our save data
                _currentSaveData.saveData.Add(data);
            }
        }

        public SavableObject? GetData(string id)
        {
            int index = _currentSaveData.saveData.FindIndex(s => s.Id == id);
            return index >= 0 ? _currentSaveData.saveData[index] : null;
        }

        // -------------------------
        // Public functions for saving
        // -------------------------
        public void SaveGame()
        {
            // find all of the scripts that inherit from the SaveSystemInterface
            // call the save function on each of these scripts
            foreach (var saveInterface in GetAllSaveInterfaces())
                saveInterface.SaveData();

            // now that each class has saved their info, write to disk
            string json = JsonUtility.ToJson(_currentSaveData);
            PlayerPrefs.SetString("SaveData", json);
            PlayerPrefs.Save();
            
            DebugUtils.LogSuccess("Game has been saved!");
            
        }

        public void LoadGame()
        {
            // read from this file and populate our save data struct
            if (PlayerPrefs.HasKey("SaveData"))
            {
                string json = PlayerPrefs.GetString("SaveData");
                _currentSaveData = JsonUtility.FromJson<SaveData>(json);
            }

            // when the load function is called, we will find all scripts across the entire project
            // that inherit from the SaveSystemInterface, and we will call the load function
            foreach (var saveInterface in GetAllSaveInterfaces())
            {
                saveInterface.LoadData();
            }
            DebugUtils.LogSuccess("Game has been loaded!");
        }

        public void DeleteSaveData()
        {
            // delete the save data from disk and clear our current save data struct
            PlayerPrefs.DeleteKey("SaveData");
            _currentSaveData = new SaveData { saveData = new List<SavableObject>() };
        }

        private List<ISaveSystemInterface> GetAllSaveInterfaces()
        {
            return FindObjectsOfType<MonoBehaviour>().OfType<ISaveSystemInterface>().ToList();
        }

        public bool DoesSaveGameExist()
        {
            return PlayerPrefs.HasKey("SaveData");
        }
    }

    // Non-generic base interface — allows SaveSystem to find and call all saveable objects
    public interface ISaveSystemInterface
    {
        void SaveData();
        void LoadData();
    }

    // Generic version — implementing classes use this for type safety
    public interface ISaveSystemInterface<T> : ISaveSystemInterface
    {
        string SaveId { get; }

        void ISaveSystemInterface.SaveData()
        {
            SavableObject savableObject = new SavableObject
            {
                Id = SaveId,
                Data = JsonUtility.ToJson(OnSave())
            };
            SaveSystem.Instance.SaveData(savableObject);
        }

        void ISaveSystemInterface.LoadData()
        {
            SavableObject? data = SaveSystem.Instance.GetData(SaveId);
            if (data.HasValue)
                OnLoad(JsonUtility.FromJson<T>(data.Value.Data));
        }

        T OnSave();
        void OnLoad(T data);
    }
}