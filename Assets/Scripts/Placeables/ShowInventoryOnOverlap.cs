using Managers;
using Player;
using UI;
using UnityEngine;
using Types = System.Types;

public class ShowInventoryOnOverlap : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    private BoxCollider _collider;
    void Start()
    {
        _collider = GetComponent<BoxCollider>();
        if (_collider == null)
        {
            Debug.LogError("ShowInventoryOnOverlap requires a BoxCollider component.");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // show the inventory
            PlayerHUDController.Instance.ShowInventory();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // hide the inventory
            PlayerHUDController.Instance.HideInventory();
        }
    }
}
