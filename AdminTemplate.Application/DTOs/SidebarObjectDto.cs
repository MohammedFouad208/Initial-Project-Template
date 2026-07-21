namespace AdminTemplate.Application.DTOs;

public record SidebarObjectDto(
    string Name,
    string DisplayName,
    string Icon,
    string Controller,
    IReadOnlyList<string> Functions);
