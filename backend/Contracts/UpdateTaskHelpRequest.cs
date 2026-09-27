namespace backend.Contracts;

public class UpdateTaskHelpRequest
{
    // Nullable so we can distinguish missing input from false.
    public bool? NeedsHelp { get; set; }
}