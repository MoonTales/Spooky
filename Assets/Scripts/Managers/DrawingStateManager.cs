using System;
using System.Collections.Generic;
using Interaction.drawings;
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
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.O))
            {
                UpdateDrawingTransformData();
            }
            if (Input.GetKeyDown(KeyCode.K))
            {
                // debug print all of the drawing transform data
                foreach (DrawingTransformData data in _drawingTransformDataList)
                {
                    Debug.Log(data.ToString());
                }
                if (_drawingTransformDataList.Count == 0)
                {
                    Debug.Log("No drawing transform data saved.");
                }
            }
            if (Input.GetKeyDown(KeyCode.X))
            {
                RestoreDrawingsToTransform();
            }
        }
        
        

        // Collect all drawings in the scene and update their transform data
        public void UpdateDrawingTransformData()
        {
            DebugUtils.LogSuccess("Saving drawing transform data...");
            
            // Dont worry about optimization for now
            _drawingsInScene.Clear();
            Drawing[] allDrawings = FindObjectsOfType<Drawing>();

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
            }
        }

        private void RestoreDrawingsToTransform()
        {
            // Dont worry about optimization for now
            _drawingsInScene.Clear();
            Drawing[] allDrawings = FindObjectsOfType<Drawing>();

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
                    DebugUtils.LogSuccess($"Restoring Transform for Drawing ID: {drawing.GetUniqueDrawingID()}");
                    DebugUtils.LogSuccess($"Current Transform: {drawing.transform.position}, Rotation: {drawing.transform.rotation}, Scale: {drawing.transform.localScale}");
                    DebugUtils.LogSuccess($"Saved Transform: {_drawingTransformDataList[existingIndex].Position}, Rotation: {_drawingTransformDataList[existingIndex].Rotation}, Scale: {_drawingTransformDataList[existingIndex].Scale}");
                    DrawingTransformData data = _drawingTransformDataList[existingIndex];
                    drawing.transform.position = data.Position;
                    drawing.transform.rotation = data.Rotation;
                    drawing.transform.localScale = data.Scale;
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