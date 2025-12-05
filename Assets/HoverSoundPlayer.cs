using UnityEngine;
using UnityEngine.EventSystems; // Essential for hover detection

// TITLE: REUSABLE HOVER SOUND PLAYER
// ROLE: Attaches to any UI Button to play a sound when the mouse enters its area.

// We implement the IPointerEnterHandler interface to receive mouse enter events.
public class HoverSoundPlayer : MonoBehaviour, IPointerEnterHandler
{
    // The specific sound we want to play on hover (usually a lighter sound than the click)
    [Tooltip("Drag the specific hover sound clip here, or leave blank to use the default.")]
    public AudioClip hoverSFX;

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 1. Check if the AudioController is available
        if (AudioController.Instance != null)
        {
            // 2. Play the hover sound.
            // If we assigned a specific clip, use it. Otherwise, use the default hover sound.
            if (hoverSFX != null)
            {
                AudioController.Instance.PlayHoverSFX(hoverSFX);
            }
            else
            {
                AudioController.Instance.PlayHoverSFX();
            }
        }
    }
}