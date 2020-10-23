namespace Atoll.Transport.Client.Bundle.Dto
{
    public enum PacketProcessingResult
    {
        Unknown = 0,
        Saved = 1,
        Error = 2,
        Resend = 3,
        PacketsNeedUpdate = 4
    }
}
