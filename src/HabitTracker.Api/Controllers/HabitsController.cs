using System.Dynamic;
using System.Linq.Expressions;
using FluentValidation;
using FluentValidation.Results;
using HabitTracker.Api.Database;
using HabitTracker.Api.DTOs;
using HabitTracker.Api.DTOs.Common;
using HabitTracker.Api.DTOs.Habits;
using HabitTracker.Api.Entities;
using HabitTracker.Api.Services;
using HabitTracker.Api.Services.Sorting;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Api.Controllers;

[ApiController]
[Route("habits")]
public sealed class HabitsController(ApplicationDbContext dbContext, LinkService linkService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginationResult<HabitDto>>> GetHabits(
        [FromQuery] HabitQueryParameters queryParameters,
        SortMappingProvider sortMappingProvider,
        DataShapingService shapingService)
    {
        string? search = queryParameters.Search?.Trim().ToLower();

        if(!sortMappingProvider.ValidateMappings<HabitDto, Habit>(queryParameters.Sort) )
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"the provided sort parameter is not valid: '{queryParameters.Sort}'");
        }

        if(!shapingService.Validate<HabitDto>(queryParameters.Fields))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"the provided data shaping fields are not valid: '{queryParameters.Fields}'");
        }

        SortMapping[] sortMappings = sortMappingProvider.GetMappings<HabitDto, Habit>();

        var habitsQuery = dbContext.Habits
            .Where(h => search == null || h.Name.ToLower().Contains(search) || h.Description != null && h.Description.ToLower().Contains(search))
            .Where(h => queryParameters.Type == null || h.Type == queryParameters.Type)
            .Where(h => queryParameters.Status == null || h.Status == queryParameters.Status)
            .ApplySort(queryParameters.Sort, sortMappings)
            .Select(HabitQueries.ProjectToDto());

        int count = await habitsQuery.CountAsync();

        List<HabitDto> habitsDto = await habitsQuery.ToListAsync();

        List<ExpandoObject> shapedHabits = shapingService.ShapeDataCollection(habitsDto, queryParameters.Fields, h => CreateLinksForHabit(h.Id, queryParameters.Fields));

        var paginationResult = PaginationResult<ExpandoObject>.Create(shapedHabits, queryParameters.Page, queryParameters.PageSize, count);

        paginationResult.Links = CreateLinksForHabit(queryParameters, paginationResult.HasNextPage, paginationResult.HasPreviousPage);

        return Ok(paginationResult);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetHabit(string id, string? fields, DataShapingService shapingService)
    {
        if (!shapingService.Validate<HabitWithTagsDto>(fields))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"the provided data shaping fields are not valid: '{fields}'");
        }

        HabitWithTagsDto? habit = await dbContext
            .Habits
            .Where(h => h.Id == id)
            .Select(HabitQueries.ProjectToDtoWithTags())
            .FirstOrDefaultAsync();

        if (habit is null)
        {
            return NotFound();
        }

        ExpandoObject result = shapingService.ShapeData(habit, fields);

        List<LinkDto> links = CreateLinksForHabit(id, fields);

        result.TryAdd("links", links);

        return Ok(result);
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

        habitDto.Links = CreateLinksForHabit(habit.Id, null);

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

    private List<LinkDto> CreateLinksForHabit(HabitQueryParameters queryParameters, bool hasNext, bool hasPrev)
    {
        List<LinkDto> links =
        [
            linkService.Create(nameof(GetHabits), "self", HttpMethods.Get, new { 
                page = queryParameters.Page,
                pageSize = queryParameters.PageSize,
                fields = queryParameters.Fields,
                q = queryParameters.Search,
                sort = queryParameters.Sort,
                type = queryParameters.Type,
                status = queryParameters.Status
            }),
             linkService.Create(nameof(CreateHabit), "create", HttpMethods.Post)
        ];
         
        if(hasNext)
        {
            links.Add(linkService.Create(nameof(GetHabits), "next-page", HttpMethods.Get, new
            {
                page = queryParameters.Page + 1,
                pageSize = queryParameters.PageSize,
                fields = queryParameters.Fields,
                q = queryParameters.Search,
                sort = queryParameters.Sort,
                type = queryParameters.Type,
                status = queryParameters.Status
            }));
        }

        if (hasPrev)
        {
            links.Add(linkService.Create(nameof(GetHabits), "previous-page", HttpMethods.Get, new
            {
                page = queryParameters.Page - 1,
                pageSize = queryParameters.PageSize,
                fields = queryParameters.Fields,
                q = queryParameters.Search,
                sort = queryParameters.Sort,
                type = queryParameters.Type,
                status = queryParameters.Status
            }));
        }

        return links;
    }

    private List<LinkDto> CreateLinksForHabit(string id, string? fields)
    {
        return [
            linkService.Create(nameof(GetHabit), "self", HttpMethods.Get, new { id, fields }),
            linkService.Create(nameof(UpdateHabit), "update", HttpMethods.Put, new { id }),
            linkService.Create(nameof(PatchHabit), "partial-update", HttpMethods.Patch, new { id }),
            linkService.Create(nameof(DeleteHabit), "delete", HttpMethods.Delete, new { id }),
            linkService.Create(nameof(HabitTagController.UpsertHabitTags), "upsert-tags", HttpMethods.Put, new { habitId = id }, HabitTagController.Name)
        ];
    }
}
