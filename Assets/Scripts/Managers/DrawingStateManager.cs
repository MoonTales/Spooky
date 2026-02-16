using System;
using System.Collections.Generic;
using Interaction.drawings;
using Player;
using UnityEngine;
using Types = System.Types;

namespace Managers
{
    
    [Serializable]
    struct DrawingTransformData
    {
        public int DrawingID;
        public UnityEngine.Vector3 Position;  // Use UnityEngine.Vector3
        public UnityEngine.Quaternion Rotation;  // Use UnityEngine.Quaternion
        public UnityEngine.Vector3 Scale;
        
        public override string ToString()
        {
            return $"DrawingTransformData(ID: {DrawingID}, Position: {Position}, Rotation: {Rotation}, Scale: {Scale})";
        }
    }
    
    /// <summary>
    /// Manager in charge of storing and handling the drawings positions between scenes
    /// </summary>
    public class DrawingStateManager : Singleton<DrawingStateManager>
    {
        
        // Internal list, to hold the drawings ID, and its current Transform
        private List<DrawingTransformData> _drawingTransformDataList = new List<DrawingTransformData>();
        
        // Cache of drawings in the current scene
        private List<Drawing> _drawingsInScene = new List<Drawing>();
        
        // Determine how many drawings are in the "correct" position
        private int totalNumberOfDrawings = 9; // hardcoded game value for now
        
        
        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();
            TrackSubscription(() => EventBroadcaster.OnWorldLocationChangedEvent += OnWorldLocationChanged,
                () => EventBroadcaster.OnWorldLocationChangedEvent -= OnWorldLocationChanged);
        }

        protected override void OnGameRestarted()
        {
            // since this is emulating a full reset, we will clear all drawing data
            _drawingTransformDataList.Clear();
        }

        private void OnWorldLocationChanged(Types.WorldLocation worldLocation)
        {
            if (worldLocation == Types.WorldLocation.Bedroom)
            {
                RestoreDrawingsToTransform();
            }
        }

        // Collect all drawings in the scene and update their transform data
        public void UpdateDrawingTransformData()
        {
            
            // Dont worry about optimization for now
            _drawingsInScene.Clear();
            Drawing[] allDrawings = FindObjectsByType<Drawing>(FindObjectsSortMode.None);

            foreach (Drawing drawing in allDrawings)
            {
                if (drawing.GetLocation() == Types.WorldLocation.Bedroom)
                {
                    _drawingsInScene.Add(drawing);
                }
            }
            
            
            // Update transform data for all drawings
            foreach (Drawing drawing in _drawingsInScene)
            {
                if (drawing == null){ continue;}
                
                UpdateOrAddDrawingTransform(drawing);
                // Check if the drawing is in the "correct" position (this is just a placeholder condition, replace with actual logic)
            }

            
            CheckForAllCorrectPlacements();
        }

        public int CheckForAllCorrectPlacements()
        {
            // loop through all of the drawings in the scene (only enabled ones)
            int count = 0;
            foreach (Drawing drawing in _drawingsInScene)
            {
                if (drawing == null){ continue;}
                UpdateOrAddDrawingTransform(drawing);
                // Check if the drawing is in the "correct" position (this is just a placeholder condition, replace with actual logic)
                if (drawing.IsInCorrectPosition())
                {
                    count++;
                }
            }
            if (count >= totalNumberOfDrawings)
            {
                Types.NotificationData data = new(
                    duration: 3.0f, 
                    messageKey: new TextKey { place = "Notifications", id = "AllDrawingsCorrect"},
                    messageOverride: $"All drawings are in the correct position! YOU WIN!!"
                );
                data.Send();
                
                // Now we will play credits
                SceneSwapper.Instance.SwapScene("Credits");
            }
            
            return count;
            
        }

        private void RestoreDrawingsToTransform()
        {
            // Dont worry about optimization for now
            _drawingsInScene.Clear();
            Drawing[] allDrawings = FindObjectsByType<Drawing>(FindObjectsSortMode.None);

            foreach (Drawing drawing in allDrawings)
            {
                if (drawing.GetLocation() == Types.WorldLocation.Bedroom)
                {
                    _drawingsInScene.Add(drawing);
                }
            }
            
            // Restore transform data for all drawings
            foreach (Drawing drawing in _drawingsInScene)
            {
                if (drawing == null){ continue;}
                
                // Find if this drawing exists in our data list
                int existingIndex = _drawingTransformDataList.FindIndex(d => d.DrawingID == drawing.GetUniqueDrawingID());
                if (existingIndex >= 0)
                {
                    DrawingTransformData data = _drawingTransformDataList[existingIndex];
                    drawing.transform.position = data.Position;
                    drawing.transform.rotation = data.Rotation;
                    drawing.transform.localScale = data.Scale;
                }
            }
            
            // this is called when we load in, so we can use it to see if we should Tick the world clock
            //TODO: this is VERY messy rn, I dont like it
            // all of these also only run if we are in the bedroom
            if (GameStateManager.Instance.GetCurrentWorldLocation() != Types.WorldLocation.Bedroom)
            {
                return;
            }
            int numberOfCorrectDrawings = PlayerInventory.Instance.GetDrawingCount();
            int numberOfDrawingsToAdvanceClock = GameStateManager.Instance.GetMaxDrawingsPerAct();
            // essentially, this could be 3, which means
            // 0-2 is Act 1, 3-5 is Act 2, and 6-9 is Act 3 (which we can win from)
            int currentHour = GameStateManager.Instance.GetCurrentWorldClockHour();
            // if numberOfCorrectDrawings is greater than
            if (currentHour == 1)
            {
                if (numberOfCorrectDrawings >= numberOfDrawingsToAdvanceClock)
                {
                    GameStateManager.Instance.SetWorldClockHour(currentHour + 1);
                }
            }else if (currentHour == 2)
            {
                if (numberOfCorrectDrawings >= numberOfDrawingsToAdvanceClock * 2)
                {
                    GameStateManager.Instance.SetWorldClockHour(currentHour + 1);
                }
            }
        }
        private void UpdateOrAddDrawingTransform(Drawing drawing)
        {
            // Find if this drawing already exists in our data list
            int existingIndex = _drawingTransformDataList.FindIndex(d => d.DrawingID == drawing.GetUniqueDrawingID());
            
            // Create the transform data
            DrawingTransformData data = new DrawingTransformData
            {
                DrawingID = drawing.GetUniqueDrawingID(),
                Position = drawing.transform.position,
                Rotation = drawing.transform.rotation,
                Scale = drawing.transform.localScale
            };
            
            if (existingIndex >= 0)
            {
                // Update existing entry
                _drawingTransformDataList[existingIndex] = data;
            }
            else
            {
                // Add new entry
                _drawingTransformDataList.Add(data);
            }
        }

        public bool IsAnyDrawingCurrentlyHeld()
        {
            
            // we will access GetCurrentlyHeldDrawing, which returns a Drawing if one is being held, or null if none are being held
            foreach (Drawing drawing in _drawingsInScene)
            {
                if (drawing.GetCurrentlyHeldDrawing())
                {
                    return true;
                }
            }
            return false;
        }
    }
}