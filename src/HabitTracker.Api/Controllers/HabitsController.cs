using System.Linq.Expressions;
using FluentValidation;
using FluentValidation.Results;
using HabitTracker.Api.Database;
using HabitTracker.Api.DTOs;
using HabitTracker.Api.DTOs.Habits;
using HabitTracker.Api.Entities;
using HabitTracker.Api.Services.Sorting;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Api.Controllers;

[ApiController]
[Route("habits")]
public sealed class HabitsController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<HabitsCollectionDto>> GetHabits([FromQuery] HabitQueryParameters habitQuery, SortMappingProvider sortMappingProvider)
    {
        string? search = habitQuery.Search?.Trim().ToLower();

        if(!sortMappingProvider.ValidateMappings<HabitDto, Habit>(habitQuery.Sort) )
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"the provided sort parameter is not valid: '{habitQuery.Sort}'");
        }

        SortMapping[] sortMappings = sortMappingProvider.GetMappings<HabitDto, Habit>();

        List<HabitDto> habits = await dbContext.Habits
            .Where(h => search == null || h.Name.ToLower().Contains(search) || h.Description != null && h.Description.ToLower().Contains(search))
            .Where(h => habitQuery.Type == null || h.Type == habitQuery.Type)
            .Where(h => habitQuery.Status == null || h.Status == habitQuery.Status)
            .ApplySort(habitQuery.Sort, sortMappings)
            .Select(HabitQueries.ProjectToDto())
            .ToListAsync();

        HabitsCollectionDto habitsCollection = new()
        {
            Data = habits
        };

        return Ok(habitsCollection);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<HabitWithTagsDto>> GetHabit(string id)
    {
        HabitWithTagsDto? habit = await dbContext
            .Habits
            .Where(h => h.Id == id)
            .Select(HabitQueries.ProjectToDtoWithTags())
            .FirstOrDefaultAsync();

        if(habit is null)
        {
            return NotFound();
        }

        return Ok(habit);
    }

    [HttpPost]
    public async Task<ActionResult<HabitDto>> CreateHabit(CreateHabitDto createHabit, IValidator<CreateHabitDto> validator, ProblemDetailsFactory problemDetailsFactory)
    {
        ValidationResult validationResult = await validator.ValidateAsync(createHabit);

        if(!validationResult.IsValid)
        {
            ProblemDetails problemDetails = problemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status400BadRequest);
            problemDetails.Extensions.Add("errors", validationResult.ToDictionary());
            return BadRequest(problemDetails);
        }

        Habit habit = createHabit.ToEntity();

        dbContext.Habits.Add(habit);

        await dbContext.SaveChangesAsync();

        HabitDto habitDto = habit.ToDto();

        return CreatedAtAction(nameof(GetHabit), new { id = habitDto.Id }, habitDto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateHabit(string id, [FromBody] UpdateHabitDto dto)
    {
        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);

        if (habit is null)
        {
            return NotFound();
        }

        habit.UpdateFromDto(dto);

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult> PatchHabit(string id, [FromBody] JsonPatchDocument<HabitDto> patchDocument)
    {
        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);

        if (habit is null)
        {
            return NotFound();
        }

        HabitDto dto = habit.ToDto();

        patchDocument.ApplyTo(dto, ModelState);

        if(!TryValidateModel(dto))
        {
            return ValidationProblem();
        }

        habit.Name = dto.Name;
        habit.Description = dto.Description;
        habit.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHabit(string id)
    {
        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);

        if (habit is null)
        {
            return NotFound();
        }

        dbContext.Habits.Remove(habit);

        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}
