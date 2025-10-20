using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    private Vector2Int currentFoodPosition;
    private GameObject currentFoodObject;
    private SnakeController snakeController;
    private Sprite circleSprite;

    private void Start()
    {
        snakeController = FindObjectOfType<SnakeController>();
        circleSprite = CreateCircleSprite();
        
        // Небольшая задержка перед созданием первой еды
        Invoke("SpawnFood", 0.1f);
    }

    private Sprite CreateCircleSprite()
    {
        // Создаем круглый спрайт
        int resolution = 32;
        Texture2D texture = new Texture2D(resolution, resolution);
        
        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float radius = resolution / 2f;
        
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    texture.SetPixel(x, y, Color.white);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution);
    }

    private void SpawnFood()
    {
        // Удаляем предыдущую еду, если она была
        if (currentFoodObject != null)
        {
            Destroy(currentFoodObject);
        }

        Vector2Int newPosition;
        int attempts = 0;
        int maxAttempts = 100;

        do
        {
            newPosition = new Vector2Int(
                Random.Range(0, GameManager.Instance.gridWidth),
                Random.Range(0, GameManager.Instance.gridHeight)
            );
            attempts++;
        }
        while (snakeController != null && 
               snakeController.GetSnakeBody().Contains(newPosition) && 
               attempts < maxAttempts);

        currentFoodPosition = newPosition;
        CreateFoodObject(newPosition);
        
        Debug.Log($"Food spawned at ({newPosition.x}, {newPosition.y})");
    }

    private void CreateFoodObject(Vector2Int gridPos)
    {
        Vector3 worldPos = GameManager.Instance.GetWorldPosition(gridPos.x, gridPos.y);

        // Создаем GameObject для еды
        currentFoodObject = new GameObject("Food");
        currentFoodObject.transform.position = worldPos;
        currentFoodObject.transform.parent = transform;
        
        // Добавляем SpriteRenderer
        SpriteRenderer sr = currentFoodObject.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.color = GameManager.Instance.foodColor;
        sr.sortingOrder = 2;
        
        // Устанавливаем размер еды (немного меньше клетки)
        currentFoodObject.transform.localScale = new Vector3(
            GameManager.Instance.cellSize * 0.7f,
            GameManager.Instance.cellSize * 0.7f,
            1f
        );
    }

    public bool IsFoodAtPosition(Vector2Int position)
    {
        return position == currentFoodPosition;
    }

    public void OnFoodEaten()
    {
        SpawnFood();
    }
}
