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
        AudioManager.Instance.PlayFootstep("metal", transform);
    }
    // Update is called once per frame
    void Update()
    {
        /*
        if (Input.GetKeyDown(KeyCode.J))
        {
            InspectionSystem.Instance.StartInspection(gameObject);
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            InspectionSystem.Instance.EndInspection();
        }
        */
    }
}
