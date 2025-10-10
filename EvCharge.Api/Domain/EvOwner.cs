// =============================================
// File: EvOwner.cs
// Description: Represents the EV owner data model for the EV Charging app.
//              Defines entity structure used in data storage and transfer.
// Date: 10/10/2025
// =============================================

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EvCharge.Api.Domain
{
    public class EvOwner
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string NIC { get; set; } = string.Empty; // Primary key in DB

        [BsonElement("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [BsonElement("lastName")]
        public string LastName { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty; // used for login

        [BsonElement("phone")]
        public string Phone { get; set; } = string.Empty;

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("passwordHash")]
        public string? PasswordHash { get; set; }
    }
}
