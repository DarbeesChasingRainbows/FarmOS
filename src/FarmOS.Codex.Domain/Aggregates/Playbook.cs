using FarmOS.Codex.Domain.Events;
using FarmOS.SharedKernel;

namespace FarmOS.Codex.Domain.Aggregates;

public sealed class Playbook : AggregateRoot<PlaybookId>
{
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public AudienceRole Audience { get; private set; }
    private readonly List<PlaybookTask> _tasks = [];
    public IReadOnlyList<PlaybookTask> Tasks => _tasks;

    public static Playbook Create(string title, string? description, AudienceRole audience)
    {
        var playbook = new Playbook();
        playbook.RaiseEvent(new PlaybookCreated(PlaybookId.New(), title, description, audience, DateTimeOffset.UtcNow));
        return playbook;
    }

    public void AddTask(PlaybookTask task) =>
        RaiseEvent(new PlaybookTaskAdded(Id, task, DateTimeOffset.UtcNow));

    public Result<PlaybookId, DomainError> RemoveTask(int month, string taskTitle)
    {
        var existing = _tasks.FirstOrDefault(t => t.Month == month && t.Title == taskTitle);
        if (existing is null)
            return DomainError.NotFound("PlaybookTask", $"{month}:{taskTitle}");
        RaiseEvent(new PlaybookTaskRemoved(Id, month, taskTitle, DateTimeOffset.UtcNow));
        return Id;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case PlaybookCreated e: Id = e.Id; Title = e.Title; Description = e.Description; Audience = e.Audience; break;
            case PlaybookTaskAdded e: _tasks.Add(e.Task); break;
            case PlaybookTaskRemoved e: _tasks.RemoveAll(t => t.Month == e.Month && t.Title == e.TaskTitle); break;
        }
    }
}
