using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using WeddingWebsite.Models.Registry;

namespace WeddingWebsite.Data.Stores;

[Authorize]
public class RegistryStore : DataStoreBase, IRegistryStore
{
    public RegistryStore(IConfiguration configuration) : base(configuration)
    {
    }
    
    [Authorize (Roles = "Admin")]
    public void AddItem(RegistryItem item)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();
        var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = @"
            INSERT INTO RegistryItems (Id, GenericName, Name, Description, ImageUrl, MaxQuantity, Priority, Hide, AllowsPartialContributions, IsDonation)
            VALUES (:id, :genericName, :name, :description, :imageUrl, :maxQuantity, :priority, :hide, :allowsPartialContributions, :isDonation);
        ";
        AddParameter(cmd, ":id", item.Id);
        AddParameter(cmd, ":genericName", item.GenericName);
        AddParameter(cmd, ":name", item.Name);
        AddParameter(cmd, ":description", item.Description ?? (object)DBNull.Value);
        AddParameter(cmd, ":imageUrl", item.ImageUrl ?? (object)DBNull.Value);
        AddParameter(cmd, ":maxQuantity", item.MaxQuantity);
        AddParameter(cmd, ":priority", item.Priority);
        AddParameter(cmd, ":hide", item.Hide ? 1 : 0);
        AddParameter(cmd, ":allowsPartialContributions", item.AllowsPartialContributions ? 1 : 0);
        AddParameter(cmd, ":isDonation", item.IsDonation ? 1 : 0);

        cmd.ExecuteNonQuery();

        AddPurchaseMethods(item, connection, transaction);

        transaction.Commit();
    }

    [Authorize (Roles = "Admin")]
    private void AddPurchaseMethods(RegistryItem registryItem, DbConnection connection, DbTransaction transaction)
    {
        foreach (var method in registryItem.PurchaseMethods)
        {
            var methodCmd = connection.CreateCommand();
            methodCmd.Transaction = transaction;
            methodCmd.CommandText = @"
                INSERT INTO RegistryItemPurchaseMethods
                (Id, ItemId, Name, Cost, AllowBringOnDay, AllowDeliverToUs, Url, Instructions, DeliveryCost)
                VALUES
                (:id, :itemId, :name, :cost, :allowBringOnDay, :allowDeliverToUs, :url, :instructions, :deliveryCost);
            ";
            AddParameter(methodCmd, ":id", method.Id);
            AddParameter(methodCmd, ":itemId", registryItem.Id);
            AddParameter(methodCmd, ":name", method.Name);
            AddParameter(methodCmd, ":cost", method.Cost);
            AddParameter(methodCmd, ":allowBringOnDay", method.AllowBringOnDay ? 1 : 0);
            AddParameter(methodCmd, ":allowDeliverToUs", method.AllowDeliverToUs ? 1 : 0);
            AddParameter(methodCmd, ":url", method.Url ?? (object)DBNull.Value);
            AddParameter(methodCmd, ":instructions", method.Instructions ?? (object)DBNull.Value);
            AddParameter(methodCmd, ":deliveryCost", method.DeliveryCost);

            methodCmd.ExecuteNonQuery();
        }
    }

    [Authorize (Roles = "Admin")]
    public void UpdateItem(RegistryItem item)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();
        
        using var transaction = connection.BeginTransaction();
        var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = @"
            UPDATE RegistryItems
            SET GenericName = :genericName,
                Name = :name,
                Description = :description,
                ImageUrl = :imageUrl,
                MaxQuantity = :maxQuantity,
                Priority = :priority,
                Hide = :hide,
                AllowsPartialContributions = :allowsPartialContributions,
                IsDonation = :isDonation
            WHERE Id = :id;
        ";
        AddParameter(cmd, ":id", item.Id);
        AddParameter(cmd, ":genericName", item.GenericName);
        AddParameter(cmd, ":name", item.Name);
        AddParameter(cmd, ":description", item.Description ?? (object)DBNull.Value);
        AddParameter(cmd, ":imageUrl", item.ImageUrl ?? (object)DBNull.Value);
        AddParameter(cmd, ":maxQuantity", item.MaxQuantity);
        AddParameter(cmd, ":priority", item.Priority);
        AddParameter(cmd, ":hide", item.Hide ? 1 : 0);
        AddParameter(cmd, ":allowsPartialContributions", item.AllowsPartialContributions ? 1 : 0);
        AddParameter(cmd, ":isDonation", item.IsDonation ? 1 : 0);

        var rowsAffected = cmd.ExecuteNonQuery();
        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"No registry item found with ID {item.Id}");
        }

        // Get IDs of existing purchase methods
        var existingMethodIds = new HashSet<string>();
        var getMethodsCmd = connection.CreateCommand();
        getMethodsCmd.Transaction = transaction;
        getMethodsCmd.CommandText = @"
            SELECT Id
            FROM RegistryItemPurchaseMethods
            WHERE ItemId = :itemId;
        ";
        AddParameter(getMethodsCmd, ":itemId", item.Id);
        using var reader = getMethodsCmd.ExecuteReader();
        while (reader.Read())
        {
            existingMethodIds.Add(reader.GetString(0));
        }
        
        // Update or insert purchase methods
        foreach (var method in item.PurchaseMethods)
        {
            if (existingMethodIds.Contains(method.Id))
            {
                // Update existing method
                var updateCmd = connection.CreateCommand();
                updateCmd.Transaction = transaction;
                updateCmd.CommandText = @"
                    UPDATE RegistryItemPurchaseMethods
                    SET Name = :name,
                        Cost = :cost,
                        AllowBringOnDay = :allowBringOnDay,
                        AllowDeliverToUs = :allowDeliverToUs,
                        Url = :url,
                        Instructions = :instructions,
                        DeliveryCost = :deliveryCost
                    WHERE Id = :id AND ItemId = :itemId;
                ";
                AddParameter(updateCmd, ":id", method.Id);
                AddParameter(updateCmd, ":itemId", item.Id);
                AddParameter(updateCmd, ":name", method.Name);
                AddParameter(updateCmd, ":cost", method.Cost);
                AddParameter(updateCmd, ":allowBringOnDay", method.AllowBringOnDay ? 1 : 0);
                AddParameter(updateCmd, ":allowDeliverToUs", method.AllowDeliverToUs ? 1 : 0);
                AddParameter(updateCmd, ":url", method.Url ?? (object)DBNull.Value);
                AddParameter(updateCmd, ":instructions", method.Instructions ?? (object)DBNull.Value);
                AddParameter(updateCmd, ":deliveryCost", method.DeliveryCost);
                updateCmd.ExecuteNonQuery();
            }
            else
            {
                // Insert new method
                var insertCmd = connection.CreateCommand();
                insertCmd.Transaction = transaction;
                insertCmd.CommandText = @"
                    INSERT INTO RegistryItemPurchaseMethods
                    (Id, ItemId, Name, Cost, AllowBringOnDay, AllowDeliverToUs, Url, Instructions, DeliveryCost)
                    VALUES
                    (:id, :itemId, :name, :cost, :allowBringOnDay, :allowDeliverToUs, :url, :instructions, :deliveryCost);
                ";
                AddParameter(insertCmd, ":id", method.Id);
                AddParameter(insertCmd, ":itemId", item.Id);
                AddParameter(insertCmd, ":name", method.Name);
                AddParameter(insertCmd, ":cost", method.Cost);
                AddParameter(insertCmd, ":allowBringOnDay", method.AllowBringOnDay ? 1 : 0);
                AddParameter(insertCmd, ":allowDeliverToUs", method.AllowDeliverToUs ? 1 : 0);
                AddParameter(insertCmd, ":url", method.Url ?? (object)DBNull.Value);
                AddParameter(insertCmd, ":instructions", method.Instructions ?? (object)DBNull.Value);
                AddParameter(insertCmd, ":deliveryCost", method.DeliveryCost);
                insertCmd.ExecuteNonQuery();
            }
            
            existingMethodIds.Remove(method.Id); // Mark as processed
        }
        
        // Delete removed purchase methods
        foreach (var methodId in existingMethodIds)
        {
            var deleteCmd = connection.CreateCommand();
            deleteCmd.Transaction = transaction;
            deleteCmd.CommandText = @"
                DELETE FROM RegistryItemPurchaseMethods
                WHERE Id = :id AND ItemId = :itemId;
            ";
            AddParameter(deleteCmd, ":id", methodId);
            AddParameter(deleteCmd, ":itemId", item.Id);
            deleteCmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }
    
    [Authorize (Roles = "Admin")]
    public bool DeleteItem(string itemId)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();
        var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = @"
            DELETE FROM RegistryItems
            WHERE Id = :id;
        ";
        AddParameter(cmd, ":id", itemId);

        var rowsAffected = cmd.ExecuteNonQuery();
        transaction.Commit();

        return rowsAffected > 0;
    }
    
    public RegistryItem? GetRegistryItemById(string itemId)
    {
        using (DbConnection connection = CreateConnection())
        {
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, GenericName, Name, Description, ImageUrl, MaxQuantity, Priority, Hide, AllowsPartialContributions, IsDonation
                FROM RegistryItems
                WHERE Id = :id;
            ";
            AddParameter(cmd, ":id", itemId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            var id = reader.GetString(0);
            var genericName = reader.GetString(1);
            var name = reader.GetString(2);
            var description = reader.IsDBNull(3) ? null : reader.GetString(3);
            var imageUrl = reader.IsDBNull(4) ? null : reader.GetString(4);
            var maxQuantity = reader.GetInt32(5);
            var priority = reader.GetInt32(6);
            // TODO for postgres
            // var hide = reader.GetBoolean(7);
            // var allowsPartialContributions = reader.GetBoolean(8);
            // var isDonation = reader.GetBoolean(9);

            var hide = reader.GetInt64(7) != 0;
            var allowsPartialContributions = reader.GetInt64(8) != 0;
            var isDonation = reader.GetInt64(9) != 0;

            var purchaseMethods = GetPurchaseMethodsForItem(id);
            var claims = GetClaimsForItem(id, connection);

            return new RegistryItem(
                id,
                genericName,
                name,
                description,
                imageUrl,
                purchaseMethods,
                claims,
                maxQuantity,
                priority,
                hide,
                allowsPartialContributions,
                isDonation
            );
        }
    }

    private List<RegistryItemPurchaseMethod> GetPurchaseMethodsForItem(string itemId, DbConnection? connection = null)
    {
        if (connection == null)
        {
            using (DbConnection conn = CreateConnection())
            {
                conn.Open();
                return GetPurchaseMethodsForItem(itemId, conn);
            }
        }
        var methods = new List<RegistryItemPurchaseMethod>();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, Name, Cost, AllowBringOnDay, AllowDeliverToUs, Url, Instructions, DeliveryCost
            FROM RegistryItemPurchaseMethods
            WHERE ItemId = :itemId;
        ";
        AddParameter(cmd, ":itemId", itemId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var method = new RegistryItemPurchaseMethod(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetDecimal(2),
                reader.GetInt64(3) != 0,
                reader.GetInt64(4) != 0,
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.GetDecimal(7)
            );
            methods.Add(method);
        }

        return methods;
    }
    
    private List<RegistryItemClaim> GetClaimsForItem(string itemId, DbConnection? connection = null)
    {
        if (connection == null)
        {
            using (DbConnection conn = CreateConnection())
            {
                conn.Open();
                return GetClaimsForItem(itemId, conn);
            }
        }
        var claims = new List<RegistryItemClaim>();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT ItemId, ClaimedBy, PurchaseMethodId, DeliveryAddress, ClaimedAt, CompletedAt, Quantity, Notes, Contribution
            FROM RegistryItemClaims
            WHERE ItemId = :itemId;
        ";
        AddParameter(cmd, ":itemId", itemId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var claim = new RegistryItemClaim(
                reader.GetString(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                new DateTime(reader.GetInt64(4), DateTimeKind.Utc),
                reader.IsDBNull(5) ? null : new DateTime(reader.GetInt64(5), DateTimeKind.Utc),
                reader.GetInt32(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.GetDecimal(8)
            );
            claims.Add(claim);
        }

        return claims;
    }
    
    public async Task<IEnumerable<RegistryItem>> GetAllRegistryItems(bool includeHidden = false)
    {
        var items = new List<RegistryItem>();

        await using DbConnection connection = CreateConnection();
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, GenericName, Name, Description, ImageUrl, MaxQuantity, Priority, Hide, AllowsPartialContributions, IsDonation
            FROM RegistryItems
            " + (includeHidden ? "" : "WHERE Hide = 0 ") + @"
            ORDER BY Priority DESC, Name ASC;
        ";

        await using var reader = await cmd.ExecuteReaderAsync();
        while (reader.Read())
        {
            var id = reader.GetString(0);
            var genericName = reader.GetString(1);
            var name = reader.GetString(2);
            var description = reader.IsDBNull(3) ? null : reader.GetString(3);
            var imageUrl = reader.IsDBNull(4) ? null : reader.GetString(4);
            var maxQuantity = reader.GetInt32(5);
            var priority = reader.GetInt32(6);
            // TODO for postgres
            // var hide = reader.GetBoolean(7);
            // var allowsPartialContributions = reader.GetBoolean(8);
            // var isDonation = reader.GetBoolean(9);

            var hide = reader.GetInt64(7) != 0;
            var allowsPartialContributions = reader.GetInt64(8) != 0;
            var isDonation = reader.GetInt64(9) != 0;

            var purchaseMethods = GetPurchaseMethodsForItem(id);
            var claims = GetClaimsForItem(id);

            var item = new RegistryItem(
                id,
                genericName,
                name,
                description,
                imageUrl,
                purchaseMethods,
                claims,
                maxQuantity,
                priority,
                hide,
                allowsPartialContributions,
                isDonation
            );
            items.Add(item);
        }

        return items;
    }
    
    public bool ClaimRegistryItem(string itemId, string userId, decimal contribution, int quantity = 1)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        var item = GetRegistryItemById(itemId);
        if (item is null)
        {
            throw new InvalidOperationException($"No registry item found with ID {itemId}");
        }

        if (item.IsFullyClaimed())
        {
            return false; // Exceeds max quantity
        }

        // Add the claim
        var claimCmd = connection.CreateCommand();
        claimCmd.Transaction = transaction;
        claimCmd.CommandText = @"
            INSERT INTO RegistryItemClaims
            (ItemId, ClaimedBy, PurchaseMethodId, DeliveryAddress, ClaimedAt, CompletedAt, Quantity, Notes, Contribution)
            VALUES
            (:itemId, :claimedBy, NULL, NULL, :claimedAt, NULL, :quantity, NULL, :contribution);
        ";
        AddParameter(claimCmd, ":itemId", itemId);
        AddParameter(claimCmd, ":claimedBy", userId);
        AddParameter(claimCmd, ":claimedAt", DateTime.UtcNow.Ticks);
        AddParameter(claimCmd, ":quantity", quantity);
        AddParameter(claimCmd, ":contribution", contribution);

        claimCmd.ExecuteNonQuery();

        transaction.Commit();
        return true;
    }
    
    public bool UnclaimRegistryItem(string itemId, string userId)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        // Check if claim exists and is not completed
        var checkCmd = connection.CreateCommand();
        checkCmd.Transaction = transaction;
        checkCmd.CommandText = @"
            SELECT CompletedAt
            FROM RegistryItemClaims
            WHERE ItemId = :itemId AND ClaimedBy = :claimedBy;
        ";
        AddParameter(checkCmd, ":itemId", itemId);
        AddParameter(checkCmd, ":claimedBy", userId);

        var completedAtObj = checkCmd.ExecuteScalar();
        if (completedAtObj == null)
        {
            throw new InvalidOperationException($"No claim found for item ID {itemId} by user {userId}");
        }
        if (completedAtObj != DBNull.Value)
        {
            return false; // Claim already completed
        }

        // Delete the claim
        var deleteCmd = connection.CreateCommand();
        deleteCmd.Transaction = transaction;
        deleteCmd.CommandText = @"
            DELETE FROM RegistryItemClaims
            WHERE ItemId = :itemId AND ClaimedBy = :claimedBy;
        ";
        AddParameter(deleteCmd, ":itemId", itemId);
        AddParameter(deleteCmd, ":claimedBy", userId);

        var rowsAffected = deleteCmd.ExecuteNonQuery();

        transaction.Commit();
        return rowsAffected == 1;
    }
    
    public void ChoosePurchaseMethod(string itemId, string userId, string? purchaseMethodId)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();

        var updateCmd = connection.CreateCommand();
        updateCmd.CommandText = @"
            UPDATE RegistryItemClaims
            SET PurchaseMethodId = :purchaseMethodId
            WHERE ItemId = :itemId AND ClaimedBy = :claimedBy;
        ";
        AddParameter(updateCmd, ":purchaseMethodId", purchaseMethodId ?? (object)DBNull.Value);
        AddParameter(updateCmd, ":itemId", itemId);
        AddParameter(updateCmd, ":claimedBy", userId);

        var rowsAffected = updateCmd.ExecuteNonQuery();
        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"No claim found for item ID {itemId} by user {userId}");
        }
    }

    public void ChooseDeliveryAddress(string itemId, string userId, string? address)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();

        var updateCmd = connection.CreateCommand();
        updateCmd.CommandText = @"
            UPDATE RegistryItemClaims
            SET DeliveryAddress = :address
            WHERE ItemId = :itemId AND ClaimedBy = :claimedBy;
        ";
        AddParameter(updateCmd, ":address", address ?? (object)DBNull.Value);
        AddParameter(updateCmd, ":itemId", itemId);
        AddParameter(updateCmd, ":claimedBy", userId);
        var rowsAffected = updateCmd.ExecuteNonQuery();
        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"No claim found for item ID {itemId} by user {userId}");
        }
    }
    
    public void MarkClaimAsCompleted(string itemId, string userId)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();

        var updateCmd = connection.CreateCommand();
        updateCmd.CommandText = @"
            UPDATE RegistryItemClaims
            SET CompletedAt = :completedAt
            WHERE ItemId = :itemId AND ClaimedBy = :claimedBy AND CompletedAt IS NULL;
        ";
        AddParameter(updateCmd, ":completedAt", DateTime.UtcNow.Ticks);
        AddParameter(updateCmd, ":itemId", itemId);
        AddParameter(updateCmd, ":claimedBy", userId);

        var rowsAffected = updateCmd.ExecuteNonQuery();
        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"No uncompleted claim found for item ID {itemId} by user {userId}");
        }
    }

    [Authorize(Roles = "Admin")]
    public void MarkClaimAsNotCompleted(string itemId, string userId)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();

        var updateCmd = connection.CreateCommand();
        updateCmd.CommandText = @"
            UPDATE RegistryItemClaims
            SET CompletedAt = NULL
            WHERE ItemId = :itemId AND ClaimedBy = :claimedBy AND CompletedAt IS NOT NULL;
        ";
        AddParameter(updateCmd, ":itemId", itemId);
        AddParameter(updateCmd, ":claimedBy", userId);

        var rowsAffected = updateCmd.ExecuteNonQuery();
        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"No completed claim found for item ID {itemId} by user {userId}");
        }
    }

    public void SetClaimNotes(string itemId, string userId, string? notes)
    {
        using DbConnection connection = CreateConnection();
        connection.Open();

        var updateCmd = connection.CreateCommand();
        updateCmd.CommandText = @"
            UPDATE RegistryItemClaims
            SET Notes = :notes
            WHERE ItemId = :itemId AND ClaimedBy = :claimedBy;
        ";
        AddParameter(updateCmd, ":notes", notes ?? (object)DBNull.Value);
        AddParameter(updateCmd, ":itemId", itemId);
        AddParameter(updateCmd, ":claimedBy", userId);

        var rowsAffected = updateCmd.ExecuteNonQuery();
        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"No claim found for item ID {itemId} by user {userId}");
        }
    }
}
