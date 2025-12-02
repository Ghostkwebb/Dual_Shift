using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    [Header("Lane Switching")]
    [SerializeField] private float topLaneY = 2.0f;
    [SerializeField] private float bottomLaneY = -2.0f;
    [SerializeField] private float laneSwitchDuration = 0.1f;
    [SerializeField] private ParticleSystem speedEffect;


    [Header("Melee Attack")]
    [SerializeField] private Transform meleeHitboxTransform;
    [SerializeField] private Vector2 hitboxSize = new Vector2(1, 1);
    [SerializeField] private float attackCooldown = 0.2f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameObject visualSlash;
    [SerializeField] private float slashDuration = 0.1f;

    private PlayerInputActions playerInputActions;
    private bool isTopLane = true;
    private Vector3 targetPosition;
    private Vector3 velocity = Vector3.zero;
    private float lastAttackTime;
    private bool laneSwitchTriggered = false;

    private void Awake()
    {
        playerInputActions = new PlayerInputActions();
        Vector3 startPos = transform.position;
        startPos.y = bottomLaneY;
        transform.position = startPos;
        targetPosition = startPos;
    }

    private void OnEnable()
    {
        if (speedEffect != null)
        {
            if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
            {
                if (!speedEffect.isPlaying) speedEffect.Play();
            }
            else
            {
                if (speedEffect.isPlaying) speedEffect.Stop();
            }
        }
        playerInputActions.Player.Enable();
        playerInputActions.Player.LaneSwitch.performed += OnLaneSwitch;
    }

    private void OnDisable()
    {
        playerInputActions.Player.Disable();
        playerInputActions.Player.LaneSwitch.performed -= OnLaneSwitch;
    }

    private void Update()
    {
        if (speedEffect != null)
        {
            if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
            {
                if (!speedEffect.isPlaying) speedEffect.Play();
            }
            else
            {
                if (speedEffect.isPlaying) speedEffect.Stop();
            }
        }
        HandleLaneSwitch();
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, laneSwitchDuration);
    }

    private void OnLaneSwitch(InputAction.CallbackContext context)
    {
        laneSwitchTriggered = true;
    }

    private void HandleLaneSwitch()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (!laneSwitchTriggered) return;
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            isTopLane = !isTopLane;
            targetPosition.y = isTopLane ? topLaneY : bottomLaneY;
        }
        laneSwitchTriggered = false;
    }

    public void MeleeAttack()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time;

        visualSlash.SetActive(true);
        Invoke(nameof(DisableSlash), slashDuration);

        Collider2D hitEnemy = Physics2D.OverlapBox(meleeHitboxTransform.position, hitboxSize, 0, enemyLayer);
        if (hitEnemy != null)
        {
            GameManager.Instance.AddKill();
            Destroy(hitEnemy.gameObject);
        }
    }

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