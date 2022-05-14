using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.IoC.Internal;

namespace Dalamud.Game.Gui;

/// <summary>
/// This interface handles interacting with the native chat UI.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
public interface IChatGui
{
    /// <summary>
    /// Event that will be fired when a chat message is sent to chat by the game.
    /// </summary>
    event ChatGui.OnMessageDelegate ChatMessage;

    /// <summary>
    /// Event that allows you to stop messages from appearing in chat by setting the isHandled parameter to true.
    /// </summary>
    event ChatGui.OnCheckMessageHandledDelegate CheckMessageHandled;

    /// <summary>
    /// Event that will be fired when a chat message is handled by Dalamud or a Plugin.
    /// </summary>
    event ChatGui.OnMessageHandledDelegate ChatMessageHandled;

    /// <summary>
    /// Event that will be fired when a chat message is not handled by Dalamud or a Plugin.
    /// </summary>
    event ChatGui.OnMessageUnhandledDelegate ChatMessageUnhandled;

    /// <summary>
    /// Gets the ID of the last linked item.
    /// </summary>
    int LastLinkedItemId { get; }

    /// <summary>
    /// Gets the flags of the last linked item.
    /// </summary>
    byte LastLinkedItemFlags { get; }

    /// <summary>
    /// Enables this module.
    /// </summary>
    void Enable();

    /// <summary>
    /// Queue a chat message. While method is named as PrintChat, it only add a entry to the queue,
    /// later to be processed when UpdateQueue() is called.
    /// </summary>
    /// <param name="chat">A message to send.</param>
    void PrintChat(XivChatEntry chat);

    /// <summary>
    /// Queue a chat message. While method is named as PrintChat (it calls it internally), it only add a entry to the queue,
    /// later to be processed when UpdateQueue() is called.
    /// </summary>
    /// <param name="message">A message to send.</param>
    void Print(string message);

    /// <summary>
    /// Queue a chat message. While method is named as PrintChat (it calls it internally), it only add a entry to the queue,
    /// later to be processed when UpdateQueue() is called.
    /// </summary>
    /// <param name="message">A message to send.</param>
    void Print(SeString message);

    /// <summary>
    /// Queue an error chat message. While method is named as PrintChat (it calls it internally), it only add a entry to
    /// the queue, later to be processed when UpdateQueue() is called.
    /// </summary>
    /// <param name="message">A message to send.</param>
    void PrintError(string message);

    /// <summary>
    /// Queue an error chat message. While method is named as PrintChat (it calls it internally), it only add a entry to
    /// the queue, later to be processed when UpdateQueue() is called.
    /// </summary>
    /// <param name="message">A message to send.</param>
    void PrintError(SeString message);

    /// <summary>
    /// Process a chat queue.
    /// </summary>
    void UpdateQueue();
}
