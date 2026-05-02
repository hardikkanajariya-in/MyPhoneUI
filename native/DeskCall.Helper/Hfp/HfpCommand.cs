namespace DeskCall.Helper.Hfp;

public static class HfpCommand
{
    public const string Answer = "ATA";
    public const string HangUp = "AT+CHUP";
    public const string ListCurrentCalls = "AT+CLCC";
    public const string EnableCallerId = "AT+CLIP=1";

    public static string Dial(string number) => $"ATD{number};";
}
