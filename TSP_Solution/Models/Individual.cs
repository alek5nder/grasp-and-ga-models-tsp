using TSP_Solution.Models; // Upewnij się, że to jest
using System.Collections.Generic;
using System.Linq;
using System; // Potrzebne dla Math.Max

namespace TSP_Solution.Models
{
    public class Individual
    {
        // Trasa jako permutacja ZADAŃ (np. [0, 2, 1, 3])
        public List<int> Tour { get; set; }
        
        // Wartość funkcji celu - Cmax (makespan)
        public double Fitness { get; set; }

        public Individual(List<int> tour)
        {
            Tour = tour;
            Fitness = -1; 
        }

        // --- STARA METODA (TSP)
        /*
        public void CalculateFitness(TSPData data)
        {
            double totalDistance = 0;
            for (int i = 0; i < Tour.Count - 1; i++)
            {
                totalDistance += data.GetDistance(Tour[i], Tour[i + 1]);
            }
            totalDistance += data.GetDistance(Tour.Last(), Tour.First());
            Fitness = totalDistance;
        }
        */

        // --- NOWA METODA (PFSP) - ZASTĄPIĆ NIĄ STARĄ ---
        public void CalculateFitness(PFSPData data)
        {
            int numJobs = data.NumberOfJobs;
            int numMachines = data.NumberOfMachines;

            // Tworzymy tablicę czasów zakończenia [zadanie, maszyna]
            // Uwaga: indeksujemy ją 0..numJobs-1, 0..numMachines-1
            double[,] completionTimes = new double[numJobs, numMachines];
            
            // Pętla po maszynach (kolumny)
            for (int j = 0; j < numMachines; j++)
            {
                // Pętla po zadaniach (wiersze, wg kolejności z permutacji Tour)
                for (int i = 0; i < numJobs; i++)
                {
                    // Pobierz ID zadania z permutacji
                    int currentJobId = Tour[i]; 

                    // Pobierz czas przetwarzania tego zadania na tej maszynie
                    double processingTime = data.GetProcessingTime(currentJobId, j);

                    // Czas zakończenia na poprzedniej maszynie (ten sam job)
                    double completionOnPrevMachine = (j == 0) ? 0 : completionTimes[i, j - 1];
                    
                    // Czas zakończenia poprzedniego zadania (ta sama maszyna)
                    double completionOfPrevJob = (i == 0) ? 0 : completionTimes[i - 1, j];
                    
                    // Wzór na Cmax
                    completionTimes[i, j] = Math.Max(completionOnPrevMachine, completionOfPrevJob) + processingTime;
                }
            }

            // Fitness to Cmax, czyli czas zakończenia ostatniego zadania na ostatniej maszynie
            Fitness = completionTimes[numJobs - 1, numMachines - 1];
        }


        // Ta funkcja jest uniwersalna dla permutacji - NIE ZMIENIAĆ
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
    }
}