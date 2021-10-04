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
    public int boardToView = 0;
    public int flipTimeDim = 1;
    public string viewMode = "Vanilla";

    private int lastboardViewed = -1;
    private List<byte[]> initData;
    private Dispatcher dispatcher;
    private SimulationData data;
    private System.Threading.Thread dispatcherThread;
    private Texture2D dataBuffer;
    private RenderTexture canvas;
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
        this.kernelIDs = new Dictionary<string, int>(){
            {"Vanilla", this.viewportShader.FindKernel("Vanilla")},
            {"Rule", this.viewportShader.FindKernel("Rule")},
        };
        //this.initData = GenerateRandom(width, 16);
        this.initData = new List<byte[]>();
        this.initData.Add(new byte[2]{0x00, 0x80});
        this.dispatcher = new Dispatcher(initData, width, generations, rule);
        this.data = this.dispatcher.data;
        this.dispatcherThread = new System.Threading.Thread(this.dispatcher.RunDispatcher);
        this.dispatcherThread.Start(this.dispatcher);
        this.dataBuffer = new Texture2D(width, generations, TextureFormat.RGBA32, false);
        this.dataBuffer.filterMode = FilterMode.Point;
        this.canvas = new RenderTexture(width, generations, 24);
        this.canvas.enableRandomWrite = true;
        this.canvas.filterMode = FilterMode.Point;
        this.colorBuffer = new ComputeBuffer(8, 16);
        this.colorBuffer.SetData(ruleColors);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest){
        bool dispatchShader = true;
        if(lastboardViewed != boardToView){
            try{
                if(this.data.boards[boardToView].isSimulated){
                    //var forChecking = this.data.boards[boardToView].ToByteStream(sizeof(int));
                    //foreach(var t in forChecking){Debug.Log(t.ToString());}
                    this.dataBuffer.SetPixelData(this.data.boards[boardToView].ToByteStream(sizeof(int)), 0, 0);
                    this.dataBuffer.Apply();
                    lastboardViewed = boardToView;
                }
                else {
                    dispatchShader = false;
                    Debug.Log("Requested board is not yet simulated!");
                }
            }
            catch (System.IndexOutOfRangeException a){
                dispatchShader = false;
                boardToView = 0;
                Debug.Log("Requested board does not exist!");
            }
            
        }
        if(dispatchShader){
            this.viewportShader.SetInts("dims", new int[2]{width, generations});
            this.viewportShader.SetInt("flipVertical", flipTimeDim);
            this.viewportShader.SetBuffer(this.kernelIDs[viewMode], "ruleColors", colorBuffer);
            this.viewportShader.SetTexture(this.kernelIDs[viewMode], "input", this.dataBuffer);
            this.viewportShader.SetTexture(this.kernelIDs[viewMode], "output", this.canvas);
            this.viewportShader.Dispatch(this.kernelIDs[viewMode], width / SHADERTHREADGROUPS, generations / SHADERTHREADGROUPS, 1);
        }
        Graphics.Blit(this.canvas, dest);
    }

    void OnApplicationquit(){
        this.colorBuffer.Release();
    }

    private List<byte[]> GenerateRandom(int width, int boards){
        List<byte[]> output = new List<byte[]>();
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
