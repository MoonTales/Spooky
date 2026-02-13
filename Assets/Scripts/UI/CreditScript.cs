using UnityEngine;
using UnityEngine.UI;
using System;
using Managers;
using Types = System.Types;

public class CreditScript : MonoBehaviour
{
    public float scrollSpeed = 100f;
    int timer = 1280;
    private RectTransform rectTransform;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.MainMenu); // used main menu for now can/probably should be changed to something else?
        rectTransform = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Tab))
        {
            scrollSpeed = 300f; 
            timer -= 3;
        } else
        {
            scrollSpeed = 100f;
            timer -=1;
        }

        if (timer <= 0)
        {
            EventBroadcaster.Broadcast_GameStateChanged(Types.GameState.MainMenu); 
            SceneSwapper.Instance.SwapScene("MainMenu");
        }

        rectTransform.anchoredPosition += new Vector2(0, scrollSpeed * Time.deltaTime);
    }
}
