using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    [Header("Lane Settings")]
    [SerializeField] private float topLaneY = 3.3f;
    [SerializeField] private float bottomLaneY = -3.3f;
    [SerializeField] private float movementSmoothTime = 0.1f;

    [Header("Surge & Drift")]
    [SerializeField] private float surgeAmount = 2.0f;
    [SerializeField] private float driftFactor = 0.5f; 
    [SerializeField] private float maxForwardDist = 5.0f;

    [Header("Dash Strike (Dev Toggle)")]
    [SerializeField] private bool useDashStrike = false;
    [SerializeField] private float dashStrikeSurge = 4.0f;

    [Header("Game Feel")]
    [SerializeField] private float tiltStrength = 2.0f;
    [SerializeField] private float stretchStrength = 0.005f;
    [SerializeField] private ParticleSystem speedEffect;
    [SerializeField] private TrailRenderer trail;

    [Header("Melee Attack")]
    [SerializeField] private Transform meleeHitboxTransform;
    [SerializeField] private Vector2 hitboxSize = new Vector2(1, 1);
    [SerializeField] private float attackCooldown = 0.2f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameObject visualSlash;
    [SerializeField] private float slashDuration = 0.1f;
    [SerializeField] private GameObject deathVFXPrefab;

    private PlayerInputActions playerInputActions;
    private bool isTopLane = false; 
    private Vector3 targetPosition;
    private Vector3 velocity = Vector3.zero; 
    private float anchorX; 
    private float lastAttackTime;
    private bool laneSwitchTriggered = false;
    private bool isKeyboardInput = false;
    private bool isSurging = false;
    private bool isDashStriking = false;
    
    // NEW: Lethal State Flag
    private bool isLethalDash = false; 
    
    private Vector3 originalScale;

    private void Awake()
    {
        playerInputActions = new PlayerInputActions();
        originalScale = transform.localScale;
        if (originalScale.x == 0) originalScale = Vector3.one;

        Vector3 startPos = transform.position;
        startPos.y = bottomLaneY;
        transform.position = startPos;
        
        anchorX = startPos.x;
        targetPosition = startPos;
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
        playerInputActions.Player.Pause.performed -= OnPause;
    }

    private void Update()
    {
        bool isPlaying = GameManager.Instance.CurrentState == GameManager.GameState.Playing;

        HandleLaneSwitch();
        HandleSurgeAndDrift();

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, movementSmoothTime);

        // Visuals
        float verticalSpeed = velocity.y;
        float tiltAngle = verticalSpeed * tiltStrength;
        if (!float.IsNaN(tiltAngle)) transform.rotation = Quaternion.Euler(0, 0, tiltAngle);

        float stretch = Mathf.Abs(verticalSpeed) * stretchStrength;
        if (!float.IsNaN(stretch))
        {
            transform.localScale = new Vector3(originalScale.x - stretch, originalScale.y + stretch, originalScale.z);
        }

        UpdateEffects(isPlaying);
    }

    private void HandleSurgeAndDrift()
    {
        if (isSurging || isDashStriking)
        {
            if (isSurging && Mathf.Abs(transform.position.y - targetPosition.y) < 0.05f)
            {
                isSurging = false;
            }
            return;
        }

        float returnSpeed = GameManager.Instance.worldSpeed * driftFactor;
        targetPosition.x = Mathf.MoveTowards(targetPosition.x, anchorX, returnSpeed * Time.deltaTime);
    }

    private void HandleLaneSwitch()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (!laneSwitchTriggered) return;

        if (isKeyboardInput || !EventSystem.current.IsPointerOverGameObject())
        {
            isTopLane = !isTopLane;
            targetPosition.y = isTopLane ? topLaneY : bottomLaneY;
            
            isSurging = true;
            float surgeTarget = transform.position.x + surgeAmount;
            targetPosition.x = Mathf.Clamp(surgeTarget, anchorX, anchorX + maxForwardDist);

            AudioManager.Instance.PlayJump();
        }
        laneSwitchTriggered = false;
    }

    public void ToggleDashStrike(bool state)
    {
        useDashStrike = state;
    }

    public void MeleeAttack()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time;

        if (useDashStrike)
        {
            isDashStriking = true;
            isLethalDash = true;  
            
            float surgeTarget = transform.position.x + dashStrikeSurge;
            targetPosition.x = Mathf.Clamp(surgeTarget, anchorX, anchorX + maxForwardDist);
            
            Invoke(nameof(EndDashStrike), 0.2f);
        }

        visualSlash.SetActive(true);
        Invoke(nameof(DisableSlash), slashDuration);
        AudioManager.Instance.PlayAttack();

        // Rule 4: Attack Hitbox (Standard check)
        Collider2D hitEnemy = Physics2D.OverlapBox(meleeHitboxTransform.position, hitboxSize, 0, enemyLayer);
        if (hitEnemy != null)
        {
            KillEnemy(hitEnemy.gameObject);
        }
    }
    
    private void EndDashStrike()
    {
        isDashStriking = false; // Drift resumes
        isLethalDash = false;   // iFrames off
    }

    private void EndLethalDash()
    {
        isLethalDash = false;
    }

    // Helper method to handle kills (used by both Hitbox and Dash collision)
    private void KillEnemy(GameObject enemy)
    {
        GameManager.Instance.AddKill();
        CameraShake.Instance.Shake(0.1f, 0.2f);
        HitStop.Instance.Stop(0.05f);
        AudioManager.Instance.PlayHit();
        Instantiate(deathVFXPrefab, enemy.transform.position, Quaternion.identity);
        Destroy(enemy);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Handle Enemy Collision
        if (other.CompareTag("Enemy"))
        {
            if (isLethalDash)
            {
                // If dashing, WE kill THEM
                KillEnemy(other.gameObject);
            }
            else
            {
                // If not dashing, THEY kill US
                GameManager.Instance.GameOver();
                Time.timeScale = 0;
            }
        }
        // 2. Handle Obstacle Collision (Always lethal)
        else if (other.CompareTag("Obstacle"))
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
    
    // Input Handlers
    private void UpdateEffects(bool isPlaying)
    {
        bool isSurgingForward = velocity.x > 0.5f; 
        
        if (speedEffect != null)
        {
            if (isPlaying) {
                if (!speedEffect.isPlaying) speedEffect.Play();
                var emission = speedEffect.emission;
                emission.enabled = isSurgingForward; 
            } else if (speedEffect.isPlaying) speedEffect.Stop();
        }

        if (trail != null) trail.emitting = isPlaying && isSurgingForward;
    }

    private void OnLaneSwitch(InputAction.CallbackContext context)
    {
        laneSwitchTriggered = true;
        isKeyboardInput = context.control.device is Keyboard;
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Menu)
        {
            GameManager.Instance.TogglePause();
        }
    }
}