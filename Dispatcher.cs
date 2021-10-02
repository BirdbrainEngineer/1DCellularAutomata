using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class ThreadData{
    public int multiplier;
    public int[] destination;
    public ThreadData(int init, int[] dest){
        this.multiplier = init;
        this.destination = dest;
    }
}

public class Dispatcher : MonoBehaviour
{
    Thread[] threads;
    List<int[]> numbers;

    void Start(){
        this.numbers = new List<int[]>();
        this.threads = new Thread[8];
        for(int i = 0; i < 8; i++){
            this.numbers.Add(new int[1]);
            this.threads[i] = new Thread(mult2);
            this.threads[i].Start(new ThreadData(i, this.numbers[i]));
            Debug.Log("Started thread " + i.ToString());
        }
        for(int j = 0; j < 8; j++){
            this.threads[j].Join();
            Debug.Log(this.numbers[j][0].ToString());
        }
    }

    public void mult2(object data){
        ThreadData tData = (ThreadData)data;
        tData.destination[0] = tData.multiplier * 2;
        Thread.Sleep(2);
        Debug.Log("Thread " + tData.multiplier + " has finished");
    }
}
