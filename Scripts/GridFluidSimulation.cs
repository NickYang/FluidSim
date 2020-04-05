using UnityEngine;

public class GridFluidSimulation : MonoBehaviour
{
    RenderTexture velocityRT0;//网格点速度的RT, 它需要有两个通道分别代表x, y方向的速度值
    RenderTexture velocityRT1;//网格点速度的RT, 它需要有两个通道分别代表x, y方向的速度值

    RenderTexture dyeRT0;//用于显示染料（网格）颜色的RT
    RenderTexture dyeRT1;//用于显示染料（网格）颜色的RT

    
    RenderTexture pressureRT0;//网格点压强的RT
    RenderTexture pressureRT1;//网格点压强的RT

    
    RenderTexture tempRT;//一个用于保存中间计算数据的RT

    public int SIM_RES = 32;//由于模拟的RT分辨率
    public int DISPLAY_RES = 1024;//用于显示的RT分辨率
    //public float DENSITY_DISSIPATION = 0.0f;//1.0f;
    //public float VELOCITY_DISSIPATION = 0.0f;//0.2f;
    public float PRESSURE = 0.8f;
    public int VISCOUS_ITERATIONS = 5;
    public int PRESSURE_ITERATIONS = 20;
    public float VISCOUS = 0.0001f;
    public bool bReplay = false;

    public Material circlepaintMaterial;
    public Material advectionMaterial;
    public Material divergenceMaterial;
    public Material pressureMaterial;
    public Material gradientMaterial;
    public Material origPressureMaterial;
    public Material diffusionMaterial;


    static Color RandomColor()
    {
        float r = Random.Range(0.0f, 1.0f);
        float g = Random.Range(0.0f, 1.0f);
        float b = Random.Range(0.0f, 1.0f);
        Color c = new Color();
        c.r = r;
        c.g = g;
        c.b = b;
        return c;
    }

    static Vector2 getCorrectResolution(float resolution)
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
        Vector2 simCorrectResolution = getCorrectResolution(SIM_RES);
        Vector2 disCorrectResolution = getCorrectResolution(DISPLAY_RES);

        
        velocityRT0 = RenderTexture.GetTemporary((int)simCorrectResolution.x, (int)simCorrectResolution.y, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        velocityRT0.name = "velocityRT0";
        velocityRT1 = RenderTexture.GetTemporary((int)simCorrectResolution.x, (int)simCorrectResolution.y, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        velocityRT1.name = "velocityRT1";

        dyeRT0 = RenderTexture.GetTemporary((int)disCorrectResolution.x, (int)disCorrectResolution.y, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        dyeRT0.name = "dyeRT0";
        dyeRT1 = RenderTexture.GetTemporary((int)disCorrectResolution.x, (int)disCorrectResolution.y, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        dyeRT1.name = "dyeRT1";

        pressureRT0 = RenderTexture.GetTemporary((int)simCorrectResolution.x, (int)simCorrectResolution.y, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        pressureRT0.name = "pressureRT0";
        pressureRT1 = RenderTexture.GetTemporary((int)simCorrectResolution.x, (int)simCorrectResolution.y, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        pressureRT1.name = "pressureRT1";

        tempRT = RenderTexture.GetTemporary((int)simCorrectResolution.x, (int)simCorrectResolution.y, 0);
        tempRT.name = "tempRT";
    }

    void SwapRenderTexture(ref RenderTexture rt1, ref RenderTexture rt2)
    {
        RenderTexture temp = rt1;
        rt1 = rt2;
        rt2 = temp;
    }
    float baseVeclocity = 1000f;

    static float CorrectRadius(float radius)
    {
        float aspectRatio = Screen.width / Screen.height;
        if (aspectRatio > 1)
            radius *= aspectRatio;
        return radius;
    }

    void CreateDye(float x, float y, float dx, float dy, Color color, float radius)
    {
        circlepaintMaterial.SetTexture("_MainTex", velocityRT0);
        circlepaintMaterial.SetVector("pointAndRadius", new Vector4(x, y, radius, 0));
        circlepaintMaterial.SetVector("color", new Vector4(dx, dy, 0, 1.0f));
        Graphics.Blit(velocityRT0, velocityRT1, circlepaintMaterial);
        SwapRenderTexture(ref velocityRT0, ref velocityRT1);

        circlepaintMaterial.SetVector("color", new Vector4(color.r, color.g, color.b, 1.0f));
        circlepaintMaterial.SetTexture("_MainTex", dyeRT0);
        Graphics.Blit(dyeRT0, dyeRT1, circlepaintMaterial);
        SwapRenderTexture(ref dyeRT0, ref dyeRT1);
        Graphics.SetRenderTarget(null);

    }
  

    void CreateDyes(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            Color color = RandomColor();

            // Position
            float x = Random.Range(0.0f, 1.0f);
            float y = Random.Range(0.0f, 1.0f);
            // Velocity
            float dx = baseVeclocity * (Random.Range(0.0f, 1.0f) - 0.5f);
            float dy = baseVeclocity * (Random.Range(0.0f, 1.0f) - 0.5f);
            float radius = Random.Range(0.0f, 1.0f)/100.0f;
            CreateDye(x, y, dx, dy, color, radius);
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
        tempRT.Release();
        pressureRT0.Release();
        pressureRT1.Release();
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
        Graphics.Blit(dyeRT0, null as RenderTexture);
    }

    void SimulateStep()
    {
        // velocity advection
        advectionMaterial.SetTexture("_VelocityTex", velocityRT0);
        advectionMaterial.SetTexture("_PhysicTex", velocityRT0);
        Graphics.Blit(velocityRT0, velocityRT1, advectionMaterial);
        SwapRenderTexture(ref velocityRT0, ref velocityRT1);

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

        // divergence
        Graphics.Blit(velocityRT0, tempRT, divergenceMaterial);

        // pressure
        origPressureMaterial.SetFloat("value", PRESSURE);
        Graphics.Blit(pressureRT0, pressureRT1, origPressureMaterial);
        SwapRenderTexture(ref pressureRT0, ref pressureRT1);

        pressureMaterial.SetTexture("_SecondTex", tempRT);
        for (int i = 0; i < PRESSURE_ITERATIONS; i++)
        {
            Graphics.Blit(pressureRT0, pressureRT1, pressureMaterial);
            SwapRenderTexture(ref pressureRT0, ref pressureRT1);
        }

        gradientMaterial.SetTexture("_SecondTex", velocityRT0);
        Graphics.Blit(pressureRT0, velocityRT1, gradientMaterial);
        SwapRenderTexture(ref velocityRT0, ref velocityRT1);

        //颜色平流（对流）
        advectionMaterial.SetTexture("_VelocityTex", velocityRT0);
        advectionMaterial.SetTexture("_PhysicTex", dyeRT1);
        Graphics.Blit(velocityRT0, dyeRT0, advectionMaterial);
        SwapRenderTexture(ref dyeRT0, ref dyeRT1);

    }
    void Update()
    {
        if (bReplay)
        {
            Restart();
            bReplay = !bReplay;
        }

        SimulateStep();
    }



}

