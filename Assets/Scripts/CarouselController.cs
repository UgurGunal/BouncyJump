using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// UI Carousel: swap pages with left/right buttons. Pages loop (last+right = first, first+left = last).
/// No LeanTween - uses a simple coroutine for smooth sliding.
/// </summary>
public class CarouselController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The moving content that holds all pages as direct children.")]
    public RectTransform content;
    [Tooltip("The visible viewport (used to get page width). If null, first page width is used.")]
    public RectTransform viewport;
    [Tooltip("Left button = previous page.")]
    public Button leftButton;
    [Tooltip("Right button = next page.")]
    public Button rightButton;

    [Header("Settings")]
    [Tooltip("Duration of the slide animation in seconds.")]
    public float transitionDuration = 0.25f;
    [Tooltip("If true, content slides smoothly. If false, pages switch instantly.")]
    public bool animateTransition = true;

    int _currentIndex;
    int _pageCount;
    // Distance between the start of one page and the next (page width + spacing).
    float _pageStep;
    Vector2 _baseAnchoredPosition;
    Coroutine _transitionCoroutine;

    public int CurrentPageIndex => _currentIndex;
    public int PageCount => _pageCount;

    void Awake()
    {
        if (content == null)
        {
            content = GetComponent<RectTransform>();
            if (content == null)
            {
                Debug.LogError("CarouselController: No content RectTransform assigned.");
                return;
            }
        }

        _pageCount = content.childCount;
        if (_pageCount == 0)
        {
            Debug.LogError("CarouselController: Content has no children (pages).");
            return;
        }

        _baseAnchoredPosition = content.anchoredPosition;
        ComputePageStep();
    }

    void Start()
    {
        if (leftButton != null)
            leftButton.onClick.AddListener(GoToPreviousPage);
        if (rightButton != null)
            rightButton.onClick.AddListener(GoToNextPage);

        // Snap to first page without animation
        _currentIndex = 0;
        SnapToPage(0);
    }

    void ComputePageStep()
    {
        float pageWidth = 0f;
        if (viewport != null)
        {
            // Use viewport width as page width when provided
            pageWidth = viewport.rect.width;
        }
        else if (content.childCount > 0)
        {
            RectTransform first = content.GetChild(0) as RectTransform;
            if (first != null)
                pageWidth = first.rect.width;
            else
                pageWidth = content.rect.width / _pageCount;
        }

        float spacing = 0f;
        var layout = content.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            spacing = layout.spacing;
        }

        _pageStep = pageWidth + spacing;
    }

    /// <summary>Go to next page. Loops to first if currently on last.</summary>
    public void GoToNextPage()
    {
        if (_pageCount == 0) return;
        int next = (_currentIndex + 1) % _pageCount;
        GoToPage(next);
    }

    /// <summary>Go to previous page. Loops to last if currently on first.</summary>
    public void GoToPreviousPage()
    {
        if (_pageCount == 0) return;
        int prev = (_currentIndex - 1 + _pageCount) % _pageCount;
        GoToPage(prev);
    }

    /// <summary>Go to a specific page index (0-based). Wraps if out of range.</summary>
    public void GoToPage(int index)
    {
        if (_pageCount == 0) return;
        index = ((index % _pageCount) + _pageCount) % _pageCount;
        if (index == _currentIndex) return;

        _currentIndex = index;

        if (_transitionCoroutine != null)
            StopCoroutine(_transitionCoroutine);

        if (animateTransition && transitionDuration > 0f)
            _transitionCoroutine = StartCoroutine(AnimateToPage(index));
        else
            SnapToPage(index);
    }

    void SnapToPage(int index)
    {
        // Horizontal: we move content so that page at index is in view.
        // Assuming content is left-aligned and grows to the right,
        // target X = -index * (page width + spacing).
        float targetX = -index * _pageStep;
        Vector2 target = _baseAnchoredPosition;
        target.x = targetX;
        content.anchoredPosition = target;
    }

    IEnumerator AnimateToPage(int index)
    {
        float targetX = -index * _pageStep;
        Vector2 start = content.anchoredPosition;
        Vector2 end = _baseAnchoredPosition;
        end.x = targetX;

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            // Smooth ease
            t = t * t * (3f - 2f * t);
            content.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }

        content.anchoredPosition = end;
        _transitionCoroutine = null;
    }
}
