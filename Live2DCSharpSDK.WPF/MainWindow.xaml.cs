using OpenTK.Wpf;
using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OpenTK.Graphics.OpenGL4;
using Live2DCSharpSDK.App;
using System.Windows.Threading;
using System.Diagnostics;
using OpenTK.Mathematics;

namespace Live2DCSharpSDK.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        GLWpfControl GLControl;
        private LAppDelegate lapp;
        private const float UpdateTime = 1f / 60f; // 60 FPS

        public MainWindow()
        {
            InitializeComponent();
            GLControl = new();

            GLControl.Loaded += GLControl_Loaded;
            GLControl.SizeChanged += GLControl_Resized;

            BorderOpenTK.Child = GLControl;

            CompositionTarget.Rendering += CompositionTarget_Rendering;

        }
        LAppModel model;
        private void GLControl_Loaded(object sender, RoutedEventArgs e)
        {
            var settings = new GLWpfControlSettings
            {
                MajorVersion = 3,
                MinorVersion = 3
            };
            GLControl.Start(settings);
            lapp = new(new OpenTKWPFApi(GLControl), Console.WriteLine)
            {
                BGColor = new(0, 1, 0, 1)
            };

            var model = lapp.Live2dManager.LoadModel("F:\\live2d\\Resources\\Mao", "Mao");
            //model = lapp.Live2dManager.LoadModel("F:\\Downloads\\haru_greeter_pro_jp\\haru_greeter_pro_jp\\runtime", "haru_greeter_t05");
        }
        private void CompositionTarget_Rendering(object? sender, EventArgs e)
        {
            if (lapp == null)
                return;
            lapp.Run(UpdateTime);
            var code = GL.GetError();
            if (code != ErrorCode.NoError)
            {
                throw new Exception(code.ToString());
            }
        }

        private void GLControl_Resized(object sender, SizeChangedEventArgs e)
        {
            if (lapp == null)
                return;
            lapp.Resize();
            GL.Viewport(0, 0, (int)GLControl.ActualWidth, (int)GLControl.ActualHeight);
        }
    }
}