using Managers;
using UnityEngine;

public class AlarmClock : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //InvokeRepeating(nameof(PlayAlarm), 0f, 1f);
    }


    private void PlayAlarm()
    {
        AudioManager.Instance.PlayPlayerWalkingMetal(1,0.2f,transform);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
