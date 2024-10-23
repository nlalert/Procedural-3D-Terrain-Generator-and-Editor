using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class ThreadedDataRequester : MonoBehaviour
{
    // Singleton instance of ThreadedDataRequester
    static ThreadedDataRequester instance;
    
    // Queue to store thread results and associated callbacks
    Queue<ThreadInfo> dataQueue = new Queue<ThreadInfo>();

    // Awake is called when the script instance is being loaded
    void Awake() {
        // Assign the singleton instance by finding an existing ThreadedDataRequester in the scene
        instance = FindObjectOfType<ThreadedDataRequester>();
    }

    // Static method to request data in a separate thread
    public static void RequestData(Func<object> generateData, Action<object> callback) {
        // Create a thread that will execute the DataThread method
        ThreadStart threadStart = delegate {
            // Calls DataThread on the new thread to process the data generation and enqueue the result
            instance.DataThread(generateData, callback);
        };
        // Start the new thread
        new Thread(threadStart).Start();
    }

    // Method that runs on a separate thread for data generation
    void DataThread(Func<object> generateData, Action<object> callback) {
        // Call the generateData function to generate data
        object data = generateData();
        // Lock the dataQueue to prevent race conditions when enqueuing
        lock (dataQueue) {
            // Enqueue the generated data and its callback in a ThreadInfo struct
            dataQueue.Enqueue(new ThreadInfo(callback, data));
        }
    }

    // Update is called once per frame to process the queue
    void Update() {
        // If there are items in the queue, process them
        if (dataQueue.Count > 0) {
            // Loop through the queued items and invoke their callbacks
            for (int i = 0; i < dataQueue.Count; i++) {
                ThreadInfo threadInfo = dataQueue.Dequeue(); // Dequeue the next item
                // Invoke the callback with the generated data
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    // Struct to store callback and the associated generated data (thread result)
    readonly struct ThreadInfo {
        public readonly Action<object> callback; // The callback function to be executed
        public readonly object parameter; // The generated data

        // Constructor to initialize ThreadInfo with the callback and data
        public ThreadInfo(Action<object> callback, object parameter) {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}
