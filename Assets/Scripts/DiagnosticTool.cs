using UnityEngine;
using UnityEngine.EventSystems;

public class DiagnosticTool : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("========================================");
        Debug.Log("🔍 DIAGNOSTIC TOOL STARTED");
        Debug.Log("========================================");
        
        CheckTimeScale();
        CheckGameManager();
        CheckSnakeController();
        CheckFoodSpawner();
        CheckEventSystem();
        CheckCamera();
        
        Debug.Log("========================================");
        Debug.Log("✅ DIAGNOSTIC COMPLETE");
        Debug.Log("========================================");
    }

    private void Update()
    {
        // Проверяем Time.timeScale каждую секунду
        if (Time.frameCount % 60 == 0)
        {
            if (Time.timeScale != 1f)
            {
                Debug.LogWarning($"⚠️ Time.timeScale = {Time.timeScale} (should be 1!)");
            }
        }
    }

    private void CheckTimeScale()
    {
        Debug.Log($"⏱️ Time.timeScale: {Time.timeScale}");
        if (Time.timeScale == 0f)
        {
            Debug.LogError("❌ Time.timeScale is 0! Game is PAUSED!");
            Debug.LogError("🔧 Setting Time.timeScale to 1...");
            Time.timeScale = 1f;
        }
        else if (Time.timeScale != 1f)
        {
            Debug.LogWarning($"⚠️ Time.timeScale is {Time.timeScale} (unusual)");
        }
        else
        {
            Debug.Log("✅ Time.timeScale is OK");
        }
    }

    private void CheckGameManager()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm == null)
        {
            Debug.LogError("❌ GameManager NOT FOUND in scene!");
            return;
        }
        
        Debug.Log("✅ GameManager found");
        Debug.Log($"   - Instance: {(GameManager.Instance != null ? "OK" : "NULL")}");
        Debug.Log($"   - Enabled: {gm.enabled}");
        Debug.Log($"   - GameObject active: {gm.gameObject.activeInHierarchy}");
    }

    private void CheckSnakeController()
    {
        SnakeController snake = FindObjectOfType<SnakeController>();
        if (snake == null)
        {
            Debug.LogError("❌ SnakeController NOT FOUND in scene!");
            return;
        }
        
        Debug.Log("✅ SnakeController found");
        Debug.Log($"   - Enabled: {snake.enabled}");
        Debug.Log($"   - GameObject active: {snake.gameObject.activeInHierarchy}");
        Debug.Log($"   - GameObject name: {snake.gameObject.name}");
    }

    private void CheckFoodSpawner()
    {
        FoodSpawner food = FindObjectOfType<FoodSpawner>();
        if (food == null)
        {
            Debug.LogError("❌ FoodSpawner NOT FOUND in scene!");
            return;
        }
        
        Debug.Log("✅ FoodSpawner found");
        Debug.Log($"   - Enabled: {food.enabled}");
        Debug.Log($"   - GameObject active: {food.gameObject.activeInHierarchy}");
    }

    private void CheckEventSystem()
    {
        EventSystem es = FindObjectOfType<EventSystem>();
        if (es == null)
        {
            Debug.LogError("❌ EventSystem NOT FOUND in scene!");
            Debug.LogError("🔧 UI buttons will NOT work!");
            return;
        }
        
        Debug.Log("✅ EventSystem found");
        Debug.Log($"   - Enabled: {es.enabled}");
        Debug.Log($"   - GameObject active: {es.gameObject.activeInHierarchy}");
        
        var inputModule = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        if (inputModule == null)
        {
            Debug.LogWarning("⚠️ StandaloneInputModule NOT FOUND!");
        }
        else
        {
            Debug.Log($"   - StandaloneInputModule: {inputModule.enabled}");
        }
    }

    private void CheckCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("❌ Main Camera NOT FOUND!");
            return;
        }
        
        Debug.Log("✅ Main Camera found");
        Debug.Log($"   - Position: {cam.transform.position}");
        Debug.Log($"   - Orthographic: {cam.orthographic}");
        Debug.Log($"   - Size: {cam.orthographicSize}");
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 18;
        style.normal.textColor = Color.cyan;
        style.fontStyle = FontStyle.Bold;
        
        string info = $"🔍 DIAGNOSTICS:\n";
        info += $"Time.timeScale: {Time.timeScale}\n";
        info += $"Frame: {Time.frameCount}\n";
        info += $"FPS: {(int)(1f / Time.unscaledDeltaTime)}\n\n";
        
        var snake = FindObjectOfType<SnakeController>();
        if (snake != null)
        {
            info += $"Snake: {(snake.enabled ? "✅" : "❌")}\n";
        }
        else
        {
            info += $"Snake: ❌ NOT FOUND\n";
        }
        
        var gm = GameManager.Instance;
        if (gm != null)
        {
            info += $"GameManager: ✅\n";
        }
        else
        {
            info += $"GameManager: ❌ NULL\n";
        }
        
        GUI.Label(new Rect(Screen.width - 300, 10, 290, 200), info, style);
    }
}
