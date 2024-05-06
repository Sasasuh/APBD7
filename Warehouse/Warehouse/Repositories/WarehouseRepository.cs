using System.Data;
using Microsoft.Data.SqlClient;
using Warehouse.Models;

namespace Warehouse.Repositories;

public class WarehouseRepository : IWarehouseRepository
{
    private readonly IConfiguration _configuration;

    public WarehouseRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<int> AddToWarehouse(WarehouseRequest request)
    {
        if (!await ProductExists(request.IdProduct))
        {
            throw new Exception("Product with given Id does not exist.");
        }

        if (!await WarehouseExists(request.IdWarehouse))
        {
            throw new Exception("Warehouse with given Id does not exist.");
        }

        if (!await OrderExists(request.IdProduct, request.Amount, request.CreatedAt))
        {
            throw new Exception("Order for the product with given Id does not exist or the conditions are not met.");
        }

        if (await OrderFulfilled(request.IdProduct))
        {
            throw new Exception("Order has already been fulfilled.");
        }

        await UpdateOrderFulfilledAt(request.IdProduct);

        var insertedId = await InsertProductWarehouse(request);

        return insertedId;
    }

    public async Task<bool> ProductExists(int id)
    {
        var query = "SELECT 1 FROM Product WHERE IdProduct = @ID";

        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        command.CommandText = query;
        command.Parameters.AddWithValue("@ID", id);

        await connection.OpenAsync();

        var res = await command.ExecuteScalarAsync();

        return res is not null;
    }

    public async Task<bool> WarehouseExists(int id)
    {
        var query = "SELECT 1 FROM Warehouse WHERE IdWarehouse = @ID";

        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        command.CommandText = query;
        command.Parameters.AddWithValue("@ID", id);

        await connection.OpenAsync();

        var res = await command.ExecuteScalarAsync();

        return res is not null;
    }

    public async Task<bool> OrderExists(int id, int amount, DateTime orderCreatedDate)
    {
        var query =
            "SELECT 1 FROM [Order] WHERE IdProduct = @idProduct AND Amount >= @amount AND CreatedAt < @orderCreatedDate";

        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        command.CommandText = query;
        command.Parameters.AddWithValue("@idProduct", id);
        command.Parameters.AddWithValue("@amount", amount);
        command.Parameters.AddWithValue("@orderCreatedDate", orderCreatedDate);

        await connection.OpenAsync();

        var res = await command.ExecuteScalarAsync();

        return res is not null;
    }

    public async Task<bool> OrderFulfilled(int id)
    {
        var query = "SELECT 1 FROM [Order] WHERE IdOrder = @IdOrder AND FulfilledAt IS NOT NULL";

        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        command.CommandText = query;
        command.Parameters.AddWithValue("@IdOrder", id);

        await connection.OpenAsync();

        var res = await command.ExecuteScalarAsync();
        return res is not null;
    }

    public async Task UpdateOrderFulfilledAt(int id)
    {
        var query = "UPDATE [Order] SET FulfilledAt = GETDATE() WHERE IdProduct = @IdProduct AND FulfilledAt IS NULL";

        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        command.CommandText = query;
        command.Parameters.AddWithValue("@IdProduct", id);

        await connection.OpenAsync();

        await command.ExecuteNonQueryAsync();
    }

    public async Task<int> InsertProductWarehouse(WarehouseRequest request)
    {
        //Pobieramy price produktu oraz wyliczamy totalprice (price * amount)
        var priceQuery = "SELECT Price FROM Product WHERE IdProduct = @IdProduct";

        await using SqlConnection priceConnection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand priceCommand = new SqlCommand();

        priceCommand.Connection = priceConnection;
        priceCommand.CommandText = priceQuery;
        priceCommand.Parameters.AddWithValue("@IdProduct", request.IdProduct);

        await priceConnection.OpenAsync();

        var price = (decimal)await priceCommand.ExecuteScalarAsync();
        var totalPrice = price * request.Amount;

        //INSERT
        var insertQuery = "INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)" +
                          "VALUES (@IdWarehouse, @IdInsertProduct, @IdOrder, @Amount, @Price, @CreatedAt); SELECT SCOPE_IDENTITY();";

        await using SqlConnection insertConnection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand insertCommand = new SqlCommand();

        priceCommand.Connection = insertConnection;
        priceCommand.CommandText = insertQuery;

        //DODAJE PARAMETRY DO QUERY
        priceCommand.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
        priceCommand.Parameters.AddWithValue("@IdInsertProduct", request.IdProduct);
        priceCommand.Parameters.AddWithValue("@IdOrder", request.IdProduct);
        priceCommand.Parameters.AddWithValue("@Amount", request.Amount);
        priceCommand.Parameters.AddWithValue("@Price", totalPrice);
        priceCommand.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

        await insertConnection.OpenAsync();

        return Convert.ToInt32(await priceCommand.ExecuteScalarAsync());
    }
}