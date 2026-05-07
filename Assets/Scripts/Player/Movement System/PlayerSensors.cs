using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerSensors : MonoBehaviour
{
    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;        // Current ground. Treated as one-way platforms.
    [SerializeField] private LayerMask solidBlockerLayer;  // Solid blockers. Cannot be passed from below.
    [SerializeField] private LayerMask ladderLayer;

    [Header("Ground Check")]
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.65f, 0.12f);
    [SerializeField] private float groundCheckOffsetY = 0f;

    [Tooltip("Extra downward cast distance used to detect ground on edges and slopes.")]
    [SerializeField] private float groundCastDistance = 0.12f;

    [Tooltip("Minimum upward normal required for a contact to count as ground.")]
    [Range(0f, 1f)]
    [SerializeField] private float minGroundNormalY = 0.45f;

    [Tooltip("Keeps grounded true for a short time after a valid ground contact. Helps edge and side landings.")]
    [SerializeField] private float groundedMemoryTime = 0.08f;

    [Tooltip("Current ground layer is treated as one-way. It only counts while falling or standing, not while jumping upward.")]
    [SerializeField] private float oneWayMaxUpwardSpeed = 0.05f;

    [Header("One Way Ground Fix")]
    [Tooltip("Small upward offset from the player's feet where the ray starts.")]
    [SerializeField] private float oneWayFootRayStartOffset = 0.03f;

    [Tooltip("How far below the player's feet we search for one-way ground.")]
    [SerializeField] private float oneWayFootRayDistance = 0.18f;

    [Tooltip("Inset from left and right sides of the capsule for side foot rays.")]
    [SerializeField] private float oneWayFootRaySideInset = 0.08f;

    [Tooltip("Allowed difference between the hit point and the player's feet height.")]
    [SerializeField] private float oneWayFootPointTolerance = 0.04f;

    [Header("Ladder Check")]
    [SerializeField] private Tilemap ladderTilemap;

    [Tooltip("Additional ladder tilemaps. Use this when ladders are split across multiple duplicated Tilemap layers.")]
    [SerializeField] private Tilemap[] additionalLadderTilemaps;

    [Tooltip("Small inward offset for ladder boundary checks. Prevents unstable edge detection.")]
    [SerializeField] private float ladderEdgeInset = 0.03f;

    [Tooltip("Point distance below the feet used to detect a ladder entrance from above.")]
    [SerializeField] private float ladderBelowFeetDistance = 0.18f;

    [Tooltip("Point distance above the head used to know if the ladder continues upward.")]
    [SerializeField] private float ladderTopProbeOffset = 0.12f;

    [Tooltip("Vertical probe offset around the body center used to detect ladder body contact.")]
    [SerializeField] private float ladderBodyProbeOffset = 0.20f;

    [Tooltip("Draw ladder boundary points in Scene view.")]
    [SerializeField] private bool drawLadderPoints = true;

    [Header("Ceiling Check")]
    [SerializeField] private LayerMask ceilingBlockLayer;

    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCol;

    public bool IsGrounded { get; private set; }
    public bool IsOnOneWayGround { get; private set; }
    public bool IsOnSolidBlocker { get; private set; }

    public bool IsOnLadder { get; private set; }
    public bool IsFullyInsideLadder { get; private set; }

    public bool IsLadderAtBody { get; private set; }
    public bool IsLadderBelowFeet { get; private set; }
    public bool IsAtLadderTop { get; private set; }

    public LayerMask GroundLayer => groundLayer;

    private Vector2 lastGroundCheckCenter;
    private Vector2[] lastLadderBoundaryPoints = new Vector2[0];

    private float lastValidGroundContactTime = -10f;

    private readonly RaycastHit2D[] groundCastHits = new RaycastHit2D[8];
    private readonly ContactPoint2D[] groundContacts = new ContactPoint2D[16];

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCol = GetComponent<CapsuleCollider2D>();
    }

    public void Refresh()
    {
        RefreshGround();
        RefreshLadder();
    }

    private void RefreshGround()
    {
        Bounds b = capsuleCol.bounds;

        lastGroundCheckCenter = new Vector2(
            b.center.x,
            b.min.y + groundCheckOffsetY
        );

        bool canStandOnOneWayGround = rb.velocity.y <= oneWayMaxUpwardSpeed;

        bool oneWayGround = canStandOnOneWayGround && HasOneWayGroundDirectlyUnderFeet();

        bool solidGround =
            HasGroundOverlap(solidBlockerLayer) ||
            HasGroundBelowByCast(solidBlockerLayer) ||
            HasGroundContactNormal(solidBlockerLayer);

        IsOnOneWayGround = oneWayGround;
        IsOnSolidBlocker = solidGround;

        bool hasGround = oneWayGround || solidGround;

        if (hasGround)
            lastValidGroundContactTime = Time.time;

        bool rememberedGround = Time.time - lastValidGroundContactTime <= groundedMemoryTime;

        IsGrounded = hasGround || rememberedGround;
    }

    private bool HasOneWayGroundDirectlyUnderFeet()
    {
        Bounds b = capsuleCol.bounds;

        float footY = b.min.y;

        float inset = Mathf.Max(0f, oneWayFootRaySideInset);
        float leftX = b.min.x + inset;
        float centerX = b.center.x;
        float rightX = b.max.x - inset;

        if (leftX > rightX)
        {
            leftX = centerX;
            rightX = centerX;
        }

        Vector2 leftOrigin = new Vector2(leftX, footY + oneWayFootRayStartOffset);
        Vector2 centerOrigin = new Vector2(centerX, footY + oneWayFootRayStartOffset);
        Vector2 rightOrigin = new Vector2(rightX, footY + oneWayFootRayStartOffset);

        if (CheckOneWayFootRay(leftOrigin, footY))
            return true;

        if (CheckOneWayFootRay(centerOrigin, footY))
            return true;

        if (CheckOneWayFootRay(rightOrigin, footY))
            return true;

        return false;
    }

    private bool CheckOneWayFootRay(Vector2 origin, float footY)
    {
        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            Vector2.down,
            oneWayFootRayDistance,
            groundLayer
        );

        if (hit.collider == null)
            return false;

        if (hit.fraction <= 0.001f)
            return false;

        if (hit.normal.y < minGroundNormalY)
            return false;

        if (hit.point.y > footY + oneWayFootPointTolerance)
            return false;

        return true;
    }

    private bool HasGroundOverlap(LayerMask layerMask)
    {
        Collider2D hit = Physics2D.OverlapBox(
            lastGroundCheckCenter,
            groundCheckSize,
            0f,
            layerMask
        );

        return hit != null;
    }

    private bool HasGroundBelowByCast(LayerMask layerMask)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = layerMask;
        filter.useTriggers = false;

        int hitCount = capsuleCol.Cast(
            Vector2.down,
            filter,
            groundCastHits,
            groundCastDistance
        );

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = groundCastHits[i];

            if (hit.collider == null)
                continue;

            if (hit.normal.y >= minGroundNormalY)
                return true;
        }

        return false;
    }

    private bool HasGroundContactNormal(LayerMask layerMask)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = layerMask;
        filter.useTriggers = false;

        int contactCount = rb.GetContacts(filter, groundContacts);

        for (int i = 0; i < contactCount; i++)
        {
            ContactPoint2D contact = groundContacts[i];

            if (contact.collider == null)
                continue;

            if (contact.normal.y >= minGroundNormalY)
                return true;
        }

        return false;
    }

    private void RefreshLadder()
    {
        Bounds b = capsuleCol.bounds;

        IsOnLadder = IsAnyPartTouchingLadder(b);

        lastLadderBoundaryPoints = GetCapsuleBoundaryCheckPoints(b);

        IsFullyInsideLadder = AreAllPointsInsideLadder(lastLadderBoundaryPoints);

        Vector2 center = b.center;

        Vector2 bodyLowerPoint = new Vector2(center.x, center.y - ladderBodyProbeOffset);
        Vector2 bodyCenterPoint = center;
        Vector2 bodyUpperPoint = new Vector2(center.x, center.y + ladderBodyProbeOffset);

        IsLadderAtBody =
            IsPointInsideLadder(bodyLowerPoint) ||
            IsPointInsideLadder(bodyCenterPoint) ||
            IsPointInsideLadder(bodyUpperPoint);

        Vector2 belowFeetPoint = new Vector2(center.x, b.min.y - ladderBelowFeetDistance);
        IsLadderBelowFeet = IsPointInsideLadder(belowFeetPoint);

        Vector2 aboveHeadPoint = new Vector2(center.x, b.max.y + ladderTopProbeOffset);
        bool hasLadderAboveHead = IsPointInsideLadder(aboveHeadPoint);

        IsAtLadderTop = IsLadderAtBody && !hasLadderAboveHead;
    }

    private bool IsAnyPartTouchingLadder(Bounds b)
    {
        Vector2 centerPoint = b.center;
        Vector2 bottomPoint = new Vector2(b.center.x, b.min.y + 0.05f);
        Vector2 topPoint = new Vector2(b.center.x, b.max.y - 0.05f);
        Vector2 leftPoint = new Vector2(b.min.x + 0.05f, b.center.y);
        Vector2 rightPoint = new Vector2(b.max.x - 0.05f, b.center.y);

        if (IsPointInsideLadder(centerPoint))
            return true;

        if (IsPointInsideLadder(bottomPoint))
            return true;

        if (IsPointInsideLadder(topPoint))
            return true;

        if (IsPointInsideLadder(leftPoint))
            return true;

        if (IsPointInsideLadder(rightPoint))
            return true;

        Collider2D touchHit = Physics2D.OverlapCapsule(
            b.center,
            b.size,
            CapsuleDirection2D.Vertical,
            0f,
            ladderLayer
        );

        return touchHit != null;
    }

    private Vector2[] GetCapsuleBoundaryCheckPoints(Bounds b)
    {
        float inset = Mathf.Max(0f, ladderEdgeInset);

        float left = b.min.x + inset;
        float right = b.max.x - inset;
        float bottom = b.min.y + inset;
        float top = b.max.y - inset;

        float centerX = b.center.x;
        float centerY = b.center.y;

        float width = b.size.x;
        float height = b.size.y;

        float radius = width * 0.5f;
        float straightHalfHeight = Mathf.Max(0f, height * 0.5f - radius);

        float upperSideY = centerY + straightHalfHeight;
        float lowerSideY = centerY - straightHalfHeight;

        return new Vector2[]
        {
            new Vector2(centerX, centerY),

            new Vector2(centerX, top),
            new Vector2(centerX, bottom),

            new Vector2(left, centerY),
            new Vector2(right, centerY),

            new Vector2(left, upperSideY),
            new Vector2(right, upperSideY),
            new Vector2(left, lowerSideY),
            new Vector2(right, lowerSideY),

            new Vector2(centerX, Mathf.Lerp(centerY, top, 0.5f)),
            new Vector2(centerX, Mathf.Lerp(centerY, bottom, 0.5f)),

            new Vector2(left, Mathf.Lerp(centerY, upperSideY, 0.5f)),
            new Vector2(right, Mathf.Lerp(centerY, upperSideY, 0.5f)),
            new Vector2(left, Mathf.Lerp(centerY, lowerSideY, 0.5f)),
            new Vector2(right, Mathf.Lerp(centerY, lowerSideY, 0.5f))
        };
    }

    private bool AreAllPointsInsideLadder(Vector2[] points)
    {
        if (points == null || points.Length == 0)
            return false;

        for (int i = 0; i < points.Length; i++)
        {
            if (!IsPointInsideLadder(points[i]))
                return false;
        }

        return true;
    }

    private bool IsPointInsideLadder(Vector2 worldPosition)
    {
        if (HasLadderTileAtWorldPosition(worldPosition))
            return true;

        Collider2D hit = Physics2D.OverlapPoint(worldPosition, ladderLayer);
        return hit != null;
    }

    private bool HasLadderTileAtWorldPosition(Vector2 worldPosition)
    {
        if (HasTileInTilemap(ladderTilemap, worldPosition))
            return true;

        if (additionalLadderTilemaps != null)
        {
            for (int i = 0; i < additionalLadderTilemaps.Length; i++)
            {
                if (HasTileInTilemap(additionalLadderTilemaps[i], worldPosition))
                    return true;
            }
        }

        return false;
    }

    private bool HasTileInTilemap(Tilemap tilemap, Vector2 worldPosition)
    {
        if (tilemap == null)
            return false;

        Vector3Int cellPosition = tilemap.WorldToCell(worldPosition);
        return tilemap.HasTile(cellPosition);
    }

    public bool CanUseStandingCollider(Vector2 standingSize, Vector2 standingOffset)
    {
        Vector2 worldCenter = (Vector2)transform.position + standingOffset;

        Collider2D hit = Physics2D.OverlapCapsule(
            worldCenter,
            standingSize,
            CapsuleDirection2D.Vertical,
            0f,
            ceilingBlockLayer
        );

        return hit == null;
    }

    private void OnDrawGizmosSelected()
    {
        if (capsuleCol == null)
            capsuleCol = GetComponent<CapsuleCollider2D>();

        if (capsuleCol == null)
            return;

        DrawGroundCheck();
        DrawGroundCast();
        DrawOneWayFootRays();
        DrawLadderBoundaryPoints();
        DrawModernLadderPoints();
    }

    private void DrawGroundCheck()
    {
        Bounds b = capsuleCol.bounds;

        Vector2 center = Application.isPlaying
            ? lastGroundCheckCenter
            : new Vector2(b.center.x, b.min.y + groundCheckOffsetY);

        Gizmos.color = IsGrounded ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(center, groundCheckSize);
    }

    private void DrawGroundCast()
    {
        Bounds b = capsuleCol.bounds;

        Gizmos.color = Color.magenta;

        Vector3 from = new Vector3(b.center.x, b.min.y, 0f);
        Vector3 to = from + Vector3.down * groundCastDistance;

        Gizmos.DrawLine(from, to);
    }

    private void DrawOneWayFootRays()
    {
        Bounds b = capsuleCol.bounds;

        float footY = b.min.y;

        float inset = Mathf.Max(0f, oneWayFootRaySideInset);
        float leftX = b.min.x + inset;
        float centerX = b.center.x;
        float rightX = b.max.x - inset;

        if (leftX > rightX)
        {
            leftX = centerX;
            rightX = centerX;
        }

        Gizmos.color = Color.red;

        DrawOneWayFootRayGizmo(new Vector2(leftX, footY + oneWayFootRayStartOffset));
        DrawOneWayFootRayGizmo(new Vector2(centerX, footY + oneWayFootRayStartOffset));
        DrawOneWayFootRayGizmo(new Vector2(rightX, footY + oneWayFootRayStartOffset));
    }

    private void DrawOneWayFootRayGizmo(Vector2 origin)
    {
        Vector3 from = new Vector3(origin.x, origin.y, 0f);
        Vector3 to = from + Vector3.down * oneWayFootRayDistance;

        Gizmos.DrawLine(from, to);
        Gizmos.DrawSphere(from, 0.025f);
    }

    private void DrawLadderBoundaryPoints()
    {
        if (!drawLadderPoints)
            return;

        Vector2[] points;

        if (Application.isPlaying && lastLadderBoundaryPoints != null && lastLadderBoundaryPoints.Length > 0)
            points = lastLadderBoundaryPoints;
        else
            points = GetCapsuleBoundaryCheckPoints(capsuleCol.bounds);

        Gizmos.color = IsFullyInsideLadder ? Color.green : Color.cyan;

        for (int i = 0; i < points.Length; i++)
        {
            Gizmos.DrawSphere(points[i], 0.06f);
        }
    }

    private void DrawModernLadderPoints()
    {
        if (!drawLadderPoints)
            return;

        Bounds b = capsuleCol.bounds;
        Vector2 center = b.center;

        Vector2 bodyLowerPoint = new Vector2(center.x, center.y - ladderBodyProbeOffset);
        Vector2 bodyCenterPoint = center;
        Vector2 bodyUpperPoint = new Vector2(center.x, center.y + ladderBodyProbeOffset);
        Vector2 belowFeetPoint = new Vector2(center.x, b.min.y - ladderBelowFeetDistance);
        Vector2 aboveHeadPoint = new Vector2(center.x, b.max.y + ladderTopProbeOffset);

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(bodyLowerPoint, 0.05f);
        Gizmos.DrawSphere(bodyCenterPoint, 0.05f);
        Gizmos.DrawSphere(bodyUpperPoint, 0.05f);

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(belowFeetPoint, 0.06f);

        Gizmos.color = Color.black;
        Gizmos.DrawSphere(aboveHeadPoint, 0.06f);
    }
}