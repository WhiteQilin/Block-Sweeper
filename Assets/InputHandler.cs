using UnityEngine;
using UnityEngine.UI; // Needed for button colors

// TITLE: MOBILE INPUT HANDLER
// UPDATED FOR: HUD TOGGLE SWITCH
public class InputHandler : MonoBehaviour
{
    [Header("Configuration")]
    public LayerMask tileLayer;

    [Header("UI References")]
    public Button revealButton; // Drag Btn_Reveal here
    public Button flagButton;   // Drag Btn_Flag here

    [Header("Visual Feedback")]
    public Color activeColor = Color.white;
    public Color inactiveColor = new Color(0.7f, 0.7f, 0.7f); // Darker grey

    // STATE
    private bool isFlagMode = false; // false = Reveal, true = Flag
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;

        // Initialize Mode (Start in Reveal Mode)
        SetRevealMode();
    }

    private void Update()
    {
        HandleTouchInput();
    }

    // --- BUTTON FUNCTIONS (Connect these in Unity) ---

    public void SetRevealMode()
    {
        isFlagMode = false;
        UpdateUI();
    }

    public void SetFlagMode()
    {
        isFlagMode = true;
        UpdateUI();
    }

    private void UpdateUI()
    {
        // Visually darken the button that is NOT selected
        // and brighten the one that IS selected
        if (isFlagMode)
        {
            flagButton.image.color = activeColor;
            revealButton.image.color = inactiveColor;
        }
        else
        {
            flagButton.image.color = inactiveColor;
            revealButton.image.color = activeColor;
        }
    }

    // --- CLICK LOGIC ---

    private void HandleTouchInput()
    {
        // Combine PC (Mouse) and Mobile (Touch) into one check
        if (Input.GetMouseButtonUp(0))
        {
            Vector3 pos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.zero, 0f, tileLayer);

            if (hit.collider != null)
            {
                Tile clickedTile = hit.collider.GetComponent<Tile>();
                if (clickedTile != null)
                {
                    // DECIDE ACTION BASED ON MODE
                    if (isFlagMode)
                    {
                        clickedTile.InteractFlag();
                    }
                    else
                    {
                        clickedTile.InteractReveal();
                    }
                }
            }
        }
    }
}