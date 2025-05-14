namespace HTrack.Api.Exceptions;

public class EmployeeWithUIDNotFoundException(string uid) 
    : Exception($"Employee with uid: {uid} is not found")
{ 
    public string UID { get; set; } = uid;
}