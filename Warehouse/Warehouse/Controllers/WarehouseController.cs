using Microsoft.AspNetCore.Mvc;
using Warehouse.Models;
using Warehouse.Repositories;

namespace Warehouse.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WarehouseController : ControllerBase
{
    private readonly IWarehouseRepository _warehouseRepository;

    public WarehouseController(IWarehouseRepository warehouseRepository)
    {
        _warehouseRepository = warehouseRepository;
    }

    [HttpPost("addProduct")]
    public async Task<IActionResult> AddProductToWarehouse(WarehouseRequest request)
    {
        try
        {
            var insertedId = await _warehouseRepository.AddToWarehouse(request);
            return Ok(insertedId);
        }
        catch (Exception ex)
        {
            if (ex is InvalidOperationException || ex is ArgumentException)
            {
                return BadRequest(ex.Message);
            }
            else if (ex is UnauthorizedAccessException)
            {
                return Unauthorized(ex.Message);
            }
            else
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
    
}