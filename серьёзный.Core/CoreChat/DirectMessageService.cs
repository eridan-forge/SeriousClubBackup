using System;
using System.Collections.Generic;
using System.Linq;

namespace серьёзный.Core.CoreChat;

public enum DirectMessageType
{
    Text,
    Voice
}

public class DirectMessage
{
    public Guid Id { get; set; }
    public Guid From { get; set; }
    public Guid To { get; set; }
    public DateTime Time { get; set; }
    public string Text { get; set; } = "";
    public string VoiceBase64 { get; set; } = "";
    public DirectMessageType Type { get; set; }
    public bool Read { get; set; }
}

public class DirectMessageService
{
    private readonly ChatService chat = new();

    public event Action<DirectMessage>? MessageReceived;

    public DirectMessageService()
    {
        ChatLiveEvents.MessageReceived += msg =>
        {
            MessageReceived?.Invoke(ToDirect(msg));
        };
    }

    public IEnumerable<DirectMessage> Dialog(Guid a, Guid b)
    {
        return chat.Get(a, b).Select(ToDirect);
    }

    public void Send(Guid from, Guid to, string text)
    {
        chat.Send(from, to, text);
    }

    public void SendVoice(Guid from, Guid to, string base64)
    {
        chat.SendVoice(from, to, base64);
    }

    public int Unread(Guid player)
    {
        return chat.Unread(player);
    }

    public void MarkRead(Guid me, Guid friend)
    {
        chat.MarkRead(me, friend);
    }

    private static DirectMessage ToDirect(ChatMessage msg)
    {
        return new DirectMessage
        {
            Id = msg.Id,
            From = msg.From,
            To = msg.To,
            Time = msg.Time,
            Text = msg.Text,
            VoiceBase64 = msg.VoiceFile ?? "",
            Type = string.IsNullOrWhiteSpace(msg.VoiceFile)
                ? DirectMessageType.Text
                : DirectMessageType.Voice,
            Read = msg.Read
        };
    }
}