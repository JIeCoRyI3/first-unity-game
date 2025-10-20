using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Grid Settings")]
    public int gridWidth = 16;
    public int gridHeight = 16;
    public float cellSize = 1f;

    [Header("Prefabs")]
    public GameObject snakeSegmentPrefab;
    public GameObject foodPrefab;
    public GameObject gridCellPrefab;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;

    [Header("Colors")]
    public Color gridColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    public Color snakeHeadColor = new Color(0.18f, 0.84f, 0.45f, 1f);
    public Color snakeBodyColor = new Color(0.15f, 0.87f, 0.51f, 1f);
    public Color foodColor = new Color(1f, 0.28f, 0.34f, 1f);

    private int score = 0;
    private GameObject[,] gridCells;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        CreateGrid();
        UpdateScoreUI();
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void CreateGrid()
    {
        gridCells = new GameObject[gridWidth, gridHeight];

        // Создаем родительский объект для сетки
        GameObject gridParent = new GameObject("Grid");
        gridParent.transform.position = Vector3.zero;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 position = GetWorldPosition(x, y);
                GameObject cell = null;

                if (gridCellPrefab != null)
                {
                    cell = Instantiate(gridCellPrefab, position, Quaternion.identity, gridParent.transform);
                }
                else
                {
                    // Создаем простую клетку, если префаб не назначен
                    cell = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    cell.transform.position = position;
                    cell.transform.localScale = new Vector3(cellSize * 0.98f, cellSize * 0.98f, 1f);
                    
                    SpriteRenderer sr = cell.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = gridColor;
                        sr.sortingOrder = -10;
                    }
                    else
                    {
                        Renderer renderer = cell.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            renderer.material.color = gridColor;
                        }
                    }
                }

                cell.name = $"Cell_{x}_{y}";
                cell.transform.parent = gridParent.transform;
                gridCells[x, y] = cell;
            }
        }

        // Центрируем камеру
        CenterCamera();
    }

    private void CenterCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            float centerX = (gridWidth - 1) * cellSize / 2f;
            float centerY = (gridHeight - 1) * cellSize / 2f;
            mainCamera.transform.position = new Vector3(centerX, centerY, -10f);

            // Настраиваем размер камеры
            float verticalSize = gridHeight * cellSize / 2f + 1f;
            mainCamera.orthographicSize = verticalSize;
        }
    }

    public Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x * cellSize, y * cellSize, 0);
    }

    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt(worldPosition.x / cellSize);
        int y = Mathf.RoundToInt(worldPosition.y / cellSize);
        return new Vector2Int(x, y);
    }

    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    public void AddScore(int points)
    {
        score += points;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Счёт: " + score;
        }
    }

    public void GameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (finalScoreText != null)
        {
            finalScoreText.text = "Ваш счёт: " + score;
        }

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        score = 0;
        UpdateScoreUI();
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    public int GetScore()
    {
        return score;
    }
}
