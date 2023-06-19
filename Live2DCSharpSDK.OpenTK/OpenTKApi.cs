using Live2DCSharpSDK.Framework.Rendering.OpenGL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;

namespace Live2DCSharpSDK.OpenTK;

public class OpenTKApi : OpenGLApi
{
    private NativeWindow Window;

    public override bool IsES2 => false;

    public override bool IsPhoneES2 => false;

    public OpenTKApi(NativeWindow window)
    {
        Window = window;
    }

    public override void GetWindowSize(out int w, out int h)
    {
        w = Window.ClientSize.X;
        h = Window.ClientSize.Y;
    }

    public override void glActiveTexture(int bit)
    {
        GL.ActiveTexture((TextureUnit)bit);
    }

    public override void glAttachShader(int a, int b)
    {
        GL.AttachShader(a, b);
    }

    public override void glBindBuffer(int bit, int index)
    {
        GL.BindBuffer((BufferTarget)bit, index);
    }

    public override void glBindFramebuffer(int type, int data)
    {
        GL.BindFramebuffer((FramebufferTarget)type, data);
    }

    public override void glBindTexture(int bit, int index)
    {
        GL.BindTexture((TextureTarget)bit, index);
    }

    public override void glBindVertexArrayOES(int data)
    {
        //TODO ES2
        GL.BindVertexArray(data);
    }

    public override void glBlendFunc(int a, int b)
    {
        GL.BlendFunc((BlendingFactor)a, (BlendingFactor)b);
    }

    public override void glBlendFuncSeparate(int a, int b, int c, int d)
    {
        GL.BlendFuncSeparate((BlendingFactorSrc)a, (BlendingFactorDest)b, (BlendingFactorSrc)c, (BlendingFactorDest)d);
    }

    public override void glClear(int bit)
    {
        GL.Clear((ClearBufferMask)bit);
    }

    public override void glClearColor(float r, float g, float b, float a)
    {
        GL.ClearColor(r, g, b, a);
    }

    public override void glClearDepthf(float data)
    {
        GL.ClearDepth(data);
    }

    public override void glColorMask(bool a, bool b, bool c, bool d)
    {
        GL.ColorMask(a, b, c, d);
    }

    public override void glCompileShader(int index)
    {
        GL.CompileShader(index);
    }

    public override int glCreateProgram()
    {
        return GL.CreateProgram();
    }

    public override int glCreateShader(int type)
    {
        return GL.CreateShader((ShaderType)type);
    }

    public override void glDeleteFramebuffer(int data)
    {
        GL.DeleteFramebuffer(data);
    }

    public override void glDeleteProgram(int index)
    {
        GL.DeleteProgram(index);
    }

    public override void glDeleteShader(int index)
    {
        GL.DeleteShader(index);
    }

    public override void glDeleteTexture(int data)
    {
        GL.DeleteTexture(data);
    }

    public override void glDetachShader(int index, int data)
    {
        GL.DetachShader(index, data);
    }

    public override void glDisable(int bit)
    {
        GL.Disable((EnableCap)bit);
    }

    public override void glDisableVertexAttribArray(int index)
    {
        GL.DisableVertexAttribArray(index);
    }

    public override unsafe void glDrawElements(int type, int count, int type1, ushort* arry)
    {
        GL.DrawElements((PrimitiveType)type, count, (DrawElementsType)type1, new IntPtr(arry));
    }

    public override void glEnable(int bit)
    {
        GL.Enable((EnableCap)bit);
    }

    public override void glEnableVertexAttribArray(int index)
    {
        GL.EnableVertexAttribArray(index);
    }

    public override void glFramebufferTexture2D(int a, int b, int c, int buff, int offset)
    {
        GL.FramebufferTexture2D((FramebufferTarget)a, (FramebufferAttachment)b,
            (TextureTarget)c, buff, offset);
    }

    public override void glFrontFace(int data)
    {
        GL.FrontFace((FrontFaceDirection)data);
    }

    public override void glGenerateMipmap(int a)
    {
        GL.GenerateMipmap((GenerateMipmapTarget)a);
    }

    public override int glGenFramebuffer()
    {
        return GL.GenFramebuffer();
    }

    public override int glGenTexture()
    {
        return GL.GenTexture();
    }

    public override int glGetAttribLocation(int index, string attr)
    {
        return GL.GetAttribLocation(index, attr);
    }

    public override void glGetBooleanv(int bit, bool[] data)
    {
        GL.GetBoolean((GetPName)bit, data);
    }

    public override void glGetIntegerv(int bit, out int data)
    {
        GL.GetInteger((GetPName)bit, out data);
    }

    public override void glGetIntegerv(int bit, int[] data)
    {
        GL.GetInteger((GetPName)bit, data);
    }

    public override void glGetProgramInfoLog(int index, out string log)
    {
        GL.GetProgramInfoLog(index, out log);
    }

    public override unsafe void glGetProgramiv(int index, int type, int* length)
    {
        GL.GetProgram(index, (GetProgramParameterName)type, length);
    }

    public override void glGetShaderInfoLog(int index, out string log)
    {
        GL.GetShaderInfoLog(index, out log);
    }

    public override unsafe void glGetShaderiv(int index, int type, int* length)
    {
        GL.GetShader(index, (ShaderParameter)type, length);
    }

    public override int glGetUniformLocation(int index, string uni)
    {
        return GL.GetUniformLocation(index, uni);
    }

    public override unsafe void glGetVertexAttribiv(int index, int bit, out int data)
    {
        GL.GetVertexAttrib(index, (VertexAttribParameter)bit, out data);
    }

    public override bool glIsEnabled(int bit)
    {
        return GL.IsEnabled((EnableCap)bit);
    }

    public override void glLinkProgram(int index)
    {
        GL.LinkProgram(index);
    }

    public override void glShaderSource(int a, string source)
    {
        GL.ShaderSource(a, source);
    }

    public override void glTexImage2D(int type, int a, int type1, int w, int h, int size, int type2, int type3, nint data)
    {
        GL.TexImage2D((TextureTarget)type, a, (PixelInternalFormat)type1, w, h, size, (PixelFormat)type2, (PixelType)type3, data);
    }

    public override void glTexParameterf(int type, int type1, float value)
    {
        GL.TexParameter((TextureTarget)type, (TextureParameterName)type1, value);
    }

    public override void glTexParameteri(int a, int b, int c)
    {
        GL.TexParameter((TextureTarget)a, (TextureParameterName)b, c);
    }

    public override void glUniform1i(int index, int data)
    {
        GL.Uniform1(index, data);
    }

    public override void glUniform4f(int index, float a, float b, float c, float d)
    {
        GL.Uniform4(index, a, b, c, d);
    }

    public override void glUniformMatrix4fv(int index, int length, bool b, float[] data)
    {
        GL.UniformMatrix4(index, length, b, data);
    }

    public override void glUseProgram(int index)
    {
        GL.UseProgram(index);
    }

    public override void glValidateProgram(int index)
    {
        GL.ValidateProgram(index);
    }

    public override unsafe void glVertexAttribPointer(int index, int length, int type, bool b, int size, float* arr)
    {
        GL.VertexAttribPointer(index, length, (VertexAttribPointerType)type, b, size, new IntPtr(arr));
    }

    public override void glViewport(int x, int y, int w, int h)
    {
        GL.Viewport(x, y, w, h);
    }

    public override int glGetError()
    {
        return (int)GL.GetError();
    }
}
