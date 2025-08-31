using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class HorizontalLayout3D : MonoBehaviour
{
    public enum Alignment { Left, Center, Right }
    public enum EntryOrigin { FromCurrent, FromParentCenter, FromLeftEdge, FromRightEdge, FromCustom }

    [Header("Layout Settings")]
    [Tooltip("Axis along which children are arranged (e.g., (1,0,0)=X, (0,0,1)=Z).")]
    public Vector3 direction = Vector3.right;
    [Tooltip("Distance between adjacent children (center-to-center).")]
    public float spacing = 1f;
    [Tooltip("Padding before the first element (along +direction).")]
    public float paddingStart = 0f;
    [Tooltip("Padding after the last element.")]
    public float paddingEnd = 0f;
    [Tooltip("Horizontal alignment relative to the parent origin.")]
    public Alignment alignment = Alignment.Center;

    [Header("Child Options")]
    [Tooltip("Skip inactive children in layout calculations.")]
    public bool ignoreInactive = true;

    [Header("Transform Overrides")]
    [Tooltip("Force children to match parent's local rotation.")]
    public bool controlRotation = false;
    [Tooltip("Force children to match parent's local scale.")]
    public bool controlScale = false;

    [Header("Animation")]
    [Tooltip("If true, children will smoothly animate into position. If false, they will snap instantly.")]
    public bool animateChildren = true;
    [Tooltip("If true, existing coins will always animate when shifting positions (overrides animateChildren for shifts).")]
    public bool alwaysAnimateShifts = true;
    [Tooltip("Units per second. Set 0 for instant snap in Play Mode.")]
    public float moveSpeed = 5f;
    [Tooltip("Where newly added/enabled children start animating from.")]
    public EntryOrigin entryOrigin = EntryOrigin.FromParentCenter;
    [Tooltip("If EntryOrigin = FromCustom, this local offset is used.")]
    public Vector3 customEntryLocalOffset = Vector3.zero;
    [Tooltip("Use unscaled time for movement (e.g., during pauses).")]
    public bool useUnscaledTime = false;

    [Header("Coin Stacking Behavior")]
    [Tooltip("If true, coins of same type will stack together. If false, coins will stack in order added.")]
    public bool groupSameTypes = true;

    // --- internals ---
    private readonly Dictionary<Transform, Vector3> _targets = new Dictionary<Transform, Vector3>();
    private readonly HashSet<int> _prevValidIds = new HashSet<int>();
    private readonly List<Transform> _validChildren = new List<Transform>();
    private int _lastStateHash = 0;
    private int _pendingCoinsCount = 0; // Track coins that are animating to this layout
    private bool _isShiftingForInsertion = false; // Track if we're currently shifting for new coin insertion

    private void OnEnable() { ForceRebuild(); }
    private void OnValidate() { ForceRebuild(); }
    private void OnTransformChildrenChanged() { ForceRebuild(); }

    private void Update()
    {
        if (Application.isPlaying)
        {
            int state = ComputeStateHash();
            if (state != _lastStateHash)
            {
                ForceRebuild();
                _lastStateHash = state;
            }

            // Animate if either animateChildren is true OR we're shifting for insertion and alwaysAnimateShifts is true
            bool shouldAnimate = (animateChildren || (_isShiftingForInsertion && alwaysAnimateShifts)) && moveSpeed > 0f;
            
            if (shouldAnimate)
            {
                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                bool anyMoving = false;
                
                foreach (var kv in _targets)
                {
                    Transform t = kv.Key;
                    if (!t) continue;
                    
                    Vector3 oldPos = t.localPosition;
                    t.localPosition = Vector3.MoveTowards(t.localPosition, kv.Value, moveSpeed * dt);
                    
                    if (Vector3.Distance(t.localPosition, kv.Value) > 0.01f)
                    {
                        anyMoving = true;
                    }
                }
                
                // If no coins are moving, we're done shifting
                if (!anyMoving)
                {
                    _isShiftingForInsertion = false;
                }
            }
        }
    }

    /// <summary>
    /// Set the number of coins that are pending (animating toward this layout)
    /// </summary>
    public void SetPendingCoinsCount(int count)
    {
        if (_pendingCoinsCount != count)
        {
            _pendingCoinsCount = count;
            _isShiftingForInsertion = true; // Mark that we're shifting for insertion
            ForceRebuild(); // Rebuild when pending count changes
        }
    }

    public void ForceRebuild()
    {
        // Collect valid children
        _validChildren.Clear();
        foreach (Transform child in transform)
        {
            if (ignoreInactive && !child.gameObject.activeSelf)
                continue;
            _validChildren.Add(child);
        }

        int currentChildCount = _validChildren.Count;
        int totalCount = currentChildCount + _pendingCoinsCount; // Include pending coins in layout calculation
        _targets.Clear();

        if (totalCount == 0)
        {
            _prevValidIds.Clear();
            return;
        }

        Vector3 dir = direction.sqrMagnitude > 1e-8f ? direction.normalized : Vector3.right;
        float contentLength = spacing * (totalCount - 1);
        float totalWidth = contentLength + paddingStart + paddingEnd;

        // Compute start offset per alignment
        Vector3 startOffset;
        switch (alignment)
        {
            case Alignment.Left:
                startOffset = dir * paddingStart;
                break;
            case Alignment.Right:
                startOffset = -dir * (contentLength + paddingEnd);
                break;
            default: // Center
                startOffset = -dir * (contentLength * 0.5f) + dir * ((paddingStart - paddingEnd) * 0.5f);
                break;
        }

        // Detect newly added/enabled children
        var currentIds = new HashSet<int>();
        foreach (var c in _validChildren) currentIds.Add(c.GetInstanceID());

        var newlyAppeared = new List<Transform>();
        foreach (var c in _validChildren)
            if (!_prevValidIds.Contains(c.GetInstanceID()))
                newlyAppeared.Add(c);

        // Assign targets to existing children
        for (int i = 0; i < currentChildCount; i++)
        {
            Transform child = _validChildren[i];
            Vector3 target = startOffset + dir * (i * spacing);
            _targets[child] = target;

            if (controlRotation) child.localRotation = transform.localRotation;
            if (controlScale) child.localScale = transform.localScale;

            // --- SNAP or ANIMATE ---
            bool shouldAnimateEntry = animateChildren && Application.isPlaying && moveSpeed > 0f;
            bool shouldAnimateShift = alwaysAnimateShifts && Application.isPlaying && moveSpeed > 0f && _isShiftingForInsertion;
            
            if (!Application.isPlaying || (!shouldAnimateEntry && !shouldAnimateShift))
            {
                // always snap instantly in Edit Mode or when animation disabled
                child.localPosition = target;
            }
            else if (newlyAppeared.Contains(child) && shouldAnimateEntry)
            {
                // only offset new children if animating entries
                Vector3 entryPos = ComputeEntryPosition(entryOrigin, dir, startOffset, contentLength);
                child.localPosition = entryPos;
            }
            // For existing children, they will smoothly animate to their new positions via Update()
        }

        _prevValidIds.Clear();
        foreach (var id in currentIds) _prevValidIds.Add(id);
        _lastStateHash = ComputeStateHash();
    }

    private Vector3 ComputeEntryPosition(EntryOrigin origin, Vector3 dir, Vector3 startOffset, float contentLength)
    {
        switch (origin)
        {
            case EntryOrigin.FromParentCenter:
                return Vector3.zero;
            case EntryOrigin.FromLeftEdge:
                return startOffset;
            case EntryOrigin.FromRightEdge:
                return startOffset + dir * contentLength;
            case EntryOrigin.FromCustom:
                return customEntryLocalOffset;
            case EntryOrigin.FromCurrent:
            default:
                return Vector3.zero;
        }
    }

    private int ComputeStateHash()
    {
        unchecked
        {
            int h = 17;
            h = h * 31 + transform.childCount;
            h = h * 31 + _pendingCoinsCount; // Include pending coins in state hash
            foreach (Transform c in transform)
            {
                bool valid = !(ignoreInactive && !c.gameObject.activeSelf);
                if (!valid) continue;
                h = h * 31 + c.GetInstanceID();
                h = h * 31 + (c.gameObject.activeSelf ? 1 : 0);
            }
            h = h * 31 + alignment.GetHashCode();
            h = h * 31 + spacing.GetHashCode();
            h = h * 31 + paddingStart.GetHashCode();
            h = h * 31 + paddingEnd.GetHashCode();
            h = h * 31 + direction.GetHashCode();
            h = h * 31 + groupSameTypes.GetHashCode();
            return h;
        }
    }

    public int GetValidChildCount()
    {
        int c = 0;
        foreach (Transform child in transform)
        {
            if (ignoreInactive && !child.gameObject.activeSelf) continue;
            c++;
        }
        return c;
    }

    public int GetInsertIndexForType(CoinType type)
    {
        if (!groupSameTypes)
        {
            // Simple stacking: always add to the end
            return GetValidChildCount();
        }

        // Grouping behavior: find the last coin of the same type
        int insertIndex = 0;
        int validIndex = 0;
        bool foundMatchingType = false;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (ignoreInactive && !child.gameObject.activeSelf) continue;

            var coinComp = child.GetComponent<Coin>();
            if (coinComp != null && coinComp.type == type)
            {
                insertIndex = validIndex + 1;
                foundMatchingType = true;
            }

            validIndex++;
        }

        // If no matching type found, add to the end
        if (!foundMatchingType)
        {
            insertIndex = validIndex;
        }

        return Mathf.Clamp(insertIndex, 0, validIndex);
    }

    public Vector3 GetWorldPositionForInsertIndex(int insertIndex)
    {
        int currentChildCount = GetValidChildCount();
        int totalCountAfterInsert = currentChildCount + _pendingCoinsCount;
        
        Vector3 dir = direction.sqrMagnitude > 1e-8f ? direction.normalized : Vector3.right;
        
        // Calculate layout parameters for the final state (including the new coin)
        int finalTotalCount = totalCountAfterInsert + 1;
        float contentLength = spacing * (finalTotalCount - 1);

        Vector3 startOffset;
        switch (alignment)
        {
            case Alignment.Left:
                startOffset = dir * paddingStart;
                break;
            case Alignment.Right:
                startOffset = -dir * (contentLength + paddingEnd);
                break;
            default:
                startOffset = -dir * (contentLength * 0.5f) + dir * ((paddingStart - paddingEnd) * 0.5f);
                break;
        }

        insertIndex = Mathf.Clamp(insertIndex, 0, Mathf.Max(0, finalTotalCount - 1));
        Vector3 localTarget = startOffset + dir * (insertIndex * spacing);
        return transform.TransformPoint(localTarget);
    }
}