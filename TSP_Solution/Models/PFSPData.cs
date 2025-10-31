namespace TSP_Solution.Models
{
    public class PFSPData
    {
  
        public int[,] ProcessingTimes { get; } 
        public int NumberOfJobs { get; }
        public int NumberOfMachines { get; }

        public PFSPData(int[,] processingTimes)
        {
            ProcessingTimes = processingTimes;
            NumberOfJobs = processingTimes.GetLength(0);
            NumberOfMachines = processingTimes.GetLength(1);
        }

        // Pobiera czas przetwarzania dla konkretnego zadania (indeks od 0)
        // i konkretnej maszyny (indeks od 0)
        public int GetProcessingTime(int jobId, int machineId)
        {
            return ProcessingTimes[jobId, machineId];
        }
    }
}