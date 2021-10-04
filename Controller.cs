using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class Controller : MonoBehaviour
{
    private static readonly int SHADERTHREADGROUPS = 8;

    public ComputeShader viewportShader;
    public int boardToView = 0;
    public int simulationToView = 0;
    public int flipTimeDim = 1;
    public string viewMode = "Vanilla";
    public int width = 32;
    public int generations = 32;
    public byte[] rules = {30};
    public List<byte[]> initData;

    private (int board, int sim) boardSimToView = (0, 0);
    private (int board, int sim) lastBoardSimViewed = (-1, -1);
    private List<SimulationData> data;
    private Thread simulationThread;
    private RenderTexture canvas;
    private ComputeBuffer dataBuffer;
    private ComputeBuffer colorBuffer;
    private string shaderToUse;
    private int timeDimDirection;

    private Dictionary<string,int> kernelIDs;

    private float[] ruleColors = new float[]{
        0.0f, 0.0f, 0.0f, 1.0f,
        1.0f, 0.0f, 0.0f, 1.0f,
        1.0f, 1.0f, 0.0f, 1.0f,
        0.0f, 1.0f, 0.0f, 1.0f,
        0.0f, 1.0f, 1.0f, 1.0f,
        0.0f, 0.0f, 1.0f, 1.0f,
        1.0f, 0.0f, 1.0f, 1.0f,
        1.0f, 1.0f, 1.0f, 1.0f,
    };

    void Start()
    {
        this.kernelIDs = new Dictionary<string, int>(){
            {"Vanilla", this.viewportShader.FindKernel("Vanilla")},
            {"Rule", this.viewportShader.FindKernel("Rule")},
            {"Test", this.viewportShader.FindKernel("Test")},
        };
        this.initData = new List<byte[]>();
        this.initData.Add(new byte[4]{0x00, 0x80, 0x80, 0x00});
        this.colorBuffer = new ComputeBuffer(8, 4 * sizeof(float));
        this.RunSimulation();
        this.simulationThread.Join();
    }

    void Update(){
        if(Input.GetKeyDown(KeyCode.V)){
            this.ReSetViewportParameters();
        }
        if(Input.GetKeyDown(KeyCode.R)){
            if(this.simulationThread.IsAlive){
                this.simulationThread.Abort();
                this.simulationThread.Join();
            }
            this.RunSimulation();
            this.lastBoardSimViewed = (-1, -1);
        }
        if(Input.GetKeyDown(KeyCode.U)){
            //user code stuff todo
            this.initData = GenerateCount(width, 16);
            foreach(var t in this.initData[3]){Debug.Log(t.ToString());}
        }
    }

    void ReSetViewportParameters(){
        this.shaderToUse = viewMode;
        this.timeDimDirection = flipTimeDim;
        this.boardSimToView.board = boardToView;
        this.boardSimToView.sim = simulationToView;
        this.colorBuffer.SetData(ruleColors);
    }

    void RunSimulation(){
        boardToView = 0;
        simulationToView = 0;
        this.data = new List<SimulationData>();
        this.simulationThread = new Thread(this.RunSimulationThread);
        this.simulationThread.Start();
        if(this.dataBuffer != null){ this.dataBuffer.Release(); }
        if(this.canvas != null){ this.canvas.Release(); }
        this.dataBuffer = new ComputeBuffer(width * generations, sizeof(int));
        this.canvas = new RenderTexture(width, generations, 24);
        this.canvas.enableRandomWrite = true;
        this.canvas.filterMode = FilterMode.Point;
        this.ReSetViewportParameters();
    }

    public void RunSimulationThread(){
        System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        timer.Start();
        Dispatcher dispatcher;
        Thread dispatcherThread;
        for(int i = 0; i < this.rules.Length; i++){
            dispatcher = new Dispatcher(initData, width, generations, rules[i]);
            this.data.Add(dispatcher.data);
            dispatcherThread = new Thread(dispatcher.RunDispatcher);
            dispatcherThread.Start(dispatcher);
            dispatcherThread.Join();
        }
        timer.Stop();
        Debug.Log("All rules simulated in " + timer.Elapsed.ToString() + " seconds.");
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest){
        bool dispatchShader = true;
        if(this.lastBoardSimViewed != boardSimToView){
            try{
                lock(this.data[boardSimToView.sim].boards[boardSimToView.board]){
                    this.dataBuffer.SetData(this.data[boardSimToView.sim].boards[boardSimToView.board].ToByteStream(sizeof(int)));
                }
                lastBoardSimViewed = boardSimToView;
            }
            catch(System.IndexOutOfRangeException){
                dispatchShader = false;
                boardSimToView = this.lastBoardSimViewed;
                Debug.Log("Requested board does not exist! Maybe not simulated yet or wrong index?");
            }
        }
        if(dispatchShader){
            (int w, int h) dims;
            lock(this.data[boardSimToView.sim]){
                dims = (this.data[boardSimToView.sim].width, this.data[boardSimToView.sim].generations);
            }
            this.viewportShader.SetInt("flipVertical", this.timeDimDirection);
            this.viewportShader.SetInts("dims", new int[2]{dims.w, dims.h});
            this.viewportShader.SetBuffer(this.kernelIDs[this.shaderToUse], "ruleColors", this.colorBuffer);
            this.viewportShader.SetBuffer(this.kernelIDs[this.shaderToUse], "input", this.dataBuffer);
            this.viewportShader.SetTexture(this.kernelIDs[this.shaderToUse], "output", this.canvas);
            this.viewportShader.Dispatch(this.kernelIDs[this.shaderToUse], dims.w / SHADERTHREADGROUPS, dims.h / SHADERTHREADGROUPS, 1);
        }
        Graphics.Blit(this.canvas, dest);
    }

    void OnApplicationquit(){
        this.colorBuffer.Release();
        this.dataBuffer.Release();
    }

    private List<byte[]> GenerateRandom(int width, int boards){
        List<byte[]> output = new List<byte[]>(boards);
        var bytesPerBoard = (int)Mathf.Ceil(width / 8);
        for(int i = 0; i < boards; i++){
            output.Add(new byte[bytesPerBoard]);
            for(int j = 0; j < bytesPerBoard; j++){
                output[i][j] = (byte)Random.Range(0, 256);
            }
        }
        return output;
    }

    private List<byte[]> GenerateCount(int width, byte bits){
        var shiftAmount = bits > 30 ? 30 : bits;
        int configurations = 0x1 << shiftAmount;
        List<byte[]> output = new List<byte[]>(configurations);
        var bytesPerBoard = (int)Mathf.Ceil(width / 8);
        for(int i = 0; i < configurations; i++){
            var configuration = new byte[bytesPerBoard];
            for(int j = 0; j < bytesPerBoard; j++){
                if(j < 4){
                    configuration[j] = (byte)(i >> (j * 8));
                }
                else {
                    configuration[j] = 0x00;
                }
            }
            output.Add(configuration);
        }
        return output;
    }

    private byte[] SetRuleRange(byte from, byte to){
        int start = from < to ? from : to;
        int finish = to > from ? to : from;
        byte[] result = new byte[(finish - start) + 1];
        for(int i = start; i <= finish; i++){
            result[i] = (byte)i;
        }
        return result;
    }
}
