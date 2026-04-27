var sync = new SyncClient();

await sync.Connect("ws://127.0.0.1");

DualScreenService.State.TabChanged += async (index) =>
{
    await sync.SendState(index);
};
