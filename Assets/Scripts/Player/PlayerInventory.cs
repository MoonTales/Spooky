using System;
using System.Collections.Generic;
using UnityEngine;
using Types = System.Types;

namespace Player
{
    public class PlayerInventory : Singleton<PlayerInventory>
    {
        // Customization variablews for the feel of the game
        [SerializeField] private int _maxDrawingsPerNight = 3; public int GetMaxDrawingsPerNight() { return _maxDrawingsPerNight; }
        
        // Internal variables to help this new system
        private int _currentDrawingsThisNight = 0; public int GetCurrentDrawingsThisNight() { return _currentDrawingsThisNight; }
        private List<int> _collectedDrawingsThisNight = new List<int>();
        
        
        // Store only the IDs of collected drawings
        private readonly HashSet<int> _collectedDrawingIDs = new HashSet<int>();
        // we will also need to store "dropped" drawings
        private readonly HashSet<int> _droppedDrawingIDs = new HashSet<int>();
    
        
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
        
    }
}