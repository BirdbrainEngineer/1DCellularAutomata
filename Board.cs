using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Cell{
    public byte state;
    public byte rule;
}

public class Board
{
    public List<Cell[]> board;
    private int width;
    private int generations;

    public Board(byte[] initData, int width, int generations){
        this.width = width;
        this.generations = generations;
        this.board = new List<Cell[]>(generations);
        this.board.Add(new Cell[width]);
        for(int i = 0; i < (int)(Mathf.Ceil(width / 8)); i++){
            var index = i * 8;
            for(int j = 0; j < 8; j++){
                if(index + j == width){ break; }
                this.board[0][index + j].state = ((initData[i] >> j) & 0x01) == 0 ? (byte)0 : (byte)1;
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
}
