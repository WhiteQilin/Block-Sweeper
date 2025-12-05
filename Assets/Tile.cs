using UnityEngine;
using System.Collections.Generic;

// TITLE: TILE OBJECT & RECURSIVE REVEAL SYSTEM
// FINAL VERSION (Uses FindFirstObjectByType to eliminate compiler warnings)

public class Tile : MonoBehaviour
{
    [Header("State Data")]
    public int x;
    public int y;
    public bool isMine;
    public int adjacentMines;
    public bool IsRevealed { get; private set; }
    public bool IsFlagged { get; private set; }

    [Header("References")]
    private SpriteRenderer spriteRenderer;
    public List<Tile> neighbors = new List<Tile>();

    [Header("Visual Assets")]
    public Sprite hiddenSprite;
    public Sprite flaggedSprite;
    public Sprite mineSprite;
    public Sprite explodedMineSprite;
    public Sprite[] numberSprites;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        IsRevealed = false;
        IsFlagged = false;
        UpdateVisuals();
    }

    private void OnMouseEnter()
    {
        if (!IsRevealed)
        {
            spriteRenderer.color = new Color(0.9f, 0.9f, 0.9f);
        }
    }

    private void OnMouseExit()
    {
        spriteRenderer.color = Color.white;
    }

    private void OnMouseUpAsButton()
    {
        // This function is called automatically by Unity when the left mouse button
        // is released over a Box Collider 2D that is on the correct Layer.
        // It immediately calls the main interaction logic.
        InteractReveal();
    }

    // This handles placing/removing the flag sprite
    public void InteractFlag()
    {
        if (IsRevealed) return;

        GridManager manager = FindFirstObjectByType<GridManager>();
        if (manager == null) return;

        if (!IsFlagged)
        {
            // --- NEW GUARD: Check if we have hit the mine limit ---
            if (manager.GetFlagsPlacedCount() >= manager.mineCount)
            {
                // Prevent placing a new flag if the limit is reached
                UnityEngine.Debug.Log("Flag limit reached!");
                return;
            }
        }

        // Toggle the flag state
        IsFlagged = !IsFlagged;
        UpdateVisuals();

        // --- NEW: PLAY FLAG SOUND ---
        if (AudioController.Instance != null)
        {
            AudioController.Instance.PlayFlagToggleSFX();
        }
        // --- END NEW ---

        // REPORT FLAG TOGGLE TO GRID MANAGER (FOR MINE COUNTER)
        manager.UpdateFlagCount();
    }

    // --- CORE INTERACT METHOD (UPDATED FOR MOBILE TOGGLE) ---
    public void InteractReveal()
    {
        if (IsRevealed) return;

        // 1. CHECK MODE: If Flag Mode is ACTIVE on the UI, perform flag toggle instead of reveal.
        GameUIController uiController = FindFirstObjectByType<GameUIController>();
        if (uiController != null && uiController.IsFlagMode)
        {
            InteractFlag();
            return;
        }

        // 2. CHECK IF FLAGGED: If it's flagged, do nothing.
        if (IsFlagged) return;

        // 3. PROCEED WITH REVEAL/LOSS
        if (isMine)
        {
            TriggerGameOver();
        }
        else
        {
            Reveal();
        }
    }
    // --- END UPDATED CORE INTERACT METHOD ---


    public void Reveal()
    {
        if (IsRevealed || IsFlagged) return;

        IsRevealed = true;
        UpdateVisuals();

        // --- NEW: PLAY TILE REVEAL SOUND ---
        if (AudioController.Instance != null)
        {
            AudioController.Instance.PlayTileRevealSFX();
        }
        // --- END NEW ---

        // REPORT REVEALED TILE TO GRID MANAGER (FOR WIN CONDITION)
        GridManager manager = FindFirstObjectByType<GridManager>();
        if (manager != null) manager.OnTileRevealed(this);

        if (adjacentMines == 0)
        {
            // Defensive check: Only attempt flood fill if neighbors list is initialized
            if (neighbors != null)
            {
                foreach (Tile neighbor in neighbors)
                {
                    if (!neighbor.IsRevealed)
                    {
                        neighbor.Reveal();
                    }
                }
            }
        }
    }

    private void TriggerGameOver()
    {
        IsRevealed = true;
        spriteRenderer.sprite = explodedMineSprite;
        UnityEngine.Debug.Log("GAME OVER! Stepped on a mine.");

        // --- NEW: PLAY MINE EXPLODE SOUND ---
        if (AudioController.Instance != null)
        {
            AudioController.Instance.PlayMineExplodeSFX();
        }
        // --- END NEW ---

        // REPORT EXPLOSION TO GRID MANAGER (FOR LOSE CONDITION)
        GridManager manager = FindFirstObjectByType<GridManager>();
        if (manager != null) manager.OnMineExploded();
    }

    public void UpdateVisuals()
    {
        if (IsFlagged)
        {
            spriteRenderer.sprite = flaggedSprite;
        }
        else if (IsRevealed)
        {
            if (isMine)
            {
                spriteRenderer.sprite = mineSprite;
            }
            else
            {
                if (adjacentMines >= 0 && adjacentMines < numberSprites.Length)
                {
                    spriteRenderer.sprite = numberSprites[adjacentMines];
                }
            }
        }
        else
        {
            spriteRenderer.sprite = hiddenSprite;
        }
    }

    public void SetData(bool isMineData, int mineCount)
    {
        isMine = isMineData;
        adjacentMines = mineCount;
    }
}