using UnityEngine;

public class SimpleTest : MonoBehaviour
{
    private int frameCount = 0;
    
    private void Awake()
    {
        Debug.Log("========================================");
        Debug.Log("🟢 SimpleTest.Awake() CALLED!");
        Debug.Log("========================================");
    }
    
    private void Start()
    {
        Debug.Log("🟢 SimpleTest.Start() CALLED!");
        Debug.Log($"🟢 Time.timeScale = {Time.timeScale}");
        Debug.Log($"🟢 Time.deltaTime = {Time.deltaTime}");
    }

    private void Update()
    {
        frameCount++;
        
        // Логируем каждую секунду
        if (frameCount % 60 == 0)
        {
            Debug.Log($"🟢 SimpleTest.Update() is RUNNING! Frame: {frameCount}, Time: {Time.time:F1}s");
        }
        
        // Проверяем ввод ПРЯМО ЗДЕСЬ
        if (Input.anyKeyDown)
        {
            Debug.Log("🟢 SimpleTest detected: Input.anyKeyDown = TRUE");
            
            if (Input.GetKeyDown(KeyCode.UpArrow))
                Debug.Log("🟢🟢🟢 UP ARROW in SimpleTest.Update()!");
            if (Input.GetKeyDown(KeyCode.Space))
                Debug.Log("🟢🟢🟢 SPACE in SimpleTest.Update()!");
        }
        
#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.upArrowKey.wasPressedThisFrame)
                Debug.Log("🟢🟢🟢 UP ARROW (New Input) in SimpleTest.Update()!");
            if (keyboard.spaceKey.wasPressedThisFrame)
                Debug.Log("🟢🟢🟢 SPACE (New Input) in SimpleTest.Update()!");
        }
#endif
    }

    private void FixedUpdate()
    {
        // Логируем раз в секунду
        if (Time.fixedTime % 1f < Time.fixedDeltaTime)
        {
            Debug.Log($"🟢 SimpleTest.FixedUpdate() is RUNNING! Time: {Time.fixedTime:F1}s");
        }
    }

    private void LateUpdate()
    {
        // Логируем каждые 2 секунды
        if (frameCount % 120 == 0)
        {
            Debug.Log($"🟢 SimpleTest.LateUpdate() is RUNNING! Frame: {frameCount}");
        }
    }
}
