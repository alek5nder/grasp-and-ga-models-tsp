namespace TSP_Solution.Models
{
    public class ResultRecord
    {
        public string Algorithm { get; set; }
        public string InstanceName { get; set; }
        public string Parameters { get; set; } // np. "Pop:100,Mut:0.1,CX:PMX"
        public double BestDistance { get; set; }
        public double AvgDistance { get; set; } // 
        public double TimeInSeconds { get; set; } // 
        public string BestTour { get; set; } // np. "0-1-2-3-4"
    }
}