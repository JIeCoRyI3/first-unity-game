using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class SnakeController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveInterval = 0.5f;  // Замедлено для видимости

    [Header("Snake Settings")]
    public int initialLength = 2;

    private List<Vector2Int> snakeBody = new List<Vector2Int>();
    private List<GameObject> snakeSegments = new List<GameObject>();
    private Vector2Int direction = Vector2Int.right;
    private Vector2Int nextDirection = Vector2Int.right;
    private float moveTimer = 0f;
    private bool isGameOver = false;
    private bool gameStarted = false;
    private Sprite squareSprite;
    
    // Для предотвращения множественных нажатий
    private bool upPressed = false;
    private bool downPressed = false;
    private bool leftPressed = false;
    private bool rightPressed = false;

    private void Start()
    {
        squareSprite = CreateSquareSprite();
        InitializeSnake();
        
        Debug.Log("=== SNAKE GAME STARTED ===");
        Debug.Log("🎮 Controls: Arrow Keys or WASD");
        Debug.Log($"📦 Input System: " +
#if ENABLE_INPUT_SYSTEM
            "NEW INPUT SYSTEM ENABLED"
#else
            "LEGACY INPUT SYSTEM"
#endif
        );
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private void InitializeSnake()
    {
        snakeBody.Clear();
        snakeSegments.Clear();

        int startX = GameManager.Instance.gridWidth / 2;
        int startY = GameManager.Instance.gridHeight / 2;

        for (int i = 0; i < initialLength; i++)
        {
            Vector2Int pos = new Vector2Int(startX - i, startY);
            snakeBody.Add(pos);
            CreateSnakeSegment(pos, i == 0);
        }

        direction = Vector2Int.right;
        nextDirection = Vector2Int.right;
        gameStarted = false;
        
        Debug.Log($"✅ Snake ready at ({startX}, {startY})");
    }

    private void CreateSnakeSegment(Vector2Int gridPos, bool isHead)
    {
        Vector3 worldPos = GameManager.Instance.GetWorldPosition(gridPos.x, gridPos.y);
        GameObject segment = new GameObject(isHead ? "SnakeHead" : "SnakeBody");
        segment.transform.position = worldPos;
        segment.transform.parent = transform;
        
        SpriteRenderer sr = segment.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.color = isHead ? GameManager.Instance.snakeHeadColor : GameManager.Instance.snakeBodyColor;
        sr.sortingOrder = 1;
        
        segment.transform.localScale = new Vector3(
            GameManager.Instance.cellSize * 0.85f,
            GameManager.Instance.cellSize * 0.85f,
            1f
        );

        snakeSegments.Add(segment);
        
        Debug.Log($"🟢 Created segment at ({gridPos.x}, {gridPos.y}), World: {worldPos}, Color: {(isHead ? "HEAD" : "BODY")}");
    }

    private void Update()
    {
        if (isGameOver)
        {
            if (Time.frameCount % 120 == 0)
            {
                Debug.Log("💀 Game is over, waiting for restart...");
            }
            return;
        }
        
        HandleInput();

        moveTimer += Time.deltaTime;
        
        if (gameStarted && Time.frameCount % 30 == 0)
        {
            Debug.Log($"⏱️ Move timer: {moveTimer:F2}/{moveInterval:F2}");
        }
        
        if (moveTimer >= moveInterval)
        {
            moveTimer = 0f;
            Move();
        }
    }

    private void HandleInput()
    {
        bool inputThisFrame = false;
        
#if ENABLE_INPUT_SYSTEM
        // NEW INPUT SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            // UP
            if ((keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) && !upPressed)
            {
                if (direction != Vector2Int.down)
                {
                    nextDirection = Vector2Int.up;
                    gameStarted = true;
                    inputThisFrame = true;
                    upPressed = true;
                    Debug.Log("<color=green>⬆️ UP (New Input System)</color>");
                }
            }
            if (keyboard.upArrowKey.wasReleasedThisFrame || keyboard.wKey.wasReleasedThisFrame)
                upPressed = false;
            
            // DOWN
            if ((keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) && !downPressed)
            {
                if (direction != Vector2Int.up)
                {
                    nextDirection = Vector2Int.down;
                    gameStarted = true;
                    inputThisFrame = true;
                    downPressed = true;
                    Debug.Log("<color=green>⬇️ DOWN (New Input System)</color>");
                }
            }
            if (keyboard.downArrowKey.wasReleasedThisFrame || keyboard.sKey.wasReleasedThisFrame)
                downPressed = false;
            
            // LEFT
            if ((keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) && !leftPressed)
            {
                if (direction != Vector2Int.right)
                {
                    nextDirection = Vector2Int.left;
                    gameStarted = true;
                    inputThisFrame = true;
                    leftPressed = true;
                    Debug.Log("<color=green>⬅️ LEFT (New Input System)</color>");
                }
            }
            if (keyboard.leftArrowKey.wasReleasedThisFrame || keyboard.aKey.wasReleasedThisFrame)
                leftPressed = false;
            
            // RIGHT
            if ((keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) && !rightPressed)
            {
                if (direction != Vector2Int.left)
                {
                    nextDirection = Vector2Int.right;
                    gameStarted = true;
                    inputThisFrame = true;
                    rightPressed = true;
                    Debug.Log("<color=green>➡️ RIGHT (New Input System)</color>");
                }
            }
            if (keyboard.rightArrowKey.wasReleasedThisFrame || keyboard.dKey.wasReleasedThisFrame)
                rightPressed = false;
        }
        else
        {
            Debug.LogWarning("⚠️ Keyboard.current is NULL!");
        }
#endif

        // LEGACY INPUT SYSTEM (fallback)
        if (!inputThisFrame)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                if (direction != Vector2Int.down)
                {
                    nextDirection = Vector2Int.up;
                    gameStarted = true;
                    Debug.Log("<color=yellow>⬆️ UP (Legacy Input)</color>");
                }
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                if (direction != Vector2Int.up)
                {
                    nextDirection = Vector2Int.down;
                    gameStarted = true;
                    Debug.Log("<color=yellow>⬇️ DOWN (Legacy Input)</color>");
                }
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                if (direction != Vector2Int.right)
                {
                    nextDirection = Vector2Int.left;
                    gameStarted = true;
                    Debug.Log("<color=yellow>⬅️ LEFT (Legacy Input)</color>");
                }
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                if (direction != Vector2Int.left)
                {
                    nextDirection = Vector2Int.right;
                    gameStarted = true;
                    Debug.Log("<color=yellow>➡️ RIGHT (Legacy Input)</color>");
                }
            }
        }
    }

    private void Move()
    {
        if (!gameStarted)
        {
            Debug.Log("⏸️ Game not started yet, waiting for input...");
            return;
        }
        
        direction = nextDirection;
        Vector2Int newHead = snakeBody[0] + direction;
        
        Debug.Log($"🐍 MOVING from ({snakeBody[0].x}, {snakeBody[0].y}) to ({newHead.x}, {newHead.y})");

        if (!GameManager.Instance.IsValidPosition(newHead.x, newHead.y))
        {
            Debug.Log($"<color=red>💀 Hit wall!</color>");
            GameOver();
            return;
        }

        FoodSpawner foodSpawner = FindObjectOfType<FoodSpawner>();
        bool willEatFood = foodSpawner != null && foodSpawner.IsFoodAtPosition(newHead);
        
        if (!willEatFood)
        {
            for (int i = 0; i < snakeBody.Count - 1; i++)
            {
                if (snakeBody[i] == newHead)
                {
                    Debug.Log($"<color=red>💀 Hit self!</color>");
                    GameOver();
                    return;
                }
            }
        }
        else
        {
            for (int i = 0; i < snakeBody.Count; i++)
            {
                if (snakeBody[i] == newHead)
                {
                    Debug.Log($"<color=red>💀 Hit self!</color>");
                    GameOver();
                    return;
                }
            }
        }

        snakeBody.Insert(0, newHead);
        CreateSnakeSegment(newHead, true);

        if (willEatFood)
        {
            foodSpawner.OnFoodEaten();
            GameManager.Instance.AddScore(1);
            Debug.Log($"<color=green>🍎 Eaten! Score: {GameManager.Instance.GetScore()}</color>");
        }
        else
        {
            RemoveTail();
        }

        UpdateSegmentColors();
    }

    private void RemoveTail()
    {
        if (snakeBody.Count > 0)
            snakeBody.RemoveAt(snakeBody.Count - 1);

        if (snakeSegments.Count > 0)
        {
            GameObject tail = snakeSegments[snakeSegments.Count - 1];
            snakeSegments.RemoveAt(snakeSegments.Count - 1);
            Destroy(tail);
        }
    }

    private void UpdateSegmentColors()
    {
        for (int i = 0; i < snakeSegments.Count; i++)
        {
            SpriteRenderer sr = snakeSegments[i].GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = i == 0 ? GameManager.Instance.snakeHeadColor : GameManager.Instance.snakeBodyColor;
            }
        }
    }

    private void GameOver()
    {
        isGameOver = true;
        Debug.Log("<color=red>=== 💀 GAME OVER 💀 ===</color>");
        GameManager.Instance.GameOver();
    }

    public List<Vector2Int> GetSnakeBody()
    {
        return snakeBody;
    }
}
