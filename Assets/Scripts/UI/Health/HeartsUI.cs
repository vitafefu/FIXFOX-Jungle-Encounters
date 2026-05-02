using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class HeartsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Heart Sprites")]
    [SerializeField] private Sprite emptyHeart;
    [SerializeField] private Sprite quarterHeart;
    [SerializeField] private Sprite halfHeart;
    [SerializeField] private Sprite threeQuarterHeart;
    [SerializeField] private Sprite fullHeart;

    [Header("Layout")]
    [SerializeField] private Vector2 anchoredPosition = new Vector2(25f, -25f);
    [SerializeField] private Vector2 heartSize = new Vector2(36f, 36f);
    [SerializeField] private float spacing = 6f;

    [Header("Optional")]
    [SerializeField] private bool autoFindPlayerByTag = true;
    [SerializeField] private string playerTag = "Player";

    private readonly List<Image> heartImages = new List<Image>();

    private RectTransform rectTransform;
    private HorizontalLayoutGroup layoutGroup;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        layoutGroup = GetComponent<HorizontalLayoutGroup>();

        SetupRootRect();
        SetupLayoutGroup();

        if (playerHealth == null && autoFindPlayerByTag)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
                playerHealth = player.GetComponent<PlayerHealth>();
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged += Refresh;
    }

    private void Start()
    {
        if (playerHealth != null)
            Refresh(playerHealth.CurrentHealthUnits, playerHealth.MaxHealthUnits);
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= Refresh;
    }

    private void SetupRootRect()
    {
        if (rectTransform == null)
            return;

        // Fixed to top-left of screen
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(400f, heartSize.y);
    }

    private void SetupLayoutGroup()
    {
        if (layoutGroup == null)
            layoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();

        layoutGroup.spacing = spacing;
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childScaleWidth = false;
        layoutGroup.childScaleHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
    }

    private void Refresh(int currentHealthUnits, int maxHealthUnits)
    {
        if (playerHealth == null)
            return;

        int unitsPerHeart = Mathf.Max(1, playerHealth.UnitsPerHeart);
        int requiredHeartCount = Mathf.CeilToInt((float)maxHealthUnits / unitsPerHeart);

        EnsureHeartCount(requiredHeartCount);

        for (int i = 0; i < heartImages.Count; i++)
        {
            bool shouldShow = i < requiredHeartCount;
            heartImages[i].gameObject.SetActive(shouldShow);

            if (!shouldShow)
                continue;

            int heartStartUnit = i * unitsPerHeart;
            int unitsInsideThisHeart = Mathf.Clamp(
                currentHealthUnits - heartStartUnit,
                0,
                unitsPerHeart
            );

            heartImages[i].sprite = GetHeartSprite(unitsInsideThisHeart, unitsPerHeart);
            heartImages[i].preserveAspect = true;
        }
    }

    private void EnsureHeartCount(int requiredCount)
    {
        while (heartImages.Count < requiredCount)
        {
            CreateHeartImage();
        }
    }

    private void CreateHeartImage()
    {
        GameObject heartObject = new GameObject("Heart_" + heartImages.Count, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        heartObject.transform.SetParent(transform, false);

        RectTransform heartRect = heartObject.GetComponent<RectTransform>();
        heartRect.sizeDelta = heartSize;

        Image image = heartObject.GetComponent<Image>();
        image.sprite = emptyHeart;
        image.preserveAspect = true;

        heartImages.Add(image);
    }

    private Sprite GetHeartSprite(int units, int unitsPerHeart)
    {
        if (units <= 0)
            return emptyHeart;

        if (units >= unitsPerHeart)
            return fullHeart;

        // Special support for the common case: 4 units = 1 heart
        if (unitsPerHeart == 4)
        {
            if (units == 1)
                return quarterHeart != null ? quarterHeart : halfHeart;

            if (units == 2)
                return halfHeart;

            if (units == 3)
                return threeQuarterHeart != null ? threeQuarterHeart : fullHeart;
        }

        float ratio = (float)units / unitsPerHeart;

        if (ratio <= 0.25f)
            return quarterHeart != null ? quarterHeart : halfHeart;

        if (ratio <= 0.5f)
            return halfHeart;

        if (ratio <= 0.75f)
            return threeQuarterHeart != null ? threeQuarterHeart : fullHeart;

        return fullHeart;
    }
}