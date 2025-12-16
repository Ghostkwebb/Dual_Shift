using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    [Header("Lane Switching")]
    [SerializeField] private float topLaneY = 3.3f;
    [SerializeField] private float bottomLaneY = -3.3f;
    [SerializeField] private float laneSwitchDuration = 0.1f;
    [Range(45f, 90f)] [SerializeField] private float switchAngle = 75f;
    [SerializeField] private ParticleSystem speedEffect;
    [SerializeField] private TrailRenderer trail;

    [Header("Game Feel")]
    [SerializeField] private float tiltStrength = 2.0f;
    [SerializeField] private float stretchStrength = 0.005f;

    [Header("Melee Attack")]
    [SerializeField] private Transform meleeHitboxTransform;
    [SerializeField] private Vector2 hitboxSize = new Vector2(1, 1);
    [SerializeField] private float attackCooldown = 0.2f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameObject visualSlash;
    [SerializeField] private float slashDuration = 0.1f;
    [SerializeField] private GameObject deathVFXPrefab;

    [Header("Dash Strike (Dev Toggle)")]
    [SerializeField] private bool useDashStrike = false;
    [SerializeField] private float dashStrikeDistance = 3.0f;
    [SerializeField] private float dashStrikeDuration = 0.2f;

    private PlayerInputActions playerInputActions;
    private bool isTopLane = false; 
    private Vector3 targetPosition;
    private Vector3 velocity = Vector3.zero;
    private float lastAttackTime;
    private bool laneSwitchTriggered = false;
    private bool isDashStriking = false; 
    private bool isKeyboardInput = false;
    
    private Vector3 originalScale;
    private float defaultX;

    private void Awake()
    {
        playerInputActions = new PlayerInputActions();
        originalScale = transform.localScale;

        Vector3 startPos = transform.position;
        startPos.y = bottomLaneY;
        transform.position = startPos;
        
        targetPosition = startPos;
        defaultX = startPos.x;
    }

    private void OnEnable()
    {
        playerInputActions.Player.Enable();
        playerInputActions.Player.LaneSwitch.performed += OnLaneSwitch;
        playerInputActions.Player.Pause.performed += OnPause;
        if (trail != null) trail.emitting = true;
    }

    private void OnDisable()
    {
        playerInputActions.Player.Disable();
        playerInputActions.Player.LaneSwitch.performed -= OnLaneSwitch;
    }
    
    private void OnPause(InputAction.CallbackContext context)
    {
        // Toggle pause regardless of game state (unless in Menu)
        if (GameManager.Instance.CurrentState != GameManager.GameState.Menu)
        {
            GameManager.Instance.TogglePause();
        }
    }

    private void Update()
    {
        bool isDriftingBack = velocity.x < -0.1f;
        bool isPlaying = GameManager.Instance.CurrentState == GameManager.GameState.Playing;

        if (speedEffect != null)
        {
            if (isPlaying)
            {
                if (!speedEffect.isPlaying) speedEffect.Play();
                var emission = speedEffect.emission;
                emission.enabled = !isDriftingBack; 
            }
            else if (speedEffect.isPlaying) speedEffect.Stop();
        }

        if (trail != null) trail.emitting = isPlaying && !isDriftingBack;

        HandleLaneSwitch();

        // LOGIC CHANGE: Only auto-reset X if NOT Dash Striking
        if (!isDashStriking)
        {
            if (Mathf.Abs(transform.position.y - targetPosition.y) < 0.2f)
            {
                targetPosition.x = defaultX;
            }
        }

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, laneSwitchDuration);

        float verticalSpeed = velocity.y;
        float tiltAngle = verticalSpeed * tiltStrength;
        transform.rotation = Quaternion.Euler(0, 0, tiltAngle);

        float stretch = Mathf.Abs(verticalSpeed) * stretchStrength;
        transform.localScale = new Vector3(originalScale.x - stretch, originalScale.y + stretch, originalScale.z);
    }

    private void OnLaneSwitch(InputAction.CallbackContext context)
    {
        laneSwitchTriggered = true;
        isKeyboardInput = context.control.device is Keyboard;
    }

    private void HandleLaneSwitch()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (!laneSwitchTriggered) return;
        if (isKeyboardInput || !EventSystem.current.IsPointerOverGameObject())
        {
            isTopLane = !isTopLane;
            targetPosition.y = isTopLane ? topLaneY : bottomLaneY;

            if (switchAngle < 90f)
            {
                float height = Mathf.Abs(topLaneY - bottomLaneY);
                float forwardDist = height / Mathf.Tan(switchAngle * Mathf.Deg2Rad);
                targetPosition.x = defaultX + forwardDist;
            }
            
            // Play sound only if move is successful
            AudioManager.Instance.PlayJump();
        }
        laneSwitchTriggered = false;
        AudioManager.Instance.PlayJump();
    }

    public void MeleeAttack()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time;

        // --- DASH STRIKE LOGIC ---
        if (useDashStrike)
        {
            isDashStriking = true;
            targetPosition.x = defaultX + dashStrikeDistance;
            Invoke(nameof(EndDashStrike), dashStrikeDuration);
        }
        // -------------------------

        visualSlash.SetActive(true);
        Invoke(nameof(DisableSlash), slashDuration);
        AudioManager.Instance.PlayAttack();

        // Note: Hitbox moves with player, so we don't need to change overlap logic
        Collider2D hitEnemy = Physics2D.OverlapBox(meleeHitboxTransform.position, hitboxSize, 0, enemyLayer);
        if (hitEnemy != null)
        {
            GameManager.Instance.AddKill();
            CameraShake.Instance.Shake(0.1f, 0.2f);
            HitStop.Instance.Stop(0.05f);
            AudioManager.Instance.PlayHit();
            Instantiate(deathVFXPrefab, hitEnemy.transform.position, Quaternion.identity);
            Destroy(hitEnemy.gameObject);
        }
    }

    private void EndDashStrike()
    {
        isDashStriking = false;
        // X will be reset by Update loop automatically now
    }

    public void ToggleDashStrike(bool state)
    {
        useDashStrike = state;
    }

    // ... (Triggers and Gizmos unchanged) ...
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy") || other.CompareTag("Obstacle"))
        {
            GameManager.Instance.GameOver();
            Time.timeScale = 0;
        }
    }

    private void DisableSlash()
    {
        visualSlash.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        if (meleeHitboxTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(meleeHitboxTransform.position, hitboxSize);
        }
    }
}