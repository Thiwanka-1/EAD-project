// =============================================
// File: User.cs
// Description: Represents the user data model for the EV Charging app.
//              Defines entity structure used in data storage and transfer.
// Author: Gamithu / IT22295224
// Date: 10/10/2025
// =============================================

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EvCharge.Api.Domain
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("username")]
        public string Username { get; set; } = string.Empty;

        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        [BsonElement("role")]
        public string Role { get; set; } = string.Empty; // "Backoffice" or "Operator"

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;
    }
}
