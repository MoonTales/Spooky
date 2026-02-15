using System;
using System.Collections;
using Managers;
using Player;
using Unity.Cinemachine;
using UnityEngine;
using Types = System.Types;


// EventBroadcaster.Broadcast_OnPlayerDamaged(10.0f); damage
//_hudSanityValueText.text = Mathf.RoundToInt(PlayerStats.Instance.GetPlayerStats().GetCurrentMentalHealth()).ToString(); check damage

/*

[System.Serializable]
public class DistanceSettings
{
    //Distance
    public float veryClose = 5f;
    public float close = 10f;
    public float midRange = 20f;
    public float far = 30f;

    //Damage
    public float veryCloseDmg = 10f;
    public float closeDmg = 5f;
    public float midRangeDmg = 2f;
    public float farDmg = 0.5f;
}

*/

public class TerrorRadius : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private Transform terrorAudioSourceTransform;

    // Internal variables such as timer intervals to check distance, and distance float
    private float timer;
    private float interval = 0.8f;
    private float distance;

    // Initialize settings
    // Distance
    public float veryClose = 5f;
    public float close = 10f;
    public float midRange = 20f;
    public float far = 30f;

    // Damage
    public float veryCloseDmg = 10f;
    public float closeDmg = 5f;
    public float midRangeDmg = 2f;
    public float farDmg = 0.5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        distance = PlayerManager.Instance.GetDistance(transform.position);

        bool isNightmare = GameStateManager.Instance != null
            && GameStateManager.Instance.GetCurrentWorldLocation() == Types.WorldLocation.Nightmare;

        if (isNightmare)
        {
            float terrorIntensity = CalculateNormalizedTerrorIntensity();
            EventBroadcaster.Broadcast_OnTerrorIntensityChanged(terrorIntensity, GetTerrorAudioSourceTransform());
        }
        else
        {
            EventBroadcaster.Broadcast_OnTerrorIntensityChanged(0f, null);
        }

        // Timer keeps damage cadence separate from audio updates.
        timer += Time.deltaTime;

        if (timer >= interval)
        {
            //Can manually set ranges to adjust the volume/visual/anxiety effects based on distance here
            terrorDamage();
            
            timer = 0f;
        }

    }


    public void terrorDamage()
    {
        // Calculate a damage amount based on distances
        if (distance <= veryClose)
        {
            EventBroadcaster.Broadcast_OnPlayerDamaged(veryCloseDmg);
            Debug.Log("Terror Damage: " + veryCloseDmg);
        } 
        else if (distance <= close)
        {
            EventBroadcaster.Broadcast_OnPlayerDamaged(closeDmg);
            Debug.Log("Terror Damage: " + closeDmg);
        }
        else if (distance <= midRange)
        {
            EventBroadcaster.Broadcast_OnPlayerDamaged(midRangeDmg);
            Debug.Log("Terror Damage: " + midRangeDmg);
        }
        else if (distance <= far)
        {
            EventBroadcaster.Broadcast_OnPlayerDamaged(farDmg);
            Debug.Log("Terror Damage: " + farDmg);
        }

    }

    private float CalculateNormalizedTerrorIntensity()
    {   
        // Returns terror as a percentage in [0, 1]
        if (distance <= veryClose)
        {
            return 1f;
        }

        if (distance >= far)
        {
            return 0f;
        }

        return Mathf.InverseLerp(far, veryClose, distance);
    }

    private Transform GetTerrorAudioSourceTransform()
    {
        // Default to this GameObject for quick testing scenes without a monster hierarchy.
        return terrorAudioSourceTransform != null ? terrorAudioSourceTransform : transform;
    }

    /*

    IEnumerator TerrorTimer()
    {
        


    }

    */

    /*

    public float calcDistance()
    {

        //Calculates the Euclidian distance between the monster and player
        //Fetch the player and object Vector3 location. Calculate and return the distance between them.


        GameObject player = PlayerManager.Instance.GetPlayer();
        float distanceToTerrorObject;

        distanceToTerrorObject = Vector3.Distance(transform.position, player.transform.position);

        return distanceToTerrorObject;

    }

    */

}

