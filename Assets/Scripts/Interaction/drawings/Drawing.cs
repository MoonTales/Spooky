using System;
using Managers;
using Player;
using UnityEngine;
using Types = System.Types;

public class Drawing : MonoBehaviour, IInteractable
{

    
    [SerializeField, Tooltip("What area does this item exist within?")] private Types.WorldLocation location = Types.WorldLocation.Bedroom;
    [SerializeField] private int drawingID;
    
    
    // Internal variables
    private bool _isPickedUp = false;
    
    // Internal Fields
    public string Prompt { get; } = "Examine Drawing";
    public bool CanInteract(Interactor interactor)
    {
        return true;
    }

    public void Start()
    {
        // check to see if we already have this drawing (if its contained in our inventory)
        if (PlayerInventory.Instance.HasDrawing(drawingID))
        {
            
            // Check what location we are in to determine if we should disable the in-world instance
            if (Types.WorldLocation.Bedroom == location)
            {
                DebugUtils.Log($"Player already has Drawing ID {drawingID} in inventory and is in the Bedroom, Maintaining it in the world.");
                gameObject.SetActive(true);
                return;
            }
            else if (Types.WorldLocation.Nightmare == location)
            {
                DebugUtils.Log($"Player already has Drawing ID {drawingID} in inventory and is in the Gallery, disabling in-world instance.");
                gameObject.SetActive(false);
                return;
            }
        }
        
        // if we dont have it in our inventory, we will set its default "state"
        if (Types.WorldLocation.Bedroom == location)
        {
            DebugUtils.Log($"Player already has Drawing ID {drawingID} in inventory and is in the Bedroom, Maintaining it in the world.");
            gameObject.SetActive(false);
            return;
        }
        else if (Types.WorldLocation.Nightmare == location)
        {
            DebugUtils.Log($"Player already has Drawing ID {drawingID} in inventory and is in the Gallery, disabling in-world instance.");
            gameObject.SetActive(true);
            return;
        }
    }

    public void Interact(Interactor interactor)
    {
        DebugUtils.Log($"Player interacted with Drawing ID {drawingID}");
        // create a new Drawing instance and add it to the player's inventory
        
        // if we already have it in the inventory, we don't need to add it again
        if (PlayerInventory.Instance.HasDrawing(drawingID))
        {
            DebugUtils.LogWarning($"Player already has Drawing ID {drawingID} in inventory, not adding again.");
            return;
        }
        else
        {
            PlayerInventory.Instance.AddDrawing(drawingID);
            // based on where we are, we might want to do different things here, for now we disable it
            gameObject.SetActive(false);
        }

    }


}
