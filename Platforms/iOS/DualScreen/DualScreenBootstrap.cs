using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public class SyncClient
{
    ClientWebSocket socket = new();

    public async Task Connect(string uri)
    {
        await socket.ConnectAsync(new Uri(uri), CancellationToken.None);
        _ = Listen();
    }

    public async Task SendState(int tabIndex)
    {
        var json = JsonSerializer.Serialize(new { tabIndex });

        var bytes = Encoding.UTF8.GetBytes(json);

        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    async Task Listen()
    {
        var buffer = new byte[1024];

        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(buffer, CancellationToken.None);

            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);

            var state = JsonSerializer.Deserialize<StateDto>(json);

            DualScreenService.State.SetTab(state.tabIndex);
        }
    }

    class StateDto
    {
        public int tabIndex { get; set; }
    }
}
