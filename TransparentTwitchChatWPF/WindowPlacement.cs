using System.Windows;
using System.Windows.Threading;

namespace TransparentTwitchChatWPF
{
    static class WindowPlacement
    {
        public static void Remember(Window window, Action persist)
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                if (window.IsLoaded)
                    persist();
            };

            void Schedule()
            {
                if (!window.IsLoaded)
                    return;

                timer.Stop();
                timer.Start();
            }

            window.LocationChanged += (_, _) => Schedule();
            window.SizeChanged += (_, _) => Schedule();
            window.StateChanged += (_, _) => Schedule();
        }
    }
}
