using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    private Vector2Int currentFoodPosition;
    private GameObject currentFoodObject;
    private SnakeController snakeController;

    private void Start()
    {
        snakeController = FindObjectOfType<SnakeController>();
        SpawnFood();
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
    }

    private void CreateFoodObject(Vector2Int gridPos)
    {
        Vector3 worldPos = GameManager.Instance.GetWorldPosition(gridPos.x, gridPos.y);

        if (GameManager.Instance.foodPrefab != null)
        {
            currentFoodObject = Instantiate(GameManager.Instance.foodPrefab, worldPos, Quaternion.identity);
        }
        else
        {
            // Создаем простую сферу, если префаб не назначен
            currentFoodObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            currentFoodObject.transform.position = worldPos;
            currentFoodObject.transform.localScale = new Vector3(
                GameManager.Instance.cellSize * 0.7f,
                GameManager.Instance.cellSize * 0.7f,
                GameManager.Instance.cellSize * 0.7f
            );

            // Удаляем коллайдер, он нам не нужен
            Collider collider = currentFoodObject.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            SpriteRenderer sr = currentFoodObject.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = GameManager.Instance.foodColor;
                sr.sortingOrder = 2;
            }
            else
            {
                Renderer renderer = currentFoodObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = GameManager.Instance.foodColor;
                }
            }
        }

        currentFoodObject.name = "Food";
        currentFoodObject.transform.parent = transform;
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
