using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public struct Cell{
    public static readonly int Size = 2;
    public byte state;
    public byte rule;
}

public class ThreadData
{
    public int threadID;
    public Dispatcher parent;
    public ThreadData(Dispatcher parent, int id){
        this.parent = parent;
        this.threadID = id;
    }
}

public class Board
{
    public enum DataRequestType{
        State, Rule,
    };

    public List<Cell[]> board;
    public bool isSimulated; 
    private int width;
    private int generations;

    public Board(byte[] initData, int width, int generations){
        this.isSimulated = false;
        this.width = width;
        this.generations = generations;
        this.board = new List<Cell[]>(generations);
        this.board.Add(new Cell[width]);
        for(int i = 0; i < initData.Length; i++){
            var index = i * 8;
            for(int j = 0; j < 8; j++){
                this.board[0][index + j].state = ((initData[i] << j) & 0x80) == 0 ? (byte)0 : (byte)1;
                this.board[0][index + j].rule = this.board[0][index + j].state == 0 ? (byte)0 : (byte)7;
            }
        }
    }

    public void Simulate(byte rule){
        for(int i = 0; i < this.generations - 1; i++){
            Cell[] row = new Cell[this.width];
            this.board.Add(row);
            for(int j = 0; j < this.width; j++){
                var ruleVec = j == 0 ? this.board[i][this.width - 1].state << 2 : this.board[i][j - 1].state << 2;
                ruleVec |= this.board[i][j].state << 1;
                ruleVec |= (j == (this.width - 1)) ? this.board[i][0].state : this.board[i][j + 1].state;
                row[j].state = ((rule >> ruleVec) & 0x01) == 0 ? (byte)0 : (byte)1;
                row[j].rule = (byte)ruleVec; 
            }
        }
        this.isSimulated = true;
    }

    public byte[] ToByteStream(){
        byte[] output = new byte[this.width * this.generations * Cell.Size];
        int index = 0;
        foreach(var row in this.board){
            foreach(var cell in row){
                output[index] = cell.state;
                index++;
                output[index] = cell.rule;
                index++;
            }
        }
        return output;
    }
    public byte[] ToByteStream(int elementSize){
        if(elementSize <= 1){ return new byte[1]; }
        byte[] output = new byte[this.width * this.generations * elementSize];
        int index = 0;
        foreach(var row in this.board){
            foreach(var cell in row){
                output[index] = cell.state;
                index++;
                output[index] = cell.rule;
                index++;
                for(int i = 2; i < elementSize; i++){
                    output[index] = 0;
                    index++;
                }
            }
        }
        return output;
    }
    public byte[] ToByteStream(DataRequestType dataType){
        byte[] output = new byte[this.width * this.generations];
        int index = 0;
        foreach(var row in this.board){
            foreach(var cell in row){
                switch(dataType){
                    case DataRequestType.State:    
                        output[index] = cell.state;
                        break;
                    case DataRequestType.Rule:
                        output[index] = cell.rule;
                        break;
                    default:
                        output[index] = 0;
                        break;
                }
                index++;
            }
        }
        return output;
    }
    public byte[] ToByteStream(int elementSize, DataRequestType dataType){
        if(elementSize <= 0){ return new byte[1]; }
        byte[] output = new byte[this.width * this.generations * elementSize];
        int index = 0;
        foreach(var row in this.board){
            foreach(var cell in row){
                switch(dataType){
                    case DataRequestType.State:    
                        output[index] = cell.state;
                        break;
                    case DataRequestType.Rule:
                        output[index] = cell.rule;
                        break;
                    default:
                        output[index] = 0;
                        break;
                }
                index++;
                for(int i = 1; i < elementSize; i++){
                    output[index] = 0;
                    index++;
                }
            }
        }
        return output;
    }
}

public class SimulationData
{
    public Board[] boards;
    public readonly List<byte[]> initData;
    public readonly int width;
    public readonly int generations;
    public readonly byte rule;

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
    public enum SimulationState{
        Uninitialized, Initialized, Running, Finished,
    };

    private static readonly int NUMTHREADS = 8;
    public SimulationData data;
    public SimulationState status = SimulationState.Uninitialized;
    private List<Thread> threads;

    public Dispatcher(List<byte[]> initData, int width, int generations, byte rule){
        this.data = new SimulationData(initData, width, generations, rule);
        var threadsToDeploy = initData.Count >= NUMTHREADS ? NUMTHREADS : initData.Count;
        this.threads = new List<Thread>(threadsToDeploy);
        for(int i = 0; i < threadsToDeploy; i++){
            this.threads.Add(new Thread(this.RunComputeThread));
        }
        this.status = SimulationState.Initialized;
    }

    public void RunDispatcher(object callingDispatcher){
        System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        timer.Start();
        Dispatcher caller = (Dispatcher)callingDispatcher;
        lock(caller){ caller.status = SimulationState.Running; }
        for(int i = 0; i < this.threads.Count; i++){
            this.threads[i].Start(new ThreadData(caller, i));
        }
        foreach(var thread in caller.threads){
            thread.Join();
        }
        lock(caller){ caller.status = SimulationState.Finished; }
        timer.Stop();
        Debug.Log("Simulation done in " + timer.ElapsedMilliseconds.ToString() + " milliseconds.");
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
