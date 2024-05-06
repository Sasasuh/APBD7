using Warehouse.Models;

namespace Warehouse.Repositories;

public interface IWarehouseRepository
{
    public Task<bool> ProductExists(int id);
    public Task<bool> WarehouseExists(int id);
    public Task<bool> OrderExists(int id, int amount, DateTime orderCreatedDate);
    public Task<int> AddToWarehouse(WarehouseRequest request);
    public Task<bool> OrderFulfilled(int id);
    public Task UpdateOrderFulfilledAt(int id);
    public Task<int> InsertProductWarehouse(WarehouseRequest request);
}