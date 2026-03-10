using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProjectResourceNode : MonoBehaviour
{
    public enum ResourceType
    {
        Requirement = 0,
        Design = 1,
        Implemented = 2,
        Testing = 3,
        Deploy = 4
    }

    [Header("Node")]
    [SerializeField] private ResourceType resourceType = ResourceType.Requirement;
    [SerializeField] private float gatherSeconds = 1.1f;
    [SerializeField] private bool hideAfterCollected = true;
    [SerializeField] private bool autoSetupTrigger = true;
    [SerializeField] private float triggerRadius = 0.75f;

    [Header("Progress UI")]
    [SerializeField] private GameObject progressRoot;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private TMP_Text progressPercentText;

    private float progress01;
    private bool collected;
    private bool playerInside;
    private CircleCollider2D triggerCollider;

    public int TypeIndex => (int)resourceType;
    public bool IsCollected => collected;
    public bool IsPlayerInside => playerInside;

    public void ResetNode()
    {
        progress01 = 0f;
        collected = false;
        playerInside = false;
        EnsureTrigger();
        UpdateProgressVisuals();
        SetProgressVisible(false);
        gameObject.SetActive(true);
    }

    private void OnValidate()
    {
        triggerRadius = Mathf.Max(0.1f, triggerRadius);
        if (!Application.isPlaying)
            EnsureTrigger();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other != null && other.CompareTag("Player"))
            playerInside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other != null && other.CompareTag("Player"))
            playerInside = false;
    }

    public bool TickGather(bool gathering, float deltaTime)
    {
        if (collected)
            return false;

        if (!gathering)
        {
            progress01 = 0f;
            UpdateProgressVisuals();
            SetProgressVisible(false);
            return false;
        }

        SetProgressVisible(true);
        float duration = Mathf.Max(0.1f, gatherSeconds);
        progress01 = Mathf.Clamp01(progress01 + (deltaTime / duration));
        UpdateProgressVisuals();

        if (progress01 < 1f)
            return false;

        collected = true;
        SetProgressVisible(false);
        if (hideAfterCollected)
            gameObject.SetActive(false);
        return true;
    }

    private void SetProgressVisible(bool value)
    {
        if (progressRoot != null)
            progressRoot.SetActive(value);
    }

    private void UpdateProgressVisuals()
    {
        if (progressSlider != null)
            progressSlider.normalizedValue = progress01;

        if (progressFillImage != null)
            progressFillImage.fillAmount = progress01;

        if (progressPercentText != null)
            progressPercentText.text = Mathf.RoundToInt(progress01 * 100f) + "%";
    }

    private void EnsureTrigger()
    {
        if (!autoSetupTrigger)
            return;

        triggerCollider = GetComponent<CircleCollider2D>();
        if (triggerCollider == null)
            triggerCollider = gameObject.AddComponent<CircleCollider2D>();

        triggerCollider.isTrigger = true;
        triggerCollider.radius = triggerRadius;
    }
}
