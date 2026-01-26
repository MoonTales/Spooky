using System;
using System.Collections.Generic;
using Interaction.drawings;
using UnityEngine;

namespace Managers
{
    
    [Serializable]
    struct DrawingTransformData
    {
        public int DrawingID;
        public UnityEngine.Vector3 Position;  // Use UnityEngine.Vector3
        public UnityEngine.Quaternion Rotation;  // Use UnityEngine.Quaternion
        public UnityEngine.Vector3 Scale;
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


        protected override void OnGameStarted()
        {
            // Save every 5 seconds
            //InvokeRepeating(nameof(UpdateDrawingTransformData), 5f, 5f);
            
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.O))
            {
                UpdateDrawingTransformData();
            }
            if (Input.GetKeyDown(KeyCode.P))
            {
                RestoreDrawingTransformData();
            }
        }
        
        

        // Collect all drawings in the scene and update their transform data
        private void UpdateDrawingTransformData()
        {
            DebugUtils.LogSuccess("Saving drawing transform data...");
            
            // If first time, collect all drawings in the scene
            if (_drawingsInScene.Count == 0)
            {
                _drawingsInScene.Clear();
                Drawing[] drawingsInScene = GameObject.FindObjectsOfType<Drawing>();
                _drawingsInScene.AddRange(drawingsInScene);
            }
            
            // Update transform data for all drawings
            foreach (Drawing drawing in _drawingsInScene)
            {
                if (drawing == null) continue; // Skip if drawing was destroyed
                
                UpdateOrAddDrawingTransform(drawing);
            }
        }

        private void RestoreDrawingTransformData()
        {
            DebugUtils.LogSuccess("Restoring drawing transform data...");
            // find all drawings in the scene
            Drawing[] drawingsInScene = GameObject.FindObjectsOfType<Drawing>();
            foreach (Drawing drawing in drawingsInScene)
            {
                // find the corresponding transform data
                DrawingTransformData? data = _drawingTransformDataList.Find(d => d.DrawingID == drawing.GetDrawingID());
                if (data.HasValue)
                {
                    // restore the transform
                    drawing.transform.position = data.Value.Position;
                    drawing.transform.rotation = data.Value.Rotation;
                    drawing.transform.localScale = data.Value.Scale;
                }
            }
        }


        private void UpdateOrAddDrawingTransform(Drawing drawing)
        {
            // Find if this drawing already exists in our data list
            int existingIndex = _drawingTransformDataList.FindIndex(d => d.DrawingID == drawing.GetDrawingID());
            
            // Create the transform data
            DrawingTransformData data = new DrawingTransformData
            {
                DrawingID = drawing.GetDrawingID(),
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

    }
}