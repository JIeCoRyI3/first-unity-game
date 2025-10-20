using UnityEngine;

public class InputDebugger : MonoBehaviour
{
    private void Update()
    {
        // Проверяем все возможные клавиши
        if (Input.anyKeyDown)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
                Debug.Log("<color=cyan>UP ARROW pressed!</color>");
            
            if (Input.GetKeyDown(KeyCode.DownArrow))
                Debug.Log("<color=cyan>DOWN ARROW pressed!</color>");
            
            if (Input.GetKeyDown(KeyCode.LeftArrow))
                Debug.Log("<color=cyan>LEFT ARROW pressed!</color>");
            
            if (Input.GetKeyDown(KeyCode.RightArrow))
                Debug.Log("<color=cyan>RIGHT ARROW pressed!</color>");
            
            if (Input.GetKeyDown(KeyCode.W))
                Debug.Log("<color=cyan>W pressed!</color>");
            
            if (Input.GetKeyDown(KeyCode.A))
                Debug.Log("<color=cyan>A pressed!</color>");
            
            if (Input.GetKeyDown(KeyCode.S))
                Debug.Log("<color=cyan>S pressed!</color>");
            
            if (Input.GetKeyDown(KeyCode.D))
                Debug.Log("<color=cyan>D pressed!</color>");
                
            if (Input.GetMouseButtonDown(0))
                Debug.Log("<color=yellow>MOUSE CLICKED!</color>");
        }
    }
}
