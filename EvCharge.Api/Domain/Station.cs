// =============================================
// File: Station.cs
// Description: Represents the charging station data model for the EV Charging app.
//              Defines entity structure used in data storage and transfer.
// Author: Gamithu / IT22295224
// Date: 10/10/2025
// =============================================

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EvCharge.Api.Domain
{
    public class Station
    {
        // Custom human-readable ID (e.g., "ST1001")
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string StationId { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        // For maps + nearby search
        [BsonElement("latitude")]
        public double Latitude { get; set; }

        [BsonElement("longitude")]
        public double Longitude { get; set; }

        [BsonElement("address")]
        public string Address { get; set; } = string.Empty;

        // "AC" or "DC"
        [BsonElement("type")]
        public string Type { get; set; } = "AC";

        [BsonElement("availableSlots")]
        public int AvailableSlots { get; set; } = 0;

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        // Operators assigned to this station (User.Id of role "Operator")
        [BsonElement("operatorUserIds")]
        public List<string> OperatorUserIds { get; set; } = new();
    }
}
