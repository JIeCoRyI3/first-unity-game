using UnityEngine;
using System.Linq;

public class SceneInspector : MonoBehaviour
{
    private void Start()
    {
        Invoke("InspectScene", 1f); // Проверяем через секунду после старта
    }

    private void InspectScene()
    {
        Debug.Log("========================================");
        Debug.Log("🔍 SCENE INSPECTION");
        Debug.Log("========================================");
        
        InspectCamera();
        InspectAllSpriteRenderers();
        InspectCanvas();
        InspectEventSystem();
        InspectSnake();
        
        Debug.Log("========================================");
        Debug.Log("✅ INSPECTION COMPLETE");
        Debug.Log("========================================");
    }

    private void InspectCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("❌ NO MAIN CAMERA!");
            return;
        }
        
        Debug.Log($"📷 CAMERA:");
        Debug.Log($"  Position: {cam.transform.position}");
        Debug.Log($"  Rotation: {cam.transform.rotation.eulerAngles}");
        Debug.Log($"  Orthographic: {cam.orthographic}");
        Debug.Log($"  Size: {cam.orthographicSize}");
        Debug.Log($"  Depth: {cam.depth}");
        Debug.Log($"  Culling Mask: {cam.cullingMask}");
        Debug.Log($"  Clear Flags: {cam.clearFlags}");
        Debug.Log($"  Background: {cam.backgroundColor}");
        
        // Проверяем что камера видит
        var allRenderers = FindObjectsOfType<Renderer>();
        int visible = 0;
        int invisible = 0;
        
        foreach (var r in allRenderers)
        {
            if (r.isVisible)
                visible++;
            else
                invisible++;
        }
        
        Debug.Log($"  Visible renderers: {visible}");
        Debug.Log($"  Invisible renderers: {invisible}");
    }

    private void InspectAllSpriteRenderers()
    {
        var sprites = FindObjectsOfType<SpriteRenderer>();
        Debug.Log($"\n🎨 SPRITE RENDERERS: {sprites.Length} total");
        
        int snakeCount = 0;
        int foodCount = 0;
        int gridCount = 0;
        
        foreach (var sr in sprites)
        {
            string name = sr.gameObject.name;
            
            if (name.Contains("Snake"))
            {
                snakeCount++;
                Debug.Log($"  🐍 {name}: Pos={sr.transform.position}, Color={sr.color}, Sorting={sr.sortingOrder}, Enabled={sr.enabled}, Visible={sr.isVisible}");
            }
            else if (name == "Food")
            {
                foodCount++;
                Debug.Log($"  🍎 {name}: Pos={sr.transform.position}, Color={sr.color}, Sorting={sr.sortingOrder}, Enabled={sr.enabled}, Visible={sr.isVisible}");
            }
            else if (name.Contains("Cell"))
            {
                gridCount++;
                if (gridCount <= 3) // Показываем только первые 3
                {
                    Debug.Log($"  📐 {name}: Pos={sr.transform.position}, Color={sr.color}, Sorting={sr.sortingOrder}");
                }
            }
        }
        
        Debug.Log($"\n  Summary: Snake={snakeCount}, Food={foodCount}, Grid={gridCount}");
        
        if (snakeCount == 0)
        {
            Debug.LogError("❌ NO SNAKE SPRITES FOUND!");
        }
    }

    private void InspectCanvas()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("❌ NO CANVAS!");
            return;
        }
        
        Debug.Log($"\n🖼️ CANVAS:");
        Debug.Log($"  Render Mode: {canvas.renderMode}");
        Debug.Log($"  Sort Order: {canvas.sortingOrder}");
        Debug.Log($"  Active: {canvas.gameObject.activeInHierarchy}");
        Debug.Log($"  Enabled: {canvas.enabled}");
        
        var canvasScaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        if (canvasScaler != null)
        {
            Debug.Log($"  Scaler Mode: {canvasScaler.uiScaleMode}");
        }
    }

    private void InspectEventSystem()
    {
        var eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogError("❌ NO EVENT SYSTEM! UI WILL NOT WORK!");
            return;
        }
        
        Debug.Log($"\n🎮 EVENT SYSTEM:");
        Debug.Log($"  Active: {eventSystem.gameObject.activeInHierarchy}");
        Debug.Log($"  Enabled: {eventSystem.enabled}");
        
        var inputModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        if (inputModule != null)
        {
            Debug.Log($"  Input Module: FOUND, Enabled={inputModule.enabled}");
        }
        else
        {
            Debug.LogError("  Input Module: NOT FOUND!");
        }
    }

    private void InspectSnake()
    {
        var snake = FindObjectOfType<SnakeController>();
        if (snake == null)
        {
            Debug.LogError("❌ NO SNAKE CONTROLLER!");
            return;
        }
        
        var body = snake.GetSnakeBody();
        Debug.Log($"\n🐍 SNAKE:");
        Debug.Log($"  Length: {body.Count}");
        Debug.Log($"  Active: {snake.gameObject.activeInHierarchy}");
        Debug.Log($"  Enabled: {snake.enabled}");
        
        if (body.Count > 0)
        {
            Debug.Log($"  Head Grid Pos: ({body[0].x}, {body[0].y})");
            
            var gm = GameManager.Instance;
            if (gm != null)
            {
                var worldPos = gm.GetWorldPosition(body[0].x, body[0].y);
                Debug.Log($"  Head World Pos: {worldPos}");
            }
        }
    }

    private void Update()
    {
        // Показываем информацию каждые 3 секунды
        if (Time.frameCount % 180 == 0)
        {
            var sprites = FindObjectsOfType<SpriteRenderer>();
            Debug.Log($"📊 Current sprites in scene: {sprites.Length}");
        }
    }
}
