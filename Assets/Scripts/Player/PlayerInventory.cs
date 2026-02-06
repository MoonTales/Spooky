using System;
using System.Collections.Generic;
using UnityEngine;
using Types = System.Types;

namespace Player
{
    public class PlayerInventory : Singleton<PlayerInventory>
    {
        // Customization variablews for the feel of the game
        [SerializeField] private int _maxDrawingsPerNight = 3;
        [SerializeField] private int _maxDrawingsPerAct = 3;
        
        // Internal variables to help this new system
        private int _currentDrawingsThisNight = 0;
        
        
        // Store only the IDs of collected drawings
        private readonly HashSet<int> _collectedDrawingIDs = new HashSet<int>();
    
        
        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            TrackSubscription(() => EventBroadcaster.OnWorldLocationChangedEvent += OnWorldLocationChanged,
                () => EventBroadcaster.OnWorldLocationChangedEvent -= OnWorldLocationChanged);
        }

        private void OnWorldLocationChanged(Types.WorldLocation worldLocation)
        {
            // we want to check some logic anytime we leave or enter a location
            // whenever we return to the bedroom, we want to reset how many drawins we have collected this night
            if (worldLocation == Types.WorldLocation.Bedroom)
            {
                _currentDrawingsThisNight = 0;
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
                messageOverride: $"Collected a Drawing"
            );
            data.Send();
            if (_collectedDrawingIDs.Add(drawingID))
            {
                DebugUtils.LogSuccess($"Added Drawing ID {drawingID} to Player Inventory. Total Drawings: {_collectedDrawingIDs.Count}");
            }
            else
            {
                DebugUtils.LogWarning($"Drawing ID {drawingID} is already in Player Inventory.");
            }
            _currentDrawingsThisNight ++;
        }

        public bool CanAddDrawing()
        {
            if (_currentDrawingsThisNight >= _maxDrawingsPerNight)
            {
                DebugUtils.LogWarning($"Cannot add drawing. Reached max drawings for the night ({_maxDrawingsPerNight}).");
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
        
        public void RemoveDrawing(int drawingID)
        {
            if (_collectedDrawingIDs.Remove(drawingID))
            {
                DebugUtils.LogSuccess($"Removed Drawing ID {drawingID} from Player Inventory. Total Drawings: {_collectedDrawingIDs.Count}");
            }
            else
            {
                DebugUtils.LogWarning($"Drawing ID {drawingID} is not in Player Inventory.");
            }
        }
        
        public void ClearInventory()
        {
            _collectedDrawingIDs.Clear();
            DebugUtils.LogSuccess("Cleared Player Inventory.");
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
        
        public void Update()
        {
            // For testing purposes, press 'I' to list inventory contents
            if (Input.GetKeyDown(KeyCode.I))
            {
                DebugListInventory();
            }
        }
    }
}