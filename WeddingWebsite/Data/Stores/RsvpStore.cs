using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using WeddingWebsite.Data.Models;
using WeddingWebsite.Models.People;

namespace WeddingWebsite.Data.Stores;

public class RsvpStore : DataStoreBase, IRsvpStore
{
    public RsvpStore(IConfiguration configuration) : base(configuration)
    {
    }
    
    [Authorize]
    public bool SubmitRsvp(string guestId, bool isAttending, IReadOnlyList<string?> rsvpData)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();

        // Single atomic INSERT that is a no-op if a row for this guest already exists.
        // This avoids opening a reader and then running a second command on the same
        // connection (PostgreSQL forbids a new command while a reader is still open).
        var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = """
            INSERT INTO "RsvpFormResponses" ("GuestId", "IsAttending", "Data0", "Data1", "Data2", "Data3", "Data4", "Data5", "Data6", "Data7", "Data8", "Data9", "Data10",
                                       "Data11", "Data12", "Data13", "Data14", "Data15", "Data16", "Data17", "Data18", "Data19", "Data20")
            SELECT :guestId, :isAttending, :data0, :data1, :data2, :data3, :data4, :data5, :data6, :data7, :data8, :data9, :data10,
                   :data11, :data12, :data13, :data14, :data15, :data16, :data17, :data18, :data19, :data20
            WHERE NOT EXISTS (SELECT 1 FROM "RsvpFormResponses" WHERE "GuestId" = :guestId)
            """;

        AddParameter(insertCommand, ":guestId", guestId);
        AddParameter(insertCommand, ":isAttending", isAttending);

        for (int i = 0; i <= 20; i++)
        {
            var paramName = $":data{i}";
            var dataValue = rsvpData.ElementAtOrDefault(i);
            AddParameter(insertCommand, paramName, (object?) dataValue ?? DBNull.Value);
        }

        // rowsUpdated == 0 means the WHERE NOT EXISTS condition was false (already RSVPed)
        var rowsUpdated = insertCommand.ExecuteNonQuery();
        return rowsUpdated == 1;
    }

    [Authorize]
    public RsvpResponseData? GetRsvp(string guestId)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();

        var selectCommand = connection.CreateCommand();
        selectCommand.CommandText = """
            SELECT "IsAttending", "Guests"."FirstName", "Guests"."LastName", "Data0", "Data1", "Data2", "Data3", "Data4", "Data5", "Data6", "Data7", "Data8", "Data9", "Data10",
                   "Data11", "Data12", "Data13", "Data14", "Data15", "Data16", "Data17", "Data18", "Data19", "Data20"
            FROM "RsvpFormResponses"
            LEFT JOIN "Guests" on "RsvpFormResponses"."GuestId" = "Guests"."GuestId"
            WHERE "RsvpFormResponses"."GuestId" = :guestId
            """;
        
        AddParameter(selectCommand, ":guestId", guestId);

        bool isAttending;
        string firstName;
        string lastName;
        var rsvpData = new List<string?>();

        using (var reader = selectCommand.ExecuteReader())
        {
            if (!reader.Read())
            {
                return null; // No RSVP found
            }

            isAttending = reader.GetBoolean(0);
            firstName = reader.GetString(1);
            lastName = reader.GetString(2);

            for (int i = 0; i <= 20; i++)
            {
                rsvpData.Add(reader.IsDBNull(i + 3) ? null : reader.GetString(i + 3));
            }
        } // reader disposed here — safe to run further commands on the connection if needed

        return new RsvpResponseData(guestId, new Name(firstName, lastName), isAttending, rsvpData);
    }

    [Authorize(Roles = "Admin")]
    public IEnumerable<RsvpResponseData> GetAllRsvps()
    {
        using DbConnection connection = CreateConnection();
        connection.Open();

        var selectCommand = connection.CreateCommand();
        selectCommand.CommandText = """
            SELECT "RsvpFormResponses"."GuestId", "IsAttending", "Guests"."FirstName", "Guests"."LastName", "Data0", "Data1", "Data2", "Data3", "Data4", "Data5", "Data6", "Data7", "Data8", "Data9", "Data10",
                   "Data11", "Data12", "Data13", "Data14", "Data15", "Data16", "Data17", "Data18", "Data19", "Data20"
            FROM "RsvpFormResponses"
            LEFT JOIN "Guests" on "RsvpFormResponses"."GuestId" = "Guests"."GuestId"
            """;

        // Materialise into a List before returning so the reader and connection are
        // disposed here rather than staying open for the full duration of the caller's
        // iteration (which could span a Razor render and trigger concurrent-command errors).
        var results = new List<RsvpResponseData>();
        using (var reader = selectCommand.ExecuteReader())
        {
            while (reader.Read())
            {
                var guestId = reader.GetString(0);
                var isAttending = reader.GetBoolean(1);
                var firstName = reader.GetString(2);
                var lastName = reader.GetString(3);
                var rsvpData = new List<string?>();

                for (int i = 0; i <= 20; i++)
                {
                    rsvpData.Add(reader.IsDBNull(i + 4) ? null : reader.GetString(i + 4));
                }

                results.Add(new RsvpResponseData(guestId, new Name(firstName, lastName), isAttending, rsvpData));
            }
        } // reader and connection disposed here

        return results;
    }

    [Authorize(Roles = "Admin")]
    public void DeleteRsvp(string guestId)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();

        var deleteCommand = connection.CreateCommand();
        deleteCommand.CommandText = "DELETE FROM \"RsvpFormResponses\" WHERE \"GuestId\" = :guestId";
        AddParameter(deleteCommand, ":guestId", guestId);

        deleteCommand.ExecuteNonQuery();
    }
}