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
        @"
            SELECT Guests.GuestId, Guests.FirstName, Guests.LastName, RsvpFormResponses.IsAttending
            FROM Guests
            LEFT JOIN RsvpFormResponses ON Guests.GuestId = RsvpFormResponses.GuestId
            WHERE UserId = :userId
        ";
        AddParameter(command, ":userId", userId);

        using var reader = command.ExecuteReader();
        var guests = new List<GuestWithId>();
        while (reader.Read())
        {
            guests.Add(new GuestWithId(
                reader.GetString(0),
                new Name(reader.GetString(1), reader.GetString(2)),
                reader.IsDBNull(3) ? RsvpStatus.NotResponded : reader.GetInt16(3) == 1 ? RsvpStatus.Yes : RsvpStatus.No
            ));
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
        @"
            INSERT INTO Guests (GuestId, UserId, FirstName, LastName, RsvpStatus)
            VALUES (:guestId, :userId, :firstName, :lastName, :rsvpStatus)
        ";
        AddParameter(command, ":guestId", Guid.NewGuid().ToString());
        AddParameter(command, ":userId", userId);
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
                SELECT account.Id, account.UserName, account.Email, guest.FirstName, guest.LastName, rsvp.IsAttending, MAX(log.Timestamp) timestamp
                FROM AspNetUsers account
                LEFT JOIN Guests guest ON account.Id = guest.UserId
                LEFT JOIN AccountLog log ON account.Id = log.AffectedUserId AND log.EventType = :loginEventType
                LEFT JOIN RsvpFormResponses rsvp ON guest.GuestId = rsvp.GuestId
                GROUP BY account.UserName, guest.GuestId, account.Id, account.Email, guest.FirstName, guest.LastName, rsvp.IsAttending
                ORDER BY account.UserName
            """;
            // above only had first two group bys
        
        AddParameter(command, ":loginEventType", AccountLogTypeEnumConverter.AccountLogTypeToDatabaseInteger(AccountLogType.LogIn));
        
        using var reader = command.ExecuteReader();
        var accounts = new List<AccountWithGuests>();
        string? currentAccountId = null;
        string? currentAccountUserName = null;
        string? currentAccountEmail = null;
        bool currentAccountHasLoggedIn = false;
        var currentGuests = new List<Guest>();
        
        while (reader.Read())
        {
            var accountId = reader.GetString(0);
            var accountUserName = reader.GetString(1);
            var accountEmail = reader.IsDBNull(2) ? null : reader.GetString(2);
            var guestFirstName = reader.IsDBNull(3) ? null : reader.GetString(3);
            var guestLastName = reader.IsDBNull(4) ? null : reader.GetString(4);
            var guestRsvpStatus = reader.IsDBNull(5) ? RsvpStatus.NotResponded : reader.GetInt16(5) == 1 ? RsvpStatus.Yes : RsvpStatus.No;
            var accountLastLoginTimestamp = reader.IsDBNull(6) ? (DateTime?)null : new DateTime(reader.GetInt64(6), DateTimeKind.Utc);
            
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
                SELECT Guests.GuestId, Guests.FirstName, Guests.LastName, RsvpFormResponses.IsAttending
                FROM Guests
                LEFT JOIN RsvpFormResponses ON Guests.GuestId = RsvpFormResponses.GuestId
                WHERE UserId = :userId
            """;
        AddParameter(command, ":userId", userId);
        
        using var reader = command.ExecuteReader();
        var guests = new List<GuestWithId>();
        while (reader.Read())
        {
            var guestId = reader.GetString(0);
            var firstName = reader.GetString(1);
            var lastName = reader.GetString(2);
            var rsvpStatus = reader.IsDBNull(3) ? RsvpStatus.NotResponded : reader.GetInt16(3) == 1 ? RsvpStatus.Yes : RsvpStatus.No;
            guests.Add(new GuestWithId(guestId, new Name(firstName, lastName), rsvpStatus));
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
                UPDATE Guests
                SET FirstName = :newFirstName, LastName = :newLastName
                WHERE GuestId = :guestId
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
                DELETE FROM Guests
                WHERE GuestId = :guestId
            """;
        
        AddParameter(command, ":guestId", guestId);
        
        command.ExecuteNonQuery();
    }
    
    [Authorize(Roles = "Admin")]
    public string? GetAccountIdFromGuestId(string guestId)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText =
            """
                SELECT UserId
                FROM Guests
                WHERE GuestId = :guestId
            """;
        
        AddParameter(command, ":guestId", guestId);
        
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            var userId = reader.GetString(0);
            return userId;
        }
        
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
                INSERT INTO AccountLog (LogId, Timestamp, AffectedUserId, ActorId, EventType, Description)
                VALUES (:logId, :timestamp, :affectedUserId, :actorId, :eventType, :description)
            """;
        
        AddParameter(command, ":logId", Guid.NewGuid().ToString());
        AddParameter(command, ":timestamp", DateTime.UtcNow.Ticks);
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
                SELECT Id
                FROM AspNetUsers
                WHERE NormalizedUserName = :username
            """;
        
        AddParameter(command, ":username", username.Normalize().ToUpperInvariant());
        
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            var userId = reader.GetString(0);
            return userId;
        }
        
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
                SELECT log.Timestamp, 
                       affectedUser.Id, affectedUser.UserName, 
                       actor.Id, actor.UserName, 
                       log.EventType, log.Description
                FROM AccountLog log
                JOIN AspNetUsers affectedUser ON log.AffectedUserId = affectedUser.Id
                JOIN AspNetUsers actor ON log.ActorId = actor.Id
                WHERE log.AffectedUserId = :userId
                ORDER BY log.Timestamp DESC
            """;
        
        AddParameter(command, ":userId", userId);
        
        using var reader = command.ExecuteReader();
        var logs = new List<AccountLog>();
        while (reader.Read())
        {
            var timestampTicks = reader.GetInt64(0);
            var affectedUserId = reader.GetString(1);
            var affectedUserName = reader.GetString(2);
            var actorId = reader.GetString(3);
            var actorUserName = reader.GetString(4);
            var eventTypeInt = reader.GetInt16(5);
            
            var logType = AccountLogTypeEnumConverter.DatabaseIntegerToAccountLogType(eventTypeInt);
            
            var description = reader.IsDBNull(6) ? logType.GetEnumDescription() : reader.GetString(6);
            
            var log = new AccountLog(
                new DateTime(timestampTicks, DateTimeKind.Utc),
                new Account { Id = affectedUserId, UserName = affectedUserName },
                new Account { Id = actorId, UserName = actorUserName },
                logType,
                description
            );
            
            logs.Add(log);
        }

        return logs;
    }
}
