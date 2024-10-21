using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
public class ThreadedDataRequester : MonoBehaviour
{

    static ThreadedDataRequester instance;
    Queue<ThreadInfo> dataQueue = new Queue<ThreadInfo>();

    void Awake(){
        instance = FindObjectOfType<ThreadedDataRequester>();
    }

    //Thread
    public static void RequestData(Func<object> generateData, Action<object> callback) {// receive void callback(HeightMap)
        ThreadStart threadStart = delegate {//method 
            instance.DataThread(generateData, callback);
        };
        //new thread exectutes the HeightMapThread method. 
        new Thread(threadStart).Start();
    }

    //this method run from different thread (create from RequestHeightMap() ->HeightMapThread(callback);)
    void DataThread(Func<object> generateData, Action<object> callback) {
        object data = generateData();
        //lock queue
        lock (dataQueue) {
            dataQueue.Enqueue(new ThreadInfo(callback, data));
        }
    }

    //Update Thread
    void Update() {
        if(dataQueue.Count > 0) {
            for (int i = 0; i < dataQueue.Count; i++){
                ThreadInfo threadInfo = dataQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    //struct for handle both heightMap and meshData
    readonly struct ThreadInfo {
        public readonly Action<object> callback;
        public readonly object parameter;

        public ThreadInfo(Action<object> callback, object parameter) {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}
