// When tab changes
Shell.Current.CurrentItemChanged += (s, e) =>
{
    var renderer = (PerTabDualScreenRenderer)Shell.Current.GetRenderer();
    renderer?.SwitchExternalTab(Shell.Current.CurrentItem.Stack.IndexOf(Shell.Current.CurrentItem.CurrentItem));
};

// Scroll effect
void OnScrollChanged(object sender, ScrolledEventArgs e)
{
    var scrollOffset = (float)Math.Min(1.0, e.ScrollY / 200);
    var renderer = (PerTabDualScreenRenderer)Shell.Current.GetRenderer();
    renderer?.OnScroll(scrollOffset);
}
