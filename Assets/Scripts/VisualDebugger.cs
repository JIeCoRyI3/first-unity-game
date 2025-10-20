using UnityEngine;

public class VisualDebugger : MonoBehaviour
{
    private void OnGUI()
    {
        // Показываем информацию о змейке на экране
        SnakeController snake = FindObjectOfType<SnakeController>();
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;
        
        string info = "🐍 SNAKE INFO:\n\n";
        
        if (snake != null)
        {
            var body = snake.GetSnakeBody();
            info += $"Length: {body.Count}\n";
            if (body.Count > 0)
            {
                info += $"Head: ({body[0].x}, {body[0].y})\n";
                if (body.Count > 1)
                {
                    info += $"Tail: ({body[body.Count-1].x}, {body[body.Count-1].y})\n";
                }
            }
        }
        else
        {
            info += "❌ Snake NOT FOUND!\n";
        }
        
        // Проверяем сколько объектов в сцене
        var allObjects = FindObjectsOfType<SpriteRenderer>();
        info += $"\nSprites in scene: {allObjects.Length}\n";
        
        int snakeSprites = 0;
        int foodSprites = 0;
        int gridSprites = 0;
        
        foreach (var sr in allObjects)
        {
            if (sr.gameObject.name.Contains("Snake"))
                snakeSprites++;
            else if (sr.gameObject.name == "Food")
                foodSprites++;
            else if (sr.gameObject.name.Contains("Cell"))
                gridSprites++;
        }
        
        info += $"  Snake: {snakeSprites}\n";
        info += $"  Food: {foodSprites}\n";
        info += $"  Grid: {gridSprites}\n";
        
        GUI.Box(new Rect(10, 300, 300, 250), "");
        GUI.Label(new Rect(20, 310, 280, 230), info, style);
    }
}
