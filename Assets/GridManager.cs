using System;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("References")]
    public Tile tilePrefab;
    public Transform cameraTransform;

    [Header("Settings")]
    public float padding = 0f; // Confirmed: Set padding to 0 for seamless tiles

    // --- Final Scale Units and Buffers ---
    private const float BASE_SCALE_UNIT = 0.12f;
    private const float CAMERA_HUD_BUFFER = 1.0f; // Buffer to clear top/bottom UI

    // --- FINAL POSITIONING OFFSETS (VISUALLY TESTED VALUES) ---
    // These offsets push the camera to correctly frame the dynamically sized grid.
    private const float EASY_X_OFFSET = -0.14f; // Easy is centered well already
    private const float EASY_Y_OFFSET = 1.70f;

    private const float NORMAL_X_OFFSET = -0.08f; // Nudge Normal grid slightly right
    private const float NORMAL_Y_OFFSET = 2.00f; // Nudge Normal grid slightly up/down

    private const float HARD_X_OFFSET = -0.05f; // Nudge Hard grid aggressively right
    private const float HARD_Y_OFFSET = 2.40f; // Nudge Hard grid up/down
    // --- 

    // Game Data
    private int width;
    private int height;
    public int mineCount;
    private HashSet<Tile> revealedTilesTracker = new HashSet<Tile>();
    private int totalSafeTiles;
    private int difficultyIndex; // Stores 0, 1, or 2

    private Tile[,] grid; // 2D Array to store all tiles
    private bool gameIsOver = false;

    private void Start()
    {
        ApplyDifficulty();
        GenerateGrid();
        PlaceMines();
        CalculateNumbers();
        CenterCamera();

        // Setup Win Condition Math
        revealedTilesTracker.Clear();
        int totalTiles = width * height;
        totalSafeTiles = totalTiles - mineCount;

        UnityEngine.Debug.Log($"Total Tiles: {totalTiles}, Mines: {mineCount}, Safe: {totalSafeTiles}");

        // Update the UI with the starting mine count
        GameUIController ui = FindFirstObjectByType<GameUIController>();
        // Ensure the initial mine count is set correctly on the UI
        if (ui != null) ui.UpdateMineCount(mineCount);
    }

    private void ApplyDifficulty()
    {
        difficultyIndex = PlayerPrefs.GetInt("Difficulty", 0); // Store index

        // --- UPDATED MINE COUNTS FOR INCREASED DIFFICULTY ---
        // (Note: Time limit setting moved to GameUIController.cs)
        switch (difficultyIndex)
        {
            case 0: // Easy (10 x 10 Grid)
                width = 10;
                height = 10;
                mineCount = 10;
                break;
            case 1: // Normal (20 x 20 Grid)
                width = 20;
                height = 20;
                mineCount = 50;
                break;
            case 2: // Hard (30 x 30 Grid)
                width = 30;
                height = 30;
                mineCount = 180;
                break;
            default:
                width = 10;
                height = 10;
                mineCount = 10;
                break;
        }
    }

    private void GenerateGrid()
    {
        grid = new Tile[width, height];

        // --- DYNAMIC SCALE CALCULATION ---
        float scale = BASE_SCALE_UNIT * (30f / width);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Position calculation is clean because the pivot is now Bottom-Left
                Vector3 position = new Vector3(
                    (x * scale),
                    (y * scale)
                );

                // Spawn the tile
                Tile spawnedTile = Instantiate(tilePrefab, position, Quaternion.identity);
                spawnedTile.name = $"Tile {x} {y}";
                spawnedTile.transform.parent = transform;

                // Apply the dynamic scale
                spawnedTile.transform.localScale = Vector3.one * scale;

                // Store logic data
                spawnedTile.x = x;
                spawnedTile.y = y;
                grid[x, y] = spawnedTile;
            }
        }
    }

    private void PlaceMines()
    {
        int minesPlaced = 0;
        while (minesPlaced < mineCount)
        {
            int x = UnityEngine.Random.Range(0, width);
            int y = UnityEngine.Random.Range(0, height);

            Tile t = grid[x, y];

            if (!t.isMine)
            {
                t.isMine = true;
                minesPlaced++;
            }
        }
    }

    private void CalculateNumbers()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile t = grid[x, y];
                t.neighbors = GetNeighbors(x, y);

                if (!t.isMine)
                {
                    int count = 0;
                    foreach (Tile n in t.neighbors)
                    {
                        if (n.isMine) count++;
                    }
                    t.SetData(false, count);
                }
                else
                {
                    t.SetData(true, 0);
                }
            }
        }
    }

    private List<Tile> GetNeighbors(int x, int y)
    {
        List<Tile> neighbors = new List<Tile>();

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;

                int checkX = x + i;
                int checkY = y + j;

                if (checkX >= 0 && checkX < width && checkY >= 0 && checkY < height)
                {
                    neighbors.Add(grid[checkX, checkY]);
                }
            }
        }
        return neighbors;
    }

    private void CenterCamera()
    {
        if (cameraTransform == null) return;

        // Calculate the effective scale based on current width (this changes per difficulty)
        float currentScale = BASE_SCALE_UNIT * (30f / width);

        // Calculate the total pixel width/height of the generated grid *after* scaling
        float effectiveWidth = width * currentScale;
        float effectiveHeight = height * currentScale;

        // --- DYNAMIC POSITIONING ---
        float finalX, finalY;

        switch (difficultyIndex)
        {
            case 0: // Easy (10x10)
                finalX = (effectiveWidth / 2f) + EASY_X_OFFSET;
                finalY = EASY_Y_OFFSET;
                break;
            case 1: // Normal (20x20)
                finalX = (effectiveWidth / 2f) + NORMAL_X_OFFSET;
                finalY = NORMAL_Y_OFFSET;
                break;
            case 2: // Hard (30x30)
                finalX = (effectiveWidth / 2f) + HARD_X_OFFSET;
                finalY = HARD_Y_OFFSET;
                break;
            default:
                finalX = effectiveWidth / 2f;
                finalY = EASY_Y_OFFSET;
                break;
        }
        // --- END DYNAMIC POSITIONING ---

        // 1. Calculate Required Zoom (Orthographic Size)
        float targetHeightZoom = effectiveHeight / 2f;
        float targetWidthZoom = (effectiveWidth / Camera.main.aspect) / 2f;

        // Choose the largest zoom required to fit the screen and add the fixed HUD buffer
        float finalZoom = Mathf.Max(targetHeightZoom, targetWidthZoom) + CAMERA_HUD_BUFFER;

        // 2. Set the Camera Position 
        cameraTransform.position = new Vector3(finalX, finalY, -10);

        // Final zoom value
        Camera.main.orthographicSize = finalZoom;
    }

    // --- GAME LOGIC & UI CALLBACKS ---

    // NEW: Function to check current flag state for TILE.CS
    public int GetFlagsPlacedCount()
    {
        int flagsPlaced = 0;
        foreach (Tile t in grid)
        {
            if (t.IsFlagged)
            {
                flagsPlaced++;
            }
        }
        return flagsPlaced;
    }

    // NEW: Robust flag counting logic
    public void UpdateFlagCount()
    {
        int flagsPlaced = GetFlagsPlacedCount();

        // Calculate remaining mines (total mines - flags placed)
        int minesRemaining = mineCount - flagsPlaced;

        GameUIController ui = FindFirstObjectByType<GameUIController>();
        if (ui != null)
        {
            // Update the UI with the calculated remaining count
            ui.UpdateMineCount(minesRemaining);
        }
        // TODO: Add Win Condition check here for advanced rules: 
        // if (flagsPlaced == mineCount && all flags are correctly placed) then GameOver(true)
    }

    public void OnTileRevealed(Tile tile)
    {
        if (gameIsOver) return;

        // --- FIXED: Only count unique tiles ---
        if (revealedTilesTracker.Add(tile))
        {
            // The logic relies on the final check below.
        }
        // --- END FIXED ---

        // --- FINAL WIN CONDITION CHECK ---
        // We iterate through the tracker to count revealed safe tiles.
        int nonMineRevealed = 0;
        foreach (Tile t in revealedTilesTracker)
        {
            if (!t.isMine)
            {
                nonMineRevealed++;
            }
        }

        if (nonMineRevealed >= totalSafeTiles)
        {
            GameOver(true);
        }
    }

    public void OnMineExploded()
    {
        if (gameIsOver) return;
        GameOver(false);
    }

    private void GameOver(bool isWin)
    {
        gameIsOver = true;

        GameUIController ui = FindFirstObjectByType<GameUIController>();
        if (ui != null)
        {
            if (isWin)
            {
                // --- NEW: PLAY WIN SOUND ---
                if (AudioController.Instance != null)
                {
                    AudioController.Instance.PlayGameWinSFX();
                }
                // --- END NEW ---

                UnityEngine.Debug.Log("YOU WIN!");
                ui.TriggerWin();
            }
            else
            {
                // --- NEW: PLAY MINE EXPLODE SOUND is handled in Tile.cs/TriggerGameOver ---
                // We just stop the music here.
                if (AudioController.Instance != null)
                {
                    AudioController.Instance.FadeOutMusic();
                }
                // --- END NEW ---

                UnityEngine.Debug.Log("YOU LOSE!");
                RevealAllMines();
                ui.TriggerGameOver();
            }
        }
    }

    private void RevealAllMines()
    {
        foreach (Tile t in grid)
        {
            if (t.isMine)
            {
                t.Reveal();
            }
        }
    }
}