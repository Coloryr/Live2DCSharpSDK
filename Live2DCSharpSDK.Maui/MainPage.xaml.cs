

using OpenTK.Graphics.OpenGL4;

namespace Live2DCSharpSDK.Maui;

public partial class MainPage : ContentPage
{
    float red, green, blue;
    public MainPage()
    {
        InitializeComponent();

        Title = "OpenGL";

        glview.OnDisplay = r => {

            //GL.ClearColor(red, green, blue, 1.0f);
            //GL.Clear((ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

            //red += 0.01f;
            //if (red >= 1.0f)
            //    red -= 1.0f;
            //green += 0.02f;
            //if (green >= 1.0f)
            //    green -= 1.0f;
            //blue += 0.03f;
            //if (blue >= 1.0f)
            //    blue -= 1.0f;
        };

        glview.HasRenderLoop = true;
        glview.Display();
    }

   
}