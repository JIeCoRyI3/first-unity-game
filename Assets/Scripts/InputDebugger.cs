using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InputDebugger : MonoBehaviour
{
    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        // NEW INPUT SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.upArrowKey.wasPressedThisFrame)
                Debug.Log("<color=cyan>🆕 [NEW] ⬆️ UP ARROW</color>");
            if (keyboard.downArrowKey.wasPressedThisFrame)
                Debug.Log("<color=cyan>🆕 [NEW] ⬇️ DOWN ARROW</color>");
            if (keyboard.leftArrowKey.wasPressedThisFrame)
                Debug.Log("<color=cyan>🆕 [NEW] ⬅️ LEFT ARROW</color>");
            if (keyboard.rightArrowKey.wasPressedThisFrame)
                Debug.Log("<color=cyan>🆕 [NEW] ➡️ RIGHT ARROW</color>");
            
            if (keyboard.wKey.wasPressedThisFrame)
                Debug.Log("<color=cyan>🆕 [NEW] W</color>");
            if (keyboard.aKey.wasPressedThisFrame)
                Debug.Log("<color=cyan>🆕 [NEW] A</color>");
            if (keyboard.sKey.wasPressedThisFrame)
                Debug.Log("<color=cyan>🆕 [NEW] S</color>");
            if (keyboard.dKey.wasPressedThisFrame)
                Debug.Log("<color=cyan>🆕 [NEW] D</color>");
                
            if (keyboard.anyKey.wasPressedThisFrame)
            {
                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    Debug.Log("<color=yellow>🖱️ MOUSE CLICK</color>");
                }
            }
        }
        else
        {
            Debug.LogError("❌ Keyboard.current is NULL! New Input System not working!");
        }
#endif

        // LEGACY INPUT
        if (Input.GetKeyDown(KeyCode.UpArrow))
            Debug.Log("<color=orange>📟 [LEGACY] ⬆️ UP ARROW</color>");
        if (Input.GetKeyDown(KeyCode.DownArrow))
            Debug.Log("<color=orange>📟 [LEGACY] ⬇️ DOWN ARROW</color>");
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            Debug.Log("<color=orange>📟 [LEGACY] ⬅️ LEFT ARROW</color>");
        if (Input.GetKeyDown(KeyCode.RightArrow))
            Debug.Log("<color=orange>📟 [LEGACY] ➡️ RIGHT ARROW</color>");
        
        if (Input.GetKeyDown(KeyCode.W))
            Debug.Log("<color=orange>📟 [LEGACY] W</color>");
        if (Input.GetKeyDown(KeyCode.A))
            Debug.Log("<color=orange>📟 [LEGACY] A</color>");
        if (Input.GetKeyDown(KeyCode.S))
            Debug.Log("<color=orange>📟 [LEGACY] S</color>");
        if (Input.GetKeyDown(KeyCode.D))
            Debug.Log("<color=orange>📟 [LEGACY] D</color>");
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.yellow;
        style.fontStyle = FontStyle.Bold;
        
        string status = "🎮 PRESS ARROW KEYS or WASD\n\n";
        
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            status += "✅ NEW INPUT SYSTEM ACTIVE\n";
            
            if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed)
                status += "⬆️ UP\n";
            if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed)
                status += "⬇️ DOWN\n";
            if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed)
                status += "⬅️ LEFT\n";
            if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed)
                status += "➡️ RIGHT\n";
        }
        else
        {
            status += "❌ KEYBOARD NULL!\n";
        }
#else
        status += "📟 LEGACY INPUT SYSTEM\n";
#endif
        
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            status += "⬆️ UP (Legacy)\n";
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            status += "⬇️ DOWN (Legacy)\n";
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            status += "⬅️ LEFT (Legacy)\n";
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            status += "➡️ RIGHT (Legacy)\n";
        
        GUI.Label(new Rect(10, Screen.height - 200, 500, 200), status, style);
    }
}
