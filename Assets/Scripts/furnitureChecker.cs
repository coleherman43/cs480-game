using System.Collections.Generic;
using UnityEngine;

public class ApartmentDecorManager : MonoBehaviour
{
    [Header("Enter in order: Lounge, Bedroom, Bathroom, Kitchen")]
    public List<GameObject> decorZones = new List<GameObject>();

    private readonly string[] upgradeNames =
    {
        "Lounge",
        "Bedroom",
        "Bathroom",
        "Kitchen"
    };

    private float checkTimer;
    private const float CHECK_INTERVAL = 1.0f; // Check once per second

    private void Start()
    {
        UpdateDecor();
    }

    private void Update()
    {
        checkTimer += Time.deltaTime;

        if (checkTimer >= CHECK_INTERVAL)
        {
            checkTimer = 0f;
            UpdateDecor();
        }
    }

    private void UpdateDecor()
    {
        if (GameManager.Instance == null)
            return;

        for (int i = 0; i < upgradeNames.Length && i < decorZones.Count; i++)
        {
            if (decorZones[i] == null)
                continue;

            bool unlocked = GameManager.Instance.IsUpgradeUnlocked(upgradeNames[i]);

            if (decorZones[i].activeSelf != unlocked)
            {
                decorZones[i].SetActive(unlocked);
            }
        }
    }
}