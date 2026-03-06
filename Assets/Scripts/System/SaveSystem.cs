using System;
using System.Collections.Generic;
using System.Linq;
using Managers;
using UnityEngine;

///
/// There is currently a few set places where the game saves
/// 1. When the game loads a new scene (the save is AFTER we have loaded the new scene, so that we can save the new scene name in our save data)
/// 2. Whenever the player moves the drawings in the world
/// 3. Whenever the player finishes reading a letter
///
/// They all use the following function: SaveSystem.Instance.RequestSave(this);
/// 

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
        
        
        // this function will be able to be called from a function, to request we save their current data, at a different
        // time from the rest of the game (like a one off save). it will also take in their custom struct
        public void RequestSave(ISaveSystemInterface saveInterface)
        {
            saveInterface.SaveData();

            // now that each class has saved their info, write to disk
            string json = JsonUtility.ToJson(_currentSaveData);
            PlayerPrefs.SetString("SaveData", json);
            PlayerPrefs.Save();
            
            DebugUtils.LogSuccess("Game has been saved!");
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
        
        public SaveData GetCurrentSaveData()
        {
            return _currentSaveData;
        }
        public void ReadSaveFromDisk()
        {
            if (PlayerPrefs.HasKey("SaveData"))
            {
                string json = PlayerPrefs.GetString("SaveData");
                _currentSaveData = JsonUtility.FromJson<SaveData>(json);
            }
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