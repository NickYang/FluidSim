using UnityEngine;
using System.Collections.Generic;


public class GridFluidSimulation : MonoBehaviour
{
    RenderTexture velocityRT0;//网格点速度的RT, 它需要有两个通道分别代表x, y方向的速度值
    RenderTexture velocityRT1;//网格点速度的RT, 它需要有两个通道分别代表x, y方向的速度值

    RenderTexture dyeRT0;//用于显示染料（网格）颜色的RT
    RenderTexture dyeRT1;//用于显示染料（网格）颜色的RT

    
    RenderTexture pressureRT0;//网格点压强的RT
    RenderTexture pressureRT1;//网格点压强的RT

    
    RenderTexture divergenRT;//散度RT
    RenderTexture curlRT;//旋度RT

    public int SIM_RES = 32;//由于模拟的RT分辨率
    public int DISPLAY_RES = 1024;//用于显示的RT分辨率

    public float PRESSURE = 0.8f;
    public int VISCOUS_ITERATIONS = 5;
    public int PRESSURE_ITERATIONS = 20;
    public float VISCOUS = 0f;
    public bool bReplay = false;
    public float CURL = 30;
    public float FLOW_RADIUS = 0.001f;
    public float VELOCITY_DISSIPATOIN = 1f;
    public float COLOR_DISSIPATOIN = 0.2f;

    public Material circlepaintMaterial;
    public Material advectionMaterial;
    public Material divergenceMaterial;
    public Material pressureMaterial;
    public Material gradientMaterial;
    public Material origPressureMaterial;
    public Material diffusionMaterial;
    public Material curlMaterial;
    public Material vorticityMaterial;
    public Material displayMat;
    public static List<FlowPointer> pointers = new List<FlowPointer>();
    public float colorUpdateInterval =0.25f        ;

    float colorUpdateTimer = 0.0f;
    float baseVeclocity = 1000f;

    static public void AddPointer(int id, float texcoordX, float texcoordY)
    {
        Color color = RandomColor();
        FlowPointer point = new FlowPointer(id, texcoordX, texcoordY, color);
        pointers.Add(point);
    }

    static public void UpdatePointer(int id, float texcoordX, float texcoordY)
    {
        FlowPointer p = pointers[0];
        p.UpdatePointer(id, texcoordX, texcoordY);
    }

    static Color HSVtoRGB(float h, float s, float v)
    {
        float r = 0;
        float g = 0;
        float b = 0;
        float i, f, p, q, t;
        i = Mathf.Floor(h * 6);
        f = h * 6 - i;
        p = v * (1 - s);
        q = v * (1 - f * s);
        t = v * (1 - (1 - f) * s);

        switch (i % 6)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            case 5: r = v; g = p; b = q; break;
        }

        return new Color(r, g, b);
    }

    static Color RandomColor()
    {
        Color c = HSVtoRGB(Random.Range(0.0f, 1.0f), 1.0f, 1.0f);
        c.r *= 0.15f;
        c.g *= 0.15f;
        c.b *= 0.15f;
        return c;

    }

    static float correctDeltaX(float delta)
    {
        float aspectRatio = Screen.width / Screen.height;
        if (aspectRatio < 1) delta *= aspectRatio;
        return delta;
    }

    static Vector2 CorrectResolution(float resolution)
    {
        float aspectRatio = (float)Screen.width / Screen.height;
        if (aspectRatio < 1)
            aspectRatio = 1.0f / aspectRatio;

        float min = Mathf.Round(resolution);
        float max = Mathf.Round(resolution * aspectRatio);

        if (Screen.width > Screen.height)
            return new Vector2(max, min);
        else
            return new Vector2(min, max);
    }

    void InitRenderBuffers()
    {

        // 算出实际分辨率

        Vector2 simCorrectResolution = CorrectResolution(SIM_RES);
        Vector2 disCorrectResolution = CorrectResolution(DISPLAY_RES);

        
        velocityRT0 = RenderTexture.GetTemporary((int)simCorrectResolution.x, (int)simCorrectResolution.y, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        velocityRT0.name = "velocityRT0";
        velocityRT0.wrapMode = TextureWrapMode.Clamp;
        velocityRT0.filterMode = FilterMode.Bilinear;
        velocityRT1 = RenderTexture.GetTemporary((int)simCorrectResolution.x, (int)simCorrectResolution.y, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        velocityRT1.filterMode = FilterMode.Bilinear;
        velocityRT1.name = "velocityRT1";
        velocityRT1.wrapMode = TextureWrapMode.Clamp;

        dyeRT0 = RenderTexture.GetTemporary((int)disCorrectResolution.x, (int)disCorrectResolution.y, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        dyeRT0.name = "dyeRT0";
        dyeRT0.filterMode = FilterMode.Trilinear;
        dyeRT0.wrapMode = TextureWrapMode.Clamp;
        dyeRT0.antiAliasing = 4;

        dyeRT1 = RenderTexture.GetTemporary((int)disCorrectResolution.x, (int)disCorrectResolution.y, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        dyeRT1.name = "dyeRT1";
        dyeRT1.filterMode = FilterMode.Trilinear;
        dyeRT1.wrapMode = TextureWrapMode.Clamp;
        dyeRT1.antiAliasing = 4;
        pressureRT0 = RenderTexture.GetTemporary((int)simCorrectResolution.x, (int)simCorrectResolution.y, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        pressureRT0.name = "pressureRT0";
        pressureRT0.filterMode = FilterMode.Point;
        pressureRT0.wrapMode = TextureWrapMode.Clamp;

        pressureRT1 = RenderTexture.GetTemporary((int)simCorrectResolution.x, (int)simCorrectResolution.y, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        pressureRT1.name = "pressureRT1";
        pressureRT1.filterMode = FilterMode.Point;
        pressureRT1.wrapMode = TextureWrapMode.Clamp;

        divergenRT = RenderTexture.GetTemporary((int)simCorrectResolution.x, (int)simCorrectResolution.y, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        divergenRT.name = "divergenRT";
        divergenRT.filterMode = FilterMode.Point;
        divergenRT.wrapMode = TextureWrapMode.Clamp;

        curlRT = RenderTexture.GetTemporary((int)simCorrectResolution.x, (int)simCorrectResolution.y, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        curlRT.name = "curlRT";
        curlRT.filterMode = FilterMode.Point;
        curlRT.wrapMode = TextureWrapMode.Clamp;

    }

    void SwapRenderTexture(ref RenderTexture rt1, ref RenderTexture rt2)
    {
        RenderTexture temp = rt1;
        rt1 = rt2;
        rt2 = temp;
    }

    static float CorrectRadius(float radius)
    {
        float aspectRatio = Screen.width / Screen.height;
        if (aspectRatio > 1)
            radius *= aspectRatio;
        return radius;
    }

    static float correctDeltaY(float delta)
    {
        float aspectRatio = Screen.width / Screen.height;
        if (aspectRatio > 1) delta /= aspectRatio;
        return delta;
    }

    void CreateDye(float x, float y, float dx, float dy, Color color, float radius)
    {
        circlepaintMaterial.SetTexture("_MainTex", velocityRT0);
        circlepaintMaterial.SetVector("pointAndRadius", new Vector4(x, y, radius/100.0f, 0));
        circlepaintMaterial.SetVector("color", new Vector4(dx, dy, 0, 1.0f));
        Graphics.Blit(velocityRT0, velocityRT1, circlepaintMaterial);
        SwapRenderTexture(ref velocityRT0, ref velocityRT1);

        circlepaintMaterial.SetVector("color", new Vector4(color.r, color.g, color.b, 1.0f));
        circlepaintMaterial.SetTexture("_MainTex", dyeRT0);
        Graphics.Blit(dyeRT0, dyeRT1, circlepaintMaterial);
        SwapRenderTexture(ref dyeRT0, ref dyeRT1);
        Graphics.SetRenderTarget(null);

    }

    void TouchDye(FlowPointer pointer)
    {

        float deltaX = correctDeltaX(pointer.texcoordX - pointer.prevTexcoordX);
        float deltaY = correctDeltaY(pointer.texcoordY - pointer.prevTexcoordY);

        float dx = deltaX * baseVeclocity;
        float dy = deltaY * baseVeclocity;

        ///float radius = 0.0025f;        //Random.Range(0.0f, 1.0f) / 100.0f;
        CreateDye(pointer.texcoordX, pointer.texcoordY, dx, dy, pointer.color, CorrectRadius(FLOW_RADIUS));
    }


    void CreateDyes(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            Color color = RandomColor()*10.0f;

            // Position
            float x = Random.Range(0.0f, 1.0f);
            float y = Random.Range(0.0f, 1.0f);
            // Velocity
            float dx = baseVeclocity * (Random.Range(0.0f, 1.0f) - 0.5f);
            float dy = baseVeclocity * (Random.Range(0.0f, 1.0f) - 0.5f);
            CreateDye(x, y, dx, dy, color, CorrectRadius(FLOW_RADIUS));
        }
    }

    void Start()
    {
        InitRenderBuffers();
        CreateDyes((int)Random.Range(10.0f, 20.0f));
    }

    void ReleaseAllRT()
    {
        dyeRT0.Release();
        dyeRT1.Release();
        velocityRT0.Release();
        velocityRT1.Release();
        divergenRT.Release();
        pressureRT0.Release();
        pressureRT1.Release();
        curlRT.Release();
    }

    void Restart()
    {
        ReleaseAllRT();
        Start();
    }

    void DrawShapeTargets(RenderTexture shape)
    {
        float b = 30;
        float h = Screen.height / 8;
        float w = h + b;
        float x = Screen.width - 120;
        float y = (h - 15);
        //GUI.Label(new Rect(x, y - 20, 100, h), shape.name);
        GUI.DrawTexture(new Rect(x + b, y, h - b, h - b), shape, ScaleMode.ScaleAndCrop, false);
    }

    void OnGUI()
    {
        DrawShapeTargets(velocityRT0);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Graphics.Blit(dyeRT0, dest, displayMat,0);
    }

    void SimulateStep()
    {
         // diffusion
        if (VISCOUS > 0.0)
        {
            diffusionMaterial.SetFloat("_Viscosity", VISCOUS);
            for (int i = 0; i < VISCOUS_ITERATIONS; i++)
            {
                Graphics.Blit(velocityRT0, velocityRT1, diffusionMaterial);
                SwapRenderTexture(ref velocityRT0, ref velocityRT1);
            }
        }
        // 计算旋度
        if (CURL > 0)
        {
            curlMaterial.SetTexture("_MainTex", velocityRT0);
            Graphics.Blit(velocityRT0, curlRT, curlMaterial);

            vorticityMaterial.SetTexture("_SecondTex", velocityRT0);
            vorticityMaterial.SetFloat("curl", CURL);
            Graphics.Blit(curlRT, velocityRT1, vorticityMaterial);
            SwapRenderTexture(ref velocityRT0, ref velocityRT1);
        }
        //  divergence
        divergenceMaterial.SetTexture("_MainTex", velocityRT0);
        Graphics.Blit(velocityRT0, divergenRT, divergenceMaterial);

        // pressure
        origPressureMaterial.SetTexture("_MainTex", pressureRT0);
        origPressureMaterial.SetFloat("value", PRESSURE);
        Graphics.Blit(pressureRT0, pressureRT1, origPressureMaterial);
        SwapRenderTexture(ref pressureRT0, ref pressureRT1);

        pressureMaterial.SetTexture("_SecondTex", divergenRT);
        for (int i = 0; i < PRESSURE_ITERATIONS; i++)
        {
            Graphics.Blit(pressureRT0, pressureRT1, pressureMaterial);
            SwapRenderTexture(ref pressureRT0, ref pressureRT1);
        }
        gradientMaterial.SetTexture("_MainTex", pressureRT0);
        gradientMaterial.SetTexture("_SecondTex", velocityRT0);
        Graphics.Blit(pressureRT0, velocityRT1, gradientMaterial);
        SwapRenderTexture(ref velocityRT0, ref velocityRT1);

        // velocity advection
        advectionMaterial.SetTexture("_VelocityTex", velocityRT0);
        advectionMaterial.SetTexture("_PhysicTex", velocityRT0);
        advectionMaterial.SetFloat("_Dissipation", VELOCITY_DISSIPATOIN);
        Graphics.Blit(velocityRT0, velocityRT1, advectionMaterial);
        SwapRenderTexture(ref velocityRT0, ref velocityRT1);

        //颜色平流（对流）
        advectionMaterial.SetTexture("_VelocityTex", velocityRT0);
        advectionMaterial.SetTexture("_PhysicTex", dyeRT0);
        advectionMaterial.SetFloat("_Dissipation", COLOR_DISSIPATOIN);
        Graphics.Blit(velocityRT0, dyeRT1, advectionMaterial);
        SwapRenderTexture(ref dyeRT0, ref dyeRT1);
    }

    void SimulateTouch()
    {
        if (pointers.Count <= 0) return;
        FlowPointer p = pointers[0];
        if (p.moved)
        {
            p.moved = false;
            TouchDye(p);
        }
    }

    void UpdateColor()
    {
        colorUpdateTimer += Time.deltaTime;
        if (colorUpdateTimer >= colorUpdateInterval)
        {
            if (pointers.Count <= 0) return;

            FlowPointer p = pointers[0];
            p.color = RandomColor();
            colorUpdateTimer = colorUpdateTimer - colorUpdateInterval;
        }
    }

    void Update()
    {
        if (bReplay)
        {
            Restart();
            bReplay = !bReplay;
        }

        SimulateStep();
        SimulateTouch();
        UpdateColor();
    }
}

