namespace Kromer.Models.Api.Krist.Misc;

public class KristWhatsNewResponse : KristResult
{
    public IEnumerable<Commit> Commits { get; set; }

    public class Commit
    {

    }
}