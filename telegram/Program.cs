using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

using sukalambda;
using System.Collections.Concurrent;
using System.Text;

ConcurrentDictionary<long, RootController> chatIdToController = new();
ConcurrentDictionary<long, Task> chatIdToTimedExecution = new();

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
    Console.WriteLine($"{timeSent}[{chatId}] {message.From}: {messageText}");

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

    void ExecuteOneRoundAfterMilliseconds(int milliseconds, RootController controller)
    {
        while (true)
        {
            if (controller.vm != null && controller.vm.gameEnded)  // Ended by external force
            {
                controller.vm = null;
                chatIdToController.Remove(chatId, out _);
                chatIdToTimedExecution.Remove(chatId, out _);
                botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Ready for the next game",
                    cancellationToken: cancellationToken);
                break;
            }
            Thread.Sleep(milliseconds);
            if (controller.vm != null && !controller.vm.gameEnded && !controller.vm.gamePaused)
            {
                controller.vm.ExecuteRound(releaseSemaphore: false);
                List<string> logs = controller.logCollector.PopGameLog();
                string contentToSend = String.Join("\r\n", logs);
                if (contentToSend != "")
                    botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: contentToSend,
                        cancellationToken: cancellationToken);
                if (controller.vm.map != null)
                    botClient.SendTextMessageAsync(chatId: chatId, text: controller.vm.map.RenderAsText(Language.cn), cancellationToken: cancellationToken);
                controller.vm.semaphore.Release();
            }
        }
    }


    if (message.From != null)
    {
        long senderId = message.From.Id;
        RootController controller = chatIdToController.GetOrAdd(
            chatId, new RootController(chatId.ToString(), GamePlatform.Telegram)
        );
        controller.cmdRouter.ExecuteCommand(
            senderId.ToString(),
            messageText.TrimStart().TrimStart('/'),
            controller
        );
        List<string> logs = controller.logCollector.PopGameLog();
        string contentToSend = String.Join("\r\n", logs);
        if (contentToSend != "")
            sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: contentToSend,
                cancellationToken: cancellationToken
            );
        if (controller.vm?.map != null && !chatIdToTimedExecution.ContainsKey(chatId))
            _ = chatIdToTimedExecution.GetOrAdd(chatId,
                Task.Run(() => ExecuteOneRoundAfterMilliseconds(15000, controller)));
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