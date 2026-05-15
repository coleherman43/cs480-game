/*
Pickup Item script to be attached to pickup item prefab game objects
Opens control for shader toggling and pikcup logic from player child object.
*/

using UnityEngine;

public class PickupItem : MonoBehaviour
{
    private Renderer rend;
    private MaterialPropertyBlock mpb;
    
    //ID for shader parameter
    private static readonly int ThicknessID =
        Shader.PropertyToID("_thickness");

    [SerializeField] private int scoreValue = 10;
    // Value for highlighted state
    private float def = 1.06f;

    private void Awake()
    {
        //Grabbing relevant components
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
    }
    //Called by player child object when pickup enters or exits the radius
    public void toggleShader()
    {
        //Get property 
        rend.GetPropertyBlock(mpb);
        //toggle b/w highlight and un-highlight
        float current = mpb.GetFloat(ThicknessID);
        if(current == def)
        {
            mpb.SetFloat(ThicknessID, 0.8f);
        }else
        {
            mpb.SetFloat(ThicknessID, def);
        }

        rend.SetPropertyBlock(mpb);
    }
    //Called when player hits E and pickup is in radius
    public void OnPickup()
    {
        // Fire the event
        GameEvents.OnPickupCollected?.Invoke(scoreValue);

        // Destroy the object
        Destroy(gameObject);
    }
}
