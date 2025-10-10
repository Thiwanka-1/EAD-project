// =============================================
// File: Booking.cs
// Description: Represents the booking data model for the EV Charging app.
//              Defines entity structure used in data storage and transfer.
// Date: 10/10/2025
// =============================================

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EvCharge.Api.Domain
{
    public static class BookingStatus
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string InProgress = "InProgress";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";

        // considered 'active' for station deactivation checks and overlap counting
        public static readonly string[] ActiveStatuses = { Pending, Approved, InProgress };

        // owner can change only while these statuses are present AND 12h rule is satisfied
        public static readonly string[] OwnerCanChange = { Pending, Approved };
    }

    public class Booking
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("ownerNic")]
        public string OwnerNic { get; set; } = string.Empty;

        [BsonElement("stationId")]
        public string StationId { get; set; } = string.Empty;

        [BsonElement("startTimeUtc")]
        public DateTime StartTimeUtc { get; set; }

        [BsonElement("endTimeUtc")]
        public DateTime EndTimeUtc { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = BookingStatus.Pending;

        // stored as a simple token string; client can render QR image
        [BsonElement("qrCode")]
        public string? QrCode { get; set; }

        [BsonElement("createdUtc")]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedUtc")]
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

        [BsonElement("rejectionReason")]
        public string? RejectionReason { get; set; }
    }
}
