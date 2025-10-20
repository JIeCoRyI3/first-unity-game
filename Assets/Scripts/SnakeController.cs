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
    private Sprite squareSprite;

    private void Start()
    {
        // Создаем спрайт для сегментов змейки
        squareSprite = CreateSquareSprite();
        InitializeSnake();
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
        
        Debug.Log($"Snake initialized at position ({startX}, {startY})");
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
        // Управление стрелками
        if (Input.GetKeyDown(KeyCode.UpArrow) && direction != Vector2Int.down)
        {
            nextDirection = Vector2Int.up;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) && direction != Vector2Int.up)
        {
            nextDirection = Vector2Int.down;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) && direction != Vector2Int.right)
        {
            nextDirection = Vector2Int.left;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) && direction != Vector2Int.left)
        {
            nextDirection = Vector2Int.right;
        }

        // Дополнительное управление WASD
        if (Input.GetKeyDown(KeyCode.W) && direction != Vector2Int.down)
        {
            nextDirection = Vector2Int.up;
        }
        else if (Input.GetKeyDown(KeyCode.S) && direction != Vector2Int.up)
        {
            nextDirection = Vector2Int.down;
        }
        else if (Input.GetKeyDown(KeyCode.A) && direction != Vector2Int.right)
        {
            nextDirection = Vector2Int.left;
        }
        else if (Input.GetKeyDown(KeyCode.D) && direction != Vector2Int.left)
        {
            nextDirection = Vector2Int.right;
        }
    }

    private void Move()
    {
        direction = nextDirection;

        // Вычисляем новую позицию головы
        Vector2Int newHead = snakeBody[0] + direction;

        // Проверка столкновения с краями
        if (!GameManager.Instance.IsValidPosition(newHead.x, newHead.y))
        {
            Debug.Log($"Game Over: Hit wall at ({newHead.x}, {newHead.y})");
            GameOver();
            return;
        }

        // Проверка столкновения с собственным телом
        if (snakeBody.Contains(newHead))
        {
            Debug.Log($"Game Over: Hit self at ({newHead.x}, {newHead.y})");
            GameOver();
            return;
        }

        // Добавляем новую голову
        snakeBody.Insert(0, newHead);
        CreateSnakeSegment(newHead, true);

        // Проверяем, не съели ли мы еду
        FoodSpawner foodSpawner = FindObjectOfType<FoodSpawner>();
        if (foodSpawner != null && foodSpawner.IsFoodAtPosition(newHead))
        {
            // Съели еду - не удаляем хвост
            foodSpawner.OnFoodEaten();
            GameManager.Instance.AddScore(1);
            Debug.Log($"Food eaten! Score: {GameManager.Instance.GetScore()}");
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
            }
        }
    }

    private void GameOver()
    {
        isGameOver = true;
        GameManager.Instance.GameOver();
    }

    public List<Vector2Int> GetSnakeBody()
    {
        return snakeBody;
    }
}
