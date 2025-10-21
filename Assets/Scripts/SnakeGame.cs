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

    [Header("Enemies")]
    [SerializeField, Tooltip("Seconds between enemy spawns")] private float enemySpawnIntervalSeconds = 30f;
    [SerializeField, Tooltip("Damage tick interval while colliding with enemy")] private float enemyDamageIntervalSeconds = 0.5f;
    [SerializeField, Tooltip("HP per enemy")] private int enemyHpPer = 5;
    [SerializeField, Tooltip("Enemy tint color")] private Color enemyColor = new Color(0.9f, 0.25f, 0.85f, 1f);

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
    private readonly List<SpriteRenderer> segmentSpriteRenderers = new List<SpriteRenderer>();
    // Food rendering (now supports multiple food items)
    private GameObject foodObject; // legacy (unused in multi-food mode)
    private List<Vector2Int> foodCells;
    private List<GameObject> foodObjects;
    private List<GameObject> foodArrowContainers;
    private List<int> foodTargetEnemyIndex;
    private int maxFoodCount = 1;
    private Sprite cellSprite;
    private Sprite snakeSprite;
    private Sprite[] bodyFrames;
    private Sprite[] headFrames;
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
    
    // HUD (timers & enemy spawn progress)
    private Text gameTimeText;
    private Image enemySpawnFillImage;

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

    // Sprite animation
    [Header("Sprite Animation")]
    [SerializeField, Tooltip("Frames per second for snake sprite animations")] private float animationFps = 8f;
    private float animationTimer;
    private int headFrameIndex;
    private int bodyFrameIndex;

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
        // Timers and background systems (not paused)
        UpdateEnemySpawning(dt);
        UpdateEngagementDamage(dt);
        totalPlayTimeSeconds += dt;
        UpdateTimersHud();
        // Enemy food influence and eating checks tick each second
        UpdateEnemyFoodAttraction(dt);
        UpdateEnemyEating(dt);

        moveTimer += dt;
        if (moveTimer >= moveIntervalSeconds)
        {
            moveTimer -= moveIntervalSeconds;
            StepGame();
        }
        // Continuous visual updates (pulsations)
        UpdateFoodPulse(Time.time);
        UpdateSnakePulse(Time.time);
        
        // Sprite animation
        UpdateSpriteAnimations(dt);
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

        // Load sprite sheets from Resources (if present)
        LoadSnakeSpriteSheets();

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
        if (foodSprites == null || foodSprites.Length != 5)
        {
            foodSprites = GenerateFoodSprites();
        }

        BuildBackgroundGrid();
        BuildBorders();
    }

    private void LoadSnakeSpriteSheets()
    {
        // Prefer slicing ourselves by grid to ensure correct 6x6 (body) and 2x2 (head)
        if (bodyFrames == null)
        {
            var bodyTex = Resources.Load<Texture2D>("SnakeSprites/snake-body-spritesheet");
            if (bodyTex != null)
            {
                bodyTex.filterMode = FilterMode.Point;
                bodyFrames = SliceSpriteSheet(bodyTex, 6, 6);
            }
            // Fallback to importer-sliced sprites if runtime slicing failed
            if (bodyFrames == null || bodyFrames.Length == 0)
            {
                bodyFrames = Resources.LoadAll<Sprite>("SnakeSprites/snake-body-spritesheet");
                if (bodyFrames != null && bodyFrames.Length == 0) bodyFrames = null;
            }
        }

        if (headFrames == null)
        {
            var headTex = Resources.Load<Texture2D>("SnakeSprites/snake-head");
            if (headTex != null)
            {
                headTex.filterMode = FilterMode.Point;
                headFrames = SliceSpriteSheet(headTex, 2, 2);
            }
            if (headFrames == null || headFrames.Length == 0)
            {
                headFrames = Resources.LoadAll<Sprite>("SnakeSprites/snake-head");
                if (headFrames != null && headFrames.Length == 0) headFrames = null;
            }
        }

        // Fallback single-color sprite if sheets are not present
        if (snakeSprite == null)
        {
            snakeSprite = GenerateSnakeSprite();
        }
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

        // Reset timers & enemy engagement state
        totalPlayTimeSeconds = 0f;
        enemySpawnTimerSeconds = 0f;
        isEngagedWithEnemy = false;
        engagedEnemyIndex = -1;
        engagedEnemyCell = new Vector2Int(-9999, -9999);
        enemyDamageTimer = 0f;

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

        // Reset enemies
        ClearEnemies();
        EnsureEnemyContainers();
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

        // If currently engaged with an enemy, only resume movement if we are not facing into the same enemy cell anymore
        if (isEngagedWithEnemy)
        {
            if (nextHead == engagedEnemyCell)
            {
                // Still facing the enemy: do not move this tick
                return;
            }
            // Player changed intent: disengage and proceed normally
            isEngagedWithEnemy = false;
            engagedEnemyIndex = -1;
            engagedEnemyCell = new Vector2Int(-9999, -9999);
            enemyDamageTimer = 0f;
        }

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

        // Check enemy collision: if moving into an enemy, stop movement and start damage over time
        int enemyIndex = IndexOfEnemyAtCell(nextHead);
        if (enemyIndex >= 0)
        {
            isEngagedWithEnemy = true;
            engagedEnemyIndex = enemyIndex;
            engagedEnemyCell = enemyCells[enemyIndex];
            // Movement halts; damage ticks handled by UpdateEngagementDamage
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
            var sr = segmentSpriteRenderers[index];
            // Choose head/body sprite frame
            bool isHead = (index == 0);
            Sprite desiredSprite = isHead ? GetHeadFrame() : GetBodyFrame();
            if (desiredSprite == null) desiredSprite = snakeSprite;
            sr.sprite = desiredSprite;
            // Base color; per-frame scale via UpdateSnakePulse
            sr.color = Color.white;
            // Ensure scaling so the sprite fits exactly 1x1 world units
            ApplySpriteUniformScale(go.transform, desiredSprite);

            // Rotation per segment based on direction
            Vector2Int dirVec;
            if (isHead)
            {
                dirVec = currentDirection;
                go.transform.localRotation = Quaternion.Euler(0f, 0f, GetZRotationForHead(dirVec));
            }
            else
            {
                dirVec = prevCell.HasValue ? (prevCell.Value - cell) : Vector2Int.up;
                go.transform.localRotation = Quaternion.Euler(0f, 0f, GetZRotationForBody(dirVec));
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
        RenderEnemies();
    }

    private void EnsureSegmentObjects(int countToAdd)
    {
        if (renderContainer == null) EnsureRuntimeAssets();
        for (int i = 0; i < countToAdd; i++)
        {
            var go = CreateCellGO("SnakeSegment", Color.white, snakeSprite);
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = snakeSprite;
            segmentObjects.Add(go);
            segmentSpriteRenderers.Add(sr);
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
                // Update arrow towards target enemy if any
                if (foodArrowContainers != null && i < foodArrowContainers.Count)
                {
                    var arrow = foodArrowContainers[i];
                    if (arrow != null)
                    {
                        int targetIdx = (foodTargetEnemyIndex != null && i < foodTargetEnemyIndex.Count) ? foodTargetEnemyIndex[i] : -1;
                        if (targetIdx >= 0 && enemyCells != null && targetIdx < enemyCells.Count)
                        {
                            arrow.SetActive(true);
                            var enemyPos = enemyCells[targetIdx];
                            Vector2 from = new Vector2(pos.x + 0.5f, pos.y + 0.5f);
                            Vector2 to = new Vector2(enemyPos.x + 0.5f, enemyPos.y + 0.5f);
                            Vector2 dir = (to - from);
                            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f; // sprite points up by default
                            float dist = dir.magnitude;

                            // Place arrow so that its tail is at 'from' and its head at 'to'
                            // Our generated sprite has pivot at (0.5, 0.0) and height of 32 pixels per unit
                            // So localScale.y scales its length from pivot upwards
                            arrow.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                            arrow.transform.position = new Vector3(from.x, from.y, 0f);
                            arrow.transform.localScale = new Vector3(1f, dist, 1f);
                        }
                        else
                        {
                            arrow.SetActive(false);
                        }
                    }
                }
            }
        }
    }

    private void EnsureFoodContainers()
    {
        if (foodCells == null) foodCells = new List<Vector2Int>();
        if (foodObjects == null) foodObjects = new List<GameObject>();
        if (foodArrowContainers == null) foodArrowContainers = new List<GameObject>();
        if (foodTargetEnemyIndex == null) foodTargetEnemyIndex = new List<int>();
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
            // Create arrow container for this food
            if (foodArrowContainers == null) foodArrowContainers = new List<GameObject>();
            var arrow = new GameObject("FoodArrow");
            arrow.transform.SetParent(renderContainer, false);
            var sr = arrow.AddComponent<SpriteRenderer>();
            sr.sprite = GenerateDottedArrowSprite();
            sr.color = new Color(1f, 1f, 1f, 0.6f);
            sr.sortingOrder = -1; // behind food and snake
            foodArrowContainers.Add(arrow);
            if (foodTargetEnemyIndex == null) foodTargetEnemyIndex = new List<int>();
            foodTargetEnemyIndex.Add(-1);
        }
    }

    // ===================== Enemies =====================
    private List<Vector2Int> enemyCells;
    private List<int> enemyHps;
    private List<GameObject> enemyObjects;
    private float enemySpawnTimerSeconds;

    private bool isEngagedWithEnemy;
    private int engagedEnemyIndex;
    private Vector2Int engagedEnemyCell;
    private float enemyDamageTimer;

    private void EnsureEnemyContainers()
    {
        if (enemyCells == null) enemyCells = new List<Vector2Int>();
        if (enemyHps == null) enemyHps = new List<int>();
        if (enemyObjects == null) enemyObjects = new List<GameObject>();
    }

    private void ClearEnemies()
    {
        if (enemyObjects != null)
        {
            for (int i = 0; i < enemyObjects.Count; i++)
            {
                var go = enemyObjects[i];
                if (go != null) Destroy(go);
            }
            enemyObjects.Clear();
        }
        if (enemyCells != null) enemyCells.Clear();
        if (enemyHps != null) enemyHps.Clear();
        isEngagedWithEnemy = false;
        engagedEnemyIndex = -1;
        engagedEnemyCell = new Vector2Int(-9999, -9999);
        enemyDamageTimer = 0f;
    }

    private void UpdateEnemySpawning(float dt)
    {
        if (!isAlive) return;
        if (enemySpawnIntervalSeconds <= 0f) return;
        enemySpawnTimerSeconds += dt;
        while (enemySpawnTimerSeconds >= enemySpawnIntervalSeconds)
        {
            enemySpawnTimerSeconds -= enemySpawnIntervalSeconds;
            SpawnEnemy();
        }
        // Update HUD progress each frame
        UpdateTimersHud();
    }

    private void SpawnEnemy()
    {
        EnsureEnemyContainers();
        int maxCells = gridWidth * gridHeight;
        int occupied = snakeCellSet.Count + (foodCells?.Count ?? 0) + (enemyCells?.Count ?? 0);
        if (occupied >= maxCells)
        {
            // No free space -> do nothing
            return;
        }

        for (int safety = 0; safety < 10000; safety++)
        {
            int x = Random.Range(0, gridWidth);
            int y = Random.Range(0, gridHeight);
            var p = new Vector2Int(x, y);
            if (!snakeCellSet.Contains(p) && (foodCells == null || !foodCells.Contains(p)) && (enemyCells == null || !enemyCells.Contains(p)))
            {
                enemyCells.Add(p);
                enemyHps.Add(Mathf.Max(1, enemyHpPer));
                EnsureEnemyObjectForIndex(enemyCells.Count - 1);
                RenderEnemies();
                return;
            }
        }

        // Fallback scan
        for (int yy = 0; yy < gridHeight; yy++)
        {
            for (int xx = 0; xx < gridWidth; xx++)
            {
                var p = new Vector2Int(xx, yy);
                if (!snakeCellSet.Contains(p) && (foodCells == null || !foodCells.Contains(p)) && (enemyCells == null || !enemyCells.Contains(p)))
                {
                    enemyCells.Add(p);
                    enemyHps.Add(Mathf.Max(1, enemyHpPer));
                    EnsureEnemyObjectForIndex(enemyCells.Count - 1);
                    RenderEnemies();
                    return;
                }
            }
        }
    }

    private void EnsureEnemyObjectForIndex(int index)
    {
        if (renderContainer == null) EnsureRuntimeAssets();
        while (enemyObjects.Count <= index)
        {
            var go = CreateCellGO("Enemy", enemyColor, cellSprite);
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = enemyColor;
            }
            enemyObjects.Add(go);
        }
    }

    private void RenderEnemies()
    {
        if (enemyCells == null || enemyObjects == null) return;
        // Ensure enough objects
        while (enemyObjects.Count < enemyCells.Count)
        {
            EnsureEnemyObjectForIndex(enemyObjects.Count);
        }
        for (int i = 0; i < enemyObjects.Count; i++)
        {
            bool active = i < enemyCells.Count;
            var obj = enemyObjects[i];
            if (obj == null) continue;
            obj.SetActive(active);
            if (active)
            {
                var pos = enemyCells[i];
                obj.transform.position = new Vector3(pos.x, pos.y, 0f);
                obj.transform.localScale = Vector3.one;
            }
        }
    }

    private int IndexOfEnemyAtCell(Vector2Int cell)
    {
        if (enemyCells == null) return -1;
        for (int i = 0; i < enemyCells.Count; i++)
        {
            if (enemyCells[i] == cell) return i;
        }
        return -1;
    }

    private void KillEnemyAtIndex(int index)
    {
        if (index < 0 || index >= (enemyCells?.Count ?? 0)) return;
        enemyCells.RemoveAt(index);
        enemyHps.RemoveAt(index);
        if (enemyObjects != null && index < enemyObjects.Count)
        {
            var go = enemyObjects[index];
            if (go != null) Destroy(go);
            enemyObjects.RemoveAt(index);
        }
        // Adjust engaged index if needed
        if (isEngagedWithEnemy)
        {
            if (engagedEnemyIndex == index)
            {
                isEngagedWithEnemy = false;
                engagedEnemyIndex = -1;
                engagedEnemyCell = new Vector2Int(-9999, -9999);
                enemyDamageTimer = 0f;
            }
            else if (engagedEnemyIndex > index)
            {
                engagedEnemyIndex--;
            }
        }
        RenderEnemies();
    }

    private void UpdateEngagementDamage(float dt)
    {
        if (!isAlive) return;
        if (!isEngagedWithEnemy) return;
        if (engagedEnemyIndex < 0 || engagedEnemyIndex >= (enemyCells?.Count ?? 0))
        {
            // Enemy vanished; disengage
            isEngagedWithEnemy = false;
            engagedEnemyIndex = -1;
            engagedEnemyCell = new Vector2Int(-9999, -9999);
            enemyDamageTimer = 0f;
            return;
        }

        enemyDamageTimer += dt;
        while (enemyDamageTimer >= enemyDamageIntervalSeconds)
        {
            enemyDamageTimer -= enemyDamageIntervalSeconds;

            // Lose one tail segment
            if (snakeCells.Count > 0)
            {
                var tail = snakeCells.Last.Value;
                snakeCells.RemoveLast();
                snakeCellSet.Remove(tail);
                if (snakeCells.Count <= 2)
                {
                    // After losing this segment, 2 or fewer parts remain -> defeat
                    PlaySfx(sfxDeath, 1f);
                    GameOver();
                    return;
                }
            }

            // Deal 1 damage to the engaged enemy
            if (engagedEnemyIndex >= 0 && engagedEnemyIndex < enemyHps.Count)
            {
                enemyHps[engagedEnemyIndex] = Mathf.Max(0, enemyHps[engagedEnemyIndex] - 1);
                if (enemyHps[engagedEnemyIndex] <= 0)
                {
                    // Enemy dies -> remove and resume movement
                    KillEnemyAtIndex(engagedEnemyIndex);
                    // Render snake after tail loss
                    RenderWorld(fullRebuild: false);
                    return;
                }
            }

            // Render snake after tail loss
            RenderWorld(fullRebuild: false);
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

    private void UpdateSpriteAnimations(float dt)
    {
        if ((headFrames == null || headFrames.Length == 0) && (bodyFrames == null || bodyFrames.Length == 0))
        {
            return;
        }
        if (animationFps <= 0f) return;
        animationTimer += dt;
        float frameDuration = 1f / animationFps;
        if (animationTimer >= frameDuration)
        {
            int steps = Mathf.FloorToInt(animationTimer / frameDuration);
            animationTimer -= steps * frameDuration;
            if (headFrames != null && headFrames.Length > 0)
            {
                headFrameIndex = (headFrameIndex + steps) % headFrames.Length;
            }
            if (bodyFrames != null && bodyFrames.Length > 0)
            {
                bodyFrameIndex = (bodyFrameIndex + steps) % bodyFrames.Length;
            }

            // Apply frames to all active segments
            for (int i = 0; i < segmentObjects.Count; i++)
            {
                var go = segmentObjects[i];
                if (go == null || !go.activeSelf) continue;
                var sr = segmentSpriteRenderers[i];
                bool isHead = (i == 0);
                var sp = isHead ? GetHeadFrame() : GetBodyFrame();
                if (sp == null) continue;
                sr.sprite = sp;
                ApplySpriteUniformScale(go.transform, sp);
            }
        }
    }

    private Sprite GetBodyFrame()
    {
        if (bodyFrames != null && bodyFrames.Length > 0)
        {
            int idx = Mathf.Clamp(bodyFrameIndex % bodyFrames.Length, 0, bodyFrames.Length - 1);
            return bodyFrames[idx];
        }
        return snakeSprite;
    }

    private Sprite GetHeadFrame()
    {
        if (headFrames != null && headFrames.Length > 0)
        {
            int idx = Mathf.Clamp(headFrameIndex % headFrames.Length, 0, headFrames.Length - 1);
            return headFrames[idx];
        }
        return snakeSprite;
    }

    private void ApplySpriteUniformScale(Transform t, Sprite s)
    {
        if (t == null || s == null) return;
        // Fit sprite within 1x1 cell preserving aspect: scale by max dimension
        Vector2 size = s.bounds.size;
        float maxDim = Mathf.Max(size.x, size.y);
        if (maxDim <= 0f)
        {
            t.localScale = Vector3.one;
            return;
        }
        float k = 1f / maxDim;
        t.localScale = new Vector3(k, k, 1f);
    }

    // Head base orientation: facing Down (top -> bottom). Map to desired direction.
    private float GetZRotationForHead(Vector2Int dir)
    {
        // Base frame faces Down; swap left/right mapping per user report
        if (dir == Vector2Int.up) return 180f;
        if (dir == Vector2Int.right) return 90f;    // was -90 (incorrect)
        if (dir == Vector2Int.down) return 0f;
        if (dir == Vector2Int.left) return -90f;    // was 90 (incorrect)
        return 0f;
    }

    // Body base orientation: facing Right (left -> right)
    private float GetZRotationForBody(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return 90f;
        if (dir == Vector2Int.right) return 0f;
        if (dir == Vector2Int.down) return -90f;
        if (dir == Vector2Int.left) return 180f;
        return 0f;
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

    // ================= Food attraction to enemies & enemy eating =================
    private float foodAttractTimer;
    private float enemyEatTimer;

    private void UpdateEnemyFoodAttraction(float dt)
    {
        if (enemyCells == null || enemyCells.Count == 0) return;
        if (foodCells == null || foodCells.Count == 0) return;
        foodAttractTimer += dt;
        if (foodAttractTimer < 1f) return;
        foodAttractTimer -= 1f;

        // Recompute targets (one enemy per food if ties pick one randomly)
        EnsureFoodContainers();
        while (foodTargetEnemyIndex.Count < foodCells.Count) foodTargetEnemyIndex.Add(-1);
        for (int i = 0; i < foodCells.Count; i++)
        {
            var fc = foodCells[i];
            // if already adjacent (radius 1), don't move by attraction (eating handles it)
            int bestIdx = -1;
            int bestCheb = int.MaxValue;
            int equalCount = 0;
            for (int e = 0; e < enemyCells.Count; e++)
            {
                var ec = enemyCells[e];
                int dx = Mathf.Abs(ec.x - fc.x);
                int dy = Mathf.Abs(ec.y - fc.y);
                int cheb = Mathf.Max(dx, dy);
                if (cheb > 2) continue; // only consider enemies within radius 2 (Chebyshev)
                if (cheb < bestCheb)
                {
                    bestCheb = cheb;
                    bestIdx = e;
                    equalCount = 1;
                }
                else if (cheb == bestCheb)
                {
                    // equal distance -> choose randomly among equals
                    equalCount++;
                    if (Random.Range(0, equalCount) == 0)
                    {
                        bestIdx = e;
                    }
                }
            }
            // Only target if there is an enemy within Chebyshev radius 2
            foodTargetEnemyIndex[i] = bestIdx;
        }

        // Move foods within radius 2 by one cell closer, avoiding collisions with snake, foods, enemies
        // Prepare occupied set for foods to avoid duplicate positions
        var occupied = new HashSet<Vector2Int>(snakeCellSet);
        if (foodCells != null)
        {
            for (int k = 0; k < foodCells.Count; k++) occupied.Add(foodCells[k]);
        }
        if (enemyCells != null)
        {
            for (int k = 0; k < enemyCells.Count; k++) occupied.Add(enemyCells[k]);
        }

        for (int i = 0; i < foodCells.Count; i++)
        {
            int target = (i < foodTargetEnemyIndex.Count) ? foodTargetEnemyIndex[i] : -1;
            if (target < 0 || target >= enemyCells.Count) continue;
            var fc = foodCells[i];
            var ec = enemyCells[target];
            int dx0 = Mathf.Abs(ec.x - fc.x);
            int dy0 = Mathf.Abs(ec.y - fc.y);
            int cheb0 = Mathf.Max(dx0, dy0);
            if (cheb0 > 2) continue; // outside radius 2
            if (cheb0 <= 1) continue; // already adjacent

            // Preferred direction towards enemy
            Vector2Int delta = new Vector2Int(Mathf.Clamp(ec.x - fc.x, -1, 1), Mathf.Clamp(ec.y - fc.y, -1, 1));
            if (delta == Vector2Int.zero) continue;

            // Candidate moves: primary, then neighboring cells that still reduce distance
            var candidates = new List<Vector2Int>();
            candidates.Add(fc + delta);
            // Orthogonal alternatives
            if (delta.x != 0) candidates.Add(fc + new Vector2Int(delta.x, 0));
            if (delta.y != 0) candidates.Add(fc + new Vector2Int(0, delta.y));
            // Diagonal alternatives
            if (delta.x != 0 && delta.y != 0)
            {
                candidates.Add(fc + new Vector2Int(delta.x, -delta.y));
                candidates.Add(fc + new Vector2Int(-delta.x, delta.y));
            }

            // Shuffle candidates to avoid directional bias
            for (int c = 0; c < candidates.Count; c++)
            {
                int r = Random.Range(c, candidates.Count);
                (candidates[c], candidates[r]) = (candidates[r], candidates[c]);
            }

            foreach (var cand in candidates)
            {
                if (cand.x < 0 || cand.y < 0 || cand.x >= gridWidth || cand.y >= gridHeight) continue;
                if (occupied.Contains(cand)) continue;
                // Ensure Chebyshev distance reduced
                int dx1 = Mathf.Abs(ec.x - cand.x);
                int dy1 = Mathf.Abs(ec.y - cand.y);
                int cheb1 = Mathf.Max(dx1, dy1);
                if (cheb1 >= cheb0) continue;
                // Move food
                occupied.Remove(fc);
                foodCells[i] = cand;
                occupied.Add(cand);
                break;
            }
        }

        RenderFood();
    }

    private void UpdateEnemyEating(float dt)
    {
        if (enemyCells == null || enemyCells.Count == 0) return;
        if (foodCells == null || foodCells.Count == 0) return;
        enemyEatTimer += dt;
        if (enemyEatTimer < 1f) return;
        enemyEatTimer -= 1f;

        // Each second, each enemy may eat at most one adjacent food. Randomize neighbor order.
        var neighborDirs = new List<Vector2Int>
        {
            new Vector2Int(1,0), new Vector2Int(-1,0), new Vector2Int(0,1), new Vector2Int(0,-1),
            new Vector2Int(1,1), new Vector2Int(1,-1), new Vector2Int(-1,1), new Vector2Int(-1,-1)
        };
        for (int e = 0; e < enemyCells.Count; e++)
        {
            // Shuffle order randomly per enemy
            for (int i = 0; i < neighborDirs.Count; i++)
            {
                int r = Random.Range(i, neighborDirs.Count);
                (neighborDirs[i], neighborDirs[r]) = (neighborDirs[r], neighborDirs[i]);
            }
            var ec = enemyCells[e];
            bool ateOne = false;
            foreach (var d in neighborDirs)
            {
                var cell = ec + d;
                int idx = IndexOfFoodAtCell(cell);
                if (idx >= 0)
                {
                    // Remove food
                    RemoveFoodAt(idx);
                    ateOne = true;
                    break;
                }
            }
            // only one per enemy per tick
        }
        RenderFood();
    }

    private int IndexOfFoodAtCell(Vector2Int cell)
    {
        if (foodCells == null) return -1;
        for (int i = 0; i < foodCells.Count; i++) if (foodCells[i] == cell) return i;
        return -1;
    }

    private void RemoveFoodAt(int index)
    {
        if (index < 0 || index >= (foodCells?.Count ?? 0)) return;
        foodCells.RemoveAt(index);
        if (foodObjects != null && index < foodObjects.Count)
        {
            var obj = foodObjects[index];
            if (obj != null) Destroy(obj);
            foodObjects.RemoveAt(index);
        }
        if (foodArrowContainers != null && index < foodArrowContainers.Count)
        {
            var arr = foodArrowContainers[index];
            if (arr != null) Destroy(arr);
            foodArrowContainers.RemoveAt(index);
        }
        if (foodTargetEnemyIndex != null && index < foodTargetEnemyIndex.Count)
        {
            foodTargetEnemyIndex.RemoveAt(index);
        }
        if (foodPulsePhaseOffsets != null && index < foodPulsePhaseOffsets.Count)
        {
            foodPulsePhaseOffsets.RemoveAt(index);
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
        if (foodArrowContainers != null)
        {
            foreach (var go in foodArrowContainers)
            {
                if (go != null) Destroy(go);
            }
            foodArrowContainers.Clear();
        }
        if (foodCells != null) foodCells.Clear();
        if (foodTargetEnemyIndex != null) foodTargetEnemyIndex.Clear();
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
        text.font = PixelFontProvider.Get();
        text.fontSize = 24;
        text.fontStyle = FontStyle.Bold;
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

        // Game Time text under XP bar (top-left)
        var timeGO = new GameObject("GameTime");
        timeGO.transform.SetParent(canvasGO.transform, false);
        var timeText = timeGO.AddComponent<Text>();
        timeText.font = PixelFontProvider.Get();
        timeText.fontSize = 24;
        timeText.fontStyle = FontStyle.Bold;
        timeText.alignment = TextAnchor.UpperLeft;
        timeText.color = new Color(0.9f, 0.95f, 1f, 1f);
        var timeRT = timeGO.GetComponent<RectTransform>();
        timeRT.anchorMin = new Vector2(0f, 1f);
        timeRT.anchorMax = new Vector2(0f, 1f);
        timeRT.pivot = new Vector2(0f, 1f);
        timeRT.sizeDelta = new Vector2(240, 28);
        timeRT.anchoredPosition = new Vector2(10, -40);
        gameTimeText = timeText;

        // Enemy spawn progress bar under XP bar (top-right)
        var enemyBarGO = new GameObject("EnemySpawnBar");
        enemyBarGO.transform.SetParent(canvasGO.transform, false);
        var enemyBarBG = enemyBarGO.AddComponent<Image>();
        enemyBarBG.color = new Color(0.08f, 0.1f, 0.14f, 0.9f);
        enemyBarBG.sprite = cellSprite;
        var enemyBarRT = enemyBarGO.GetComponent<RectTransform>();
        enemyBarRT.anchorMin = new Vector2(1f, 1f);
        enemyBarRT.anchorMax = new Vector2(1f, 1f);
        enemyBarRT.pivot = new Vector2(1f, 1f);
        enemyBarRT.sizeDelta = new Vector2(140, 18);
        enemyBarRT.anchoredPosition = new Vector2(-10, -42);

        var enemyFillGO = new GameObject("Fill");
        enemyFillGO.transform.SetParent(enemyBarGO.transform, false);
        var enemyFillImg = enemyFillGO.AddComponent<Image>();
        enemyFillImg.color = new Color(1.0f, 0.35f, 0.8f, 0.95f);
        enemyFillImg.type = Image.Type.Filled;
        enemyFillImg.fillMethod = Image.FillMethod.Horizontal;
        enemyFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        enemyFillImg.sprite = cellSprite;
        var enemyFillRT = enemyFillGO.GetComponent<RectTransform>();
        enemyFillRT.anchorMin = new Vector2(0f, 0f);
        enemyFillRT.anchorMax = new Vector2(1f, 1f);
        enemyFillRT.offsetMin = new Vector2(2, 2);
        enemyFillRT.offsetMax = new Vector2(-2, -2);
        enemySpawnFillImage = enemyFillImg;
    }

    private void UpdateHud()
    {
        if (hudCanvasGO == null) return;
        float fill = (xpToNext > 0) ? Mathf.Clamp01(currentXp / (float)xpToNext) : 0f;
        if (xpFillImage != null) xpFillImage.fillAmount = fill;
        if (xpText != null) xpText.text = $"Lv. {playerLevel}  XP {currentXp}/{xpToNext}";
    }

    private float totalPlayTimeSeconds;

    private void UpdateTimersHud()
    {
        if (hudCanvasGO == null) return;
        // Time text
        if (gameTimeText != null)
        {
            gameTimeText.text = $"Time {FormatTime(totalPlayTimeSeconds)}";
        }
        // Enemy spawn progress
        if (enemySpawnFillImage != null && enemySpawnIntervalSeconds > 0f)
        {
            float p = Mathf.Clamp01(enemySpawnTimerSeconds / Mathf.Max(0.0001f, enemySpawnIntervalSeconds));
            enemySpawnFillImage.fillAmount = p;
        }
    }

    private string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int mins = total / 60;
        int secs = total % 60;
        return $"{mins:00}:{secs:00}";
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
        title.text = "Level up! Choose an upgrade";
        title.font = PixelFontProvider.Get();
        title.fontSize = 32;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.95f, 0.97f, 1f, 1f);
        var titleLE = titleGO.AddComponent<LayoutElement>();
        titleLE.minHeight = 72f;

        var buttons = new List<Button>();
        Button b1 = CreateUIButton(dialog.transform, "+1 food on field");
        b1.onClick.AddListener(() => { ApplyUpgradeExtraFood(); });
        buttons.Add(b1);

        Button b2 = CreateUIButton(dialog.transform, "Increase grid by +1x+1");
        b2.onClick.AddListener(() => { ApplyUpgradeExpandGrid(); });
        buttons.Add(b2);

        Button b3 = CreateUIButton(dialog.transform, "Slow time by 5%");
        b3.onClick.AddListener(() => { ApplyUpgradeSlowTime(); });
        buttons.Add(b3);

        // Keyboard navigation: Up/Down to change selection, Enter to confirm
        var nav = dialog.AddComponent<LevelUpController>();
        nav.Initialize(buttons);
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

    private Sprite[] SliceSpriteSheet(Texture2D texture, int columns, int rows)
    {
        if (texture == null || columns <= 0 || rows <= 0) return null;
        int texW = texture.width;
        int texH = texture.height;
        int cellW = texW / columns;
        int cellH = texH / rows;
        if (cellW <= 0 || cellH <= 0) return null;

        var sprites = new List<Sprite>(columns * rows);
        // Unity's texture origin is bottom-left; user described head base oriented top->bottom
        // We iterate rows from top to bottom to match expected order visually
        for (int row = rows - 1; row >= 0; row--)
        {
            for (int col = 0; col < columns; col++)
            {
                int x = col * cellW;
                int y = row * cellH;
                var rect = new Rect(x, y, cellW, cellH);
                var sp = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), cellW, 0, SpriteMeshType.FullRect);
                sprites.Add(sp);
            }
        }
        return sprites.ToArray();
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
        // Legacy unused
        const int size = 8;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "ArrowTextureUnused";
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                tex.SetPixel(x, y, new Color(0, 0, 0, 0));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
    }

    private Sprite GenerateDottedArrowSprite()
    {
        // Create a thin dotted line arrow (8x32) pointing up; we'll rotate towards enemy
        int w = 8, h = 32;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.name = "DottedArrowTexture";
        tex.filterMode = FilterMode.Point;
        Color transparent = new Color(0, 0, 0, 0);
        Color white = Color.white;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                tex.SetPixel(x, y, transparent);
            }
        }
        // Dots every 4 pixels, center column 2..5
        for (int y = 0; y < h - 6; y += 4)
        {
            for (int x = 2; x <= 5; x++) tex.SetPixel(x, y, white);
        }
        // Arrow head (a small triangle at the top)
        int baseY = h - 6;
        tex.SetPixel(3, baseY + 0, white);
        tex.SetPixel(3, baseY + 1, white);
        tex.SetPixel(2, baseY + 1, white);
        tex.SetPixel(4, baseY + 1, white);
        tex.SetPixel(3, baseY + 2, white);
        tex.SetPixel(2, baseY + 2, white);
        tex.SetPixel(4, baseY + 2, white);
        tex.SetPixel(1, baseY + 3, white);
        tex.SetPixel(5, baseY + 3, white);
        tex.SetPixel(0, baseY + 4, white);
        tex.SetPixel(6, baseY + 4, white);
        tex.SetPixel(3, baseY + 4, white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.0f), h, 0, SpriteMeshType.FullRect);
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
        title.text = "Game Over";
        title.font = PixelFontProvider.Get();
        title.fontSize = 44;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.95f, 0.97f, 1f, 1f);
        var titleLE = titleGO.AddComponent<LayoutElement>();
        titleLE.minHeight = 84f;

        Button restartBtn = CreateUIButton(dialog.transform, "Restart");
        restartBtn.onClick.AddListener(() => { HideGameOverUI(); StartNewGame(); });

        Button menuBtn = CreateUIButton(dialog.transform, "Menu");
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
        rt.sizeDelta = new Vector2(300, 68);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var t = textGO.AddComponent<Text>();
        t.text = label;
        t.font = PixelFontProvider.Get();
        t.fontSize = 32;
        t.fontStyle = FontStyle.Bold;
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
