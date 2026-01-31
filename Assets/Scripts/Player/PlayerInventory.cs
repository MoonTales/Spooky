using System;
using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    public class PlayerInventory : Singleton<PlayerInventory>
    {
        // Store only the IDs of collected drawings
        private readonly HashSet<int> _collectedDrawingIDs = new HashSet<int>();
    
        
        
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
            if (_collectedDrawingIDs.Add(drawingID))
            {
                DebugUtils.LogSuccess($"Added Drawing ID {drawingID} to Player Inventory. Total Drawings: {_collectedDrawingIDs.Count}");
            }
            else
            {
                DebugUtils.LogWarning($"Drawing ID {drawingID} is already in Player Inventory.");
            }
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