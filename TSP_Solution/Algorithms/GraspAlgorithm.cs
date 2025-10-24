using TSP_Solution.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TSP_Solution.Algorithms
{
    public class GraspAlgorithm
    {
        private readonly TSPData _data;
        private readonly Random _rand = new Random();

        // --- Parametry GRASP ---
        // Liczba iteracji całego algorytmu (Konstrukcja + Local Search)
        private readonly int _iterations; 
        
        // Współczynnik Alpha (0 = pełna chciwość, 1 = pełna losowość)
        private readonly double _alpha; 
        
        // Rodzaj sąsiedztwa używany w fazie Local Search
        private readonly MutationMethod _neighborhoodType; 

        public GraspAlgorithm(TSPData data, int iterations, double alpha, MutationMethod neighborhoodType)
        {
            _data = data;
            _iterations = iterations;
            _alpha = alpha;
            _neighborhoodType = neighborhoodType;
        }

        /// <summary>
        /// Uruchamia główną pętlę algorytmu GRASP
        /// </summary>
        public Individual Run()
        {
            Individual bestSolutionOverall = null;

            for (int i = 0; i < _iterations; i++)
            {
                // 1. Faza Konstrukcji: Zbuduj losowo-chciwą trasę
                Individual currentSolution = ConstructGreedyRandomizedSolution();
                
                // 2. Faza Przeszukiwania Lokalnego: Popraw trasę do lokalnego optimum
                currentSolution = LocalSearch(currentSolution);

                // 3. Aktualizuj najlepsze znalezione rozwiązanie
                if (bestSolutionOverall == null || currentSolution.Fitness < bestSolutionOverall.Fitness)
                {
                    bestSolutionOverall = currentSolution;
                }
            }
            return bestSolutionOverall;
        }

        /// <summary>
        /// FAZA 1: Buduje trasę używając Listy Ograniczonych Kandydatów (RCL)
        /// </summary>
        private Individual ConstructGreedyRandomizedSolution()
        {
            var tour = new List<int>(_data.NumberOfCities);
            var unvisited = new HashSet<int>(Enumerable.Range(0, _data.NumberOfCities));

            // Zacznij od losowego miasta
            int currentCity = _rand.Next(_data.NumberOfCities);
            tour.Add(currentCity);
            unvisited.Remove(currentCity);

            while (unvisited.Count > 0)
            {
                // 1. Zbuduj listę kandydatów (posortowaną wg kosztu)
                var candidates = unvisited
                    .Select(city => new { City = city, Distance = _data.GetDistance(currentCity, city) })
                    .OrderBy(c => c.Distance)
                    .ToList();

                // 2. Określ próg "chciwości" na podstawie Alpha
                double minDistance = candidates.First().Distance;
                double maxDistance = candidates.Last().Distance;
                double threshold = minDistance + _alpha * (maxDistance - minDistance);

                // 3. Stwórz Listę Ograniczonych Kandydatów (RCL)
                var rcl = candidates.Where(c => c.Distance <= threshold).Select(c => c.City).ToList();

                // 4. Wybierz losowo następne miasto z RCL
                int nextCity = rcl[_rand.Next(rcl.Count)];
                
                tour.Add(nextCity);
                unvisited.Remove(nextCity);
                currentCity = nextCity;
            }

            var individual = new Individual(tour);
            individual.CalculateFitness(_data);
            return individual;
        }

        /// <summary>
        /// FAZA 2: Algorytm wspinaczkowy (Best Improvement Hill Climber)
        /// </summary>
        private Individual LocalSearch(Individual initialSolution)
        {
            var currentSolution = initialSolution;
            bool improvementFound;

            do
            {
                improvementFound = false;
                var bestNeighbor = currentSolution;

                int tourCount = currentSolution.Tour.Count;
                
                // Przeszukaj całe sąsiedztwo (i, j)
                for (int i = 0; i < tourCount; i++)
                {
                    for (int j = i + 1; j < tourCount; j++)
                    {
                        // Stwórz sąsiada wg wybranej metody
                        var neighbor = CreateNeighbor(currentSolution, i, j);
                        neighbor.CalculateFitness(_data);

                        if (neighbor.Fitness < bestNeighbor.Fitness)
                        {
                            bestNeighbor = neighbor;
                            improvementFound = true;
                        }
                    }
                }
                
                currentSolution = bestNeighbor; // Przejdź do najlepszego znalezionego sąsiada
            } 
            while (improvementFound); // Powtarzaj, dopóki znajdujesz poprawę

            return currentSolution;
        }

        /// <summary>
        /// Tworzy sąsiada (nowego osobnika) na podstawie wybranej metody (Swap, Inversion, Scramble)
        /// </summary>
        private Individual CreateNeighbor(Individual solution, int i, int j)
        {
            // Tworzymy głęboką kopię trasy
            var newTour = new List<int>(solution.Tour);

            switch (_neighborhoodType)
            {
                case MutationMethod.Swap:
                    (newTour[i], newTour[j]) = (newTour[j], newTour[i]);
                    break;

                case MutationMethod.Inversion:
                    newTour.Reverse(i, (j - i) + 1);
                    break;

                case MutationMethod.Scramble:
                    // Wyciągnij segment do potasowania
                    var segment = newTour.GetRange(i, (j - i) + 1);
                    
                    // Tasowanie Fisher-Yates na segmencie
                    for (int k = segment.Count - 1; k > 0; k--)
                    {
                        int l = _rand.Next(k + 1);
                        (segment[k], segment[l]) = (segment[l], segment[k]);
                    }
                    
                    // Wstaw potasowany segment z powrotem
                    for(int k = 0; k < segment.Count; k++)
                    {
                        newTour[i + k] = segment[k];
                    }
                    break;
            }
            return new Individual(newTour);
        }
    }
}