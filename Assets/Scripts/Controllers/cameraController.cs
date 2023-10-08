using UnityEngine;

public class cameraController : MonoBehaviour
{
    public MapGenerator map;

    public Transform cameraTransform;
    public Transform swivelTransform;

    public float minHeight;
    public float maxHeight;
    public float zoomAmountScaler;
    public float zoomSwivelScaler;
    public float minHeightSwivelDegrees;
    public float maxHeightSwivelDegrees;

    public float movementSpeed;
    public float movementSpeedFast;
    public float movementTime;
    public float rotationAmount;
    public float zoomAmount;

    public Vector3 newPosition;
    public Quaternion newRotation;
    public Quaternion newSwivelRotation;
    public Vector3 newZoom;

    public Vector3 dragStartPosition;
    public Vector3 dragCurrentPosition;
    public Vector3 rotateStartPosition;
    public Vector3 rotateCurrentPosition;

    // Start is called before the first frame update
    private void Start()
    {
        newPosition = transform.position;
        newRotation = transform.rotation;
        newSwivelRotation = swivelTransform.localRotation;
        newZoom = cameraTransform.localPosition;
    }

    // Update is called once per frame
    private void Update()
    {
        HandleMouseInput();
        HandleMovementInput();

        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * movementTime);
        swivelTransform.localRotation = Quaternion.Lerp(swivelTransform.localRotation, newSwivelRotation, Time.deltaTime * movementTime);
        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, newZoom, Time.deltaTime * movementTime);
    }

    private void HandleMouseInput()
    {
        // Scroll Wheel Zoom
        if (Input.mouseScrollDelta.y != 0)
        {
            CalcZoom(Input.mouseScrollDelta.y);
        }

        // Right-click and drag movement
        if (Input.GetMouseButtonDown(1))
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            float entry;

            if (plane.Raycast(ray, out entry))
            {
                Debug.Log(entry);
                dragStartPosition = ray.GetPoint(entry);
            }
        }
        if (Input.GetMouseButton(1))
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            float entry;

            if (plane.Raycast(ray, out entry))
            {
                dragCurrentPosition = ray.GetPoint(entry);

                newPosition = /*ClampPosition(*/transform.position + dragStartPosition - dragCurrentPosition/*)*/;
            }
        }

        // Middle-mouse rotate
        if (Input.GetMouseButtonDown(2))
        {
            rotateStartPosition = Input.mousePosition;
        }
        if (Input.GetMouseButton(2))
        {
            rotateCurrentPosition = Input.mousePosition;

            Vector3 difference = rotateStartPosition - rotateCurrentPosition;

            rotateStartPosition = rotateCurrentPosition;

            newRotation *= Quaternion.Euler(Vector3.up * (-difference.x / 5f));
        }
    }

    private void HandleMovementInput()
    {
        // Movement

        float moveSpeed = Input.GetKey(KeyCode.LeftShift) ? movementSpeedFast : movementSpeed;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            newPosition += (transform.forward * moveSpeed);
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            newPosition += (transform.forward * -moveSpeed);
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            newPosition += (transform.right * -moveSpeed);
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            newPosition += (transform.right * moveSpeed);
        }

        // newPosition = ClampPosition(newPosition);

        // Rotation
        if (Input.GetKey(KeyCode.Q))
        {
            newRotation *= Quaternion.Euler(Vector3.up * rotationAmount);
        }
        if (Input.GetKey(KeyCode.E))
        {
            newRotation *= Quaternion.Euler(Vector3.up * -rotationAmount);
        }

        // Zoom
        if (Input.GetKey(KeyCode.R))
        {
            CalcZoom(1);
        }
        if (Input.GetKey(KeyCode.F))
        {
            CalcZoom(-1);
        }
    }

    private void CalcZoom(float zoomValue = 1)
    {
        // Zoom slower when closer to zoom limits See
        // https://www.desmos.com/calculator and copy the below
        // \frac{\left(-n-\left(\frac{\left(a+n\right)}{\left(a+n\right)^{2}}\right)\cdot\
        // x^{2}\ +\ x\ \right)}{10}
        var maxmin = (maxHeight * 1.2f) + minHeight;
        var maxmin2 = maxmin * maxmin;
        zoomAmountScaler = (minHeight + maxmin / maxmin2 * (newZoom.z * newZoom.z) - newZoom.z) / 10;

        if (zoomAmountScaler < 0.8f)
        {
            zoomAmountScaler = 0.8f;
        }

        // Allow scale out
        if (newZoom.z + 1 > maxHeight && zoomValue < 0)
        {
            newZoom.z += (zoomValue * zoomAmountScaler) * zoomAmount;
        }

        // Allow scale in
        else if (newZoom.z - 1 < minHeight && zoomValue > 0)
        {
            newZoom.z += (zoomValue * zoomAmountScaler) * zoomAmount;
        }

        // Transition to top-down view when zoomed out
        float minSwivelHeight = minHeight * 2;
        float maxSwivelHeight = maxHeight * 0.6f;
        zoomSwivelScaler = Mathf.Clamp01(newZoom.z / (maxSwivelHeight + minSwivelHeight));
        float angle = Mathf.Lerp(minHeightSwivelDegrees, maxHeightSwivelDegrees, zoomSwivelScaler);
        newSwivelRotation = Quaternion.Euler(angle, 0, 0);
    }

    //private Vector3 ClampPosition(Vector3 position)
    //{
    //    // Clamp position to prevent camera leaving map borders
    //    float xMax =
    //        (cellGrid.chunkCountX * CellMetrics.chunkSizeX - 0.5f) *
    //        (2f * CellMetrics.size);
    //    position.x = Mathf.Clamp(position.x, 0f, xMax);
    //
    //    float zMax =
    //        (cellGrid.chunkCountZ * CellMetrics.chunkSizeZ - 1) *
    //        (1.5f * CellMetrics.size);
    //    position.z = Mathf.Clamp(position.z, 0f, zMax);
    //
    //    return position;
    //}
}