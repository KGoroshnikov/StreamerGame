using UnityEngine;
using UnityEngine.InputSystem;

public class Flashlight : MonoBehaviour
{
    [SerializeField] private bool flashlightEnabled;

    [SerializeField] private GameObject flashlightObj;

    [SerializeField] private InputActionReference toggleLight;
    private bool flashlightActive;

    void Update(){
        if (!flashlightEnabled) return;
        if (toggleLight.action.triggered){
            flashlightActive = !flashlightActive;
            flashlightObj.SetActive(flashlightActive);
        }
    }
}
