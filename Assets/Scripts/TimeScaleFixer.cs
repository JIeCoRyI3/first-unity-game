using UnityEngine;

public class TimeScaleFixer : MonoBehaviour
{
    private void OnGUI()
    {
        // OnGUI вызывается ВСЕГДА, даже при timeScale = 0
        
        // Показываем Time.timeScale на экране БОЛЬШИМИ БУКВАМИ
        GUIStyle style = new GUIStyle();
        style.fontSize = 40;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Time.timeScale == 0 ? Color.red : Color.green;
        
        GUI.Label(new Rect(Screen.width / 2 - 200, 10, 400, 50), 
            $"Time.timeScale = {Time.timeScale}", style);
        
        // ПРИНУДИТЕЛЬНО ставим timeScale = 1
        if (Time.timeScale != 1f)
        {
            Debug.LogError($"🚨 FIXING Time.timeScale from {Time.timeScale} to 1!");
            Time.timeScale = 1f;
        }
        
        // Кнопка для ручного изменения
        if (GUI.Button(new Rect(Screen.width / 2 - 100, 70, 200, 50), "SET TIME SCALE TO 1"))
        {
            Debug.Log("🔧 Button clicked! Setting Time.timeScale = 1");
            Time.timeScale = 1f;
        }
        
        // Показываем статус Update
        GUIStyle statusStyle = new GUIStyle();
        statusStyle.fontSize = 30;
        statusStyle.normal.textColor = Color.yellow;
        
        string status = Time.frameCount > 0 ? 
            $"Frame: {Time.frameCount}\nTime: {Time.time:F1}s\nUpdate: {(Time.deltaTime > 0 ? "✅ WORKING" : "❌ STOPPED")}" :
            "Starting...";
        
        GUI.Label(new Rect(Screen.width / 2 - 150, 140, 300, 150), status, statusStyle);
    }
    
    private void Awake()
    {
        Debug.Log("========================================");
        Debug.Log("🔧 TimeScaleFixer.Awake() - FORCING Time.timeScale = 1");
        Debug.Log("========================================");
        Time.timeScale = 1f;
    }
    
    private void Start()
    {
        Debug.Log("🔧 TimeScaleFixer.Start() - Time.timeScale = " + Time.timeScale);
        Time.timeScale = 1f;
    }
    
    private void Update()
    {
        // Если этот Update вызывается - значит всё работает
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"🔧 TimeScaleFixer.Update() WORKING! Frame: {Time.frameCount}");
        }
    }
    
    private void OnEnable()
    {
        Debug.Log("🔧 TimeScaleFixer.OnEnable() - FORCING Time.timeScale = 1");
        Time.timeScale = 1f;
    }
}
