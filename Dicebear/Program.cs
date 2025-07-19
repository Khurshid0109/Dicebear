using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var botToken = "";
var botClient = new TelegramBotClient(botToken);

using var cts = new CancellationTokenSource();

var supportedAvatars = new Dictionary<string, string>
{
    ["/fun-emoji"] = "fun-emoji",
    ["/avataaars"] = "avataaars",
    ["/bottts"] = "bottts",
    ["/pixel-art"] = "pixel-art"
};

var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = Array.Empty<UpdateType>()
};

botClient.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMe();
Console.WriteLine($"🤖 Bot started: @{me.Username}");

Console.ReadLine();
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
{
    if (update.Message is not { } message || string.IsNullOrWhiteSpace(message.Text))
        return;

    var chatId = message.Chat.Id;
    var text = message.Text.Trim();
    var userId = message.From?.Id ?? 0;

    Console.WriteLine($"[INFO] User {userId} sent: {text}");

    if (text == "/help")
    {
        await SendHelpMessage(bot, chatId, cancellationToken);
        return;
    }

    if (!text.StartsWith("/"))
    {
        await bot.SendMessage(chatId,
            "Iltimos, avatar olish uchun buyruqdan foydalaning.",
            cancellationToken: cancellationToken);
        return;
    }

    var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
    var command = parts[0];
    var seed = parts.Length > 1 ? parts[1] : null;

    if (!supportedAvatars.TryGetValue(command, out var style))
    {
        await bot.SendMessage(chatId,
            "Noma’lum buyruq. Quyidagilardan birini ishlating:\n" +
            "/fun-emoji, /bottts, /avataaars, /pixel-art",
            cancellationToken: cancellationToken);
        return;
    }

    if (string.IsNullOrWhiteSpace(seed))
    {
        await bot.SendMessage(chatId,
            $"Iltimos, buyruqdan keyin matn (Ism) kiriting. Masalan: {command} Ali",
            cancellationToken: cancellationToken);
        return;
    }

    var imageUrl = $"https://api.dicebear.com/8.x/{style}/png?seed={Uri.EscapeDataString(seed)}";

    try
    {
        using var http = new HttpClient();
        using var imageStream = await http.GetStreamAsync(imageUrl, cancellationToken);

        await bot.SendPhoto(chatId,
            InputFile.FromStream(imageStream, $"{seed}.png"),
            caption: $"👤 Avatar: {seed}",
            cancellationToken: cancellationToken);

        Console.WriteLine($"[SUCCESS] Avatar sent: {style} -> \"{seed}\"");
    }
    catch (HttpRequestException httpEx)
    {
        Console.WriteLine($"[ERROR] HTTP request failed: {httpEx.Message}");
        await bot.SendMessage(chatId,
            "Avatar yaratishda xatolik yuz berdi. Keyinroq urinib ko‘ring.",
            cancellationToken: cancellationToken);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Unknown error: {ex.Message}");
        await bot.SendMessage(chatId,
            "Rasmni yuborishda xatolik yuz berdi.",
            cancellationToken: cancellationToken);
    }
}

async Task SendHelpMessage(ITelegramBotClient bot, long chatId, CancellationToken cancellationToken)
{
    var helpText = """
    🧑‍🎨 Avatar yaratish uchun quyidagi buyruqlardan birini kiriting:

    🟢 /fun-emoji <ism>
    🟢 /avataaars <ism>
    🟢 /bottts <ism>
    🟢 /pixel-art <ism>

    Masalan: /bottts John Doe
    """;

    await bot.SendMessage(chatId, helpText, cancellationToken: cancellationToken);
}

Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
{
    var errorMsg = exception switch
    {
        ApiRequestException apiEx =>
            $"Telegram API xatosi: [{apiEx.ErrorCode}] {apiEx.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine($"[ERROR] Polling failed: {errorMsg}");
    return Task.CompletedTask;
}
