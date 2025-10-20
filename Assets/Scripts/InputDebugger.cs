using UnityEngine;

public class InputDebugger : MonoBehaviour
{
    private float lastLogTime = 0f;
    private float logInterval = 0.5f; // Логируем не чаще раза в 0.5 секунды

    private void Update()
    {
        // Постоянно проверяем состояние клавиш
        if (Time.time - lastLogTime > logInterval)
        {
            CheckKeyStates();
            lastLogTime = Time.time;
        }
        
        // Проверяем нажатия
        if (Input.anyKeyDown)
        {
            LogKeyPresses();
        }
        
        // Проверяем мышь отдельно
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;
            Debug.Log($"<color=yellow>🖱️ MOUSE CLICKED at ({mousePos.x}, {mousePos.y})</color>");
        }
    }

    private void CheckKeyStates()
    {
        // Проверяем, зажаты ли клавиши прямо сейчас
        bool anyArrow = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) || 
                        Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);
        bool anyWASD = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || 
                       Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);
        
        if (anyArrow || anyWASD)
        {
            string pressed = "";
            if (Input.GetKey(KeyCode.UpArrow)) pressed += "⬆️ ";
            if (Input.GetKey(KeyCode.DownArrow)) pressed += "⬇️ ";
            if (Input.GetKey(KeyCode.LeftArrow)) pressed += "⬅️ ";
            if (Input.GetKey(KeyCode.RightArrow)) pressed += "➡️ ";
            if (Input.GetKey(KeyCode.W)) pressed += "W ";
            if (Input.GetKey(KeyCode.A)) pressed += "A ";
            if (Input.GetKey(KeyCode.S)) pressed += "S ";
            if (Input.GetKey(KeyCode.D)) pressed += "D ";
            
            Debug.Log($"<color=cyan>⌨️ Keys held: {pressed}</color>");
        }
    }

    private void LogKeyPresses()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
            Debug.Log("<color=lime>⬆️ UP ARROW pressed!</color>");
        
        if (Input.GetKeyDown(KeyCode.DownArrow))
            Debug.Log("<color=lime>⬇️ DOWN ARROW pressed!</color>");
        
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            Debug.Log("<color=lime>⬅️ LEFT ARROW pressed!</color>");
        
        if (Input.GetKeyDown(KeyCode.RightArrow))
            Debug.Log("<color=lime>➡️ RIGHT ARROW pressed!</color>");
        
        if (Input.GetKeyDown(KeyCode.W))
            Debug.Log("<color=lime>🇼 W pressed!</color>");
        
        if (Input.GetKeyDown(KeyCode.A))
            Debug.Log("<color=lime>🇦 A pressed!</color>");
        
        if (Input.GetKeyDown(KeyCode.S))
            Debug.Log("<color=lime>🇸 S pressed!</color>");
        
        if (Input.GetKeyDown(KeyCode.D))
            Debug.Log("<color=lime>🇩 D pressed!</color>");
            
        // Логируем любые другие клавиши
        if (Input.inputString.Length > 0)
        {
            Debug.Log($"<color=orange>⌨️ Input string: '{Input.inputString}'</color>");
        }
    }

    private void OnGUI()
    {
        // Показываем на экране какие клавиши нажаты
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.yellow;
        
        string status = "Press Arrow Keys or WASD\n";
        
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            status += "⬆️ UP\n";
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            status += "⬇️ DOWN\n";
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            status += "⬅️ LEFT\n";
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            status += "➡️ RIGHT\n";
        
        GUI.Label(new Rect(10, Screen.height - 150, 300, 150), status, style);
    }
}
