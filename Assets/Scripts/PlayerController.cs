using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Lane Settings")]
    [SerializeField] private float topLaneY = 3.3f;
    [SerializeField] private float bottomLaneY = -3.3f;
    [SerializeField] private float movementSmoothTime = 0.1f;
    [Range(0.05f, 0.5f)] [SerializeField] private float screenPercentX = 0.15f;
    [SerializeField] private float platformLandOffset = 0.6f;
    [SerializeField] private LayerMask platformLayer;

    [Header("Surge & Drift")]
    [Range(1f, 89f)] [SerializeField] private float switchAngle = 75f;
    [SerializeField] private float driftFactor = 0.5f;
    [SerializeField] private float maxForwardDist = 5.0f;

    [Header("Dash Strike (Dev Toggle)")]
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
    private HashSet<int> hitObjectsDuringDash = new HashSet<int>();
    private GameManager gm;

    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DieHash = Animator.StringToHash("Die");

    private Collider2D[] hitResults = new Collider2D[10];
    
    private bool isInvincible = false;
    [SerializeField] private float reviveInvincibilityDuration = 2.0f;

    private void Awake()
    {
        playerInputActions = new PlayerInputActions();
        originalScale = transform.localScale;
        if (originalScale.x == 0) originalScale = Vector3.one;
    }
    
    private void Start()
    {
        gm = GameManager.Instance;
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

        bool isPlaying = gm != null && gm.CurrentState == GameManager.GameState.Playing;

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
        
        if (isDashStriking && isLethalDash)
        {
            int hitCount = Physics2D.OverlapBoxNonAlloc(meleeHitboxTransform.position, hitboxSize, 0, hitResults, enemyLayer);
            
            for (int i = 0; i < hitCount; i++)
            {
                var hitObject = hitResults[i];
                int id = hitObject.gameObject.GetInstanceID();
                if (hitObjectsDuringDash.Contains(id)) continue;
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
        if (TutorialManager.Instance != null && TutorialManager.Instance.InputsLocked) return;
        if (!laneSwitchTriggered) return;

        if (!onPlatform && Mathf.Abs(transform.position.y - targetPosition.y) > 0.1f)
        {
            laneSwitchTriggered = false;
            return;
        }

        if (isKeyboardInput || !EventSystem.current.IsPointerOverGameObject())
        {
            ExecuteLaneSwitch();
        }
        laneSwitchTriggered = false;
    }
    
    public void ExecuteAttack()
    {
        lastAttackTime = Time.time;

        isDashStriking = true;
        isLethalDash = true;
        float surgeTarget = transform.position.x + dashStrikeSurge;
        targetPosition.x = Mathf.Clamp(surgeTarget, anchorX, anchorX + maxForwardDist);
        Invoke(nameof(EndDashStrike), 0.2f);

        visualSlash.SetActive(true);
        Invoke(nameof(DisableSlash), slashDuration);
        animator.SetTrigger(AttackHash);
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
    
    public void ExecuteLaneSwitch()
    {
        if (onPlatform) onPlatform = false;
        
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

    public void MeleeAttack()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (TutorialManager.Instance != null && TutorialManager.Instance.InputsLocked) return;
        if (Time.time < lastAttackTime + attackCooldown) return;

        ExecuteAttack();
    }
    
    private void EndDashStrike()
    {
        isDashStriking = false;
        isLethalDash = false;
        hitObjectsDuringDash.Clear();
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
            ObjectPooler.Instance.GetVFX(enemy.transform.position);
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
            if (isInvincible) return;
            animator.SetTrigger(DieHash);
            GameManager.Instance.GameOver();
            Time.timeScale = 0;
            return;
        }
        
        if (other.CompareTag("Enemy"))
        {
            if (isInvincible) return;
            bool enemyAlreadyDead = false;
            if (other.TryGetComponent(out GruntAI grunt)) enemyAlreadyDead = grunt.IsDead;
            else if (other.TryGetComponent(out ShooterAI shooter)) enemyAlreadyDead = shooter.IsDead;
            
            if (enemyAlreadyDead) return;

            if (isLethalDash)
            {
                KillEnemy(other.gameObject);
            }
            else
            {
                animator.SetTrigger(DieHash);
                GameManager.Instance.GameOver();
                Time.timeScale = 0;
            }
        }

        if (other.TryGetComponent(out ProjectileBehavior projectile))
        {
            if (isLethalDash)
            {
                AudioManager.Instance.PlayHit();
                CameraShake.Instance.Shake(0.05f, 0.1f);
                projectile.HitByPlayer();
            }
            else
            {
                animator.SetTrigger(DieHash);
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

    private bool lastIsRunning = false;
    private bool lastIsGrounded = false;
    private float lastAnimSpeed = 1f;
    
    private void UpdateAnimations()
    {
        bool isPlaying = gm != null && gm.CurrentState == GameManager.GameState.Playing;
        
        if (isPlaying != lastIsRunning)
        {
            animator.SetBool(IsRunningHash, isPlaying);
            lastIsRunning = isPlaying;
        }
        
        bool isGrounded = onPlatform || Mathf.Abs(velocityY) < 0.1f;
        if (isGrounded != lastIsGrounded)
        {
            animator.SetBool(IsGroundedHash, isGrounded);
            lastIsGrounded = isGrounded;
        }
        
        spriteRenderer.flipY = targetPosition.y > 0;
        
        if (isPlaying && gm != null)
        {
            float normalizedSpeed = gm.worldSpeed / 10f;
            float newSpeed = Mathf.Max(normalizedSpeed, 0.8f);
            
            if (Mathf.Abs(newSpeed - lastAnimSpeed) > 0.05f)
            {
                animator.speed = newSpeed;
                lastAnimSpeed = newSpeed;
            }
        }
        else if (lastAnimSpeed != 1f)
        {
            animator.speed = 1f;
            lastAnimSpeed = 1f;
        }
    }
    
    public void ActivateReviveInvincibility()
    {
        StartCoroutine(InvincibilityRoutine());
    }

    private System.Collections.IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        
        // Visual Feedback: Flash the sprite
        float timer = 0f;
        float flashSpeed = 0.1f;
        
        while (timer < reviveInvincibilityDuration)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled; // Toggle visibility
            yield return new WaitForSeconds(flashSpeed);
            timer += flashSpeed;
        }

        spriteRenderer.enabled = true; // Ensure visible at end
        isInvincible = false;
    }
    
    public void ResetAnimationState()
    {
        animator.ResetTrigger("Die");
        animator.ResetTrigger("Attack");
        
        animator.Play("Player_Run"); 
        animator.SetBool("IsRunning", true);
    }
}