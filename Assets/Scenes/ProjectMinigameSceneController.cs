using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ProjectMinigameSceneController : MonoBehaviour
{
    private const string ActiveGameIdKey = "project_active_game_id";
    private const string ActiveActivityIdKey = "project_active_activity_id";
    private const string ActiveActivityTypeKey = "project_active_activity_type";
    private const string ActiveRewardPointsKey = "project_active_reward_points";
    private const string ActiveOneTimeRewardKey = "project_active_one_time_reward";
    private const string PendingRewardFlagKey = "project_pending_reward_flag";
    private const string PendingRewardActivityIdKey = "project_pending_reward_activity_id";
    private const string PendingRewardActivityTypeKey = "project_pending_reward_activity_type";
    private const string PendingRewardPointsKey = "project_pending_reward_points";
    private const string PendingRewardOneTimeKey = "project_pending_reward_one_time";

    [System.Serializable]
    private class EnemyRuntime
    {
        public Transform root;
        public Rigidbody2D body;
        public Collider2D collider;
        public Animator animator;
        public SpriteRenderer spriteRenderer;
        public Transform vision;
        public Collider2D visionCollider;
        public Vector3 roamTarget;
        public bool chasing;
        public float chaseLostTimer;
        public Vector3 previousPosition;
        public float stuckTimer;
        public float blockedTimer;
        public float detourSign;
        public Vector3 desiredPosition;
    }

    [Header("Scene Flow")]
    [SerializeField] private string fallbackMainSceneName = "MainGameScene";

    [Header("Player")]
    [SerializeField] private Transform player;
    [SerializeField] private Collider2D playerCollider;

    [Header("HUD")]
    [SerializeField] private GameObject gameplayHudRoot;
    [SerializeField] private TMP_Text checklistText;
    [SerializeField] private TMP_Text hintText;

    [Header("Result UI (optional)")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultBodyText;
    [SerializeField] private float resultDelaySeconds = 0.8f;

    [Header("Resources")]
    [SerializeField] private ProjectResourceNode[] existingResourceNodes;
    [SerializeField] private Transform[] existingEnemyObjects;
    [SerializeField] private Transform[] enemyRoamPoints;

    [Header("Gameplay")]
    [SerializeField] private string[] stepLabels = { "Requirement", "Design", "Implemented", "Testing", "Deploy" };
    [SerializeField] private int pointsPerStep = 2;
    [SerializeField] private bool useFixedHudTemplate = true;
    [SerializeField] private float collectRadius = 1.4f;
    [SerializeField] private bool requireResourceTrigger = true;
    [SerializeField] private float enemyRoamSpeed = 1.8f;
    [SerializeField] private float enemyChaseSpeed = 3.2f;
    [SerializeField] private float enemyVisionRadius = 2.4f;
    [SerializeField] private float enemyCatchRadius = 0.55f;
    [SerializeField] private float enemyCatchSurfaceThreshold = 0.1f;
    [SerializeField] private float chaseLoseDelaySeconds = 0.8f;
    [SerializeField] private float minRoamTargetDistance = 1.2f;
    [SerializeField] private float stuckRepathSeconds = 0.4f;
    [SerializeField] private float wallProbePadding = 0.05f;
    [SerializeField] private string enemyVisionChildName = "Vision";
    [SerializeField] private string attackBoolName = "AttackNow";
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private string attackStateName = "attack";
    [SerializeField] private float attackLeadSeconds = 0.5f;
    [SerializeField] private KeyCode quitKey = KeyCode.Q;

    [Header("Enemy Naming")]
    [SerializeField] private bool autoRenameEnemies = false;
    [SerializeField] private string enemyNamePrefix = "Skeleton";
    [SerializeField] private bool showEnemyNames = true;
    [SerializeField] private Vector3 enemyNameOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private int enemyNameSortingOrder = 50;
    [SerializeField] private float enemyNameFontSize = 1.35f;

    [Header("Scoring")]
    [SerializeField] private string activityId = "project";
    [SerializeField] private string activityType = "project";
    [SerializeField] private int firstWinPoints = 10;
    [SerializeField] private bool oneTimeReward = true;

    [Header("Obstacles")]
    [SerializeField] private bool useObstacleBlocking = false;
    [SerializeField] private Transform[] obstacleRoots;

    private readonly List<ProjectResourceNode> resources = new List<ProjectResourceNode>();
    private readonly List<EnemyRuntime> enemies = new List<EnemyRuntime>();
    private int[] collectedByType;
    private int gatherTypeCount;
    private int currentStep;
    private ProjectResourceNode activeGatherNode;
    private bool runActive;
    private float resultAt;
    private bool waitingReturn;
    private bool resolvingCaught;
    private bool resolvingWin;
    private static readonly string[] FixedHudLabels =
    {
        "Requirement",
        "Design",
        "Implemented",
        "Testing",
        "Deploy"
    };
    private string activeGameId = "project_game";

    private void OnValidate()
    {
        enemyRoamSpeed = Mathf.Max(0f, enemyRoamSpeed);
        enemyChaseSpeed = Mathf.Max(enemyRoamSpeed, enemyChaseSpeed);
        collectRadius = Mathf.Max(0.2f, collectRadius);
        enemyVisionRadius = Mathf.Max(0.1f, enemyVisionRadius);
        enemyCatchRadius = Mathf.Max(0.1f, enemyCatchRadius);
        enemyCatchSurfaceThreshold = Mathf.Clamp(enemyCatchSurfaceThreshold, 0f, 1f);
        chaseLoseDelaySeconds = Mathf.Max(0f, chaseLoseDelaySeconds);
        minRoamTargetDistance = Mathf.Max(0.1f, minRoamTargetDistance);
        stuckRepathSeconds = Mathf.Max(0.1f, stuckRepathSeconds);
        wallProbePadding = Mathf.Clamp(wallProbePadding, 0f, 0.5f);
        attackLeadSeconds = Mathf.Max(0.1f, attackLeadSeconds);
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null)
                player = found.transform;
        }

        if (player != null && playerCollider == null)
            playerCollider = player.GetComponent<Collider2D>();
        if (player == null)
            Debug.LogError("[ProjectMinigame] Player reference is missing. Assign Player in Inspector or set tag 'Player'.", this);

        SetActiveSafe(resultPanel, false);
        SetActiveSafe(gameplayHudRoot, true);
        activeGameId = PlayerPrefs.GetString(ActiveGameIdKey, "project_game");
        activityId = NormalizeActivityId(PlayerPrefs.GetString(ActiveActivityIdKey, activityId));
        activityType = NormalizeActivityType(PlayerPrefs.GetString(ActiveActivityTypeKey, activityType));
        firstWinPoints = Mathf.Max(0, PlayerPrefs.GetInt(ActiveRewardPointsKey, firstWinPoints));
        oneTimeReward = PlayerPrefs.GetInt(ActiveOneTimeRewardKey, oneTimeReward ? 1 : 0) == 1;

        SpawnResources();
        SpawnEnemies();
        gatherTypeCount = GetHudLabelCount();
        collectedByType = new int[gatherTypeCount];
        currentStep = 0;
        runActive = true;
        waitingReturn = false;
        activeGatherNode = null;
        UpdateHud();
        Debug.Log("[ProjectMinigame] Started. Controlled enemies: " + enemies.Count, this);
    }

    private void Update()
    {
        if (waitingReturn)
        {
            if (Time.time >= resultAt)
                ReturnToMainScene();
            return;
        }

        if (!runActive || player == null)
            return;

        if (Input.GetKeyDown(quitKey))
        {
            ShowResultAndReturn("Project Quit", "You left the minigame.", "quit");
            return;
        }

        UpdateEnemies();
        if (!resolvingCaught && CheckEnemyCatch())
            StartCoroutine(HandleCaughtSequence());

        UpdateCollecting();
    }

    private void SpawnResources()
    {
        resources.Clear();

        HashSet<ProjectResourceNode> unique = new HashSet<ProjectResourceNode>();
        if (existingResourceNodes != null && existingResourceNodes.Length > 0)
        {
            for (int i = 0; i < existingResourceNodes.Length; i++)
            {
                ProjectResourceNode node = existingResourceNodes[i];
                if (node == null)
                    continue;
                if (!unique.Add(node))
                    continue;

                node.gameObject.SetActive(true);
                node.ResetNode();
                resources.Add(node);
            }
        }

        ProjectResourceNode[] found = FindObjectsByType<ProjectResourceNode>(FindObjectsSortMode.None);
        for (int i = 0; i < found.Length; i++)
        {
            ProjectResourceNode node = found[i];
            if (node == null)
                continue;
            if (!unique.Add(node))
                continue;

            node.gameObject.SetActive(true);
            node.ResetNode();
            resources.Add(node);
        }

        Debug.Log("[ProjectMinigame] Resource nodes registered: " + resources.Count, this);
    }

    private void SpawnEnemies()
    {
        enemies.Clear();

        if (existingEnemyObjects == null || existingEnemyObjects.Length == 0)
            return;

        for (int i = 0; i < existingEnemyObjects.Length; i++)
        {
            Transform e = existingEnemyObjects[i];
            if (e == null)
                continue;

            if (autoRenameEnemies)
                e.name = enemyNamePrefix + "_" + (i + 1).ToString("00");

            e.gameObject.SetActive(true);

            EnemyRuntime runtime = new EnemyRuntime
            {
                root = e,
                body = e.GetComponent<Rigidbody2D>(),
                collider = e.GetComponent<Collider2D>(),
                animator = e.GetComponent<Animator>() != null ? e.GetComponent<Animator>() : e.GetComponentInChildren<Animator>(),
                spriteRenderer = e.GetComponentInChildren<SpriteRenderer>(),
                vision = string.IsNullOrWhiteSpace(enemyVisionChildName) ? null : e.Find(enemyVisionChildName),
                visionCollider = null,
                roamTarget = GetRandomRoamPointFar(e.position),
                chasing = false,
                chaseLostTimer = 0f,
                previousPosition = e.position,
                stuckTimer = 0f,
                blockedTimer = 0f,
                detourSign = Random.value < 0.5f ? -1f : 1f,
                desiredPosition = e.position
            };

            if (runtime.body != null)
            {
                // Keep physics simple and deterministic: movement is transform-driven.
                runtime.body.simulated = false;
            }

            if (runtime.collider != null)
                runtime.collider.isTrigger = true;

            if (runtime.animator != null)
                runtime.animator.applyRootMotion = false;

            if (runtime.vision != null)
            {
                float d = enemyVisionRadius * 2f;
                runtime.vision.localScale = new Vector3(d, d, 1f);
                runtime.visionCollider = runtime.vision.GetComponent<Collider2D>();
            }

            if (showEnemyNames)
                EnsureEnemyNameTag(runtime);

            enemies.Add(runtime);
        }
    }

    private void UpdateEnemies()
    {
        Vector2 p = player.position;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyRuntime e = enemies[i];
            if (e.root == null)
                continue;

            if (IsPlayerInVision(e, p))
            {
                e.chasing = true;
                e.chaseLostTimer = 0f;
            }
            else if (e.chasing)
            {
                e.chaseLostTimer += Time.deltaTime;
                if (e.chaseLostTimer >= chaseLoseDelaySeconds)
                {
                    e.chasing = false;
                    e.chaseLostTimer = 0f;
                    e.roamTarget = GetRandomRoamPointFar(e.root.position);
                }
            }

            Vector2 target = e.chasing ? p : (Vector2)e.roamTarget;
            float speed = e.chasing ? enemyChaseSpeed : enemyRoamSpeed;
            Vector2 dir = (target - (Vector2)e.root.position);
            if (dir.sqrMagnitude > 0.0001f)
                dir.Normalize();

            float step = speed * Time.deltaTime;
            Vector2 current = e.root.position;
            Vector2 next = Vector2.MoveTowards(current, target, step);
            if (IsPathBlocked(e, next))
            {
                // Try smart detour first so enemy can slide around obstacles.
                if (TryFindDetourStep(e, current, target, step, out Vector2 detourNext))
                {
                    next = detourNext;
                    e.blockedTimer = 0f;
                }
                else
                {
                    e.blockedTimer += Time.deltaTime;
                    if (e.chasing)
                    {
                        if (TryFindEscapeStep(e, current, dir, step, out Vector2 escapeNext))
                        {
                            next = escapeNext;
                            e.blockedTimer = 0f;
                        }
                        else
                        {
                            e.detourSign *= -1f;
                            next = current;
                        }
                    }
                    else
                    {
                        e.roamTarget = GetRandomRoamPointFar(e.root.position);
                        next = current;
                    }
                }
            }
            else
            {
                e.blockedTimer = 0f;
            }
            e.desiredPosition = new Vector3(next.x, next.y, e.root.position.z);

            if (!e.chasing && Vector2.Distance(e.root.position, e.roamTarget) <= 0.25f)
                e.roamTarget = GetRandomRoamPointFar(e.root.position);

            float movedSqr = (e.root.position - e.previousPosition).sqrMagnitude;
            e.previousPosition = e.root.position;
            if (movedSqr < 0.00002f)
            {
                e.stuckTimer += Time.deltaTime;
                if (e.stuckTimer >= stuckRepathSeconds)
                {
                    e.stuckTimer = 0f;
                    e.roamTarget = GetRandomRoamPointFar(e.root.position);
                    // Fallback nudge when blocked by collider/tile edge.
                    e.desiredPosition += (Vector3)(dir * 0.12f);
                }
            }
            else
            {
                e.stuckTimer = 0f;
            }

            SpriteRenderer sr = e.spriteRenderer;
            if (sr != null && Mathf.Abs(dir.x) > 0.01f)
                sr.flipX = dir.x < 0f;
        }
    }

    private void LateUpdate()
    {
        // Apply enemy positions in LateUpdate so animation/root transforms cannot overwrite movement.
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyRuntime e = enemies[i];
            if (e.root == null)
                continue;
            e.root.position = e.desiredPosition;
        }
    }

    private bool CheckEnemyCatch()
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyRuntime e = enemies[i];
            if (e.root == null)
                continue;

            if (e.collider != null && playerCollider != null)
            {
                ColliderDistance2D d = e.collider.Distance(playerCollider);
                if (d.isOverlapped || d.distance <= enemyCatchSurfaceThreshold)
                    return true;
            }
            else if (Vector2.Distance(e.root.position, player.position) <= enemyCatchRadius)
                return true;
        }
        return false;
    }

    private System.Collections.IEnumerator HandleCaughtSequence()
    {
        if (resolvingCaught)
            yield break;

        resolvingCaught = true;
        runActive = false;

        EnemyRuntime nearest = null;
        float best = float.MaxValue;
        Vector2 p = player.position;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyRuntime e = enemies[i];
            if (e.root == null)
                continue;

            float d = Vector2.Distance(e.root.position, p);
            if (d < best)
            {
                best = d;
                nearest = e;
            }
        }

        if (nearest != null && nearest.animator != null)
        {
            if (!string.IsNullOrWhiteSpace(attackBoolName) && HasAnimatorBool(nearest.animator, attackBoolName))
                nearest.animator.SetBool(attackBoolName, true);
            if (!string.IsNullOrWhiteSpace(attackTriggerName) && HasAnimatorTrigger(nearest.animator, attackTriggerName))
                nearest.animator.SetTrigger(attackTriggerName);
            if (!string.IsNullOrWhiteSpace(attackStateName))
                nearest.animator.Play(attackStateName, 0, 0f);
            nearest.animator.Update(0f);
        }

        yield return new WaitForSeconds(Mathf.Max(0.1f, attackLeadSeconds));
        ShowResultAndReturn(
            "Project Failed",
            "The project died somewhere between chaos, meetings, and \"small feature requests.\" " +
            "The system is now officially classified as legacy and will be maintained by future interns.",
            "lose");
    }

    private void UpdateCollecting()
    {
        if (resources.Count == 0 || collectedByType == null || collectedByType.Length == 0)
            return;

        currentStep = GetCurrentStepIndex();
        if (currentStep >= collectedByType.Length)
            return;

        ProjectResourceNode active = null;
        float best = float.MaxValue;
        Vector2 p = player.position;
        for (int i = 0; i < resources.Count; i++)
        {
            ProjectResourceNode node = resources[i];
            if (node == null || node.IsCollected)
                continue;
            if (Mathf.Clamp(node.TypeIndex, 0, collectedByType.Length - 1) != currentStep)
                continue;

            float d = Vector2.Distance(p, node.transform.position);
            bool canGather = requireResourceTrigger ? node.IsPlayerInside : d <= collectRadius;
            if (!canGather)
                continue;

            if (d < best)
            {
                best = d;
                active = node;
            }
        }

        bool changed = false;
        if (activeGatherNode != active && activeGatherNode != null && !activeGatherNode.IsCollected)
        {
            activeGatherNode.TickGather(false, Time.deltaTime);
        }

        activeGatherNode = active;
        if (activeGatherNode != null && !activeGatherNode.IsCollected)
        {
            bool completedNow = activeGatherNode.TickGather(true, Time.deltaTime);
            if (!completedNow)
            {
                // Keep gathering until full.
            }
            else
            {
                int typeIndex = Mathf.Clamp(activeGatherNode.TypeIndex, 0, collectedByType.Length - 1);
                if (collectedByType[typeIndex] < pointsPerStep)
                    collectedByType[typeIndex]++;
                changed = true;
                activeGatherNode = null;
            }
        }

        if (changed)
        {
            currentStep = GetCurrentStepIndex();
            UpdateHud();
        }

        if (HasReachedAllTargets() && !resolvingWin)
            StartCoroutine(HandleWinSequence());
    }

    private void UpdateHud()
    {
        if (checklistText != null)
        {
            int maxPoints = Mathf.Max(1, pointsPerStep);
            string[] labels = useFixedHudTemplate ? FixedHudLabels : stepLabels;
            if (labels == null || labels.Length == 0)
                labels = FixedHudLabels;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < labels.Length; i++)
            {
                int current = 0;
                if (collectedByType != null && i < collectedByType.Length)
                    current = Mathf.Clamp(collectedByType[i], 0, maxPoints);
                string state = current + "/" + maxPoints;
                sb.Append(labels[i]).Append(": ").Append(state).Append('\n');
            }
            checklistText.text = sb.ToString().TrimEnd();
        }

        if (hintText != null)
            hintText.text = "Collect in order. Current: " + GetStepLabel(Mathf.Clamp(currentStep, 0, GetHudLabelCount() - 1)) + ". Press Q to quit.";
    }

    private void ShowResultAndReturn(string title, string body, string resultCode = "info", int awardedPoints = 0)
    {
        runActive = false;
        SetActiveSafe(gameplayHudRoot, false);
        SaveReturnResult(resultCode, title, body, awardedPoints);

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            if (resultTitleText != null) resultTitleText.text = title;
            if (resultBodyText != null) resultBodyText.text = body;
        }

        waitingReturn = true;
        resultAt = Time.time + Mathf.Max(0.1f, resultDelaySeconds);
    }

    private void ReturnToMainScene()
    {
        string sceneName = PlayerPrefs.GetString("project_return_scene_name", fallbackMainSceneName);
        SceneManager.LoadScene(sceneName);
    }

    private int GetHudLabelCount()
    {
        string[] labels = useFixedHudTemplate ? FixedHudLabels : stepLabels;
        if (labels == null || labels.Length == 0)
            labels = FixedHudLabels;
        return Mathf.Max(1, labels.Length);
    }

    private bool HasReachedAllTargets()
    {
        if (collectedByType == null || collectedByType.Length == 0)
            return false;

        int needed = Mathf.Max(1, pointsPerStep);
        for (int i = 0; i < collectedByType.Length; i++)
        {
            if (collectedByType[i] < needed)
                return false;
        }
        return true;
    }

    private int GetCurrentStepIndex()
    {
        if (collectedByType == null || collectedByType.Length == 0)
            return 0;

        int needed = Mathf.Max(1, pointsPerStep);
        for (int i = 0; i < collectedByType.Length; i++)
        {
            if (collectedByType[i] < needed)
                return i;
        }
        return collectedByType.Length;
    }

    private string GetStepLabel(int index)
    {
        string[] labels = useFixedHudTemplate ? FixedHudLabels : stepLabels;
        if (labels == null || labels.Length == 0)
            labels = FixedHudLabels;

        if (index < 0 || index >= labels.Length)
            return "Done";
        return labels[index];
    }

    private Vector3 GetRandomRoamPoint()
    {
        if (enemyRoamPoints == null || enemyRoamPoints.Length == 0)
            return GetFallbackRoamPoint();

        Transform t = enemyRoamPoints[Random.Range(0, enemyRoamPoints.Length)];
        return t != null ? t.position : GetFallbackRoamPoint();
    }

    private Vector3 GetRandomRoamPointFar(Vector3 from)
    {
        if (enemyRoamPoints == null || enemyRoamPoints.Length == 0)
            return GetFallbackRoamPoint();

        for (int i = 0; i < 20; i++)
        {
            Vector3 p = GetRandomRoamPoint();
            if (Vector2.Distance(from, p) >= minRoamTargetDistance)
                return p;
        }

        return GetFallbackRoamPoint();
    }

    private Vector3 GetFallbackRoamPoint()
    {
        if (player == null)
            return Vector3.zero;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Random.Range(1.5f, 5f);
        return player.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
    }

    private float GetEffectiveVisionRadius(EnemyRuntime enemy)
    {
        float r = enemyVisionRadius;
        if (enemy.vision != null)
        {
            // Vision child is scaled as diameter.
            float fromVision = Mathf.Abs(enemy.vision.lossyScale.x) * 0.5f;
            r = Mathf.Max(r, fromVision);
        }
        return Mathf.Max(0.1f, r);
    }

    private bool IsPlayerInVision(EnemyRuntime enemy, Vector2 playerPos)
    {
        if (enemy.visionCollider != null && playerCollider != null)
            return enemy.visionCollider.bounds.Intersects(playerCollider.bounds);

        Vector2 visionCenter = enemy.vision != null ? (Vector2)enemy.vision.position : (Vector2)enemy.root.position;
        float visionRadius = GetEffectiveVisionRadius(enemy);
        return Vector2.Distance(visionCenter, playerPos) <= visionRadius;
    }

    private bool IsPathBlocked(EnemyRuntime enemy, Vector2 next)
    {
        if (!useObstacleBlocking)
            return false;

        Vector2 current = enemy.root.position;
        Vector2 delta = next - current;
        float dist = delta.magnitude;
        if (dist <= 0.0001f)
            return false;

        float probeRadius = 0.12f;
        if (enemy.collider is CircleCollider2D cc)
            probeRadius = Mathf.Max(0.05f, cc.radius * Mathf.Max(1f, enemy.root.lossyScale.x));

        // Reliable regardless of LayerMask setup: inspect colliders at next step.
        Collider2D[] hits = Physics2D.OverlapCircleAll(next, probeRadius + wallProbePadding);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
                continue;
            if (hit.isTrigger)
                continue;
            if (playerCollider != null && hit == playerCollider)
                continue;
            if (enemy.collider != null && hit == enemy.collider)
                continue;
            if (hit.transform == enemy.root || hit.transform.IsChildOf(enemy.root))
                continue;

            // Ignore other enemy bodies.
            bool isOtherEnemy = false;
            for (int j = 0; j < enemies.Count; j++)
            {
                Transform r = enemies[j].root;
                if (r == null)
                    continue;
                if (hit.transform == r || hit.transform.IsChildOf(r))
                {
                    isOtherEnemy = true;
                    break;
                }
            }
            if (isOtherEnemy)
                continue;

            if (!IsObstacleCollider(hit.transform))
                continue;

            return true;
        }

        return false;
    }

    private bool TryFindDetourStep(EnemyRuntime enemy, Vector2 current, Vector2 target, float step, out Vector2 detourNext)
    {
        detourNext = current;

        Vector2 forward = target - current;
        if (forward.sqrMagnitude <= 0.0001f)
            return false;

        forward.Normalize();

        // Ordered from small to large turn so enemy still tries to progress toward target.
        float[] turnAngles = { 30f, -30f, 55f, -55f, 85f, -85f, 120f, -120f, 170f, -170f };
        float[] stepScales = { 1f, 1.5f, 2f };
        for (int s = 0; s < stepScales.Length; s++)
        {
            for (int i = 0; i < turnAngles.Length; i++)
            {
                Vector2 dir = Rotate2D(forward, turnAngles[i]);
                Vector2 candidate = current + dir * step * stepScales[s];
                if (!IsPathBlocked(enemy, candidate))
                {
                    detourNext = candidate;
                    enemy.detourSign = Mathf.Sign(turnAngles[i]);
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryFindEscapeStep(EnemyRuntime enemy, Vector2 current, Vector2 forward, float step, out Vector2 escapeNext)
    {
        escapeNext = current;
        if (forward.sqrMagnitude <= 0.0001f)
            return false;

        float preferredSign = Mathf.Approximately(enemy.detourSign, 0f) ? 1f : Mathf.Sign(enemy.detourSign);
        float[] stepScales = { 1f, 1.5f, 2f };
        float[] turnAngles = { 90f * preferredSign, -90f * preferredSign, 135f * preferredSign, -135f * preferredSign, 180f };
        for (int a = 0; a < turnAngles.Length; a++)
        {
            Vector2 side = Rotate2D(forward.normalized, turnAngles[a]);
            for (int i = 0; i < stepScales.Length; i++)
            {
                Vector2 candidate = current + side * step * stepScales[i];
                if (!IsPathBlocked(enemy, candidate))
                {
                    escapeNext = candidate;
                    enemy.detourSign = Mathf.Sign(turnAngles[a]);
                    return true;
                }
            }
        }

        return false;
    }

    private static Vector2 Rotate2D(Vector2 v, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float cs = Mathf.Cos(rad);
        float sn = Mathf.Sin(rad);
        return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
    }

    private bool IsObstacleCollider(Transform t)
    {
        if (t == null)
            return false;

        if (obstacleRoots != null && obstacleRoots.Length > 0)
        {
            for (int i = 0; i < obstacleRoots.Length; i++)
            {
                Transform root = obstacleRoots[i];
                if (root == null)
                    continue;
                if (t == root || t.IsChildOf(root))
                    return true;
            }
            return false;
        }

        // Fallback: treat anything under an object named "Buildings" as obstacle.
        Transform cursor = t;
        while (cursor != null)
        {
            if (cursor.name.ToLowerInvariant().Contains("building"))
                return true;
            cursor = cursor.parent;
        }
        return false;
    }

    private static bool HasAnimatorBool(Animator animator, string name)
    {
        AnimatorControllerParameter[] p = animator.parameters;
        for (int i = 0; i < p.Length; i++)
        {
            if (p[i].name == name && p[i].type == AnimatorControllerParameterType.Bool)
                return true;
        }
        return false;
    }

    private static bool HasAnimatorTrigger(Animator animator, string name)
    {
        AnimatorControllerParameter[] p = animator.parameters;
        for (int i = 0; i < p.Length; i++)
        {
            if (p[i].name == name && p[i].type == AnimatorControllerParameterType.Trigger)
                return true;
        }
        return false;
    }

    private static void SetActiveSafe(GameObject obj, bool value)
    {
        if (obj != null)
            obj.SetActive(value);
    }

    private void SaveReturnResult(string resultCode, string title, string body, int awardedPoints)
    {
        SetInt("result_pending", 1);
        SetString("result_code", string.IsNullOrWhiteSpace(resultCode) ? "info" : resultCode);
        SetString("result_title", title ?? string.Empty);
        SetString("result_body", body ?? string.Empty);
        SetInt("result_awarded_points", Mathf.Max(0, awardedPoints));
        SetString("last_result_code", string.IsNullOrWhiteSpace(resultCode) ? "info" : resultCode);
        PlayerPrefs.Save();
    }

    private string Key(string suffix)
    {
        return activeGameId + "_" + suffix;
    }

    private int GetInt(string suffix, int defaultValue)
    {
        return PlayerPrefs.GetInt(Key(suffix), defaultValue);
    }

    private void SetInt(string suffix, int value)
    {
        PlayerPrefs.SetInt(Key(suffix), value);
    }

    private void SetString(string suffix, string value)
    {
        PlayerPrefs.SetString(Key(suffix), value);
    }

    private void SavePendingReward()
    {
        PlayerPrefs.SetInt(PendingRewardFlagKey, 1);
        PlayerPrefs.SetString(PendingRewardActivityIdKey, activityId);
        PlayerPrefs.SetString(PendingRewardActivityTypeKey, activityType);
        PlayerPrefs.SetInt(PendingRewardPointsKey, Mathf.Max(0, firstWinPoints));
        PlayerPrefs.SetInt(PendingRewardOneTimeKey, oneTimeReward ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log("[ProjectMinigame] Saved pending reward. activityId=" + activityId + ", activityType=" + activityType + ", points=" + Mathf.Max(0, firstWinPoints), this);
    }

    private void EnsureEnemyNameTag(EnemyRuntime enemy)
    {
        if (enemy.root == null)
            return;

        const string tagName = "EnemyNameTag";
        Transform existing = enemy.root.Find(tagName);
        TextMeshPro label = existing != null ? existing.GetComponent<TextMeshPro>() : null;

        if (label == null)
        {
            GameObject go = new GameObject(tagName);
            go.transform.SetParent(enemy.root, false);
            go.transform.localPosition = enemyNameOffset;
            label = go.AddComponent<TextMeshPro>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = Mathf.Max(0.5f, enemyNameFontSize);
            label.color = Color.white;
            label.outlineWidth = 0.2f;
            label.outlineColor = new Color(0f, 0f, 0f, 0.85f);
            label.sortingOrder = enemyNameSortingOrder;
        }
        else
        {
            label.transform.localPosition = enemyNameOffset;
            label.sortingOrder = enemyNameSortingOrder;
            label.fontSize = Mathf.Max(0.5f, enemyNameFontSize);
        }

        label.text = enemy.root.name;
    }

    private System.Collections.IEnumerator HandleWinSequence()
    {
        if (resolvingWin)
            yield break;

        resolvingWin = true;
        runActive = false;

        int awarded = Mathf.Max(0, firstWinPoints);
        string body =
            "The software reached production without collapsing. The boss is happy, the client is impressed, " +
            "and production will probably stay stable until someone says \"quick hotfix.\"";

        SavePendingReward();
        body += "\nReturn to the terminal and press ENTER to claim your reward.";

        ShowResultAndReturn("Deployment Successful", body, "win", awarded);
        yield break;
    }

    private static string NormalizeActivityId(string value)
    {
        string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? "project_game" : normalized;
    }

    private static string NormalizeActivityType(string value)
    {
        string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? "project" : normalized;
    }
}
