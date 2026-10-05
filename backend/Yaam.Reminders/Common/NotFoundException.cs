namespace Yaam.Reminders.Common;

public class NotFoundException(string entityName, Guid id)
    : Exception($"{entityName} with id '{id}' was not found.");
