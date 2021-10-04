using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class Controller : MonoBehaviour
{
    private static readonly int SHADERTHREADGROUPS = 8;

    public ComputeShader viewportShader;
    public (int board, int sim) boardSimToView = (0, 0);
    public int flipTimeDim = 1;
    public string viewMode = "Vanilla";
    public int width = 32;
    public int generations = 32;
    public byte[] rules = {30};

    private (int board, int sim) lastBoardSimViewed = (-1, -1);
    private List<byte[]> initData;
    private List<SimulationData> data;
    private Thread simulationThread;
    private RenderTexture canvas;
    private ComputeBuffer dataBuffer;
    private ComputeBuffer colorBuffer;

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
        this.data = new List<SimulationData>();
        this.kernelIDs = new Dictionary<string, int>(){
            {"Vanilla", this.viewportShader.FindKernel("Vanilla")},
            {"Rule", this.viewportShader.FindKernel("Rule")},
            {"Test", this.viewportShader.FindKernel("Test")},
        };
        this.initData = new List<byte[]>();
        this.initData.Add(new byte[4]{0x00, 0x00, 0x80, 0x00});
        this.simulationThread = new Thread(this.RunSimulationThread);
        this.simulationThread.Start();
        this.dataBuffer = new ComputeBuffer(width * generations, sizeof(int));
        this.canvas = new RenderTexture(width, generations, 24);
        this.canvas.enableRandomWrite = true;
        this.canvas.filterMode = FilterMode.Point;
        this.colorBuffer = new ComputeBuffer(8, 16);
        this.colorBuffer.SetData(ruleColors);
        this.simulationThread.Join();
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
            lock(this.data[boardSimToView.sim]){
                this.viewportShader.SetInts("dims", new int[2]{this.data[boardSimToView.sim].width, this.data[boardSimToView.sim].generations});
            }
            this.viewportShader.SetInt("flipVertical", flipTimeDim);
            this.viewportShader.SetBuffer(this.kernelIDs[viewMode], "ruleColors", this.colorBuffer);
            this.viewportShader.SetBuffer(this.kernelIDs[viewMode], "input", this.dataBuffer);
            this.viewportShader.SetTexture(this.kernelIDs[viewMode], "output", this.canvas);
            this.viewportShader.Dispatch(this.kernelIDs[viewMode], width / SHADERTHREADGROUPS, generations / SHADERTHREADGROUPS, 1);
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
}
