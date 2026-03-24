namespace HTrack.Api.Exceptions;

public class EmployeeWithUIDAlreadyExistsException(string uid)
    : Exception($"Employee with uid: {uid} already exists")
{
    public string UID { get; set; } = uid;
}
