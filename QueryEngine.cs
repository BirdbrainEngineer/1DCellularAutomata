using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QueryEngine{
    public enum QueryType{
        State, Rule, User,
    };

    public enum QueryStatus{
        Finished, Running, Waiting, Ended,
    }

    public struct QueryBoard{
        public int[] board;
        public string descriptor;
        public (int max, int min) valueRange;
        public (int width, int generations) dims;
        public int boardsHandled;
        public QueryBoard(int[] b, string desc, (int, int) valRange, (int, int) dims, int boardsHandled){
            this.board = b;
            this.descriptor = desc;
            this.valueRange = valRange;
            this.dims = dims;
            this.boardsHandled = boardsHandled;
        }
        public static void SaveAsText(QueryBoard board, string folderpath, string filename){
            string[] output = new string[board.dims.generations + 1];
            string fpathname;
            if(folderpath == ""){
                fpathname = System.IO.Directory.GetCurrentDirectory() + @"\" + filename + ".txt";
            }
            else {
                fpathname = folderpath + filename + ".txt";
            }
            output[0] = board.descriptor + 
                        " with dimensions " + 
                        board.dims.width.ToString() + 
                        " x " + 
                        board.dims.generations.ToString() + 
                        " ,over " +
                        board.boardsHandled.ToString() + 
                        "boards.";
            for(int i = 0; i < board.dims.generations; i++){
                string row = "";
                var offset = i * board.dims.width;
                for(int j = 0; j < board.dims.width; j++){
                    row += (board.board[offset + j].ToString().PadLeft(10) + ", ");
                }
                output[i + 1] = row;
            }
            System.IO.File.WriteAllLines(fpathname, output);
            Debug.Log("done saving file.");
        }
    };

    public QueryType queryType;
    public List<QueryBoard> result;
    private int width;
    private int generations;
    private List<SimulationData> data;
    public QueryStatus status;

    public QueryEngine(List<SimulationData> dataIn, QueryType type){
        this.queryType = type;
        this.status = QueryStatus.Waiting;
        this.data = dataIn;
        this.width = this.data[0].width;
        this.generations = this.data[0].generations;
        this.result = new List<QueryBoard>();
    }

    public void RunQuery(){
        System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        timer.Start();
        lock(this){
            this.status = QueryStatus.Running;
        }
        switch(this.queryType){
            case QueryType.State:   this.QueryState(); break;
            case QueryType.Rule:    this.QueryRule(); break;
            case QueryType.User:    this.QueryUser(); break;
            default:                this.QueryFail(); break;
        }
        lock(this){
            this.status = QueryStatus.Finished;
        }
        timer.Stop();
        Debug.Log("Query completed in " + timer.Elapsed.ToString());
    }

    private void QueryState(){
        int boardSize = this.width * this.generations;
        int[] aliveCounts = new int[boardSize];
        int[] deadCounts = new int[boardSize];
        for(int i = 0; i < boardSize; i++){
            aliveCounts[i] = 0;
            deadCounts[i] = 0;
        }
        foreach(var simulation in data){
            foreach(var board in simulation.boards){
                for(int i = 0; i < this.generations; i++){
                    var offset = i * this.width;
                    for(int j = 0; j < this.width; j++){
                        if(board.board[i][j].state == 0){ deadCounts[offset + j]++; }
                        else { aliveCounts[offset + j]++; }
                    }
                }
            }
        }
        this.result.Add(new QueryBoard( aliveCounts, 
                                        "Alive Count", 
                                        FindMaxMin(aliveCounts), 
                                        (this.width, this.generations), 
                                        data.Count * data[0].boards.Length));
        this.result.Add(new QueryBoard( deadCounts, 
                                        "Dead Count", 
                                        FindMaxMin(deadCounts), 
                                        (this.width, this.generations), 
                                        data.Count * data[0].boards.Length));
    }

    private void QueryRule(){
        int boardSize = this.width * this.generations;
        List<int[]> ruleCounts = new List<int[]>(8);
        for(int i = 0; i < 8; i++){
            ruleCounts.Add(new int[boardSize]);
            for(int j = 0; j < boardSize; j++){
                ruleCounts[i][j] = 0;
            }
        }
        foreach(var simulation in data){
            foreach(var board in simulation.boards){
                for(int i = 0; i < this.generations; i++){
                    var offset = i * this.width;
                    for(int j = 0; j < this.width; j++){
                        ruleCounts[board.board[i][j].rule][offset + j]++;
                    }
                }
            }
        }
        for(int i = 0; i < 8; i++){
            this.result.Add(new QueryBoard( ruleCounts[i], 
                                            ("Count for " + IntToBinString(i, 3)), 
                                            FindMaxMin(ruleCounts[i]), 
                                            (this.width, this.generations), 
                                            data.Count * data[0].boards.Length));
        }
    }

    private void QueryUser(){
        //todo
        this.QueryFail();
    }

    private void QueryFail(){
        int boardSize = this.width * this.generations;
        int[] emptyBoard = new int[boardSize];
        for(int i = 0; i < boardSize; i++){ emptyBoard[i] = 0; }
        this.result.Add(new QueryBoard( emptyBoard, 
                                        "INVALID", 
                                        (0, 0), 
                                        (this.width, this.generations), 
                                        data.Count * data[0].boards.Length));
    }

    public QueryStatus GetStatus(bool markEndedWhenFinished){
        var current = this.status;
        if(markEndedWhenFinished && (this.status == QueryStatus.Finished)) { this.status = QueryStatus.Ended; }
        return current;
    }

    private (int, int) FindMaxMin(int[] arr){
        var max = arr[0];
        var min = arr[0];
        for(int i = 0; i < arr.Length; i++){
            max = arr[i] > max ? arr[i] : max;
            min = arr[i] < min ? arr[i] : min;
        }
        return (max, min);
    }

    private string IntToBinString(int number, byte bits){
        string output = "";
        var b = bits > 32 ? 32 : bits;
        for(int i = 0; i < b; i++){
            bool bit = ((number >> i) & 0x1) > 0;
            if(bit){ output = "1" + output; }
            else {output = "0" + output; }
        }
        return output;
    }
}

