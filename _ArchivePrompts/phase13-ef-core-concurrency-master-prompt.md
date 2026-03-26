# Master Prompt: Phase 12.4 — EF Core Concurrency Resolution

Role: You are a Senior .NET Backend Developer and Entity Framework Core expert.

Context: I am getting `Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException` (0 rows affected) in my ASP.NET Core application. The error occurs during `UpdateAsync` calls in my Domain Services.

## The Problem
I have identified that my Services (`SubmissionService`, `TaskService`) are creating new instances of entities with existing IDs and passing them to the repository to update, rather than mapping properties onto the already-tracked entities loaded from the database. This is causing the EF Core Change Tracker to collide or lose track of navigation properties (like `FeedbackRecord`), leading to unexpected `DELETE` commands and concurrency failures.

## Tasks

### 1. Refactor SubmissionService.SubmitCodeAsync
Ensure that the `FeedbackRecord` navigation property is never replaced with a new object if it already exists. Instead, map the properties (`IsSuccess`, `Summary`, etc.) onto the existing `submission.Feedback` object.

### 2. Refactor TaskService.UpdateTaskAsync
Instead of creating a new `TaskDefinition`, map the `UpdateTaskDto` properties onto the entity returned by the repository.

### 3. Consistency Check
Review the `FeedbackService` to ensure it follows the "Load -> Map -> Save" pattern correctly.

### 4. Repository Pattern
Suggest the safest implementation for `UpdateAsync(T entity)` in my `EfRepository` to handle tracked vs. untracked entities without causing identity conflicts. Consider using `context.Entry(entity).State = EntityState.Modified` or `context.Set<T>().Update(entity)` vs property-by-property mapping.

## Goal
Eliminate all `DbUpdateConcurrencyException` errors caused by entity replacement and ensure the EF Core Change Tracker remains in a consistent state throughout the request lifecycle.
