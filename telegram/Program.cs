using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

using sukalambda;
using System.Collections.Concurrent;
using System.Text;

ConcurrentDictionary<long, RootController> chatIdToController = new();

var botClient = new TelegramBotClient(System.IO.File.ReadAllText(@"botToken.txt", Encoding.UTF8).TrimEnd().TrimStart());

using CancellationTokenSource cts = new();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();  // Press Enter to terminate

// Send cancellation request to stop bot
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    // Only process Message updates: https://core.telegram.org/bots/api#message
    if (update.Message is not { } message)
        return;
    // Only process text messages
    if (message.Text is not { } messageText)
        return;
    // Ignore messages from bots
    if (message.From?.IsBot == null || message.From?.IsBot == true)
        return;

    long chatId = message.Chat.Id;
    DateTime timeSent = message.Date.ToLocalTime();
    Console.WriteLine($"{timeSent}[{chatId}] {message.From}({message.From?.Id}): {messageText}");

    TimeSpan timeDelta = DateTime.Now - timeSent;
    Message sentMessage;
    if (timeDelta > TimeSpan.FromSeconds(90))
    {
        // Echo received message text
        //sentMessage = await botClient.SendTextMessageAsync(
        //    chatId: chatId,
        //    text: "Timeout:\n" + messageText,
        //    cancellationToken: cancellationToken
        //);
        return;
    }

    if (message.From != null)
    {
        long senderId = message.From.Id;
        RootController controller = chatIdToController.GetOrAdd(chatId, new RootController(GamePlatform.Telegram));
        controller.cmdRouter.ExecuteCommand(
            senderId.ToString(),
            messageText.TrimStart().TrimStart('/'),
            controller
        );
        List<string> logs = controller.logCollector.PopGameLog();
        foreach (string log in logs)
            if (log != "")
                sentMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: log,
                    cancellationToken: cancellationToken
                );
    }
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}