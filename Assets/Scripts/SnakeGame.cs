using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class SnakeGame : MonoBehaviour
{
    [Header("Grid Settings")] 
    [SerializeField] private int gridWidth = 16;
    [SerializeField] private int gridHeight = 16;

    [Header("Gameplay")] 
    [SerializeField, Tooltip("Seconds between snake moves")] private float moveIntervalSeconds = 0.15f;
    [SerializeField, Tooltip("Background color")] private Color backgroundColor = new Color(0.08f, 0.08f, 0.1f);
    [SerializeField, Tooltip("Snake head color")] private Color headColor = new Color(0.2f, 0.9f, 0.3f);
    [SerializeField, Tooltip("Snake body color")] private Color bodyColor = new Color(0.2f, 0.7f, 0.25f);
    [SerializeField, Tooltip("Food color")] private Color foodColor = new Color(0.9f, 0.2f, 0.2f);

    private LinkedList<Vector2Int> snakeCells; // head is First
    private HashSet<Vector2Int> snakeCellSet;  // for O(1) collision checks

    private Vector2Int currentDirection; // unit vector: up/down/left/right
    private Vector2Int nextDirection;

    private Vector2Int foodCell;
    private bool isAlive;
    private float moveTimer;

    // Rendering
    private Transform renderContainer;
    private readonly List<GameObject> segmentObjects = new List<GameObject>();
    private GameObject foodObject;
    private Sprite cellSprite;

    private void Start()
    {
        SetupCamera();
        EnsureRuntimeAssets();
        StartNewGame();
    }

    private void Update()
    {
        HandleInput();

        // Allow restart at any time
        if (GetRestartPressed())
        {
            StartNewGame();
            return;
        }

        if (!isAlive)
        {
            return;
        }

        moveTimer += Time.deltaTime;
        if (moveTimer >= moveIntervalSeconds)
        {
            moveTimer -= moveIntervalSeconds;
            StepGame();
        }
    }

    private void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;

        cam.orthographic = true;
        // Fit height to grid, add small margin so borders are visible
        cam.orthographicSize = (gridHeight * 0.5f) + 0.5f;
        cam.transform.position = new Vector3((gridWidth - 1) * 0.5f, (gridHeight - 1) * 0.5f, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = backgroundColor;
    }

    private void EnsureRuntimeAssets()
    {
        if (renderContainer == null)
        {
            var containerGO = new GameObject("SnakeRenderContainer");
            renderContainer = containerGO.transform;
        }

        if (cellSprite == null)
        {
            // Create a 1x1 white sprite to be tinted by SpriteRenderer.color
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.name = "CellTexture";
            tex.SetPixel(0, 0, Color.white);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            cellSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
            cellSprite.name = "CellSprite";
        }
    }

    private void StartNewGame()
    {
        // Reset state
        snakeCells = new LinkedList<Vector2Int>();
        snakeCellSet = new HashSet<Vector2Int>();
        moveTimer = 0f;
        isAlive = true;

        // Initial snake of length 2, centered
        Vector2Int head = new Vector2Int(gridWidth / 2, gridHeight / 2);
        Vector2Int tail = head + Vector2Int.left; // tail behind head

        snakeCells.AddFirst(head);
        snakeCells.AddLast(tail);
        snakeCellSet.Add(head);
        snakeCellSet.Add(tail);

        currentDirection = Vector2Int.right;
        nextDirection = currentDirection;

        SpawnFood();
        RenderWorld(fullRebuild: true);
    }

    private void HandleInput()
    {
        // Read last arrow key pressed and set as next direction if not opposite
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.upArrowKey.wasPressedThisFrame)
            {
                TrySetNextDirection(Vector2Int.up);
                return;
            }
            if (kb.downArrowKey.wasPressedThisFrame)
            {
                TrySetNextDirection(Vector2Int.down);
                return;
            }
            if (kb.leftArrowKey.wasPressedThisFrame)
            {
                TrySetNextDirection(Vector2Int.left);
                return;
            }
            if (kb.rightArrowKey.wasPressedThisFrame)
            {
                TrySetNextDirection(Vector2Int.right);
                return;
            }
        }
#endif

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            TrySetNextDirection(Vector2Int.up);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            TrySetNextDirection(Vector2Int.down);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            TrySetNextDirection(Vector2Int.left);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            TrySetNextDirection(Vector2Int.right);
        }
    }

    private bool GetRestartPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null && kb.rKey.wasPressedThisFrame)
        {
            return true;
        }
#endif
        return Input.GetKeyDown(KeyCode.R);
    }

    private void TrySetNextDirection(Vector2Int desired)
    {
        // Prevent reversing into yourself (180° turn) based on current direction
        if (desired + currentDirection == Vector2Int.zero)
        {
            return;
        }
        nextDirection = desired;
    }

    private void StepGame()
    {
        // Apply buffered direction exactly on tick
        currentDirection = nextDirection;

        var currentHead = snakeCells.First.Value;
        var nextHead = currentHead + currentDirection;

        bool outOfBounds = nextHead.x < 0 || nextHead.x >= gridWidth || nextHead.y < 0 || nextHead.y >= gridHeight;
        bool willGrow = nextHead == foodCell;

        // Check self-collision. Moving into the current tail cell is allowed if we are NOT growing
        bool hitsSelf = false;
        if (snakeCellSet.Contains(nextHead))
        {
            var currentTail = snakeCells.Last.Value;
            bool movingIntoTailCell = nextHead == currentTail;
            if (!(movingIntoTailCell && !willGrow))
            {
                hitsSelf = true;
            }
        }

        if (outOfBounds || hitsSelf)
        {
            GameOver();
            return;
        }

        // Move head
        snakeCells.AddFirst(nextHead);
        snakeCellSet.Add(nextHead);

        if (willGrow)
        {
            SpawnFood();
        }
        else
        {
            // Remove tail
            var tail = snakeCells.Last.Value;
            snakeCells.RemoveLast();
            snakeCellSet.Remove(tail);
        }

        RenderWorld(fullRebuild: false);
    }

    private void SpawnFood()
    {
        // If the board is full, end the game as a win
        if (snakeCellSet.Count >= gridWidth * gridHeight)
        {
            GameOver();
            return;
        }

        // Try random positions until an empty cell is found
        for (int safety = 0; safety < 10000; safety++)
        {
            int x = Random.Range(0, gridWidth);
            int y = Random.Range(0, gridHeight);
            var p = new Vector2Int(x, y);
            if (!snakeCellSet.Contains(p))
            {
                foodCell = p;
                RenderFood();
                return;
            }
        }

        // Fallback (should never happen)
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                var p = new Vector2Int(x, y);
                if (!snakeCellSet.Contains(p))
                {
                    foodCell = p;
                    RenderFood();
                    return;
                }
            }
        }

        GameOver();
    }

    private void GameOver()
    {
        isAlive = false;
#if UNITY_EDITOR
        Debug.Log("Game Over. Press R to restart.");
#endif
    }

    private void RenderWorld(bool fullRebuild)
    {
        if (fullRebuild)
        {
            // Ensure enough segment objects exist
            EnsureSegmentObjects(snakeCells.Count);
        }
        else
        {
            // Grow list if snake grew
            if (segmentObjects.Count < snakeCells.Count)
            {
                EnsureSegmentObjects(snakeCells.Count - segmentObjects.Count);
            }
        }

        // Position and color segments
        int index = 0;
        foreach (var cell in snakeCells)
        {
            var go = segmentObjects[index];
            go.SetActive(true);
            go.transform.position = new Vector3(cell.x, cell.y, 0f);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.color = (index == 0) ? headColor : bodyColor;
            index++;
        }
        // Disable any extras
        for (int i = index; i < segmentObjects.Count; i++)
        {
            segmentObjects[i].SetActive(false);
        }

        RenderFood();
    }

    private void EnsureSegmentObjects(int countToAdd)
    {
        if (renderContainer == null) EnsureRuntimeAssets();
        for (int i = 0; i < countToAdd; i++)
        {
            var go = CreateCellGO("SnakeSegment", bodyColor);
            segmentObjects.Add(go);
        }
    }

    private void RenderFood()
    {
        if (foodObject == null)
        {
            foodObject = CreateCellGO("Food", foodColor);
        }
        foodObject.SetActive(true);
        foodObject.transform.position = new Vector3(foodCell.x, foodCell.y, 0f);
        var sr = foodObject.GetComponent<SpriteRenderer>();
        sr.color = foodColor;
    }

    private GameObject CreateCellGO(string baseName, Color tint)
    {
        var go = new GameObject(baseName);
        go.transform.SetParent(renderContainer, worldPositionStays: false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = cellSprite;
        sr.color = tint;
        sr.sortingOrder = 0;
        // Scale to 1x1 world units (already is, but be explicit)
        go.transform.localScale = Vector3.one;
        return go;
    }
}
