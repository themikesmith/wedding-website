using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using WeddingWebsite.Core;
using WeddingWebsite.Data.Enums;
using WeddingWebsite.Data.Models;
using WeddingWebsite.Models.Accounts;
using WeddingWebsite.Models.People;

namespace WeddingWebsite.Data.Stores;

public class Store : DataStoreBase, IStore
{
    public Store(IConfiguration configuration) : base(configuration)
    {
    }

    [Authorize]
    public IEnumerable<GuestWithId> GetGuestsForUser(string userId)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT "Guests"."GuestId", "Guests"."FirstName", "Guests"."LastName", "RsvpFormResponses"."IsAttending"
            FROM "Guests"
            LEFT JOIN "RsvpFormResponses" ON "Guests"."GuestId" = "RsvpFormResponses"."GuestId"
            WHERE "UserId" = :userId
            """;
        AddParameter(command, ":userId", userId);

        var guests = new List<GuestWithId>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                guests.Add(new GuestWithId(
                    reader.GetString(0),
                    new Name(reader.GetString(1), reader.GetString(2)),
                    reader.IsDBNull(3) ? RsvpStatus.NotResponded : reader.GetBoolean(3) ? RsvpStatus.Yes : RsvpStatus.No
                ));
            }
        }

        return guests;
    }
    
    [Authorize(Roles = "Admin")]
    public void AddGuestToAccount(string userId, string firstName, string lastName, RsvpStatus rsvpStatus)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO "Guests" ("GuestId", "UserId", "FirstName", "LastName", "RsvpStatus")
            VALUES (:guestId, :userId, :firstName, :lastName, :rsvpStatus)
            """;
        AddParameter(command, ":guestId", Guid.NewGuid().ToString());
        AddParameter(command, ":userId", userId);
        // Note: Guests.RsvpStatus is a vestigial column — the live RSVP status displayed in the UI
        // comes from RsvpFormResponses.IsAttending (boolean) via LEFT JOIN, not this column.
        // This column is always inserted as NotResponded (0) and is never updated or SELECTed.
        // If it were ever added to a SELECT query, use GetInt32(n) and pass the result to
        // RsvpStatusEnumConverter.DatabaseIntegerToRsvpStatus() — NOT GetBoolean(), which is
        // only correct for the RsvpFormResponses.IsAttending boolean column.
        AddParameter(command, ":firstName", firstName);
        AddParameter(command, ":lastName", lastName);
        AddParameter(command, ":rsvpStatus", RsvpStatusEnumConverter.ToDatabaseInteger(rsvpStatus));

        command.ExecuteNonQuery();
    }

    [Authorize(Roles = "Admin")]
    public IEnumerable<AccountWithGuests> GetAllAccounts()
    {
        using DbConnection connection = CreateConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText =
            """
                SELECT account."Id", account."UserName", account."Email", guest."FirstName", guest."LastName", rsvp."IsAttending", MAX(log."Timestamp") timestamp
                FROM "AspNetUsers" account
                LEFT JOIN "Guests" guest ON account."Id" = guest."UserId"
                LEFT JOIN "AccountLog" log ON account."Id" = log."AffectedUserId" AND log."EventType" = :loginEventType
                LEFT JOIN "RsvpFormResponses" rsvp ON guest."GuestId" = rsvp."GuestId"
                GROUP BY account."UserName", guest."GuestId", account."Id", account."Email", guest."FirstName", guest."LastName", rsvp."IsAttending"
                ORDER BY account."UserName", guest."GuestId"
            """;
        
        AddParameter(command, ":loginEventType", AccountLogTypeEnumConverter.AccountLogTypeToDatabaseInteger(AccountLogType.LogIn));
        
        var accounts = new List<AccountWithGuests>();
        string? currentAccountId = null;
        string? currentAccountUserName = null;
        string? currentAccountEmail = null;
        bool currentAccountHasLoggedIn = false;
        var currentGuests = new List<Guest>();

        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                var accountId = reader.GetString(0);
                var accountUserName = reader.GetString(1);
                var accountEmail = reader.IsDBNull(2) ? null : reader.GetString(2);
                var guestFirstName = reader.IsDBNull(3) ? null : reader.GetString(3);
                var guestLastName = reader.IsDBNull(4) ? null : reader.GetString(4);
                var guestRsvpStatus = reader.IsDBNull(5) ? RsvpStatus.NotResponded : reader.GetBoolean(5) ? RsvpStatus.Yes : RsvpStatus.No;
                var accountLastLoginTimestamp = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6);

                if (currentAccountId != accountId)
                {
                    if (currentAccountId != null)
                    {
                        accounts.Add(new AccountWithGuests(currentGuests, currentAccountHasLoggedIn)
                        {
                            Id = currentAccountId,
                            Email = currentAccountEmail,
                            UserName = currentAccountUserName
                        });
                    }

                    currentAccountId = accountId;
                    currentAccountEmail = accountEmail;
                    currentAccountUserName = accountUserName;
                    currentAccountHasLoggedIn = accountLastLoginTimestamp != null;
                    currentGuests = new List<Guest>();
                }

                if (guestFirstName != null && guestLastName != null)
                {
                    currentGuests.Add(new Guest(new Name(guestFirstName, guestLastName), guestRsvpStatus));
                }
            }
        }

        if (currentAccountId != null)
        {
            accounts.Add(new AccountWithGuests(currentGuests, currentAccountHasLoggedIn)
            {
                Id = currentAccountId,
                Email = currentAccountEmail,
                UserName = currentAccountUserName
            });
        }

        return accounts;
    }

    [Authorize(Roles = "Admin")]
    public IEnumerable<GuestWithId> GetGuestsForAccount(string userId)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText =
            """
                SELECT "Guests"."GuestId", "Guests"."FirstName", "Guests"."LastName", "RsvpFormResponses"."IsAttending"
                FROM "Guests"
                LEFT JOIN "RsvpFormResponses" ON "Guests"."GuestId" = "RsvpFormResponses"."GuestId"
                WHERE "UserId" = :userId
            """;
        AddParameter(command, ":userId", userId);
        
        var guests = new List<GuestWithId>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                var guestId = reader.GetString(0);
                var firstName = reader.GetString(1);
                var lastName = reader.GetString(2);
                var rsvpStatus = reader.IsDBNull(3) ? RsvpStatus.NotResponded : reader.GetBoolean(3) ? RsvpStatus.Yes : RsvpStatus.No;
                guests.Add(new GuestWithId(guestId, new Name(firstName, lastName), rsvpStatus));
            }
        }

        return guests;
    }

    [Authorize(Roles = "Admin")]
    public void RenameGuest(string guestId, string newFirstName, string newLastName)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText =
            """
                UPDATE "Guests"
                SET "FirstName" = :newFirstName, "LastName" = :newLastName
                WHERE "GuestId" = :guestId
            """;
        
        AddParameter(command, ":newFirstName", newFirstName);
        AddParameter(command, ":newLastName", newLastName);
        AddParameter(command, ":guestId", guestId);
        
        command.ExecuteNonQuery();
    }

    [Authorize(Roles = "Admin")]
    public void DeleteGuest(string guestId)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText =
            """
                DELETE FROM "Guests"
                WHERE "GuestId" = :guestId
            """;
        
        AddParameter(command, ":guestId", guestId);
        
        command.ExecuteNonQuery();
    }
    
    [Authorize(Roles = "Admin")]
    public GuestWithId? GetGuestById(string guestId, string userId)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText =
            """
                SELECT "Guests"."GuestId", "Guests"."FirstName", "Guests"."LastName", "RsvpFormResponses"."IsAttending"
                FROM "Guests"
                LEFT JOIN "RsvpFormResponses" ON "Guests"."GuestId" = "RsvpFormResponses"."GuestId"
                WHERE "Guests"."GuestId" = :guestId AND "Guests"."UserId" = :userId
            """;
        AddParameter(command, ":guestId", guestId);
        AddParameter(command, ":userId", userId);

        using (var reader = command.ExecuteReader())
        {
            if (!reader.Read()) return null;
            var rsvpStatus = reader.IsDBNull(3) ? RsvpStatus.NotResponded : reader.GetBoolean(3) ? RsvpStatus.Yes : RsvpStatus.No;
            return new GuestWithId(reader.GetString(0), new Name(reader.GetString(1), reader.GetString(2)), rsvpStatus);
        }
    }

    [Authorize(Roles = "Admin")]
    public string? GetAccountIdFromGuestId(string guestId)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText =
            """
                SELECT "UserId"
                FROM "Guests"
                WHERE "GuestId" = :guestId
            """;
        
        AddParameter(command, ":guestId", guestId);

        using (var reader = command.ExecuteReader())
        {
            if (reader.Read())
            {
                return reader.GetString(0);
            }
        } // reader disposed here

        return null;
    }

    [Authorize]
    public void AddAccountLog(string affectedUserId, string actorId, AccountLogType logType, string description)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText =
            """
                INSERT INTO "AccountLog" ("LogId", "Timestamp", "AffectedUserId", "ActorId", "EventType", "Description")
                VALUES (:logId, :timestamp, :affectedUserId, :actorId, :eventType, :description)
            """;
        
        AddParameter(command, ":logId", Guid.NewGuid().ToString());
        AddParameter(command, ":timestamp", DateTime.UtcNow);
        AddParameter(command, ":affectedUserId", affectedUserId);
        AddParameter(command, ":actorId", actorId);
        AddParameter(command, ":eventType", logType.ToDatabaseInteger());
        AddParameter(command, ":description", description);
        
        command.ExecuteNonQuery();
    }
    
    public string? GetUserIdByUserName(string username)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText =
            """
                SELECT "Id"
                FROM "AspNetUsers"
                WHERE "NormalizedUserName" = :username
            """;
        
        AddParameter(command, ":username", username.Normalize().ToUpperInvariant());

        using (var reader = command.ExecuteReader())
        {
            if (reader.Read())
            {
                return reader.GetString(0);
            }
        } // reader disposed here

        return null;
    }

    [Authorize(Roles = "Admin")]
    public IEnumerable<AccountLog> GetAccountLogs(string userId)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText =
            """
                SELECT log."Timestamp",
                       affectedUser."Id", affectedUser."UserName",
                       actor."Id", actor."UserName",
                       log."EventType", log."Description"
                FROM "AccountLog" log
                LEFT JOIN "AspNetUsers" affectedUser ON log."AffectedUserId" = affectedUser."Id"
                LEFT JOIN "AspNetUsers" actor ON log."ActorId" = actor."Id"
                WHERE log."AffectedUserId" = :userId
                ORDER BY log."Timestamp" DESC
            """;
        
        AddParameter(command, ":userId", userId);
        
        var logs = new List<AccountLog>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                var timestamp = reader.GetDateTime(0);
                // LEFT JOINs above: guard against orphaned log rows whose user was deleted.
                var affectedUserId = reader.IsDBNull(1) ? "[deleted]" : reader.GetString(1);
                var affectedUserName = reader.IsDBNull(2) ? "[deleted user]" : reader.GetString(2);
                var actorId = reader.IsDBNull(3) ? "[deleted]" : reader.GetString(3);
                var actorUserName = reader.IsDBNull(4) ? "[deleted user]" : reader.GetString(4);
                var eventTypeInt = reader.GetInt32(5);

                var logType = AccountLogTypeEnumConverter.DatabaseIntegerToAccountLogType(eventTypeInt);

                var description = reader.IsDBNull(6) ? logType.GetEnumDescription() : reader.GetString(6);

                var log = new AccountLog(
                    timestamp,
                    new Account { Id = affectedUserId, UserName = affectedUserName },
                    new Account { Id = actorId, UserName = actorUserName },
                    logType,
                    description
                );

                logs.Add(log);
            }
        }

        return logs;
    }
}
