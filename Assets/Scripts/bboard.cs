using UnityEngine;

public class RandomBillboardTexture : MonoBehaviour
{
    [SerializeField] private Texture2D[] textures;

    private void Start()
    {
        if (textures == null || textures.Length == 0)
            return;

        Renderer rend = GetComponent<Renderer>();

        Texture2D chosenTexture = textures[Random.Range(0, textures.Length)];

        // Built-in Render Pipeline
        rend.material.mainTexture = chosenTexture;

        // If using URP, use this instead:
        // rend.material.SetTexture("_BaseMap", chosenTexture);
    }
}