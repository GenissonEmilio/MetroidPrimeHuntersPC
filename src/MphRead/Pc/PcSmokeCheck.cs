using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL;

namespace MphRead
{
    internal static class PcSmokeCheck
    {
        internal static void CheckPresentationShader()
        {
            int vertex = GL.CreateShader(ShaderType.VertexShader);
            int fragment = GL.CreateShader(ShaderType.FragmentShader);
            int program = GL.CreateProgram();
            try
            {
                GL.ShaderSource(vertex, Shaders.RttVertexShader); GL.CompileShader(vertex);
                GL.ShaderSource(fragment, Shaders.RttFragmentShader); GL.CompileShader(fragment);
                GL.GetShader(vertex, ShaderParameter.CompileStatus, out int vertexOk);
                GL.GetShader(fragment, ShaderParameter.CompileStatus, out int fragmentOk);
                if (vertexOk == 0 || fragmentOk == 0)
                    throw new InvalidOperationException("Shader de apresentacao: " + GL.GetShaderInfoLog(vertex) + GL.GetShaderInfoLog(fragment));
                GL.AttachShader(program, vertex); GL.AttachShader(program, fragment); GL.LinkProgram(program);
                GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int linked);
                if (linked == 0) throw new InvalidOperationException("Link do shader: " + GL.GetProgramInfoLog(program));
            }
            finally { GL.DeleteProgram(program); GL.DeleteShader(vertex); GL.DeleteShader(fragment); }
        }
        public static void Run(string output)
        {
            Directory.CreateDirectory(output);
            string previousRoot = PcRuntime.StateRoot;
            PcProfile previousProfile = PcRuntime.Profile;
            PcRuntime.StateRoot = output;
            PcRuntime.Profile = new PcProfile();
            string? failure = null;
            string? audio = null;
            try
            {
                using (var menu = new PcFrontEnd(output)) menu.Run();
                if (!File.ReadAllText(Path.Combine(output, "gpu.txt")).Contains("GL error: NoError"))
                    throw new InvalidOperationException("Falha na renderizacao OpenGL.");
                if (PcProfile.Load(PcRuntime.ProfilePath).MouseSensitivity != 1.1f)
                    throw new InvalidOperationException("As configuracoes nao persistiram.");
                var device = ALC.OpenDevice(null);
                if (device == ALDevice.Null) throw new InvalidOperationException("OpenAL: dispositivo de audio indisponivel.");
                try
                {
                    var context = ALC.CreateContext(device, new ALContextAttributes());
                    if (context == ALContext.Null) throw new InvalidOperationException("OpenAL: contexto indisponivel.");
                    try
                    {
                        ALC.MakeContextCurrent(context);
                        audio = AL.Get(ALGetString.Renderer);
                        if (AL.GetError() != ALError.NoError) throw new InvalidOperationException("OpenAL: erro de inicializacao.");
                    }
                    finally { ALC.MakeContextCurrent(ALContext.Null); ALC.DestroyContext(context); }
                }
                finally { ALC.CloseDevice(device); }
                // Initializes the real music backend without requiring ROM assets.
                MusicPlayer.Load(SeqId.None);
                MusicPlayer.Remove(shutdown: true);
            }
            catch (Exception error) { failure = error.ToString(); throw; }
            finally
            {
                File.WriteAllText(Path.Combine(output, "smoke.json"), JsonSerializer.Serialize(new
                {
                    Success = failure == null, Failure = failure,
                    Platform = RuntimeInformation.OSDescription,
                    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                    Runtime = RuntimeInformation.FrameworkDescription,
                    OpenAL = audio, UsesDesktopInput = false, VisibleWindows = false,
                    GameplayTested = false,
                    Checks = new[] { "OpenGL menu", "Settings persistence", "4:3 resize", "OpenAL device/context", "SoundFlow MiniAudio backend", "Scene presentation/AA shader" }
                }, new JsonSerializerOptions { WriteIndented = true }));
                PcRuntime.StateRoot = previousRoot;
                PcRuntime.Profile = previousProfile;
            }
        }
    }
}
