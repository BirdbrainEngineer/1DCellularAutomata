using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class SimulationData
{
    public Board[] boards;
    public readonly List<byte[]> initData;
    public readonly int width;
    public readonly int generations;
    public readonly byte rule;

    public SimulationData(){
        this.initData = new List<byte[]>(){new byte[1]{0x00}};
        this.boards = new Board[1]{new Board(this.initData[0], 1, 1)};
        this.width = 1;
        this.generations = 1;
        this.rule = 0x00;
    }

    public SimulationData(List<byte[]> initData, int width, int generations, byte rule){
        this.width = width;
        this.generations = generations;
        this.rule = rule;
        this.initData = initData;
        this.boards = new Board[initData.Count];
    }
}

public class Dispatcher
{
    public class ThreadData
    {
        public int threadID;
        public Dispatcher parent;
        public ThreadData(Dispatcher parent, int id){
            this.parent = parent;
            this.threadID = id;
        }
    }

    private static readonly int NUMTHREADS = 8;
    public SimulationData data;
    private List<Thread> threads;

    public Dispatcher(List<byte[]> initData, int width, int generations, byte rule){
        this.data = new SimulationData(initData, width, generations, rule);
        var threadsToDeploy = initData.Count >= NUMTHREADS ? NUMTHREADS : initData.Count;
        this.threads = new List<Thread>(threadsToDeploy);
        for(int i = 0; i < threadsToDeploy; i++){
            this.threads.Add(new Thread(this.RunComputeThread));
        }
    }

    public void RunDispatcher(object callingDispatcher){
        System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        timer.Start();
        Dispatcher caller = (Dispatcher)callingDispatcher;
        for(int i = 0; i < this.threads.Count; i++){
            this.threads[i].Start(new ThreadData(caller, i));
        }
        foreach(var thread in caller.threads){
            thread.Join();
        }
        timer.Stop();
        Debug.Log("Simulation for rule " + this.data.rule.ToString() + " done in " + timer.ElapsedMilliseconds.ToString() + " milliseconds.");
    }

    public void RunComputeThread(object threadData){
        System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        timer.Start();
        ThreadData td = (ThreadData)threadData;
        SimulationData data = td.parent.data;
        int index = td.threadID;
        do{
            Board currentBoard;
            byte rule;
            lock(data){
                currentBoard = new Board(data.initData[index], data.width, data.generations);
                rule = data.rule;
            }
            currentBoard.Simulate(rule);
            lock(data.boards){
                data.boards[index] = currentBoard;
            }
            index += Dispatcher.NUMTHREADS;
        }while(index < data.boards.Length);
        timer.Stop();
        Debug.Log("Compute thread " + td.threadID.ToString() + " done computing in " + timer.ElapsedMilliseconds.ToString() + " milliseconds.");
    }
}
