using UnityEngine;

public class ArrowScaler : MonoBehaviour
{
    [SerializeField]
    private Vector3 _arrowVector = Vector3.forward;
    public Vector3 ArrowVector
    {
        get
        {
            return _arrowVector;
        }
        set
        {
            _arrowVector = value;

            _needsToUpdate = true;
        }
    }

    [SerializeField]
    private Color _arrowColor = Color.red;
    public Color ArrowColor
    {
        get 
        { 
            return _arrowColor; 
        }
        set 
        { 
            _arrowColor = value;

            _needsToUpdate = true;
        }
    }

    [SerializeField]
    private float _arrowScale = 1f;
    public float ArrowScale
    {
        get
        {
            return _arrowScale;
        }
        set
        {
            _arrowScale = value;

            _needsToUpdate = true;
        }
    }

    public Vector3 ArrowEnd
    {
        get { return ConeEnd.position; }
    }

    bool _needsToUpdate = false;

    public Transform CylinderBase;
    public Transform CylinderEnd;
    
    public Transform ConeBase;
    public Transform ConeEnd;

    public Renderer CylinderRenderer;
    public Renderer ConeRenderer;

    // Update the arrow based on initial values
    private void Start()
    {
        UpdateArrow();
        _needsToUpdate = false;
    }

    // Update the arrow if it has changed
    private void Update()
    {
        if (_needsToUpdate) 
        {
            UpdateArrow();
            _needsToUpdate = false;
        }
    }

    // This is called when we update the inspector values
    private void OnValidate()
    {
        UpdateArrow();
    }

    private void UpdateArrow()
    {
        // ArrowGroupScale the cylinder according to the ArrowPrefab Direction magnitude (aka length)
        // We start by setting Vector3.one, which equals (1, 1, 1)
        // We then add Vector3.Forward (0, 0, 1), which is multiplied by the arrow vector length * 2 - 2
        // For example, if the arrow magnitude equals 1, this would give the formula:
        // (1, 1, 1) + (0, 0, 1) * (1 * 2 - 2) = (1, 1, 1)
        // For a magnitude of 2:
        // (1, 1, 1) + (0, 0, 1) * (2 * 2 - 2) = (1, 1, 3)
        // Since the cylinder length is 0.5 by default, it would scale to 1.5 in that situation.
        // Then, with the cone length of 0.5, the arrow total length is 2, like the vector magnitude
        CylinderBase.localScale = new Vector3(ArrowScale, ArrowScale, 1) + Vector3.forward * (ArrowVector.magnitude * 2 - 2f);

        ConeBase.localScale = new Vector3(ArrowScale, ArrowScale, 1);

        // We are multiplying the local position and local scale component-wise
        // We are trying to place the cone base transform onto the cylinder end local position, which is affected by local scale
        // We only move the cone, we don't scale it
        // I'm not considering vectors of length smaller than 1 in that examples
        ConeBase.localPosition = Vector3.Scale(CylinderEnd.localPosition, CylinderBase.localScale);

        // This function allows us to look towards the vector. It is done using the forward Z-Axis in unity
        // It will act like the Z-Axis of the game object will face the supplied direction
        transform.rotation = Quaternion.LookRotation(ArrowVector.normalized);

        // Update the arrow color, causes errors when outside of playmode. Material is not accessible outside of playmode.
        if (Application.isPlaying)
        {
            // Material is not accessible outside of Play, only sharedMaterial is.
            // Since I want to individually change the color of each arrow, let's wait until we are in playmode
            ConeRenderer.material.color = ArrowColor;
            CylinderRenderer.material.color = ArrowColor;
        }
    }
}
