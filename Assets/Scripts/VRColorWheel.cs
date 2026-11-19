using UnityEngine;

[RequireComponent(typeof(UnityEngine.UI.RawImage))]
public class VRColorWheel : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler,
    UnityEngine.EventSystems.IDragHandler
{
    [SerializeField] private UnityEngine.UI.RawImage _wheelImage;
    [SerializeField] private RectTransform _selectionMarker;
    [SerializeField] private UnityEngine.UI.Image _swatch;
    [SerializeField] private UnityEngine.Events.UnityEvent<Color> _colorSelected = new UnityEngine.Events.UnityEvent<Color>();
    private RectTransform _rect;
    private Vector2 _selectedPosition;
    public Color SelectedColor { get; private set; } = Color.white;

    private void Awake()
    {
        _rect = (RectTransform)transform;
        if (_wheelImage == null) _wheelImage = GetComponent<UnityEngine.UI.RawImage>();
    }

    private void OnEnable()
    {
        RefreshColor();
    }

    public static bool TryGetWheelColor(Vector2 position, float brightness, out Color color)
    {
        color = Color.white;
        if (!NetworkWallPaintState.IsFinite(new Vector3(position.x, position.y, brightness)) || position.sqrMagnitude > 1f) return false;
        float hue = Mathf.Repeat(Mathf.Atan2(position.y, position.x) / (Mathf.PI * 2f), 1f);
        color = Color.HSVToRGB(hue, position.magnitude, Mathf.Clamp01(brightness));
        color.a = 1f;
        return true;
    }

    public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
    {
        SelectPointer(eventData);
    }

    public void OnDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        SelectPointer(eventData);
    }

    private void SelectPointer(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (_rect == null) _rect = (RectTransform)transform;
        Vector2 local;
        if (eventData is UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceEventData)
        {
            if (eventData.pointerCurrentRaycast.gameObject == null) return;
            local = _rect.InverseTransformPoint(eventData.pointerCurrentRaycast.worldPosition);
        }
        else if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, eventData.position, eventData.pressEventCamera, out local))
        {
            return;
        }
        Vector2 normalized = new Vector2(local.x * 2f / _rect.rect.width, local.y * 2f / _rect.rect.height);
        if (normalized.sqrMagnitude > 1f) return;
        _selectedPosition = normalized;
        RefreshColor();
    }

    private void RefreshColor()
    {
        if (_rect == null) _rect = (RectTransform)transform;
        TryGetWheelColor(_selectedPosition, 1f, out var color);
        SelectedColor = color;
        if (_wheelImage != null) _wheelImage.color = Color.white;
        if (_swatch != null) _swatch.color = color;
        if (_selectionMarker != null) _selectionMarker.anchoredPosition = new Vector2(_selectedPosition.x * _rect.rect.width * 0.5f, _selectedPosition.y * _rect.rect.height * 0.5f);
        _colorSelected.Invoke(color);
    }
}
