using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// This class will have primary control of creating a confirmtion "popup" that will appear on the screen.
    ///
    /// it will automatically hook up actions to the pop up, so when someone calls for a popup, if we press "confirm"
    /// it will invoke the function they passed in
    /// and if they press cancel, it will just close the pop up and do nothing else.
    /// </summary>
    public class UiPopupConfirmation : Singleton<UiPopupConfirmation>
    {

        


        public void RequestPopupConfirmation(string displayMessage, Action onConfirm)
        {
            // we will load from the resources folder, a prefab of the confirmation popup
            GameObject popupPrefab = Resources.Load<GameObject>("Prefabs/UI/PopupConfirmation");
            /*
             * This prefab needs to have the following 3 fields:
             * TMP_TEXT: DisplayText - this is where we will set the display message to the player
             * BUTTON: ConfirmButton - this is the button that the player will press to confirm the action, and we will add a listener to it to invoke the onConfirm action
             * BUTTON: CancelButton - this is the button that the player will press to cancel the action, and we will add a listener to it to just close the pop up
             */
            
            if(popupPrefab == null)
            {
                DebugUtils.LogError("Could not find PopupConfirmation prefab in Resources/Prefabs/UI/PopupConfirmation! Make sure it exists and is in the correct folder.");
                return;
            }
            
            // otherwise, we know we have it, lets populate it
            // update this to be recursive soon, so it can be found anywhere
            GameObject popupInstance = Instantiate(popupPrefab, transform);
            Transform canvas = popupInstance.transform.Find("Canvas");
            TMP_Text displayText = canvas.Find("DisplayText").GetComponent<TMP_Text>();
            Button confirmButton = canvas.Find("ConfirmButton").GetComponent<Button>();
            Button cancelButton = canvas.Find("CancelButton").GetComponent<Button>();
            
            // if we are missing any of the components, we have a problem LOL
            if (displayText == null || confirmButton == null || cancelButton == null)
            {
                DebugUtils.LogError("PopupConfirmation prefab is missing one of the required components! Make sure it has a TMP_Text called DisplayText, and two Buttons called ConfirmButton and CancelButton.");
                Destroy(popupInstance);
                return;
            }
            
            // hook up to our buttons
            displayText.text = displayMessage;
            confirmButton.onClick.AddListener(() => OnConfirmButtonClicked(popupInstance, onConfirm));
            cancelButton.onClick.AddListener(() => OnCancelButtonClicked(popupInstance));
        }
        
        private void OnCancelButtonClicked(GameObject popupInstance)
        {
            // we will just destroy the pop up, and do nothing else
            Destroy(popupInstance);
        }

        private void OnConfirmButtonClicked(GameObject popupInstance, Action onConfirm)
        {
            // we will invoke the onConfirm action, and then destroy the pop up
            onConfirm?.Invoke();
            Destroy(popupInstance);
        }
    }
}
