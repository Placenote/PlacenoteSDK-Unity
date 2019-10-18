using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Singleton helper class that enables running delegates on the main UI thread
/// </summary>
public class MainThreadTaskQueue : MonoBehaviour
{
    public delegate void Delegate();

    private static MainThreadTaskQueue sInstance;
    private List<Delegate> delegates = new List<Delegate>();

    void Awake()
    {
        sInstance = this;
    }

    void Update()
    {
        while (delegates.Count > 0)
        {
            try
            {
                delegates[0].Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }

            lock (delegates)
            {
                delegates.RemoveAt(0);
            }
        }
    }


    /// <summary>
    /// Push a delegate to a task queue to be sequentially executed whenever
    /// <see cref="MainThreadTaskQueue"/> Monobehavior runs on the main thread.
    /// </summary>
    /// <param name="listener">A listener to be removed to the subscriber list.</param>
    public static void InvokeOnMainThread(Delegate d)
    {
        if (sInstance == null)
        {
            Debug.LogError("MainThreadTaskQueue not initialized, please attach it to an active game object enabled.");
            return;
        }

        lock (sInstance.delegates)
        {
            sInstance.delegates.Add(d);
        }
    }
}

