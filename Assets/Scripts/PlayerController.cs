using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Lane Settings")]
    [Tooltip("Y position for the top lane.")]
    [SerializeField] private float topLaneY = 3.3f;
    [Tooltip("Y position for the bottom lane.")]
    [SerializeField] private float bottomLaneY = -3.3f;
    [Tooltip("Time (seconds) to smooth damp the movement. Lower is snappier.")]
    [SerializeField] private float movementSmoothTime = 0.1f;
    [Tooltip("0.1 = 10% from left edge. 0.5 = Center.")]
    [Range(0.05f, 0.5f)] [SerializeField] private float screenPercentX = 0.15f;
    [Tooltip("Height offset to stand ON the platform instead of IN it.")]
    [SerializeField] private float platformLandOffset = 0.6f; 
    [SerializeField] private LayerMask platformLayer;

    [Header("Surge & Drift")]
    [Tooltip("The angle of movement when switching lanes (90 = Vertical, 45 = Diagonal).")]
    [Range(1f, 89f)] [SerializeField] private float switchAngle = 75f;
    [Tooltip("Multiplies world speed to calculate drift back speed. 0.5 = half world speed.")]
    [SerializeField] private float driftFactor = 0.5f;
    [Tooltip("Maximum X distance forward from the anchor point.")]
    [SerializeField] private float maxForwardDist = 5.0f;

    [Header("Dash Strike (Dev Toggle)")]
    [Tooltip("Distance the player surges forward during a dash attack.")]
    [SerializeField] private float dashStrikeSurge = 4.0f;

    [Header("Game Feel")]
    [Tooltip("Amount of rotation tilt when moving vertically.")]
    [SerializeField] private float tiltStrength = 2.0f;
    [Tooltip("Amount of squash/stretch based on vertical speed.")]
    [SerializeField] private float stretchStrength = 0.005f;
    [Tooltip("Particle system for speed lines/exhaust.")]
    [SerializeField] private ParticleSystem speedEffect;
    [Tooltip("Trail renderer for visual movement path.")]
    [SerializeField] private TrailRenderer trail;

    [Header("Melee Attack")]
    [Tooltip("Transform representing the center of the attack.")]
    [SerializeField] private Transform meleeHitboxTransform;
    [Tooltip("Size (Width, Height) of the attack hitbox.")]
    [SerializeField] private Vector2 hitboxSize = new Vector2(1, 1);
    [Tooltip("Time (seconds) required between attacks.")]
    [SerializeField] private float attackCooldown = 0.2f;
    [Tooltip("Layer mask to detect enemies and destroyables.")]
    [SerializeField] private LayerMask enemyLayer;
    [Tooltip("Visual sprite object for the slash effect.")]
    [SerializeField] private GameObject visualSlash;
    [Tooltip("Duration the slash visual remains active.")]
    [SerializeField] private float slashDuration = 0.1f;
    [Tooltip("Prefab spawned when an enemy is destroyed.")]
    [SerializeField] private GameObject deathVFXPrefab;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private PlayerInputActions playerInputActions;
    private bool isTopLane = false; 
    private Vector3 targetPosition;
    private float velocityX = 0f;
    private float velocityY = 0f;
    private float anchorX; 
    private float lastAttackTime;
    private bool laneSwitchTriggered = false;
    private bool isKeyboardInput = false;
    private bool isSurging = false;
    private bool isDashStriking = false;
    private bool isLethalDash = false; 
    private bool onPlatform = false;
    private float rigidPlatformY; 
    
    private Vector3 originalScale;
    private HashSet<int> hitObjectsDuringDash = new HashSet<int>(); // Track already-hit objects

    private void Awake()
    {
        playerInputActions = new PlayerInputActions();
        originalScale = transform.localScale;
        if (originalScale.x == 0) originalScale = Vector3.one;
    }
    
    private void Start()
    {
        Vector3 startPos = transform.position;
        startPos.y = bottomLaneY;
        transform.position = startPos;
        targetPosition = startPos;
        
        AlignAnchorToScreen();
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
        CheckPlatformCollision();

        bool isPlaying = GameManager.Instance.CurrentState == GameManager.GameState.Playing;

        HandleLaneSwitch();
        HandleSurgeAndDrift();

        float newX = Mathf.SmoothDamp(transform.position.x, targetPosition.x, ref velocityX, movementSmoothTime);
        float newY;

        if (onPlatform)
        {
            float platformCenter = rigidPlatformY;
            float targetSurfaceY;

            if (transform.position.y < platformCenter) 
            {
                targetSurfaceY = platformCenter - platformLandOffset;
            }
            else 
            {
                targetSurfaceY = platformCenter + platformLandOffset;
            }

            newY = Mathf.MoveTowards(transform.position.y, targetSurfaceY, 50f * Time.deltaTime);
            
            velocityY = 0f;
        }
        else
        {
            newY = Mathf.SmoothDamp(transform.position.y, targetPosition.y, ref velocityY, movementSmoothTime);
        }

        transform.position = new Vector3(newX, newY, transform.position.z);

        // --- VISUALS ---
        float verticalSpeed = onPlatform ? 0f : velocityY; 

        float tiltAngle = verticalSpeed * tiltStrength;
        if (!float.IsNaN(tiltAngle)) transform.rotation = Quaternion.Euler(0, 0, tiltAngle);

        float stretch = Mathf.Abs(verticalSpeed) * stretchStrength;
        if (!float.IsNaN(stretch))
        {
            transform.localScale = new Vector3(originalScale.x - stretch, originalScale.y + stretch, originalScale.z);
        }

        UpdateEffects(isPlaying);
        UpdateAnimations();
        
        // Continuous hitbox check during dash strike to prevent tunneling
        if (isDashStriking && isLethalDash)
        {
            Collider2D[] hits = Physics2D.OverlapBoxAll(meleeHitboxTransform.position, hitboxSize, 0, enemyLayer);
            foreach (var hitObject in hits)
            {
                int id = hitObject.gameObject.GetInstanceID();
                if (hitObjectsDuringDash.Contains(id)) continue; // Already hit this frame/dash
                hitObjectsDuringDash.Add(id);
                
                if (hitObject.TryGetComponent(out ProjectileBehavior projectile))
                {
                    AudioManager.Instance.PlayHit();
                    CameraShake.Instance.Shake(0.05f, 0.1f);
                    projectile.HitByPlayer();
                }
                else if (hitObject.CompareTag("Enemy"))
                {
                    KillEnemy(hitObject.gameObject);
                }
            }
        }
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
        
        targetPosition.x = Mathf.Lerp(targetPosition.x, anchorX, Time.deltaTime * driftFactor);
    }

    private void HandleLaneSwitch()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (!laneSwitchTriggered) return;

        if (!onPlatform && Mathf.Abs(transform.position.y - targetPosition.y) > 0.1f)
        {
            laneSwitchTriggered = false;
            return;
        }

        if (isKeyboardInput || !EventSystem.current.IsPointerOverGameObject())
        {
            if (onPlatform)
            {
                onPlatform = false;
            }
            
            isTopLane = !isTopLane;
            targetPosition.y = isTopLane ? topLaneY : bottomLaneY;
            
            isSurging = true;
            float height = Mathf.Abs(topLaneY - bottomLaneY);
            float angleRad = switchAngle * Mathf.Deg2Rad;
            float requiredSurge = height / Mathf.Tan(angleRad);

            float surgeTarget = transform.position.x + requiredSurge;
            targetPosition.x = Mathf.Clamp(surgeTarget, anchorX, anchorX + maxForwardDist);

            AudioManager.Instance.PlayJump();
        }
        laneSwitchTriggered = false;
    }

    public void MeleeAttack()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time;
        isDashStriking = true; 
        isLethalDash = true;   
        
        float surgeTarget = transform.position.x + dashStrikeSurge;
        targetPosition.x = Mathf.Clamp(surgeTarget, anchorX, anchorX + maxForwardDist);
        Invoke(nameof(EndDashStrike), 0.2f);
        
        visualSlash.SetActive(true);
        Invoke(nameof(DisableSlash), slashDuration);
        animator.SetTrigger("Attack"); 
        
        AudioManager.Instance.PlayAttack();
        
        Collider2D hitObject = Physics2D.OverlapBox(meleeHitboxTransform.position, hitboxSize, 0, enemyLayer);
        if (hitObject != null)
        {
            if (hitObject.TryGetComponent(out ProjectileBehavior projectile))
            {
                AudioManager.Instance.PlayHit();
                CameraShake.Instance.Shake(0.05f, 0.1f);
                projectile.HitByPlayer();
            }
            else
            {
                KillEnemy(hitObject.gameObject);
            }
        }
    }
    
    private void EndDashStrike()
    {
        isDashStriking = false; 
        isLethalDash = false;
        hitObjectsDuringDash.Clear(); // Reset for next dash
    }

    private void EndLethalDash()
    {
        isLethalDash = false;
    }
    
    private void KillEnemy(GameObject enemy)
    {
        GameManager.Instance.AddKill();
        CameraShake.Instance.Shake(0.1f, 0.2f);
        HitStop.Instance.Stop(0.05f);
        AudioManager.Instance.PlayHit();
        
        if (enemy.TryGetComponent(out ShooterAI shooter))
        {
            shooter.TriggerDeath();
        }
        else if (enemy.TryGetComponent(out GruntAI grunt))
        {
            grunt.TriggerDeath();
        }
        else
        {
            Instantiate(deathVFXPrefab, enemy.transform.position, Quaternion.identity);
            Destroy(enemy);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Platform"))
        {
            onPlatform = true;
            rigidPlatformY = other.transform.position.y;
        }
        
        if (other.CompareTag("Obstacle"))
        {
            animator.SetTrigger("Die");
            GameManager.Instance.GameOver();
            Time.timeScale = 0;
            return; 
        }
        
        if (other.CompareTag("Enemy"))
        {
            if (isLethalDash)
            {
                KillEnemy(other.gameObject);
            }
            else
            {
                animator.SetTrigger("Die");
                GameManager.Instance.GameOver();
                Time.timeScale = 0;
            }
        }

        // Projectile Collision Logic
        if (other.TryGetComponent(out ProjectileBehavior projectile))
        {
            if (isLethalDash)
            {
                // Dash Strike destroys projectile with VFX
                AudioManager.Instance.PlayHit();
                CameraShake.Instance.Shake(0.05f, 0.1f);
                projectile.HitByPlayer(); // This now spawns VFX
            }
            else
            {
                // Standard Hit -> Death
                animator.SetTrigger("Die");
                GameManager.Instance.GameOver();
                Time.timeScale = 0;
            }
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
    
    private void UpdateEffects(bool isPlaying)
    {
        bool isNotDrifting = velocityX > -0.1f;

        if (speedEffect != null)
        {
            if (isPlaying)
            {
                if (!speedEffect.isPlaying) speedEffect.Play();
                var emission = speedEffect.emission;
                emission.enabled = true; 
            }
            else if (speedEffect.isPlaying)
            {
                speedEffect.Stop();
            }
        }

        if (trail != null) 
        {
            trail.emitting = isPlaying && isNotDrifting;
        }
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
    
    private void AlignAnchorToScreen()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float distanceToCam = Mathf.Abs(cam.transform.position.z - transform.position.z);
        Vector3 viewPos = new Vector3(screenPercentX, 0.5f, distanceToCam);
        Vector3 worldPos = cam.ViewportToWorldPoint(viewPos);

        anchorX = worldPos.x;
        
        Vector3 currentPos = transform.position;
        currentPos.x = anchorX;
        transform.position = currentPos;
        targetPosition = currentPos;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Platform"))
        {
            onPlatform = false;
        }
    }
    
    private void CheckPlatformCollision()
    {
        float moveDist = Mathf.Abs(velocityY) * Time.deltaTime;
        float checkDist = moveDist + 0.2f;
        
        Vector2 direction = velocityY > 0 ? Vector2.up : Vector2.down;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, checkDist, platformLayer);

        if (hit.collider != null && hit.collider.CompareTag("Platform"))
        {
            bool movingTowards = (velocityY > 0 && hit.point.y > transform.position.y) || 
                                 (velocityY < 0 && hit.point.y < transform.position.y);

            if (movingTowards)
            {
                onPlatform = true;
                rigidPlatformY = hit.collider.transform.position.y;
                velocityY = 0f;
            }
        }
    }
    
    private void UpdateAnimations()
    {
        bool isPlaying = GameManager.Instance.CurrentState == GameManager.GameState.Playing;
        animator.SetBool("IsRunning", isPlaying);
        bool isGrounded = onPlatform || Mathf.Abs(velocityY) < 0.1f;
        animator.SetBool("IsGrounded", isGrounded);
        
        if (targetPosition.y > 0)
        {
            spriteRenderer.flipY = true;
        }
        else
        {
            spriteRenderer.flipY = false;
        }
        
        if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            float normalizedSpeed = GameManager.Instance.worldSpeed / 10f; 
            animator.speed = Mathf.Max(normalizedSpeed, 0.8f);
        }
        else
        {
            animator.speed = 1f;
        }
    }
}