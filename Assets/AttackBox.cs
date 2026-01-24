using UnityEngine;
using System;
using Types = System.Types;

public class AttackBox : MonoBehaviour
{
    public float damageAmount;

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            EventBroadcaster.Broadcast_OnPlayerDamaged(damageAmount);
            GetComponent<Collider>().enabled = false;
        }
    }
}
