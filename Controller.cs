using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    private static readonly int SHADERTHREADGROUPS = 8;

    public ComputeShader viewportShader;
    public int width = 32;
    public int generations = 32;
    public byte rule = 30;
    public (int board, int sim) boardSimToView = (0, 0);
    public int flipTimeDim = 1;
    public string viewMode = "Vanilla";

    private (int board, int sim) lastBoardSimViewed = (-1, -1);
    private List<byte[]> initData;
    private Dispatcher dispatcher;
    private List<SimulationData> data;
    private System.Threading.Thread dispatcherThread;
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
        //this.initData = GenerateRandom(width, 16);
        this.initData = new List<byte[]>();
        this.initData.Add(new byte[2]{0x00, 0x80});
        this.dispatcher = new Dispatcher(initData, width, generations, rule);
        this.data.Add(this.dispatcher.data);
        this.dispatcherThread = new System.Threading.Thread(this.dispatcher.RunDispatcher);
        this.dispatcherThread.Start(this.dispatcher);
        this.dataBuffer = new ComputeBuffer(width * generations, sizeof(int));
        this.canvas = new RenderTexture(width, generations, 24);
        this.canvas.enableRandomWrite = true;
        this.canvas.filterMode = FilterMode.Point;
        this.colorBuffer = new ComputeBuffer(8, 16);
        this.colorBuffer.SetData(ruleColors);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest){
        bool dispatchShader = true;
        if(this.lastBoardSimViewed != boardSimToView){
            try{
                if(this.data[boardSimToView.sim].boards[boardSimToView.board].isSimulated){
                    this.dataBuffer.SetData(this.data[boardSimToView.sim].boards[boardSimToView.board].ToByteStream(sizeof(int)));
                    lastBoardSimViewed = boardSimToView;
                }
                else {
                    dispatchShader = false;
                    boardSimToView = this.lastBoardSimViewed;
                    Debug.Log("Requested board is not yet simulated!");
                }
            }
            catch(System.IndexOutOfRangeException){
                dispatchShader = false;
                boardSimToView = this.lastBoardSimViewed;
                Debug.Log("Requested board does not exist!");
            }
        }
        if(dispatchShader){
            this.viewportShader.SetInts("dims", new int[2]{this.data[boardSimToView.sim].width, this.data[boardSimToView.sim].generations});
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
