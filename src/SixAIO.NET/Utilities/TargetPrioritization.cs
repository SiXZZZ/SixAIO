using System.Collections.Generic;

namespace SixAIO.Utilities
{
    internal class TargetSelection
    {
        public List<TargetPrioritization> TargetPrioritizations { get; set; } = new List<TargetPrioritization>();
    }
    internal class TargetPrioritization
    {
        public string Champion { get; set; }

        private int prioritization = 0;

        public int Prioritization
        {
            get => prioritization;
            set
            {
                if (value <= 5 && value >= 0)
                {
                    prioritization = value;
                }
            }
        }

        public override string ToString()
        {
            return $"{Champion} has Prioritization: {Prioritization}";
        }
    }
}
