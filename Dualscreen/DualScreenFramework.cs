using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(AppShell), typeof(PerTabDualScreenRenderer))]

namespace YourApp.iOS.Renderers
{
    // =========================
    // 1. GLOBAL STATE STORE
    // =========================
    public class DualScreenState
    {
        public int CurrentTabIndex { get; private set; }

        public event Action<int> TabChanged;

        public void SetTab(int index)
        {
            if (CurrentTabIndex == index) return;

            CurrentTabIndex = index;
            TabChanged?.Invoke(index);
        }
    }

    public static class DualScreenService
    {
        public static DualScreenState State { get; } = new DualScreenState();
    }

    // =========================
    // 2. EXTERNAL DISPLAY ENGINE
    // =========================
    public class ExternalDisplayManager
    {
        UIWindow window;
        UIViewController container;
        UIViewController current;

        List<Func<UIViewController>> pageFactories;

        public void RegisterPages(List<Func<UIViewController>> factories)
        {
            pageFactories = factories;
        }

        public void Connect(UIScreen screen)
        {
            window = new UIWindow(screen.Bounds)
            {
                Screen = screen
            };

            container = new UIViewController();
            window.RootViewController = container;
            window.Hidden = false;

            DualScreenService.State.TabChanged += OnTabChanged;

            // Initial render
            OnTabChanged(DualScreenService.State.CurrentTabIndex);
        }

        public void Disconnect()
        {
            DualScreenService.State.TabChanged -= OnTabChanged;

            window?.Dispose();
            window = null;
        }

        void OnTabChanged(int index)
        {
            if (pageFactories == null || index >= pageFactories.Count) return;

            var next = pageFactories[index].Invoke();
            AnimateSwap(next);
        }

        void AnimateSwap(UIViewController next)
        {
            if (container == null) return;

            UIView.Transition(
                container.View,
                0.3,
                UIViewAnimationOptions.TransitionCrossDissolve,
                () => Swap(next),
                null
            );
        }

        void Swap(UIViewController next)
        {
            if (current != null)
            {
                current.WillMoveToParentViewController(null);
                current.View.RemoveFromSuperview();
                current.RemoveFromParentViewController();
            }

            container.AddChildViewController(next);

            next.View.Frame = container.View.Bounds;
            next.View.AutoresizingMask =
                UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

            container.View.AddSubview(next.View);
            next.DidMoveToParentViewController(container);

            current = next;
        }
    }

    // =========================
    // 3. TAB BAR APPEARANCE
    // =========================
    public class PerTabDualScreenTabBarTracker : ShellTabBarAppearanceTracker
    {
        UIVisualEffectView blurView;

        public override void SetAppearance(UITabBarController controller, ShellAppearance appearance)
        {
            controller.TabBar.Translucent = true;

            if (blurView == null)
            {
                var blurEffect = UIBlurEffect.FromStyle(UIBlurEffectStyle.Light);
                blurView = new UIVisualEffectView(blurEffect)
                {
                    Frame = controller.TabBar.Bounds,
                    AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight
                };

                controller.TabBar.InsertSubview(blurView, 0);
            }

            controller.TabBar.TintColor = UIColor.SystemBlue;
            controller.TabBar.UnselectedItemTintColor = UIColor.LightGray;

            base.SetAppearance(controller, appearance);
        }

        public void UpdateTabBar(float scrollOffset, UITabBarController controller)
        {
            if (blurView != null)
            {
                var alpha = Math.Min(1f, Math.Max(0.3f, 1 - scrollOffset));
                blurView.Alpha = alpha;
            }
        }
    }

    // =========================
    // 4. MAIN RENDERER
    // =========================
    public class PerTabDualScreenRenderer : ShellRenderer
    {
        ExternalDisplayManager externalManager = new ExternalDisplayManager();
        PerTabDualScreenTabBarTracker tracker;

        protected override IShellTabBarAppearanceTracker CreateTabBarAppearanceTracker()
        {
            tracker = new PerTabDualScreenTabBarTracker();
            return tracker;
        }

        protected override void OnElementSet(Shell element)
        {
            base.OnElementSet(element);

            if (element != null)
            {
                element.Navigated += OnShellNavigated;
            }

            UIScreen.DidConnectNotification += ScreenDidConnect;
            UIScreen.DidDisconnectNotification += ScreenDidDisconnect;

            ConfigureExternalPages();
        }

        // =========================
        // 5. TAB → STATE SYNC
        // =========================
        void OnShellNavigated(object sender, ShellNavigatedEventArgs e)
        {
            var index = Shell.Current.Items.IndexOf(Shell.Current.CurrentItem);
            DualScreenService.State.SetTab(index);
        }

        // =========================
        // 6. EXTERNAL SCREEN EVENTS
        // =========================
        void ScreenDidConnect(NSNotification notification)
        {
            if (notification.Object is UIScreen screen)
            {
                externalManager.Connect(screen);
            }
        }

        void ScreenDidDisconnect(NSNotification notification)
        {
            externalManager.Disconnect();
        }

        // =========================
        // 7. EXTERNAL PAGE FACTORIES
        // =========================
        void ConfigureExternalPages()
        {
            externalManager.RegisterPages(new List<Func<UIViewController>>
            {
                () => CreatePage("Dashboard", UIColor.Black),
                () => CreatePage("Analytics", UIColor.DarkGray),
                () => CreatePage("Settings", UIColor.Brown)
            });
        }

        UIViewController CreatePage(string title, UIColor bgColor)
        {
            var label = new UILabel
            {
                Text = title,
                TextColor = UIColor.White,
                TextAlignment = UITextAlignment.Center
            };

            var view = new UIView
            {
                BackgroundColor = bgColor
            };

            label.Frame = view.Bounds;
            label.AutoresizingMask =
                UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

            view.AddSubview(label);

            return new UIViewController { View = view };
        }

        // =========================
        // 8. OPTIONAL SCROLL EFFECT
        // =========================
        public void OnScroll(float offset)
        {
            tracker?.UpdateTabBar(offset, TabBarController);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UIScreen.DidConnectNotification -= ScreenDidConnect;
                UIScreen.DidDisconnectNotification -= ScreenDidDisconnect;
            }

            base.Dispose(disposing);
        }
    }
}
