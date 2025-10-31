using System;
using System.Collections.Generic;
using System.Linq;
using TSP_Solution.Models;

namespace TSP_Solution.Algorithms
{
    public class NehAlgorithm
    {
        private readonly PFSPData _data;
        private readonly int _numJobs;
        private readonly int _numMachines;

        public NehAlgorithm(PFSPData data)
        {
            _data = data;
            _numJobs = data.NumberOfJobs;
            _numMachines = data.NumberOfMachines;
        }

        /// <summary>
        /// Uruchamia algorytm NEH i zwraca go jako gotowego 'Osobnika'
        /// </summary>
        public Individual Run()
        {
            // 1. Oblicz sumę czasów (priorytet) dla każdego zadania
            var jobPriorities = new List<(int jobId, int totalTime)>();
            for (int i = 0; i < _numJobs; i++)
            {
                int sum = 0;
                for (int j = 0; j < _numMachines; j++)
                {
                    sum += _data.GetProcessingTime(i, j);
                }
                jobPriorities.Add((i, sum));
            }

            // 2. Sortuj zadania malejąco wg sumy czasów
            var sortedJobs = jobPriorities.OrderByDescending(p => p.totalTime).Select(p => p.jobId).ToList();

            // 3. Buduj harmonogram iteracyjnie
            List<int> bestTour = new List<int>();
            foreach (int jobIdToInsert in sortedJobs)
            {
                bestTour = FindBestInsertionPosition(bestTour, jobIdToInsert);
            }

            // 4. Zwróć finalną trasę (permutację)
            return new Individual(bestTour);
        }

        /// <summary>
        /// Znajduje najlepszą pozycję do wstawienia zadania w istniejącej trasie
        /// </summary>
        private List<int> FindBestInsertionPosition(List<int> currentTour, int jobToInsert)
        {
            double bestCmax = double.MaxValue;
            List<int> bestTourForThisJob = null;

            // Sprawdź każdą możliwą pozycję wstawienia (w tym na początku i na końcu)
            for (int i = 0; i <= currentTour.Count; i++)
            {
                List<int> tempTour = new List<int>(currentTour);
                tempTour.Insert(i, jobToInsert);

                // Oblicz Cmax dla tej tymczasowej, częściowej permutacji
                double currentCmax = CalculatePartialCmax(tempTour);

                if (currentCmax < bestCmax)
                {
                    bestCmax = currentCmax;
                    bestTourForThisJob = tempTour;
                }
            }
            return bestTourForThisJob;
        }

        /// <summary>
        /// Oblicza Cmax dla częściowej trasy (kluczowe dla NEH)
        /// </summary>
        private double CalculatePartialCmax(List<int> partialTour)
        {
            int numJobs = partialTour.Count; // Tylko tyle zadań, ile jest w trasie
            if (numJobs == 0) return 0;

            double[,] completionTimes = new double[numJobs, _numMachines];

            for (int j = 0; j < _numMachines; j++)
            {
                for (int i = 0; i < numJobs; i++)
                {
                    int currentJobId = partialTour[i];
                    double processingTime = _data.GetProcessingTime(currentJobId, j);

                    double completionOnPrevMachine = (j == 0) ? 0 : completionTimes[i, j - 1];
                    double completionOfPrevJob = (i == 0) ? 0 : completionTimes[i - 1, j];

                    completionTimes[i, j] = Math.Max(completionOnPrevMachine, completionOfPrevJob) + processingTime;
                }
            }
            // Zwróć Cmax (ostatnie zadanie, ostatnia maszyna)
            return completionTimes[numJobs - 1, _numMachines - 1];
        }
    }
}