# Layer Separation Architecture

This document outlines the approach for separating concerns between layers using different model types:

## Model Types

1. **Data Models** (in `Data.Structure` namespace)
   - Used for database operations
   - Entity Framework Core entities
   - Located in `Data.Structure/Models.cs`
   - Directly map to database tables

2. **DTOs** (Data Transfer Objects) (in `DTOs` namespace)
   - Used in the service layer
   - Used for data transfer between services
   - Located in `DTOs/` directory

3. **View Models** (in `ViewModels` namespace)
   - Used only in views
   - Tailored specifically for view requirements
   - Located in `ViewModels/` directory

## AutoMapper

AutoMapper is used to map between these different model types:

1. **Data Models <-> DTOs**: For service layer
2. **DTOs <-> View Models**: For presentation layer 

The mapping configurations are defined in `Infrastructure/MappingProfiles.cs`.

## Usage Guidelines

### Controllers

Controllers should:
- Accept ViewModels from views
- Map ViewModels to DTOs for service calls
- Map DTOs back to ViewModels for return to views

```csharp
// Example controller action
public async Task<IActionResult> Index()
{
    // 1. Get data from service (which returns DTOs)
    var examDTOs = await _examService.GetExamsAsync();
    
    // 2. Map to ViewModels (for the view)
    var examViewModels = _mapper.Map<List<ExamViewModel>>(examDTOs);
    
    return View(examViewModels);
}
```

### Services

Services should:
- Accept and return DTOs
- Map DTOs to Data Models for database operations
- Map Data Models back to DTOs for return to controllers

```csharp
// Example service method
public async Task<List<ExamDTO>> GetExamsAsync()
{
    // 1. Get data from database (Data.Structure models)
    var exams = await _context.Exams
        .Include(e => e.Job)
        .OrderByDescending(e => e.CreatedAt)
        .ToListAsync();
    
    // 2. Map to DTOs
    var examDTOs = _mapper.Map<List<ExamDTO>>(exams);
    
    return examDTOs;
}
```

### Views

Views should:
- Only use ViewModels
- Never use Data Models or DTOs directly

```cshtml
@model ExamViewModel

<h1>@Model.Name</h1>
```

## Benefits

This architecture provides:
1. **Separation of concerns**: Each layer has its own model type
2. **Flexibility**: Changes to one layer don't necessarily affect others
3. **Security**: Sensitive data can be excluded from ViewModels
4. **Performance**: Only necessary data is transferred between layers 
