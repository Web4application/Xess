builder.ConfigureLifecycleEvents(events =>
{
#if IOS
    events.AddiOS(ios =>
    {
        ios.FinishedLaunching((app, options) =>
        {
            DualScreenBootstrap.Init();
            return true;
        });
    });
#endif
});
