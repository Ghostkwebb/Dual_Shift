using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    [Header("Lane Switching")]
    [SerializeField] private float topLaneY = 2.0f;
    [SerializeField] private float bottomLaneY = -2.0f;
    [SerializeField] private float laneSwitchDuration = 0.1f;

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
        HandleLaneSwitch();
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, laneSwitchDuration);
    }

    private void OnLaneSwitch(InputAction.CallbackContext context)
    {
        laneSwitchTriggered = true;
    }

    private void HandleLaneSwitch()
    {
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
        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time;

        // 1. Show Visuals
        visualSlash.SetActive(true);
        Invoke(nameof(DisableSlash), slashDuration);

        // 2. Perform Logic
        Collider2D hitEnemy = Physics2D.OverlapBox(meleeHitboxTransform.position, hitboxSize, 0, enemyLayer);
        if (hitEnemy != null)
        {
            Destroy(hitEnemy.gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Player has been destroyed!");
            Destroy(gameObject);
        }
    }

    private void DisableSlash()
    {
        visualSlash.SetActive(false);
    }
}