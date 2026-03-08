using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Application.DTOs;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Interfaces;

namespace PropertyManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionRepository _txnRepo;

    public TransactionsController(ITransactionRepository txnRepo)
    {
        _txnRepo = txnRepo;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<Transaction>>> GetTransactions(
        [FromQuery] int? propertyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string sortBy = "TransactionDate",
        [FromQuery] bool sortDesc = true)
    {
        var (items, totalCount) = await _txnRepo.GetPagedTransactionsAsync(
            propertyId, page, pageSize, category, search, startDate, endDate, sortBy, sortDesc);

        return Ok(new PagedResultDto<Transaction>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<string>>> GetCategories()
    {
        var categories = await _txnRepo.GetDistinctCategoriesAsync();
        return Ok(categories);
    }

    [HttpPost]
    public async Task<ActionResult> CreateTransaction([FromBody] Transaction txn)
    {
        var id = await _txnRepo.CreateTransactionAsync(txn);
        txn.TransactionId = id;
        return Created($"api/transactions/{id}", txn);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateTransaction(int id, [FromBody] Transaction txn)
    {
        txn.TransactionId = id;
        var ok = await _txnRepo.UpdateTransactionAsync(txn);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTransaction(int id)
    {
        var ok = await _txnRepo.DeleteTransactionAsync(id);
        return ok ? NoContent() : NotFound();
    }
}
