namespace EvCharge.Api.DTOs
{
    public class BookingCreateRequest
    {
        public string StationId { get; set; } = string.Empty;
        public DateTime StartTimeUtc { get; set; }
        public DateTime EndTimeUtc { get; set; }
    }

    public class BookingUpdateRequest
    {
        public DateTime StartTimeUtc { get; set; }
        public DateTime EndTimeUtc { get; set; }
    }

    public class ApproveRequest
    {
        public bool Approve { get; set; }
        public string? Reason { get; set; } // required when Approve=false
    }

    public class StartRequest
    {
        public string QrCode { get; set; } = string.Empty;
    }
}
