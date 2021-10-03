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
    public int viewMode = 0;

    private int kernelID;
    private List<byte[]> initData;
    private Dispatcher dispatcher;
    private SimulationData data;
    private System.Threading.Thread dispatcherThread;
    private int lastboardViewed = -1;
    private Texture2D dataBuffer;
    private RenderTexture canvas;

    void Start()
    {
        this.initData = GenerateRandom(width, 65536);
        print(this.initData.Count);
        print(this.initData[0].Length);
        this.dispatcher = new Dispatcher(initData, width, generations, rule);
        this.data = this.dispatcher.data;
        this.dispatcherThread = new System.Threading.Thread(this.dispatcher.RunDispatcher);
        this.dispatcherThread.Start(this.dispatcher);
        this.dataBuffer = new Texture2D(width, generations, TextureFormat.RGBA32, false);
        this.dataBuffer.filterMode = FilterMode.Point;
        this.canvas = new RenderTexture(width, generations, 24);
        this.canvas.enableRandomWrite = true;
        this.canvas.filterMode = FilterMode.Point;
        this.kernelID = this.viewportShader.FindKernel("Viewport");
        this.viewportShader.SetFloats("ruleColors", ruleColors);
        this.viewportShader.SetTexture(kernelID, "input", this.dataBuffer);
        this.viewportShader.SetTexture(kernelID, "output", this.canvas);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest){
        if(lastboardViewed != boardToView){
            if(this.data.boards[boardToView].IsSimulated()){
                this.dataBuffer.SetPixelData<byte>(this.data.boards[boardToView].ToByteStream(sizeof(int)), 0, 0);
                this.dataBuffer.Apply();
                this.viewportShader.SetInt("viewMode", viewMode);
                this.viewportShader.Dispatch(kernelID, width / SHADERTHREADGROUPS, generations / SHADERTHREADGROUPS, 1);
                lastboardViewed = boardToView;
            }
        }
        Graphics.Blit(this.canvas, dest);
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

    public static readonly float[] ruleColors = new float[8 * 4]{
        0, 0, 0, 1,
        1, 0, 0, 1,
        1, 1, 0, 1,
        0, 1, 0, 1,
        0, 1, 1, 1,
        0, 0, 1, 1,
        1, 0, 1, 1,
        1, 1, 1, 1,
    };
}
