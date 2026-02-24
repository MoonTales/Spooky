using System.Collections.Generic;
using Managers;
using Player;
using UnityEngine;


namespace System
{
    
    [System.Serializable]
    public struct SaveData
    {
        public int currentAct;
        public List<int> drawingsCollectedIds;
        public List<SavableObject> misc_data;
    }

    [System.Serializable]
    public struct SavableObject
    {
        public string id;
        public object data;
    }
    

    public class SaveSystem : Singleton<SaveSystem>
    {
        
        private SaveData _currentSaveData;
        public void SaveGame()
        {
            SaveData saveData = new SaveData
            {
                currentAct = GameStateManager.Instance.GetCurrentWorldClockHour(),
                drawingsCollectedIds = PlayerInventory.Instance.GetAllCollectDrawingIds(),
                misc_data = new List<SavableObject>()
                // add other data you want to save here
            };

            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString("SaveData", json);
            PlayerPrefs.Save();
            DebugUtils.LogSuccess("Game saved!");
        }

        public void SaveData(SaveData data)
        {
            // check if the ID is present within the save data, if it is, we will update it, if not, we will add it to the 
            //list of misc_data
        }
    }
}
