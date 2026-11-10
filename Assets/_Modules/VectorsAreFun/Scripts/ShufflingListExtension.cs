using System.Collections.Generic;

public static class ShufflingListExtension
{
    // The Fisher-Yates Durstenfeld Shuffling Pattern ...!!!
    // O(n) time complexity shuffling extension for generic lists in C#
    // Further reading:
    // https://exceptionnotfound.net/understanding-the-fisher-yates-card-shuffling-algorithm/
    //
    // This implementation is taken from:
    // https://discussions.unity.com/t/clever-way-to-shuffle-a-list-t-in-one-line-of-c-code/535113

    // This function acts as an extension method to the IList<T> interface in C#. That means all list types adhering to IList<T> would now have the shuffle function available
    // Read to this for more info: https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods
    public static void Shuffle<T>(this IList<T> list)
    {
        int count = list.Count;
        int lastIndex = count - 1;

        // Iterate through the list, starting first index
        for(int i = 0; i < lastIndex; ++i)
        {
            // Get random number between i and count
            int randomIndex = UnityEngine.Random.Range(i, count);

            // Swap list[i] and list[randomIndex]
            // Temporarily store list[i] value in tmp for the swap
            T tmp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = tmp;
        }
    }
}