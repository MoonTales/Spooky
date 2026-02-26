using System;
using System.Collections.Generic;
using UnityEngine;
using Types = System.Types;

namespace Player
{
    public class PlayerInventory : Singleton<PlayerInventory>, ISaveSystemInterface<PlayerInventory.PlayerInventorySaveData>
    {
        // Customization variablews for the feel of the game
        [SerializeField] private int _maxDrawingsPerNight = 3; public int GetMaxDrawingsPerNight() { return _maxDrawingsPerNight; }
        
        // Internal variables to help this new system
        private int _currentDrawingsThisNight = 0; public int GetCurrentDrawingsThisNight() { return _currentDrawingsThisNight; }
        private List<int> _collectedDrawingsThisNight = new List<int>();
        
        
        // Store only the IDs of collected drawings
        private readonly HashSet<int> _collectedDrawingIDs = new HashSet<int>(); public HashSet<int> GetCollectedDrawingIDs() { return new HashSet<int>(_collectedDrawingIDs); } public List<int> GetAllCollectDrawingIds() { return new List<int>(_collectedDrawingIDs); } // Return a copy to prevent external modification
        // we will also need to store "dropped" drawings
        private readonly HashSet<int> _droppedDrawingIDs = new HashSet<int>();  public HashSet<int> GetDroppedDrawingIDs() { return new HashSet<int>(_droppedDrawingIDs); } // return a copy to prevent external modification
    
        
        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            TrackSubscription(() => EventBroadcaster.OnWorldLocationChangedEvent += OnWorldLocationChanged,
                () => EventBroadcaster.OnWorldLocationChangedEvent -= OnWorldLocationChanged);
            TrackSubscription(() => EventBroadcaster.OnPlayerHealthStateChanged += OnPlayerHealthStateChanged,
                () => EventBroadcaster.OnPlayerHealthStateChanged -= OnPlayerHealthStateChanged);
        }

        private void OnPlayerHealthStateChanged(Types.PlayerMentalState newMentalState)
        {
            // we also need  to take note of what "zone" the player is in, as that will determine where these will need to be respawned
            
            // if the player "dies" we will want to remove any drawings they have collected from the inventory
            if (newMentalState == Types.PlayerMentalState.Breakdown)
            {
                // loop through all of the collected drawings this night, and remove them from the inventory
                foreach (int drawingID in _collectedDrawingsThisNight)
                {
                    _droppedDrawingIDs.Add(drawingID);
                    RemoveDrawing(drawingID);
                }
                
            }
        }
        public void ClearDroppedDrawings()
        {
            _droppedDrawingIDs.Clear();
        }

        private void OnWorldLocationChanged(Types.WorldLocation worldLocation)
        {
            // we want to check some logic anytime we leave or enter a location
            // whenever we return to the bedroom, we want to reset how many drawins we have collected this night
            if (worldLocation == Types.WorldLocation.Bedroom)
            {
                _currentDrawingsThisNight = 0;
                _collectedDrawingsThisNight.Clear();
            }
        }
        
        protected override void OnGameRestarted()
        {
            // Since we are emulating a full reset, clear the inventory
            ClearInventory();
        }
        
        // Public methods
        public bool HasDrawing(int drawingID)
        {
            return _collectedDrawingIDs.Contains(drawingID);
        }
        
        public void AddDrawing(int drawingID)
        {
            Types.NotificationData data = new(
                duration: 1.0f, 
                messageKey: new TextKey { place = "Notifications", id = "CollectedDrawingSuccess"},
                messageOverride: "Collected drawing!",
                shouldOnlyShowOnce: false
            );
            data.Send();
            if (_collectedDrawingIDs.Add(drawingID))
            {
                _currentDrawingsThisNight ++;
                _collectedDrawingsThisNight.Add(drawingID);
            }
        }

        public bool CanAddDrawing()
        {
            if (_currentDrawingsThisNight >= _maxDrawingsPerNight)
            {
                Types.NotificationData data = new(
                    duration: 3.0f, 
                    messageKey: new TextKey { place = "Notifications", id = "CollectedDrawingFail"},
                    messageOverride: $"Unable to hold more drawings. You have reached the maximum for the night."
                );
                data.Send();
                return false;
            }

            return true;
        }
        
        private void RemoveDrawing(int drawingID)
        {
            _collectedDrawingIDs.Remove(drawingID);
        }

        private void ClearInventory()
        {
            _collectedDrawingIDs.Clear();
        }
        
        public int GetDrawingCount()
        {
            return _collectedDrawingIDs.Count;
        }
        
        public HashSet<int> GetAllDrawingIDs()
        {
            return new HashSet<int>(_collectedDrawingIDs); // Return a copy
        }
        
        public void DebugListInventory()
        {
            DebugUtils.Log($"Player Inventory Contents ({_collectedDrawingIDs.Count} drawings):");
            foreach (var drawingID in _collectedDrawingIDs)
            {
                DebugUtils.Log($"- Drawing ID: {drawingID}");
            }
        }

        public void DebugAdjustDrawingsThisNight(int delta)
        {
            if (delta == 0)
            {
                return;
            }

            _currentDrawingsThisNight = Mathf.Clamp(_currentDrawingsThisNight + delta, 0, _maxDrawingsPerNight);
            DebugUtils.Log($"Debug: Current drawings this night set to {_currentDrawingsThisNight}/{_maxDrawingsPerNight}.");
        }

        // ------------------------
        // Save System Interface
        // -------------------------
        public struct PlayerInventorySaveData
        {
            public List<int> collectedDrawingIDs;
            public int currentDrawingsThisNight;
            public List<int> droppedDrawingIDs;
        }

        public string SaveId => "PlayerInventory";
        public PlayerInventorySaveData OnSave()
        {
            DebugUtils.LogSuccess("Saving PlayerInventory... we currently have " + _collectedDrawingIDs.Count + " drawings in our inventory, and " + _currentDrawingsThisNight + " drawings collected this night.");
            return new PlayerInventorySaveData
            {
                collectedDrawingIDs = new List<int>(_collectedDrawingIDs),
                currentDrawingsThisNight = _currentDrawingsThisNight,
                droppedDrawingIDs = new List<int>(_droppedDrawingIDs),
                
                
            };
        }

        public void OnLoad(PlayerInventorySaveData data)
        {
            _collectedDrawingIDs.Clear();
            foreach (int drawingID in data.collectedDrawingIDs)
            {
                _collectedDrawingIDs.Add(drawingID);
            }
            _currentDrawingsThisNight = data.currentDrawingsThisNight;
            _droppedDrawingIDs.Clear();
            foreach (int drawingID in data.droppedDrawingIDs)
            {
                _droppedDrawingIDs.Add(drawingID);
            }
            
            DebugUtils.LogSuccess("Player Inventory loaded... we currently have " + _collectedDrawingIDs.Count + " drawings in our inventory, and " + _currentDrawingsThisNight + " drawings collected this night.");
        }
    }
}
