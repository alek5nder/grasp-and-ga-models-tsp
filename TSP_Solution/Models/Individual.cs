using TSP_Solution.Models;
using System.Collections.Generic;
using System.Linq;

namespace TSP_Solution.Models
{
    public class Individual
    {
        // Trasa jako permutacja miast (np. [0, 2, 1, 3])
        public List<int> Tour { get; set; }
        
        // Wartość funkcji celu - całkowity dystans
        public double Fitness { get; private set; } 

        public Individual(List<int> tour)
        {
            Tour = tour;
            Fitness = -1; // -1 oznacza "jeszcze nieobliczone"
        }

        // Oblicza całkowity dystans (fitness) dla tej trasy
        public void CalculateFitness(TSPData data)
        {
            double totalDistance = 0;
            for (int i = 0; i < Tour.Count - 1; i++)
            {
                totalDistance += data.GetDistance(Tour[i], Tour[i + 1]);
            }
            // Dodaj dystans od ostatniego miasta z powrotem do pierwszego
            totalDistance += data.GetDistance(Tour.Last(), Tour.First());
            Fitness = totalDistance;
        }

        // Tworzy losowego osobnika (losową trasę)
        public static Individual CreateRandom(int numberOfCities, Random rand)
        {
            var tour = Enumerable.Range(0, numberOfCities).ToList();

            // Tasowanie Fisher-Yates
            for (int i = tour.Count - 1; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                (tour[i], tour[j]) = (tour[j], tour[i]); // Zamiana
            }
            return new Individual(tour);
        }
        public void SetFitness(double fitness)
        {
            Fitness = fitness;
        }
    }
}