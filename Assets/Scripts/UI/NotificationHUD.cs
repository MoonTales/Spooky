using System;
using TMPro;
using UnityEngine;

public class NotificationHUD : Singleton<NotificationHUD>
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private TMP_Text _hudNotificationText; public TMP_Text GetNotificationText() { return _hudNotificationText; }

    void Start()
    {
        _hudNotificationText = transform.Find("NotificationText").GetComponent<TMP_Text>();
        _hudNotificationText.gameObject.SetActive(false);
    }

}
