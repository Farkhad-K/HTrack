namespace HTrack.Api.Exceptions;

public class EmployeeNotFoundException(Guid id) 
    : Exception($"Employee with id: {id} is not found")
{
    public Guid Id { get; set; } = id;
}