using System;
using System.Collections;
using Managers;
using Player;
using Unity.Cinemachine;
using UnityEngine;
using Types = System.Types;


// EventBroadcaster.Broadcast_OnPlayerDamaged(10.0f); damage
//_hudSanityValueText.text = Mathf.RoundToInt(PlayerStats.Instance.GetPlayerStats().GetCurrentMentalHealth()).ToString(); check damage

public class terrorRadius : MonoBehaviour
{

    // Internal variables such as timer intervals to check distance, and distance float
    private float timer;
    private float interval = 0.5f;
    private float distance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // Timer so that we're not recalculating distance constantly, tune interval as needed
        timer += Time.deltaTime;

        if (timer >= interval)
        {
            distance = calcDistance();
            Debug.Log("Distance to Player: " + distance);


            //Can manually set ranges to adjust the volume/visual/anxiety effects based on distance here
            
            


            //
            
            timer = 0f;
        }

    }

    public float calcDistance()
    {

        //Calculates the Euclidian distance between the monster and player
        //Fetch the player and object Vector3 location. Calculate and return the distance


        GameObject player = PlayerManager.Instance.GetPlayer();
        float distanceToTerrorObject;

        distanceToTerrorObject = Vector3.Distance(transform.position, player.transform.position);

        return distanceToTerrorObject;

    }

}

