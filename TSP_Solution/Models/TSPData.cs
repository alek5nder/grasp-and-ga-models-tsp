namespace TSP_Solution.Models
{
    public class TSPData
    {
        public double[,] DistanceMatrix { get; }
        public int NumberOfCities { get; }

        public TSPData(double[,] distanceMatrix)
        {
            DistanceMatrix = distanceMatrix;
            NumberOfCities = distanceMatrix.GetLength(0);
        }

        // Pobiera dystans między dwoma miastami (indeksowanymi od 0)
        public double GetDistance(int city1, int city2)
        {
            return DistanceMatrix[city1, city2];
        }
    }
}