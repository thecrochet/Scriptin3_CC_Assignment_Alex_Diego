using System.Collections.Generic;
using UnityEngine;

public class VelocityVectorAddVisualization : MonoBehaviour
{
    public bool ResetArrows = false;
    public bool ShuffleArrows = false;

    public int NumberVectors = 25;

    public float MagnitudeLowRange = 1.0f;
    public float MagnitudeHighRange = 5.0f;

    public float ArrowGroupScale = 0.1f;

    public ArrowScaler ArrowPrefab;

    private Transform ArrowGroupParent;

    private List<ArrowScaler> arrowList = new List<ArrowScaler>();

    private Vector3 vectorSum = Vector3.zero;

    // Generate new vectors and show them
    private void Awake()
    {
        GenerateNewArrows();

        StartUpdateCycle();
    }

    // This is the recommended form of async functions, not coroutines!
    private async void StartUpdateCycle()
    {
        bool update = true;

        // Never EVER perform a while true loop without waiting for at least of frame like here, otherwise it will hang the application
        while (update)
        {
            await Awaitable.WaitForSecondsAsync(0.5f);
            ShuffleCurrentArrows();
        }
    }

    // Handle manual update through inspector
    private void Update()
    {
        if (ResetArrows)
        {
            GenerateNewArrows();

            ResetArrows = false;
        }

        if (ShuffleArrows)
        {
            ShuffleCurrentArrows();

            ShuffleArrows = false;
        }
    }

    private void ShuffleCurrentArrows()
    {
        // Shuffle the list of arrows
        // Made through the ShufflingListExtension script
        // Advanced example of extension, where we extend the actual List<t> class in C#!
        arrowList.Shuffle();

        Vector3 currentArrowPosition = Vector3.zero;

        // Loop through each arrows starting from the beginning of the shuffled list
        // and basically rebuild a new chain based on the new shuffled positions
        // The vector total sum should add up to the same exact vector
        foreach (ArrowScaler arrow in arrowList) 
        {
            arrow.transform.localPosition = currentArrowPosition;

            currentArrowPosition += arrow.ArrowVector;
        }

        // We don't need to recalculate the vector sum, it's the same as when we initially generated the arrows
        // There is no difference before and after shuffling the vectors.
    }

    private void GenerateNewArrows()
    {
        // Create the arrow group and clear previous arrows
        ResetArrowGroup();

        Vector3 currentArrowPosition = Vector3.zero;

        for (int i = 0; i < NumberVectors; i++)
        {
            // Get a random normalized vector, a direction vector
            Vector3 randomDirection = Random.insideUnitSphere.normalized;

            // Multiply it by a random magnitude (vector length) to scale it
            Vector3 randomVector = randomDirection * Random.Range(MagnitudeLowRange, MagnitudeHighRange);

            // Make it so that it always point up by making y absolute
            randomVector.y = Mathf.Abs(randomVector.y);

            // Use that same random vector to generate a color, which will be representative of the direction
            // Color shouldn't be negative, so we set it to a positive value with Mathf.Abs
            Color randomColor = new Color(Mathf.Abs(randomDirection.x), Mathf.Abs(randomDirection.y), Mathf.Abs(randomDirection.z));
            
            // Instantiate a new arrow
            ArrowScaler arrowInstance = InstantiateArrow(currentArrowPosition, ArrowGroupParent, randomVector, randomColor);

            // Store the created arrow in the list
            arrowList.Add(arrowInstance);

            // Add the random vector to the current position. That makes sure that we draw the next arrow at the tip of the current arrow (Since current arrow vector = randomVector)
            currentArrowPosition += randomVector;
        }

        // Since we added all those random vectors together in currentArrowPosition, this is basically the sum of all vectors!
        vectorSum = currentArrowPosition;

        ArrowScaler vectorSumArrow = InstantiateArrow(Vector3.zero, ArrowGroupParent, vectorSum, Color.orange * vectorSum.magnitude);

        // Scale the whole structure down by scaling the parent transform that holds all arrows
        ArrowGroupParent.transform.localScale = Vector3.one * ArrowGroupScale;
    }

    private ArrowScaler InstantiateArrow(Vector3 localPosition, Transform parent, Vector3 vector, Color color)
    {
        ArrowScaler arrowInstance = Instantiate(ArrowPrefab, parent);
        arrowInstance.transform.localPosition = localPosition;

        // Set the arrow vector
        arrowInstance.ArrowVector = vector;
        // Set the arrow color
        arrowInstance.ArrowColor = color;

        return arrowInstance;
    }

    private void ResetArrowGroup()
    {
        // Break the game objects and clear the list
        if (ArrowGroupParent != null)
        {
            Destroy(ArrowGroupParent.gameObject);

            // Clear the list
            arrowList.Clear();
        }

        // Create a new empty gameobject to hold all the arrows
        ArrowGroupParent = new GameObject("Arrows").transform;
        ArrowGroupParent.transform.parent = this.transform;
        ArrowGroupParent.transform.localPosition = Vector3.zero;
    }
}
