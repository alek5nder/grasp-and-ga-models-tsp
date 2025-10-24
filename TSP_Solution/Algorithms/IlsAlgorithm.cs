using TSP_Solution.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TSP_Solution.Algorithms
{
    public class IlsAlgorithm
    {
        private readonly TSPData _data;
        private readonly Random _rand = new Random();

        // --- Parametry ILS ---
        
        // 1. Główne kryterium stopu: liczba cykli (Perturbacja + LocalSearch)
        private readonly int _iterations; 
        
        // 2. Siła perturbacji: liczba losowych ruchów (Swap) wykonywanych
        //    aby "uciec" z lokalnego optimum
        private readonly int _perturbationStrength; 
        
        // 3. Rodzaj sąsiedztwa używany w fazie Local Search
        private readonly MutationMethod _neighborhoodType; 

        public IlsAlgorithm(TSPData data, int iterations, int perturbationStrength, MutationMethod neighborhoodType)
        {
            _data = data;
            _iterations = iterations;
            _perturbationStrength = perturbationStrength;
            _neighborhoodType = neighborhoodType;
        }

        /// <summary>
        /// Uruchamia główną pętlę algorytmu ILS
        /// </summary>
        public Individual Run()
        {
            // 1. Stwórz rozwiązanie początkowe (np. losowe)
            var initialSolution = Individual.CreateRandom(_data.NumberOfCities, _rand);
            initialSolution.CalculateFitness(_data);

            // 2. Znajdź pierwsze lokalne optimum
            var currentBest = LocalSearch(initialSolution);
            var overallBest = currentBest;

            // 3. Główna pętla ILS
            for (int i = 0; i < _iterations; i++)
            {
                // 3a. Perturbacja: "Kopnij" obecne najlepsze rozwiązanie
                var perturbed = Perturbation(currentBest);
                
                // 3b. Local Search: Znajdź lokalne optimum dla "kopniętego" rozwiązania
                var newLocalOptimum = LocalSearch(perturbed);

                // 3c. Kryterium Akceptacji: (Proste: zachowaj, jeśli jest lepsze)
                if (newLocalOptimum.Fitness < currentBest.Fitness)
                {
                    currentBest = newLocalOptimum;

                    if (currentBest.Fitness < overallBest.Fitness)
                    {
                        overallBest = currentBest;
                    }
                }
            }
            return overallBest;
        }

        /// <summary>
        /// FAZA A: Perturbacja (Zaburzenie)
        /// Stosuje 'N' losowych ruchów Swap, aby uciec z lokalnego optimum.
        /// </summary>
        private Individual Perturbation(Individual solution)
        {
            var newTour = new List<int>(solution.Tour);
            int tourCount = newTour.Count;

            for (int k = 0; k < _perturbationStrength; k++)
            {
                // Zastosuj prostą mutację Swap
                int i = _rand.Next(tourCount);
                int j = _rand.Next(tourCount);
                while (i == j) j = _rand.Next(tourCount);
                
                (newTour[i], newTour[j]) = (newTour[j], newTour[i]);
            }
            
            var perturbedInd = new Individual(newTour);
            perturbedInd.CalculateFitness(_data);
            return perturbedInd;
        }


        /// <summary>
        /// FAZA B: Algorytm wspinaczkowy (First Improvement Hill Climber - SZYBKI)
        /// </summary>
        private Individual LocalSearch(Individual initialSolution)
        {
            var currentSolution = initialSolution;
            bool improvementFound;

            do
            {
                improvementFound = false;
                int tourCount = currentSolution.Tour.Count;

                // Przeszukuj sąsiedztwo (i, j)
                for (int i = 0; i < tourCount; i++)
                {
                    for (int j = i + 1; j < tourCount; j++)
                    {
                        // Stwórz sąsiada wg wybranej metody
                        var neighbor = CreateNeighbor(currentSolution, i, j);
                        neighbor.CalculateFitness(_data);

                        if (neighbor.Fitness < currentSolution.Fitness) 
                        {
                            currentSolution = neighbor; // Natychmiast przejdź do lepszego sąsiada
                            improvementFound = true;
                            
                            // PRZERWIJ wewnętrzne pętle i zacznij szukać od nowa
                            goto NextIteration; 
                        }
                    }
                }

            NextIteration:; // Etykieta 'goto' do wznowienia pętli 'do-while'
            } 
            while (improvementFound); // Powtarzaj, dopóki znajdujesz poprawę

            return currentSolution;
        }
        
        /// <summary>
        /// Tworzy sąsiada (nowego osobnika) na podstawie wybranej metody (Swap, Inversion, Scramble)
        /// (Skopiowane z GRASP / GA)
        /// </summary>
        private Individual CreateNeighbor(Individual solution, int i, int j)
        {
            var newTour = new List<int>(solution.Tour);
            if (i > j) (i, j) = (j, i); // Upewnij się, że i < j dla Inversion/Scramble

            switch (_neighborhoodType)
            {
                case MutationMethod.Swap:
                    (newTour[i], newTour[j]) = (newTour[j], newTour[i]);
                    break;

                case MutationMethod.Inversion:
                    newTour.Reverse(i, (j - i) + 1);
                    break;

                case MutationMethod.Scramble:
                    var segment = newTour.GetRange(i, (j - i) + 1);
                    for (int k = segment.Count - 1; k > 0; k--)
                    {
                        int l = _rand.Next(k + 1);
                        (segment[k], segment[l]) = (segment[l], segment[k]);
                    }
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