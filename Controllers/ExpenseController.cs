using System.Security.Claims;
using Impensa.DTOs.ExpenseCategories;
using Impensa.DTOs.Expenses;
using Impensa.Models;
using Impensa.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Impensa.Controllers;

[Authorize]
[ApiController]
[Route("/api/v1/expenses")]
public class ExpenseController : ControllerBase
{
    private readonly AppDbContext _context;

    public ExpenseController(AppDbContext context)
    {
        _context = context;
    }

    private Guid GetUserIdFromJwt()
    {
        var guid = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(guid)) throw new ArgumentException("User id not found in JWT token");

        return Guid.Parse(guid);
    }

    private static ExpenseResponseDto MapExpenseToResponseDto(Expense e)
    {
        return new ExpenseResponseDto
        {
            Id = e.Id,
            Amount = e.Amount,
            Description = e.Description,
            Date = e.Date,
            ExpenseCategory = new ExpenseCategoryResponseDto
            {
                Id = e.Category!.Id,
                Name = e.Category.Name
            }
        };
    }

    private static Expense MapExpenseRequestDtoToExpense(ExpenseRequestDto expenseDto, Guid userId)
    {
        return new Expense
        {
            Amount = expenseDto.Amount,
            Description = expenseDto.Description,
            Date = expenseDto.Date,
            ExpenseCategoryId = expenseDto.ExpenseCategoryId,
            UserId = userId
        };
    }

    [HttpGet]
    public async Task<List<ExpenseResponseDto>> GetAllExpenses()
    {
        var userId = GetUserIdFromJwt();

        var expenses = await _context.Expenses
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.Date)
            .Select(e => MapExpenseToResponseDto(e))
            .ToListAsync();

        return expenses;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExpenseResponseDto>> GetExpense(Guid id)
    {
        var userId = GetUserIdFromJwt();

        var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (expense == null) return NotFound();

        return MapExpenseToResponseDto(expense);
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseResponseDto>> CreateExpense(ExpenseRequestDto expenseDto)
    {
        var userId = GetUserIdFromJwt();
        var expense = MapExpenseRequestDtoToExpense(expenseDto, userId);

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();

        return MapExpenseToResponseDto(expense);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ExpenseResponseDto>> UpdateExpense(Guid id, ExpenseRequestDto dto)
    {
        var userId = GetUserIdFromJwt();

        var existingExpense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (existingExpense == null) return NotFound();

        var updatedExpense = MapExpenseRequestDtoToExpense(dto, userId);
        updatedExpense.Id = existingExpense.Id;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExpense(Guid id)
    {
        var userId = GetUserIdFromJwt();

        var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (expense == null) return NotFound();

        _context.Expenses.Remove(expense);
        await _context.SaveChangesAsync();

        return Ok();
    }
}