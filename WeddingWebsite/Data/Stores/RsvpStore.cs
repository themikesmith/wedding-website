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

        // Check if the user has already RSVPed
        var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = "SELECT GuestId FROM RsvpFormResponses WHERE GuestId = :guestId";
        AddParameter(checkCommand, ":guestId", guestId);
        using var reader = checkCommand.ExecuteReader();
        while (reader.Read())
        {
            return false;
        }

        // Insert the RSVP data
        var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = @"
            INSERT INTO RsvpFormResponses (GuestId, IsAttending, Data0, Data1, Data2, Data3, Data4, Data5, Data6, Data7, Data8, Data9, Data10,
                                       Data11, Data12, Data13, Data14, Data15, Data16, Data17, Data18, Data19, Data20)
            VALUES ($guestId, $isAttending, $data0, $data1, $data2, $data3, $data4, $data5, $data6, $data7, $data8, $data9, $data10,
                    $data11, $data12, $data13, $data14, $data15, $data16, $data17, $data18, $data19, $data20)";
        
        AddParameter(insertCommand, "guestId", guestId);
        AddParameter(insertCommand, "isAttending", isAttending ? 1 : 0);
        
        for (int i = 0; i <= 20; i++)
        {
            var paramName = $"data{i}";
            var dataValue = rsvpData.ElementAtOrDefault(i);
            AddParameter(insertCommand, paramName, (object?) dataValue ?? DBNull.Value);
        }

        var rowsUpdated = insertCommand.ExecuteNonQuery();
        Console.WriteLine($"Rows updated: {rowsUpdated}");
        return rowsUpdated == 1;
    }

    [Authorize]
    public RsvpResponseData? GetRsvp(string guestId)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();

        var selectCommand = connection.CreateCommand();
        selectCommand.CommandText = @"
            SELECT IsAttending, Guests.FirstName, Guests.LastName, Data0, Data1, Data2, Data3, Data4, Data5, Data6, Data7, Data8, Data9, Data10,
                   Data11, Data12, Data13, Data14, Data15, Data16, Data17, Data18, Data19, Data20
            FROM RsvpFormResponses
            LEFT JOIN Guests on RsvpFormResponses.GuestId = Guests.GuestId
            WHERE RsvpFormResponses.GuestId = $guestId";
        
        AddParameter(selectCommand, "guestId", guestId);

        using var reader = selectCommand.ExecuteReader();
        if (!reader.Read())
        {
            return null; // No RSVP found
        }

        var isAttending = reader.GetInt32(0) == 1;
        var firstName = reader.GetString(1);
        var lastName = reader.GetString(2);
        var rsvpData = new List<string?>();
        
        for (int i = 0; i <= 20; i++)
        {
            if (reader.IsDBNull(i+3))
            {
                rsvpData.Add(null);
            }
            else
            {
                rsvpData.Add(reader.GetString(i+3));
            }
        }

        return new RsvpResponseData(guestId, new Name(firstName, lastName), isAttending, rsvpData);
    }

    [Authorize(Roles = "Admin")]
    public IEnumerable<RsvpResponseData> GetAllRsvps()
    {
        using DbConnection connection = CreateConnection();
        connection.Open();

        var selectCommand = connection.CreateCommand();
        selectCommand.CommandText = @"
            SELECT RsvpFormResponses.GuestId, IsAttending, Guests.FirstName, Guests.LastName, Data0, Data1, Data2, Data3, Data4, Data5, Data6, Data7, Data8, Data9, Data10,
                   Data11, Data12, Data13, Data14, Data15, Data16, Data17, Data18, Data19, Data20
            FROM RsvpFormResponses
            LEFT JOIN Guests on RsvpFormResponses.GuestId = Guests.GuestId";

        using var reader = selectCommand.ExecuteReader();
        while (reader.Read())
        {
            var guestId = reader.GetString(0);
            var isAttending = reader.GetInt32(1) == 1;
            var firstName = reader.GetString(2);
            var lastName = reader.GetString(3);
            var rsvpData = new List<string?>();
            
            for (int i = 0; i <= 20; i++)
            {
                if (reader.IsDBNull(i+4))
                {
                    rsvpData.Add(null);
                }
                else
                {
                    rsvpData.Add(reader.GetString(i+4));
                }
            }

            yield return new RsvpResponseData(guestId, new Name(firstName, lastName), isAttending, rsvpData);
        }
    }

    [Authorize(Roles = "Admin")]
    public void DeleteRsvp(string guestId)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();

        var deleteCommand = connection.CreateCommand();
        deleteCommand.CommandText = "DELETE FROM RsvpFormResponses WHERE GuestId = $guestId";
        AddParameter(deleteCommand, "guestId", guestId);

        deleteCommand.ExecuteNonQuery();
    }
}