using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LinePutScript.Localization;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VPet_Simulator.Core.MutiPlatform.Display.Shell;

namespace VPet_Simulator.MutiPlatform
{
    /// <summary>
    /// PetHelper.xaml 的交互逻辑
    /// </summary>
    /// <remarks>通过 Show(mw) 设置属主，让辅助窗口跟随当前桌宠。</remarks>
    public partial class PetHelper : VPetWindow
    {
        MainWindow mw;
        public void SetOpacity(bool isOn)
        {
            if (isOn)
                Dispatcher.UIThread.Invoke(() => { Opacity = 0.8; });
            else
                Dispatcher.UIThread.Invoke(() => { Opacity = 0.4; });
        }
        public PetHelper(MainWindow mw)
        {
            InitializeComponent();
            this.mw = mw;
            //set = mf.Set["pethelp"];
            x = mw.Set.PetHelpLeft * mw.Width;
            y = mw.Set.PetHelpTop * mw.Width;
            Width = 50 * mw.Set.ZoomLevel;
            Height = 50 * mw.Set.ZoomLevel;
            SetLocation();
        }

        private void Window_Loaded(object? sender, RoutedEventArgs e)
        {
            ToolTip.SetTip(this, "点击此处开关鼠标穿透\n右键开关置于顶层\n长按挪动位置\n可在设置中关闭".Translate());
        }

        private void Image_MouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
        {
            //跨平台: 右键在这里分流 (Avalonia 没有 MouseRightButtonDown)
            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                WindowX_MouseRightButtonDown(sender, e);
                return;
            }
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                return;
            isclick = true;
            Task.Run(() =>
            {
                Thread.Sleep(200);
                Dispatcher.UIThread.Invoke(() =>
                    {
                        if (isclick)
                        {
                            try
                            {
                                try
                                {
                                    BeginMoveDrag(e);
                                }
                                catch { }
                                isdragmove = Opacity == 0.8;
                                Cursor = new Cursor(StandardCursorType.Hand);
                                Opacity = 1;
                            }
                            catch
                            {
                            }

                        }
                    });
            });

        }
        bool? isdragmove = null;
        bool isclick = false;
        double x;
        double y;
        private void Image_MouseLeftButtonUp(object? sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton != MouseButton.Left)
                return;
            isclick = false;
            if (isdragmove.HasValue)
            {
                Cursor = new Cursor(StandardCursorType.Arrow);
                SetOpacity(isdragmove.Value);
                isdragmove = null;
                x = Left - mw.Left;
                y = Top - mw.Top;
                mw.Set.PetHelpLeft = Math.Max(Math.Min(x / mw.Width, 1.1), -.1);
                mw.Set.PetHelpTop = Math.Max(Math.Min(y / mw.Width, 1.1), -.1);

                ReloadLocation();
                return;
            }
            mw.SetTransparentHitThrough();
        }
        public void ReloadLocation()
        {
            x = mw.Set.PetHelpLeft * mw.Width;
            y = mw.Set.PetHelpTop * mw.Width;
            SetLocation();
        }
        public void SetLocation()
        {
            WindowPositioning.SetPosition(this, mw.Left + x, mw.Top + y, RenderScaling);
        }

        private void WindowX_MouseRightButtonDown(object? sender, PointerPressedEventArgs e)
        {
            mw.Topmost = !mw.Topmost;
            //同步托盘图标菜单中「置于顶层」选项的选中状态
            mw.NotifyIcon_TopMost.IsChecked = mw.Topmost;

            if (mw.Topmost == true && mw.HitThrough == true)
                mw.SetTransparentHitThrough();
            else
                SetOpacity(mw.Topmost);
        }
    }
}
