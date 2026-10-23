using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;

namespace CyberClub
{
    // Снимки интерфейса для автоматических проверок и отчёта.
    public partial class MainWindow
    {
        // Сохранить снимок интерфейса для проверок и отчёта по практике.
        public void Snapshot(string name)
        {
            Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
            {
            }));
            UpdateLayout();
            frame.UpdateLayout();
            var image = new RenderTargetBitmap((int)shell.ActualWidth, (int)shell.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            image.Render(shell);
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(image));
            Directory.CreateDirectory(System.IO.Path.Combine(Root, "docs", "screenshots"));
            using (var s = File.Create(System.IO.Path.Combine(Root, "docs", "screenshots", name + ".png")))
                enc.Save(s);
        }

        void SnapshotElement(FrameworkElement element, string name)
        {
            element.UpdateLayout();
            var bmp = new RenderTargetBitmap(Math.Max(1, (int)element.ActualWidth), Math.Max(1, (int)element.ActualHeight), 96, 96, PixelFormats.Pbgra32);
            bmp.Render(element);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            using (var s = File.Create(System.IO.Path.Combine(Root, "docs", "screenshots", name + ".png")))
                encoder.Save(s);
        }
    }
}
