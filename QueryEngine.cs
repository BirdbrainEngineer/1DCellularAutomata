using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class QueryResult{
    public struct QueryBoard{
        public int[] board;
        public string descriptor;
        public QueryBoard(int width, int generations, string descriptor){
            this.board = new int[width * generations];
            this.descriptor = descriptor;
        }
    }

    public QueryBoard[] boards;
    public QueryEngine.QueryType queryType;
    public int width;
    public int generations;
    public string rules;
    public QueryResult(QueryEngine.QueryType type, int width, int generations, string ruleString){
        switch(type){
            case QueryEngine.QueryType.State:   this.boards = new QueryBoard[2]; break;
            case QueryEngine.QueryType.Rule:    this.boards = new QueryBoard[8]; break;
            case QueryEngine.QueryType.Both:    this.boards = new QueryBoard[10]; break;
        }
        this.width = width;
        this.generations = generations;
        this.queryType = type;
        this.rules = ruleString;
    }

    public void SaveToFile(){
        //todo
    }

    public static string MakeRuleString(byte[] rules){
        string output = rules[0].ToString();
        int counter = (int)rules[0];
        byte lastRule = rules[0];
        foreach(byte rule in rules){
            //todo
        }
        return output;
    }
}

public class QueryEngine
{
    public enum QueryType{
        State, Rule, Both, User,
    };

    public QueryResult result;
    public QueryType queryType;
    public string userQueryFunction;
    private List<SimulationData> data;
    private int dataWidth;
    private int dataGenerations;

    public QueryEngine(){
        this.queryType = QueryType.State;
        var tempData = new List<SimulationData>(){new SimulationData()};
        this.SetData(tempData);
    }

    public void SetData(List<SimulationData> newData){
        this.data = newData;
        this.dataWidth = this.data[0].width;
        this.dataGenerations = this.data[0].generations;
    }

    public void DispatchQuery(){
        Debug.Log("Query started at " + System.DateTime.Now.ToString());
        switch(this.queryType){
            case QueryType.State:   this.QueryState(); break;
            case QueryType.Rule:    this.QueryRule(); break;
            case QueryType.Both:    this.QueryBoth(); break;
            case QueryType.User:    this.QueryUser(); break;
            default:                this.NoQuery(); break;
        }
    }

    public void QueryState(){
        System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        timer.Start();
        
        timer.Stop();
        Debug.Log("Query completed in " + timer.Elapsed.ToString());
    }
    public void QueryRule(){
        System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        timer.Start();
        
        timer.Stop();
        Debug.Log("Query completed in " + timer.Elapsed.ToString());
    }
    public void QueryBoth(){
        System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        timer.Start();
        
        timer.Stop();
        Debug.Log("Query completed in " + timer.Elapsed.ToString());
    }
    public void QueryUser(){
        System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        timer.Start();
        
        timer.Stop();
        Debug.Log("Query completed in " + timer.Elapsed.ToString());
    }
    public void NoQuery(){
        QueryResult output = new QueryResult(QueryType.State, 1, 1, "Invalid query");
        this.result = 
        this.result.boards
        Debug.Log("Invalid query type! Output is invalid.");
    }
}
