using System.Collections.Generic;

namespace Solink.AddIn.Helpers.Test
{
    public interface IThing
    {
        int ComputeAnswerToLifeAndUniverseEverything();
        void AddToList(IList<string> strings);
        int Id { get; set; }
    }
}