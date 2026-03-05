using UnityEngine;

public class BedroomLightSwitch : ToggleInteractable
{
    // this will just have a bit of extra feaatures, like flipping the switch, and dissabling the night light

    public GameObject nightLight;
    public GameObject flipSwitch;

    public override void Interact(Interactor interactor)
    {
        base.Interact(interactor);
        
        // now we will rotate the flip switch, by 90 degrees on the x axis, and toggle the night light
        if (flipSwitch != null)
        {
            flipSwitch.transform.Rotate(180f, 0f, 0f);
        }
        if (nightLight != null)
        {
            nightLight.SetActive(!nightLight.activeSelf);
        }
    }
}
