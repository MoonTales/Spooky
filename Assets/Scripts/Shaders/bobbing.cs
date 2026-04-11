using System;
using System.Collections;
using Managers;
using Player;
using Unity.Cinemachine;
using UnityEngine;
using Types = System.Types;
using Interaction;
public class FloatingObject : MonoBehaviour
{
    [Header("Position Bobbing")]
    [SerializeField] private float bobSpeed = 0.7f;
    [SerializeField] private float bobHeight = 0.3f;

    [Header("Rotation Sway")]
    [SerializeField] private float rotSpeed = 0.2f;
    [SerializeField] private float rotAmount = 1.5f;

    Vector3 startPos;
    Quaternion startRot;

    void Start()
    {
        // Store starting transform values
        startPos = transform.position;
        startRot = transform.rotation;
        StartCoroutine(HouseBob());
    }

    IEnumerator HouseBob()
    {
        while(true)
        {
            // -------- Position Bobbing --------
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = startPos + Vector3.up * yOffset;

            // -------- Rotation Sway --------
            float xRot = Mathf.Sin(Time.time * rotSpeed) * rotAmount;
            float zRot = Mathf.Sin((Time.time + 0.8f) * rotSpeed) * rotAmount;

            Quaternion swayRotation = Quaternion.Euler(xRot, 0f, zRot);

            transform.rotation = startRot * swayRotation;

            yield return null;
        }
    }
}


/* The following script overrides default position and has gradual drift

using UnityEngine;

public class FloatingObject : MonoBehaviour
{

    public float bobPosSpeed = 1;
    public float bobPosAmount = 1;
    
    public float bobRotSpeed = 1;
    public float bobRotAmount = 1;

    // Update is called once per frame
    void Update()
    {
        float addToPos = (Mathf.Sin(Time.time * bobPosSpeed) * bobPosAmount);
        transform.position += Vector3.up * addToPos * Time.deltaTime;

        float xRot = (Mathf.Sin(Time.time * bobRotSpeed) * bobRotAmount);
    
        float zRot = (Mathf.Sin((Time.time -1.0f) * bobRotSpeed) * bobRotAmount);
    
        transform.eulerAngles = new Vector3(xRot, transform.eulerAngles.y, zRot);
    }
}
*/