/*
Plays sound effects for the player's key actions:
  - Coin collected  → GameEvents.OnPickupCollected
  - Jump            → GameEvents.OnJump
  - Lose / game over→ GameEvents.gameOver (false = loss)

Attach this script to the PlayerNew GameObject in SoundScene.
Assign coin.wav, jump.wav, and lose.wav from Assets/Sound FX in the Inspector.
An AudioSource component is required on the same GameObject.
*/

using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerSoundController : MonoBehaviour
{
    [Header("Sound FX")]
    public AudioClip coinClip;
    public AudioClip jumpClip;
    public AudioClip loseClip;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        GameEvents.OnPickupCollected += OnCoinCollected;
        GameEvents.OnJump           += OnJump;
        GameEvents.gameOver         += OnGameOver;
    }

    private void OnDisable()
    {
        GameEvents.OnPickupCollected -= OnCoinCollected;
        GameEvents.OnJump           -= OnJump;
        GameEvents.gameOver         -= OnGameOver;
    }

    private void OnCoinCollected(int value)
    {
        if (coinClip != null)
        {
            float volume = GameManager.Instance != null ? GameManager.Instance.sfxVolume : 1f;
            audioSource.PlayOneShot(coinClip, volume);
        }
    }

    private void OnJump()
    {
        if (jumpClip != null)
        {
            float volume = GameManager.Instance != null ? GameManager.Instance.sfxVolume : 1f;
            audioSource.PlayOneShot(jumpClip, volume);
        }
    }

    private void OnGameOver(bool success)
    {
        if (!success && loseClip != null)
        {
            float volume = GameManager.Instance != null ? GameManager.Instance.sfxVolume : 1f;
            audioSource.PlayOneShot(loseClip, volume);
        }
    }
}
