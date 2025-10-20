using System.Collections.Generic;
using UnityEngine;

public class SnakeController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveInterval = 0.15f;

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

    private void Start()
    {
        // Создаем спрайт для сегментов змейки
        squareSprite = CreateSquareSprite();
        InitializeSnake();
        
        Debug.Log("=== Snake Game Started ===");
        Debug.Log($"Use Arrow Keys or WASD to move the snake!");
    }

    private Sprite CreateSquareSprite()
    {
        // Создаем простой квадратный спрайт
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private void InitializeSnake()
    {
        snakeBody.Clear();
        snakeSegments.Clear();

        // Начальная позиция змейки в центре поля
        int startX = GameManager.Instance.gridWidth / 2;
        int startY = GameManager.Instance.gridHeight / 2;

        // Создаем змейку длиной 2 клетки, движущуюся вправо
        for (int i = 0; i < initialLength; i++)
        {
            Vector2Int pos = new Vector2Int(startX - i, startY);
            snakeBody.Add(pos);
            CreateSnakeSegment(pos, i == 0);
        }

        direction = Vector2Int.right;
        nextDirection = Vector2Int.right;
        gameStarted = false;
        
        Debug.Log($"Snake initialized at position ({startX}, {startY}) with {snakeBody.Count} segments");
        Debug.Log($"Snake body: Head at ({snakeBody[0].x}, {snakeBody[0].y}), Tail at ({snakeBody[snakeBody.Count-1].x}, {snakeBody[snakeBody.Count-1].y})");
    }

    private void CreateSnakeSegment(Vector2Int gridPos, bool isHead)
    {
        Vector3 worldPos = GameManager.Instance.GetWorldPosition(gridPos.x, gridPos.y);

        // Создаем GameObject для сегмента змейки
        GameObject segment = new GameObject(isHead ? "SnakeHead" : "SnakeBody");
        segment.transform.position = worldPos;
        segment.transform.parent = transform;
        
        // Добавляем SpriteRenderer
        SpriteRenderer sr = segment.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.color = isHead ? GameManager.Instance.snakeHeadColor : GameManager.Instance.snakeBodyColor;
        sr.sortingOrder = 1;
        
        // Устанавливаем размер сегмента
        segment.transform.localScale = new Vector3(
            GameManager.Instance.cellSize * 0.9f,
            GameManager.Instance.cellSize * 0.9f,
            1f
        );

        snakeSegments.Add(segment);
    }

    private void Update()
    {
        if (isGameOver) return;

        HandleInput();

        moveTimer += Time.deltaTime;
        if (moveTimer >= moveInterval)
        {
            moveTimer = 0f;
            Move();
        }
    }

    private void HandleInput()
    {
        bool inputReceived = false;
        
        // Управление стрелками
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (direction != Vector2Int.down)
            {
                nextDirection = Vector2Int.up;
                inputReceived = true;
                gameStarted = true;
            }
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (direction != Vector2Int.up)
            {
                nextDirection = Vector2Int.down;
                inputReceived = true;
                gameStarted = true;
            }
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (direction != Vector2Int.right)
            {
                nextDirection = Vector2Int.left;
                inputReceived = true;
                gameStarted = true;
            }
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (direction != Vector2Int.left)
            {
                nextDirection = Vector2Int.right;
                inputReceived = true;
                gameStarted = true;
            }
        }

        // Дополнительное управление WASD
        if (Input.GetKeyDown(KeyCode.W))
        {
            if (direction != Vector2Int.down)
            {
                nextDirection = Vector2Int.up;
                inputReceived = true;
                gameStarted = true;
            }
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            if (direction != Vector2Int.up)
            {
                nextDirection = Vector2Int.down;
                inputReceived = true;
                gameStarted = true;
            }
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            if (direction != Vector2Int.right)
            {
                nextDirection = Vector2Int.left;
                inputReceived = true;
                gameStarted = true;
            }
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            if (direction != Vector2Int.left)
            {
                nextDirection = Vector2Int.right;
                inputReceived = true;
                gameStarted = true;
            }
        }
        
        if (inputReceived)
        {
            Debug.Log($"Input received: Moving {nextDirection}");
        }
    }

    private void Move()
    {
        // Не двигаемся пока игрок не нажал кнопку
        if (!gameStarted) return;
        
        direction = nextDirection;

        // Вычисляем новую позицию головы
        Vector2Int newHead = snakeBody[0] + direction;

        Debug.Log($"Moving to ({newHead.x}, {newHead.y}), Direction: {direction}");

        // Проверка столкновения с краями
        if (!GameManager.Instance.IsValidPosition(newHead.x, newHead.y))
        {
            Debug.Log($"<color=red>Game Over: Hit wall at ({newHead.x}, {newHead.y})</color>");
            GameOver();
            return;
        }

        // Проверка столкновения с собственным телом
        // ВАЖНО: Не проверяем последний элемент (хвост), так как он удалится при движении
        // Проверяем только если не съедим еду
        FoodSpawner foodSpawner = FindObjectOfType<FoodSpawner>();
        bool willEatFood = foodSpawner != null && foodSpawner.IsFoodAtPosition(newHead);
        
        if (!willEatFood)
        {
            // Если не едим еду, хвост удалится, поэтому проверяем столкновение без хвоста
            for (int i = 0; i < snakeBody.Count - 1; i++)
            {
                if (snakeBody[i] == newHead)
                {
                    Debug.Log($"<color=red>Game Over: Hit self at ({newHead.x}, {newHead.y})</color>");
                    GameOver();
                    return;
                }
            }
        }
        else
        {
            // Если едим еду, хвост НЕ удалится, проверяем всё тело
            for (int i = 0; i < snakeBody.Count; i++)
            {
                if (snakeBody[i] == newHead)
                {
                    Debug.Log($"<color=red>Game Over: Hit self at ({newHead.x}, {newHead.y})</color>");
                    GameOver();
                    return;
                }
            }
        }

        // Добавляем новую голову
        snakeBody.Insert(0, newHead);
        CreateSnakeSegment(newHead, true);

        // Проверяем, не съели ли мы еду
        if (willEatFood)
        {
            // Съели еду - не удаляем хвост
            foodSpawner.OnFoodEaten();
            GameManager.Instance.AddScore(1);
            Debug.Log($"<color=green>Food eaten! Score: {GameManager.Instance.GetScore()}, Length: {snakeBody.Count}</color>");
        }
        else
        {
            // Не съели еду - удаляем хвост
            RemoveTail();
        }

        UpdateSegmentColors();
    }

    private void RemoveTail()
    {
        if (snakeBody.Count > 0)
        {
            snakeBody.RemoveAt(snakeBody.Count - 1);
        }

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
                snakeSegments[i].name = i == 0 ? "SnakeHead" : $"SnakeBody_{i}";
            }
        }
    }

    private void GameOver()
    {
        isGameOver = true;
        Debug.Log("<color=red>=== GAME OVER ===</color>");
        GameManager.Instance.GameOver();
    }

    public List<Vector2Int> GetSnakeBody()
    {
        return snakeBody;
    }
}
