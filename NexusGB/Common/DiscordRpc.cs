namespace NexusGB.Common;

using DiscordRPC;
using System.Text;

public sealed class DiscordRpc : IDisposable
{
    private const string ApplicationId = "1352767756776243231";
    private const string Description = "Play GameBoy games in the command line";
    private const int ByteLimit = 128;
    private const string Ellipsis = "...";

    private readonly DiscordRpcClient _client;

    private DiscordRpc(DiscordRpcClient client)
    {
        _client = client;
        _client.Initialize();
    }

    public static DiscordRpc Initialize()
    {
        var client = new DiscordRpcClient(ApplicationId);
        return new DiscordRpc(client);
    }

    public void SetMenu()
    {
        _client.SetPresence(new RichPresence
        {
            Assets = new Assets
            {
                LargeImageKey = "NexusGB Emulator",
                LargeImageText = Description
            },
            Details = "Main Menu",
            State = "Idling",
            Timestamps = Timestamps.Now,
            Buttons =
            [
                new Button
                {
                    Label = "Website",
                    Url = "https://github.com/BlyZeDev/NexusGB-Emulator"
                }
            ]
        });
    }

    public void SetGame(string title)
    {
        _client.SetPresence(new RichPresence
        {
            Assets = new Assets
            {
                LargeImageKey = "game",
                LargeImageText = Truncate(title),
                SmallImageKey = "NexusGB Emulator",
                SmallImageText = Description
            },
            Details = Truncate($"Playing {title}"),
            State = "",
            Timestamps = Timestamps.Now,
            Buttons =
            [
                new Button
                {
                    Label = "Website",
                    Url = "https://github.com/BlyZeDev/NexusGB-Emulator"
                }
            ]
        });
    }

    public void Dispose() => _client.Dispose();

    private static string Truncate(string input)
    {
        if (Encoding.UTF8.GetByteCount(input) <= ByteLimit) return input;

        var trimLimit = ByteLimit - Encoding.UTF8.GetByteCount(Ellipsis);
        if (input.Length > trimLimit)
            input = input[..trimLimit];

        while (Encoding.UTF8.GetByteCount(input) > trimLimit)
        {
            input = input[..^1];
        }

        return input + Ellipsis;
    }
}