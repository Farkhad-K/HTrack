namespace HTrack.Api.Exceptions;

public class CompanyNotFoundException(Guid id) 
    : Exception($"Company with id: {id} is not found")
{
    public Guid Id { get; set; } = id;
}