using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class SnakeGame : MonoBehaviour
{
    [Header("Grid Settings")] 
    [SerializeField] private int gridWidth = 16;
    [SerializeField] private int gridHeight = 16;

    [Header("Gameplay")] 
    [SerializeField, Tooltip("Seconds between snake moves")] private float moveIntervalSeconds = 0.15f;
    [SerializeField, Tooltip("Background color")] private Color backgroundColor = new Color(0.08f, 0.08f, 0.1f);
    [SerializeField, Tooltip("Snake head color")] private Color headColor = new Color(0.2f, 0.9f, 0.3f);
    [SerializeField, Tooltip("Snake body color")] private Color bodyColor = new Color(0.2f, 0.7f, 0.25f);
    [SerializeField, Tooltip("Food color")] private Color foodColor = new Color(0.9f, 0.2f, 0.2f);

    [Header("Visuals")]
    [SerializeField, Tooltip("Border color")] private Color borderColor = new Color(0.85f, 0.85f, 0.95f);
    [SerializeField, Tooltip("Border thickness in world units")] private float borderThickness = 0.12f;

    [Header("Grid Visuals")]
    [SerializeField, Tooltip("Light checker cell color")] private Color gridColorLight = new Color(0.10f, 0.12f, 0.15f);
    [SerializeField, Tooltip("Dark checker cell color")] private Color gridColorDark  = new Color(0.08f, 0.09f, 0.12f);
    [SerializeField, Tooltip("Extra top world-space margin under HUD")] private float topUiWorldMargin = 1.0f;

    private LinkedList<Vector2Int> snakeCells; // head is First
    private HashSet<Vector2Int> snakeCellSet;  // for O(1) collision checks

    private Vector2Int currentDirection; // unit vector: up/down/left/right
    private Vector2Int nextDirection;

    private Vector2Int foodCell;
    private bool isAlive;
    private bool isPaused;
    private float moveTimer;
    private float defaultMoveIntervalSeconds;

    // Rendering
    private Transform renderContainer;
    private Transform backgroundContainer;
    private Transform borderContainer;
    private GameObject borderTop;
    private GameObject borderBottom;
    private GameObject borderLeft;
    private GameObject borderRight;
    private readonly List<GameObject> backgroundTiles = new List<GameObject>();
    private readonly List<GameObject> segmentObjects = new List<GameObject>();
    private readonly List<Transform> segmentArrowTransforms = new List<Transform>();
    // Food rendering (now supports multiple food items)
    private GameObject foodObject; // legacy (unused in multi-food mode)
    private List<Vector2Int> foodCells;
    private List<GameObject> foodObjects;
    private int maxFoodCount = 1;
    private Sprite cellSprite;
    private Sprite snakeSprite;
    private Sprite arrowSprite;
    private Sprite[] foodSprites;
    private bool foodNeedsSprite;
    private GameObject gameOverCanvasGO;

    // Progression / Roguelike
    [Header("Roguelike Progression")]
    [SerializeField, Tooltip("XP granted per food collected")] private int xpPerFood = 10;
    [SerializeField, Tooltip("Base XP required for first level-up")] private int baseXpToNext = 50;
    [SerializeField, Tooltip("Additional XP required per subsequent level")] private int xpIncreasePerLevel = 5;

    private int playerLevel;
    private int currentXp;
    private int xpToNext;
    private int pendingLevelUps;

    // HUD (top XP bar)
    private GameObject hudCanvasGO;
    private Image xpFillImage;
    private Text xpText;

    // Level-up modal
    private GameObject levelUpCanvasGO;

    // Animation
    [Header("Animation")]
    [SerializeField, Tooltip("Food pulsation speed (cycles/sec)")] private float foodPulseSpeed = 1.6f;
    [SerializeField, Tooltip("Food pulsation amplitude (scale add)")] private float foodPulseAmplitude = 0.12f;
    [SerializeField, Tooltip("Snake pulse amplitude on eat")] private float snakePulseAmplitude = 0.18f;
    [SerializeField, Tooltip("Duration of one segment pulse (sec)")] private float snakePulseDuration = 0.2f;
    [SerializeField, Tooltip("Delay between segment pulses (sec)")] private float snakePulseDelayPerSegment = 0.05f;
    [SerializeField, Tooltip("Max stacked pulse amplitude")] private float snakePulseMaxStack = 0.35f;

    private List<float> snakePulseWaveStartTimes;
    private List<float> foodPulsePhaseOffsets;

    [Header("Audio")]
    [SerializeField, Tooltip("Enable/disable all game sounds")] private bool enableSound = true;
    [SerializeField, Range(0f, 1f), Tooltip("Master volume for SFX")] private float sfxVolume = 0.8f;
    private AudioSource audioSource;
    private AudioClip sfxMove;
    private AudioClip sfxTurn;
    private AudioClip sfxEat;
    private AudioClip sfxDeath;

    private void Start()
    {
        SetupCamera();
        EnsureRuntimeAssets();
        EnsureAudio();
        EnsureEventSystemExists();
        EnsureHudExists();
        // Capture default speed so upgrades don't persist across restarts
        defaultMoveIntervalSeconds = moveIntervalSeconds;
        StartNewGame();
    }

    private void Update()
    {
        HandleInput();

        // Allow restart at any time
        if (GetRestartPressed())
        {
            StartNewGame();
            return;
        }

        if (!isAlive || isPaused)
        {
            return;
        }

        float dt = Time.deltaTime;
        moveTimer += dt;
        if (moveTimer >= moveIntervalSeconds)
        {
            moveTimer -= moveIntervalSeconds;
            StepGame();
        }
        // Continuous visual updates (pulsations)
        UpdateFoodPulse(Time.time);
        UpdateSnakePulse(Time.time);
    }

    private void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;

        cam.orthographic = true;
        // Fit entire grid in view accounting for aspect ratio; add margin for borders
        float margin = 0.6f;
        float halfHeight = (gridHeight * 0.5f) + margin;
        float halfWidth = (gridWidth * 0.5f) + margin;
        float sizeByHeight = halfHeight;
        float sizeByWidth = cam.aspect > 0f ? (halfWidth / cam.aspect) : halfHeight;
        float uiPad = Mathf.Max(0f, topUiWorldMargin);
        float baseSize = Mathf.Max(sizeByHeight, sizeByWidth);
        cam.orthographicSize = baseSize + (uiPad * 0.5f);
        float centerX = (gridWidth - 1) * 0.5f;
        float centerY = (gridHeight - 1) * 0.5f + (uiPad * 0.5f);
        cam.transform.position = new Vector3(centerX, centerY, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = backgroundColor;
    }

    private void EnsureRuntimeAssets()
    {
        if (renderContainer == null)
        {
            var containerGO = new GameObject("SnakeRenderContainer");
            renderContainer = containerGO.transform;
        }

        if (cellSprite == null)
        {
            // Create a 1x1 white sprite to be tinted by SpriteRenderer.color
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.name = "CellTexture";
            tex.SetPixel(0, 0, Color.white);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            cellSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
            cellSprite.name = "CellSprite";
        }

        if (snakeSprite == null)
        {
            snakeSprite = GenerateSnakeSprite();
        }
        if (arrowSprite == null)
        {
            arrowSprite = GenerateArrowSprite();
        }
        if (foodSprites == null || foodSprites.Length != 5)
        {
            foodSprites = GenerateFoodSprites();
        }

        BuildBackgroundGrid();
        BuildBorders();
    }

    private void StartNewGame()
    {
        // Reset state
        snakeCells = new LinkedList<Vector2Int>();
        snakeCellSet = new HashSet<Vector2Int>();
        moveTimer = 0f;
        isAlive = true;
        isPaused = false;
        moveIntervalSeconds = defaultMoveIntervalSeconds;
        foodNeedsSprite = false;
        pendingLevelUps = 0;
        snakePulseWaveStartTimes = new List<float>();
        foodPulsePhaseOffsets = new List<float>();

        // Reset progression
        playerLevel = 1;
        currentXp = 0;
        xpToNext = baseXpToNext;
        UpdateHud();

        // Initial snake of length 2, centered
        Vector2Int head = new Vector2Int(gridWidth / 2, gridHeight / 2);
        Vector2Int tail = head + Vector2Int.left; // tail behind head

        snakeCells.AddFirst(head);
        snakeCells.AddLast(tail);
        snakeCellSet.Add(head);
        snakeCellSet.Add(tail);

        currentDirection = Vector2Int.right;
        nextDirection = currentDirection;

        // Reset foods
        ClearFoods();
        maxFoodCount = 1;
        EnsureFoodContainers();
        EnsureFoodCount();
        RenderWorld(fullRebuild: true);

        // Hide any previous game over UI
        HideGameOverUI();
        HideLevelUpUI();
    }

    private void HandleInput()
    {
        // Read last arrow key pressed and set as next direction if not opposite
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.upArrowKey.wasPressedThisFrame)
            {
                TrySetNextDirection(Vector2Int.up);
                return;
            }
            if (kb.downArrowKey.wasPressedThisFrame)
            {
                TrySetNextDirection(Vector2Int.down);
                return;
            }
            if (kb.leftArrowKey.wasPressedThisFrame)
            {
                TrySetNextDirection(Vector2Int.left);
                return;
            }
            if (kb.rightArrowKey.wasPressedThisFrame)
            {
                TrySetNextDirection(Vector2Int.right);
                return;
            }
        }
#else
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            TrySetNextDirection(Vector2Int.up);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            TrySetNextDirection(Vector2Int.down);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            TrySetNextDirection(Vector2Int.left);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            TrySetNextDirection(Vector2Int.right);
        }
#endif
    }

    private bool GetRestartPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && kb.rKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.R);
#endif
    }

    private void TrySetNextDirection(Vector2Int desired)
    {
        // Prevent reversing into yourself (180° turn) based on current direction
        if (desired + currentDirection == Vector2Int.zero)
        {
            return;
        }
        nextDirection = desired;
    }

    private void StepGame()
    {
        // Apply buffered direction exactly on tick
        bool turnedThisStep = currentDirection != nextDirection;
        currentDirection = nextDirection;

        var currentHead = snakeCells.First.Value;
        var nextHead = currentHead + currentDirection;

        bool outOfBounds = nextHead.x < 0 || nextHead.x >= gridWidth || nextHead.y < 0 || nextHead.y >= gridHeight;
        int eatenFoodIndex = -1;
        bool willGrow = false;
        if (foodCells != null && foodCells.Count > 0)
        {
            // Linear scan; food count is small
            for (int i = 0; i < foodCells.Count; i++)
            {
                if (foodCells[i] == nextHead)
                {
                    eatenFoodIndex = i;
                    willGrow = true;
                    break;
                }
            }
        }

        // Check self-collision. Moving into the current tail cell is allowed if we are NOT growing
        bool hitsSelf = false;
        if (snakeCellSet.Contains(nextHead))
        {
            var currentTail = snakeCells.Last.Value;
            bool movingIntoTailCell = nextHead == currentTail;
            if (!(movingIntoTailCell && !willGrow))
            {
                hitsSelf = true;
            }
        }

        if (outOfBounds || hitsSelf)
        {
            PlaySfx(sfxDeath, 1f);
            GameOver();
            return;
        }

        // Move head
        snakeCells.AddFirst(nextHead);
        snakeCellSet.Add(nextHead);

        if (willGrow)
        {
            // Play eat sound for collecting food; supersedes move/turn sound this tick
            PlaySfx(sfxEat, 1f);
            OnFoodEaten(eatenFoodIndex);
        }
        else
        {
            // Remove tail
            var tail = snakeCells.Last.Value;
            snakeCells.RemoveLast();
            snakeCellSet.Remove(tail);
            // Play movement sound every step; add turn sound when direction changed
            if (turnedThisStep)
            {
                PlaySfx(sfxMove, 1f);
                PlaySfx(sfxTurn, 1f);
            }
            else
            {
                PlaySfx(sfxMove, 1f);
            }
        }

        RenderWorld(fullRebuild: false);
    }

    private void SpawnFood()
    {
        // Spawn a single food at a random empty cell (not occupied by snake or existing food)
        if (foodCells == null) EnsureFoodContainers();
        int maxCells = gridWidth * gridHeight;
        if (snakeCellSet.Count + (foodCells?.Count ?? 0) >= maxCells)
        {
            GameOver();
            return;
        }

        for (int safety = 0; safety < 10000; safety++)
        {
            int x = Random.Range(0, gridWidth);
            int y = Random.Range(0, gridHeight);
            var p = new Vector2Int(x, y);
            if (!snakeCellSet.Contains(p) && (foodCells == null || !foodCells.Contains(p)))
            {
                foodCells.Add(p);
                // Ensure there is a visual object for this food
                EnsureFoodObjectForIndex(foodCells.Count - 1);
                // Randomize sprite each time we spawn a food
                SetFoodSpriteForIndex(foodCells.Count - 1);
                // Assign a random phase so foods are out of sync
                if (foodPulsePhaseOffsets != null)
                {
                    while (foodPulsePhaseOffsets.Count < foodCells.Count)
                    {
                        foodPulsePhaseOffsets.Add(Random.Range(0f, Mathf.PI * 2f));
                    }
                }
                RenderFood();
                return;
            }
        }

        // Fallback scan
        for (int yy = 0; yy < gridHeight; yy++)
        {
            for (int xx = 0; xx < gridWidth; xx++)
            {
                var p = new Vector2Int(xx, yy);
                if (!snakeCellSet.Contains(p) && (foodCells == null || !foodCells.Contains(p)))
                {
                    foodCells.Add(p);
                    EnsureFoodObjectForIndex(foodCells.Count - 1);
                    SetFoodSpriteForIndex(foodCells.Count - 1);
                    if (foodPulsePhaseOffsets != null)
                    {
                        while (foodPulsePhaseOffsets.Count < foodCells.Count)
                        {
                            foodPulsePhaseOffsets.Add(Random.Range(0f, Mathf.PI * 2f));
                        }
                    }
                    RenderFood();
                    return;
                }
            }
        }

        GameOver();
    }

    private void GameOver()
    {
        isAlive = false;
#if UNITY_EDITOR
        Debug.Log("Game Over. Press R to restart.");
#endif
        ShowGameOverUI();
    }

    private void EnsureAudio()
    {
        if (!enableSound) return;
        if (audioSource == null)
        {
            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D audio
            }
        }
        audioSource.volume = 1f; // we'll control loudness per one-shot via sfxVolume multiplier

        // Lazily build clips if missing
        if (sfxMove == null)
        {
            // Short, light beep
            sfxMove = CreateToneClip(
                name: "sfx_move",
                durationSeconds: 0.055f,
                startFrequencyHz: 520f,
                endFrequencyHz: 560f,
                gain: 0.35f,
                waveform: Waveform.Sine,
                attackSeconds: 0.002f,
                releaseSeconds: 0.05f
            );
        }
        if (sfxTurn == null)
        {
            // Short, dull low thud-like tone
            sfxTurn = CreateToneClip(
                name: "sfx_turn",
                durationSeconds: 0.07f,
                startFrequencyHz: 170f,
                endFrequencyHz: 150f,
                gain: 0.45f,
                waveform: Waveform.Sine,
                attackSeconds: 0.001f,
                releaseSeconds: 0.06f
            );
        }
        if (sfxEat == null)
        {
            // High, short upward chirp
            sfxEat = CreateToneClip(
                name: "sfx_eat",
                durationSeconds: 0.09f,
                startFrequencyHz: 950f,
                endFrequencyHz: 1400f,
                gain: 0.4f,
                waveform: Waveform.Sine,
                attackSeconds: 0.001f,
                releaseSeconds: 0.05f
            );
        }
        if (sfxDeath == null)
        {
            // Long, sad downward glide
            sfxDeath = CreateToneClip(
                name: "sfx_death",
                durationSeconds: 1.1f,
                startFrequencyHz: 360f,
                endFrequencyHz: 110f,
                gain: 0.5f,
                waveform: Waveform.Sine,
                attackSeconds: 0.005f,
                releaseSeconds: 0.35f
            );
        }
    }

    private void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (!enableSound || clip == null) return;
        if (audioSource == null)
        {
            EnsureAudio();
            if (audioSource == null) return;
        }
        audioSource.PlayOneShot(clip, Mathf.Clamp01(volume) * sfxVolume);
    }

    private enum Waveform
    {
        Sine,
        Square
    }

    private AudioClip CreateToneClip(string name, float durationSeconds, float startFrequencyHz, float endFrequencyHz, float gain, Waveform waveform, float attackSeconds, float releaseSeconds)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(sampleRate * durationSeconds));
        var data = new float[sampleCount];
        float phase = 0f;
        float twoPi = 2f * Mathf.PI;
        float attackFrac = Mathf.Clamp01(attackSeconds / Mathf.Max(0.0001f, durationSeconds));
        float releaseFrac = Mathf.Clamp01(releaseSeconds / Mathf.Max(0.0001f, durationSeconds));
        for (int i = 0; i < sampleCount; i++)
        {
            float progress = sampleCount > 1 ? (i / (float)(sampleCount - 1)) : 0f; // 0..1
            float freq = Mathf.Lerp(startFrequencyHz, endFrequencyHz, progress);
            phase += twoPi * freq / sampleRate;
            if (phase > twoPi) phase -= twoPi;
            float raw = waveform == Waveform.Sine ? Mathf.Sin(phase) : Mathf.Sign(Mathf.Sin(phase));

            // Simple AR envelope
            float amp = gain;
            if (attackFrac > 0f && progress < attackFrac)
            {
                amp *= (progress / attackFrac);
            }
            else if (releaseFrac > 0f && progress > 1f - releaseFrac)
            {
                float relPos = (progress - (1f - releaseFrac)) / releaseFrac;
                amp *= (1f - relPos);
            }

            data[i] = raw * amp;
        }

        var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private void RenderWorld(bool fullRebuild)
    {
        if (fullRebuild)
        {
            // Ensure enough segment objects exist
            EnsureSegmentObjects(snakeCells.Count);
        }
        else
        {
            // Grow list if snake grew
            if (segmentObjects.Count < snakeCells.Count)
            {
                EnsureSegmentObjects(snakeCells.Count - segmentObjects.Count);
            }
        }

        // Position and color segments
        int index = 0;
        Vector2Int? prevCell = null;
        foreach (var cell in snakeCells)
        {
            var go = segmentObjects[index];
            go.SetActive(true);
            go.transform.position = new Vector3(cell.x, cell.y, 0f);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = snakeSprite;
            // Base color; per-frame scale via UpdateSnakePulse
            sr.color = Color.white;
            // Handle arrow rotation per segment
            if (index < segmentArrowTransforms.Count)
            {
                var arrow = segmentArrowTransforms[index];
                if (arrow != null)
                {
                    Vector2Int dir;
                    if (index == 0)
                    {
                        dir = currentDirection;
                    }
                    else
                    {
                        dir = prevCell.HasValue ? (prevCell.Value - cell) : Vector2Int.up;
                    }
                    float zRot = 0f;
                    if (dir == Vector2Int.up) zRot = 0f;
                    else if (dir == Vector2Int.right) zRot = -90f;
                    else if (dir == Vector2Int.down) zRot = 180f;
                    else if (dir == Vector2Int.left) zRot = 90f;
                    arrow.localRotation = Quaternion.Euler(0f, 0f, zRot);
                }
            }
            prevCell = cell;
            index++;
        }
        // Disable any extras (and keep arrow lists aligned)
        for (int i = index; i < segmentObjects.Count; i++)
        {
            segmentObjects[i].SetActive(false);
        }

        RenderFood();
    }

    private void EnsureSegmentObjects(int countToAdd)
    {
        if (renderContainer == null) EnsureRuntimeAssets();
        for (int i = 0; i < countToAdd; i++)
        {
            var go = CreateCellGO("SnakeSegment", Color.white, snakeSprite);
            segmentObjects.Add(go);
            // Add arrow child to visualize direction
            var arrow = new GameObject("Arrow");
            arrow.transform.SetParent(go.transform, false);
            var srA = arrow.AddComponent<SpriteRenderer>();
            srA.sprite = arrowSprite;
            srA.color = new Color(0.95f, 0.98f, 1f, 0.92f);
            srA.sortingOrder = 1;
            segmentArrowTransforms.Add(arrow.transform);
        }
    }

    private void RenderFood()
    {
        if (foodCells == null) return;
        EnsureFoodContainers();
        // Ensure there are enough objects to represent current foods
        while (foodObjects.Count < foodCells.Count)
        {
            EnsureFoodObjectForIndex(foodObjects.Count);
        }
        // Position active
        for (int i = 0; i < foodObjects.Count; i++)
        {
            bool active = i < foodCells.Count;
            var obj = foodObjects[i];
            if (obj == null) continue;
            obj.SetActive(active);
            if (active)
            {
                var pos = foodCells[i];
                obj.transform.position = new Vector3(pos.x, pos.y, 0f);
                // Base scale reset; animated every Update
                obj.transform.localScale = Vector3.one;
            }
        }
    }

    private void EnsureFoodContainers()
    {
        if (foodCells == null) foodCells = new List<Vector2Int>();
        if (foodObjects == null) foodObjects = new List<GameObject>();
    }

    private void UpdateSnakePulse(float timeNow)
    {
        if (segmentObjects == null || segmentObjects.Count == 0) return;
        if (snakePulseWaveStartTimes == null || snakePulseWaveStartTimes.Count == 0) return;
        for (int i = 0; i < segmentObjects.Count; i++)
        {
            var seg = segmentObjects[i];
            if (seg == null || !seg.activeSelf) continue;
            float baseScale = 1f;
            float scale = baseScale;
            // Sum contributions from all active waves; each wave starts at head with per-segment delay
            // We store multiple wave start times by pushing new entries; reuse same list, interpret as wave origins
            // To support multiple overlapping waves, we keep a small ring by clamping size (optional)
            for (int w = 0; w < snakePulseWaveStartTimes.Count; w++)
            {
                float t0 = snakePulseWaveStartTimes[w];
                if (t0 <= -9000f) continue;
                float localT = timeNow - t0 - (i * snakePulseDelayPerSegment);
                if (localT >= 0f && localT <= snakePulseDuration)
                {
                    // Simple ease: sin pulse 0..pi over duration
                    float phase = Mathf.PI * (localT / snakePulseDuration);
                    scale += Mathf.Sin(phase) * snakePulseAmplitude;
                }
            }
            // Cap extreme stacking
            scale = Mathf.Min(baseScale + snakePulseMaxStack, scale);
            seg.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    private void StartSnakePulseWave(float timeNow)
    {
        if (snakePulseWaveStartTimes == null) snakePulseWaveStartTimes = new List<float>();
        // Push new wave timestamp; keep only last few to avoid unbounded growth
        snakePulseWaveStartTimes.Add(timeNow);
        int maxWaves = 8;
        if (snakePulseWaveStartTimes.Count > maxWaves)
        {
            int remove = snakePulseWaveStartTimes.Count - maxWaves;
            snakePulseWaveStartTimes.RemoveRange(0, remove);
        }
    }

    private void EnsureFoodObjectForIndex(int index)
    {
        if (renderContainer == null) EnsureRuntimeAssets();
        while (foodObjects.Count <= index)
        {
            var go = CreateCellGO("Food", Color.white, cellSprite);
            foodObjects.Add(go);
        }
    }

    private void UpdateFoodPulse(float timeNow)
    {
        if (foodObjects == null || foodObjects.Count == 0) return;
        float twoPi = Mathf.PI * 2f;
        for (int i = 0; i < foodObjects.Count; i++)
        {
            var obj = foodObjects[i];
            if (obj == null || !obj.activeSelf) continue;
            float phase = (foodPulsePhaseOffsets != null && i < foodPulsePhaseOffsets.Count) ? foodPulsePhaseOffsets[i] : 0f;
            float s = 1f + Mathf.Sin((timeNow * twoPi * foodPulseSpeed) + phase) * foodPulseAmplitude;
            obj.transform.localScale = new Vector3(s, s, 1f);
        }
    }

    private void SetFoodSpriteForIndex(int index)
    {
        if (index < 0 || index >= foodObjects.Count) return;
        var obj = foodObjects[index];
        if (obj == null) return;
        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr == null) return;
        if (foodSprites != null && foodSprites.Length > 0)
        {
            int idx = Random.Range(0, foodSprites.Length);
            sr.sprite = foodSprites[idx];
        }
        else
        {
            sr.sprite = cellSprite;
        }
    }

    private void EnsureFoodCount()
    {
        if (foodCells == null) EnsureFoodContainers();
        while (foodCells.Count < maxFoodCount)
        {
            SpawnFood();
        }
    }

    private void ClearFoods()
    {
        if (foodObjects != null)
        {
            foreach (var go in foodObjects)
            {
                if (go != null) Destroy(go);
            }
            foodObjects.Clear();
        }
        if (foodCells != null) foodCells.Clear();
        if (foodPulsePhaseOffsets != null) foodPulsePhaseOffsets.Clear();
    }

    private GameObject CreateCellGO(string baseName, Color tint, Sprite sprite)
    {
        var go = new GameObject(baseName);
        go.transform.SetParent(renderContainer, worldPositionStays: false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite != null ? sprite : cellSprite;
        sr.color = tint;
        sr.sortingOrder = 0;
        // Scale to 1x1 world units (already is, but be explicit)
        go.transform.localScale = Vector3.one;
        return go;
    }

    private void BuildBorders()
    {
        if (borderContainer == null)
        {
            var go = new GameObject("Borders");
            borderContainer = go.transform;
            borderContainer.SetParent(renderContainer, worldPositionStays: false);
        }

        // Helper to create or reuse a border segment
        GameObject EnsureBorder(ref GameObject obj, string name)
        {
            if (obj == null)
            {
                obj = new GameObject(name);
                obj.transform.SetParent(borderContainer, worldPositionStays: false);
                var sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite = cellSprite;
                sr.color = borderColor;
                sr.sortingOrder = -1; // behind snake and food
            }
            return obj;
        }

        // Top
        EnsureBorder(ref borderTop, "Top");
        borderTop.transform.position = new Vector3((gridWidth - 1) * 0.5f, gridHeight - 0.5f, 0f);
        borderTop.transform.localScale = new Vector3(gridWidth, Mathf.Max(0.01f, borderThickness), 1f);

        // Bottom
        EnsureBorder(ref borderBottom, "Bottom");
        borderBottom.transform.position = new Vector3((gridWidth - 1) * 0.5f, -0.5f, 0f);
        borderBottom.transform.localScale = new Vector3(gridWidth, Mathf.Max(0.01f, borderThickness), 1f);

        // Left
        EnsureBorder(ref borderLeft, "Left");
        borderLeft.transform.position = new Vector3(-0.5f, (gridHeight - 1) * 0.5f, 0f);
        borderLeft.transform.localScale = new Vector3(Mathf.Max(0.01f, borderThickness), gridHeight, 1f);

        // Right
        EnsureBorder(ref borderRight, "Right");
        borderRight.transform.position = new Vector3(gridWidth - 0.5f, (gridHeight - 1) * 0.5f, 0f);
        borderRight.transform.localScale = new Vector3(Mathf.Max(0.01f, borderThickness), gridHeight, 1f);
    }

    private void BuildBackgroundGrid()
    {
        if (renderContainer == null) EnsureRuntimeAssets();
        if (backgroundContainer == null)
        {
            var go = new GameObject("BackgroundGrid");
            backgroundContainer = go.transform;
            backgroundContainer.SetParent(renderContainer, worldPositionStays: false);
        }

        // Clear previous tiles if grid size changed
        if (backgroundTiles.Count > 0)
        {
            for (int i = 0; i < backgroundTiles.Count; i++)
            {
                var t = backgroundTiles[i];
                if (t != null) Destroy(t);
            }
            backgroundTiles.Clear();
        }

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                var tile = new GameObject($"Cell_{x}_{y}");
                tile.transform.SetParent(backgroundContainer, false);
                tile.transform.position = new Vector3(x, y, 0f);
                var sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = cellSprite;
                bool even = (((x + y) & 1) == 0);
                sr.color = even ? gridColorLight : gridColorDark;
                sr.sortingOrder = -2; // behind borders, snake and food
                backgroundTiles.Add(tile);
            }
        }
    }

    // ===== Progression / Leveling =====
    private void AddXp(int amount)
    {
        currentXp += Mathf.Max(0, amount);
        // Queue level-ups; process one modal at a time
        while (currentXp >= xpToNext)
        {
            currentXp -= xpToNext;
            playerLevel++;
            xpToNext = baseXpToNext + (playerLevel - 1) * xpIncreasePerLevel;
            pendingLevelUps++;
        }
        UpdateHud();
        TryShowLevelUpModal();
    }

    private void TryShowLevelUpModal()
    {
        if (pendingLevelUps <= 0) return;
        if (levelUpCanvasGO != null) return; // already showing one
        ShowLevelUpUI();
    }

    private void OnFoodEaten(int eatenIndex)
    {
        if (eatenIndex >= 0 && eatenIndex < (foodCells?.Count ?? 0))
        {
            foodCells.RemoveAt(eatenIndex);
            if (foodObjects != null && eatenIndex < foodObjects.Count)
            {
                var obj = foodObjects[eatenIndex];
                if (obj != null) Destroy(obj);
                foodObjects.RemoveAt(eatenIndex);
            }
            if (foodPulsePhaseOffsets != null && eatenIndex < foodPulsePhaseOffsets.Count)
            {
                foodPulsePhaseOffsets.RemoveAt(eatenIndex);
            }
        }
        AddXp(xpPerFood);
        EnsureFoodCount();
        // Launch snake pulse wave from head
        StartSnakePulseWave(Time.time);
    }

    private void EnsureHudExists()
    {
        if (hudCanvasGO != null) return;
        var canvasGO = new GameObject("HUDCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Top bar container
        var barGO = new GameObject("XPBar");
        barGO.transform.SetParent(canvasGO.transform, false);
        var barBG = barGO.AddComponent<Image>();
        barBG.color = new Color(0.08f, 0.1f, 0.14f, 0.9f);
        // Use 1x1 sprite so UI Image renders correctly
        barBG.sprite = cellSprite;
        var barRT = barGO.GetComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0f, 1f);
        barRT.anchorMax = new Vector2(1f, 1f);
        barRT.pivot = new Vector2(0.5f, 1f);
        barRT.sizeDelta = new Vector2(0, 36);
        barRT.anchoredPosition = new Vector2(0, 0);

        // Fill
        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(barGO.transform, false);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.6f, 1.0f, 0.9f);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.sprite = cellSprite;
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(1f, 1f);
        fillRT.offsetMin = new Vector2(2, 2);
        fillRT.offsetMax = new Vector2(-2, -2);

        // Text
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(barGO.transform, false);
        var text = textGO.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.95f, 0.97f, 1f, 1f);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0f, 0f);
        textRT.anchorMax = new Vector2(1f, 1f);
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        hudCanvasGO = canvasGO;
        xpFillImage = fillImg;
        xpText = text;
    }

    private void UpdateHud()
    {
        if (hudCanvasGO == null) return;
        float fill = (xpToNext > 0) ? Mathf.Clamp01(currentXp / (float)xpToNext) : 0f;
        if (xpFillImage != null) xpFillImage.fillAmount = fill;
        if (xpText != null) xpText.text = $"Ур. {playerLevel}  XP {currentXp}/{xpToNext}";
    }

    private void ShowLevelUpUI()
    {
        isPaused = true;
        EnsureEventSystemExists();

        levelUpCanvasGO = new GameObject("LevelUpCanvas");
        var canvas = levelUpCanvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        levelUpCanvasGO.AddComponent<CanvasScaler>();
        levelUpCanvasGO.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("Panel");
        panel.transform.SetParent(levelUpCanvasGO.transform, false);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.6f);
        var panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0, 0);
        panelRT.anchorMax = new Vector2(1, 1);
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        var dialog = new GameObject("Dialog");
        dialog.transform.SetParent(panel.transform, false);
        var dialogImg = dialog.AddComponent<Image>();
        dialogImg.color = new Color(0.12f, 0.14f, 0.18f, 1f);
        var dialogRT = dialog.GetComponent<RectTransform>();
        dialogRT.sizeDelta = new Vector2(560, 300);
        dialogRT.anchorMin = new Vector2(0.5f, 0.5f);
        dialogRT.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRT.anchoredPosition = Vector2.zero;

        var v = dialog.AddComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.MiddleCenter;
        v.spacing = 12f;
        v.padding = new RectOffset(20, 20, 20, 20);

        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(dialog.transform, false);
        var title = titleGO.AddComponent<Text>();
        title.text = "Повышение уровня! Выберите улучшение";
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize = 28;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.95f, 0.97f, 1f, 1f);
        var titleLE = titleGO.AddComponent<LayoutElement>();
        titleLE.minHeight = 56f;

        Button b1 = CreateUIButton(dialog.transform, "+1 еда на поле");
        b1.onClick.AddListener(() => { ApplyUpgradeExtraFood(); });

        Button b2 = CreateUIButton(dialog.transform, "Увеличить поле на +1×+1");
        b2.onClick.AddListener(() => { ApplyUpgradeExpandGrid(); });

        Button b3 = CreateUIButton(dialog.transform, "Замедлить время на 5%");
        b3.onClick.AddListener(() => { ApplyUpgradeSlowTime(); });
    }

    private void HideLevelUpUI()
    {
        if (levelUpCanvasGO != null)
        {
            Destroy(levelUpCanvasGO);
            levelUpCanvasGO = null;
        }
        isPaused = false;
        if (pendingLevelUps > 0 && levelUpCanvasGO == null)
        {
            // If more level-ups are queued, show the next one immediately
            TryShowLevelUpModal();
        }
    }

    private void ApplyUpgradeExtraFood()
    {
        maxFoodCount += 1;
        EnsureFoodCount();
        pendingLevelUps = Mathf.Max(0, pendingLevelUps - 1);
        HideLevelUpUI();
    }

    private void ApplyUpgradeExpandGrid()
    {
        gridWidth = Mathf.Max(1, gridWidth + 1);
        gridHeight = Mathf.Max(1, gridHeight + 1);
        SetupCamera();
        BuildBackgroundGrid();
        BuildBorders();
        // Ensure all objects are within new bounds and re-render
        RenderWorld(fullRebuild: true);
        EnsureFoodCount();
        pendingLevelUps = Mathf.Max(0, pendingLevelUps - 1);
        HideLevelUpUI();
    }

    private void ApplyUpgradeSlowTime()
    {
        moveIntervalSeconds *= 1.05f; // 5% slower movement (longer interval)
        pendingLevelUps = Mathf.Max(0, pendingLevelUps - 1);
        HideLevelUpUI();
    }

    private Sprite GenerateSnakeSprite()
    {
        const int size = 8;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "SnakeTexture";
        tex.filterMode = FilterMode.Point;

        // Checker pattern with subtle shading
        var cLight = new Color(0.2f, 0.8f, 0.35f, 1f);
        var cDark  = new Color(0.12f, 0.55f, 0.22f, 1f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool even = ((x + y) & 1) == 0;
                tex.SetPixel(x, y, even ? cLight : cDark);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
    }

    private Sprite[] GenerateFoodSprites()
    {
        var sprites = new Sprite[5];
        sprites[0] = GenerateSolidCircleSprite(8, new Color(0.9f, 0.25f, 0.2f, 1f), new Color(0.65f, 0.05f, 0.05f, 1f)); // red
        sprites[1] = GenerateSolidCircleSprite(8, new Color(1.0f, 0.7f, 0.2f, 1f), new Color(0.8f, 0.45f, 0.05f, 1f)); // orange
        sprites[2] = GenerateSolidCircleSprite(8, new Color(0.9f, 0.9f, 0.25f, 1f), new Color(0.7f, 0.65f, 0.05f, 1f)); // yellow
        sprites[3] = GenerateSolidCircleSprite(8, new Color(0.55f, 0.45f, 0.9f, 1f), new Color(0.35f, 0.25f, 0.7f, 1f)); // purple
        sprites[4] = GenerateSolidCircleSprite(8, new Color(0.25f, 0.75f, 1.0f, 1f), new Color(0.05f, 0.45f, 0.8f, 1f)); // blue
        return sprites;
    }

    private Sprite GenerateArrowSprite()
    {
        const int size = 8;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "ArrowTexture";
        tex.filterMode = FilterMode.Point;
        var transparent = new Color(0, 0, 0, 0);
        var arrow = new Color(0.95f, 0.98f, 1f, 1f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                tex.SetPixel(x, y, transparent);
            }
        }
        // Shaft (pointing up)
        for (int y = 0; y <= 5; y++)
        {
            tex.SetPixel(3, y, arrow);
            tex.SetPixel(4, y, arrow);
        }
        // Head rows
        int y6 = 6;
        for (int x = 2; x <= 5; x++) tex.SetPixel(x, y6, arrow);
        int y7 = 7;
        for (int x = 1; x <= 6; x++) tex.SetPixel(x, y7, arrow);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
    }

    private Sprite GenerateSolidCircleSprite(int size, Color fill, Color border)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "FoodTexture";
        tex.filterMode = FilterMode.Point;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.35f;
        float borderWidth = Mathf.Max(1f, size * 0.12f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                if (d <= radius)
                {
                    bool isBorder = d >= radius - borderWidth;
                    tex.SetPixel(x, y, isBorder ? border : fill);
                }
                else
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
    }

    private void ShowGameOverUI()
    {
        if (gameOverCanvasGO != null) return;
        EnsureEventSystemExists();

        gameOverCanvasGO = new GameObject("GameOverCanvas");
        var canvas = gameOverCanvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        gameOverCanvasGO.AddComponent<CanvasScaler>();
        gameOverCanvasGO.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("Panel");
        panel.transform.SetParent(gameOverCanvasGO.transform, false);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.6f);
        var panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0, 0);
        panelRT.anchorMax = new Vector2(1, 1);
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        var dialog = new GameObject("Dialog");
        dialog.transform.SetParent(panel.transform, false);
        var dialogImg = dialog.AddComponent<Image>();
        dialogImg.color = new Color(0.12f, 0.14f, 0.18f, 1f);
        var dialogRT = dialog.GetComponent<RectTransform>();
        dialogRT.sizeDelta = new Vector2(420, 240);
        dialogRT.anchorMin = new Vector2(0.5f, 0.5f);
        dialogRT.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRT.anchoredPosition = Vector2.zero;

        var v = dialog.AddComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.MiddleCenter;
        v.spacing = 14f;
        v.padding = new RectOffset(20, 20, 20, 20);

        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(dialog.transform, false);
        var title = titleGO.AddComponent<Text>();
        title.text = "Игра окончена";
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize = 36;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.95f, 0.97f, 1f, 1f);
        var titleLE = titleGO.AddComponent<LayoutElement>();
        titleLE.minHeight = 64f;

        Button restartBtn = CreateUIButton(dialog.transform, "Заново");
        restartBtn.onClick.AddListener(() => { HideGameOverUI(); StartNewGame(); });

        Button menuBtn = CreateUIButton(dialog.transform, "В меню");
        menuBtn.onClick.AddListener(() => { HideGameOverUI(); SceneManager.LoadScene("Menu"); });
    }

    private Button CreateUIButton(Transform parent, string label)
    {
        var go = new GameObject(label + "Button");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.18f, 0.22f, 0.28f, 1f);
        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = img.color;
        colors.highlightedColor = new Color(0.22f, 0.26f, 0.34f, 1f);
        colors.pressedColor = new Color(0.14f, 0.18f, 0.22f, 1f);
        colors.selectedColor = colors.highlightedColor;
        btn.colors = colors;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 60);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var t = textGO.AddComponent<Text>();
        t.text = label;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 28;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(0.9f, 0.95f, 1f, 1f);
        var textRT = t.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        return btn;
    }

    private void HideGameOverUI()
    {
        if (gameOverCanvasGO != null)
        {
            Destroy(gameOverCanvasGO);
            gameOverCanvasGO = null;
        }
    }

    private void EnsureEventSystemExists()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            // Use the correct UI input module depending on active input backend
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
