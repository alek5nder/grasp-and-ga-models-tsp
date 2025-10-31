using TSP_Solution.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TSP_Solution.Algorithms
{
    public class AcoAlgorithm
    {
        private readonly PFSPData _data; 
        private readonly Random _rand = new Random();
        private readonly int _numJobs; 

        // Macierze
        private double[,] _pheromoneMatrix;
        private readonly double[,] _heuristicMatrix; 

        // Parametry ACO
        private readonly int _antCount;     
        private readonly int _iterations;   
        private readonly double _alpha;     
        private readonly double _beta;      
        private readonly double _rho;       
        private readonly double _q;         

        // --- ZMODYFIKOWANY KONSTRUKTOR ---
        // Dodaliśmy opcjonalny argument 'nehSolution'
        public AcoAlgorithm(PFSPData data, int antCount, int iterations, double alpha, double beta, double rho, double q, Individual nehSolution = null)
        {
            _data = data;
            _numJobs = data.NumberOfJobs; 
            _antCount = antCount;
            _iterations = iterations;
            _alpha = alpha;
            _beta = beta;
            _rho = rho;
            _q = q;

            // 1. Inicjalizuj macierz heurystyki (bez zmian, 1.0)
            _heuristicMatrix = new double[_numJobs, _numJobs];
            for (int i = 0; i < _numJobs; i++)
            {
                for (int j = 0; j < _numJobs; j++)
                {
                    _heuristicMatrix[i, j] = 1.0; 
                }
            }

            // 2. Inicjalizuj macierz feromonów
            _pheromoneMatrix = new double[_numJobs, _numJobs];
            
            // Ustaw bazowy poziom feromonu
            double initialPheromone = 1.0;
            for (int i = 0; i < _numJobs; i++)
                for (int j = 0; j < _numJobs; j++)
                    _pheromoneMatrix[i, j] = initialPheromone;

            // 3. "Zasiej" feromony z NEH, jeśli dostępne
            // To jest nowe "usprawnienie"
            
            if (nehSolution != null)
            {
                if (nehSolution.Fitness == -1) // Upewnij się, że Cmax jest obliczony
                {
                    nehSolution.CalculateFitness(_data);
                }
                
                // Obliczamy "bonusowy" feromon, który złożymy na ścieżce NEH.
                // Używamy dużej wartości (np. 10x standardowy depozyt), aby mocno 
                // naprowadzić pierwsze mrówki.
                double bonusDeposit = (10.0 * _q) / nehSolution.Fitness; 
                
                var tour = nehSolution.Tour;
                for (int i = 0; i < _numJobs - 1; i++)
                {
                    int city1 = tour[i];
                    int city2 = tour[i + 1];
                    _pheromoneMatrix[city1, city2] += bonusDeposit;
                    _pheromoneMatrix[city2, city1] += bonusDeposit; // Symetryczny TSP/PFSP
                }
                // Dodaj krawędź zamykającą cykl (ostatni -> pierwszy)
                _pheromoneMatrix[tour.Last(), tour.First()] += bonusDeposit;
                _pheromoneMatrix[tour.First(), tour.Last()] += bonusDeposit;
            }
            
        }

        public Individual Run()
        {
            Individual overallBestIndividual = null;

            for (int iter = 0; iter < _iterations; iter++)
            {
                Individual generationBestIndividual = null;
                var antSolutions = new List<Individual>();

                // 1. Faza Konstrukcji (każda mrówka buduje trasę)
                for (int ant = 0; ant < _antCount; ant++)
                {
                    var tour = BuildAntTour();
                    var individual = new Individual(tour);
                    
                    // ZMIANA: Automatycznie wywoła poprawną funkcję
                    // CalculateFitness(PFSPData) z klasy Individual.
                    individual.CalculateFitness(_data); 
                    antSolutions.Add(individual);

                    if (generationBestIndividual == null || individual.Fitness < generationBestIndividual.Fitness)
                    {
                        generationBestIndividual = individual;
                    }
                }

                if (overallBestIndividual == null || generationBestIndividual.Fitness < overallBestIndividual.Fitness)
                {
                    overallBestIndividual = generationBestIndividual;
                }

                // 2. Faza Aktualizacji Feromonów (bez zmian w logice)
                UpdatePheromones(overallBestIndividual);
            }

            return overallBestIndividual;
        }

        /// <summary>
        /// Buduje trasę dla jednej mrówki
        /// </summary>
        private List<int> BuildAntTour()
        {
            var tour = new List<int>(_numJobs);
            var unvisited = new HashSet<int>(Enumerable.Range(0, _numJobs)); // ZMIANA: _numJobs

            // Zacznij od losowego zadania (miasta)
            int currentCity = _rand.Next(_numJobs); // ZMIANA: _numJobs
            tour.Add(currentCity);
            unvisited.Remove(currentCity);

            while (unvisited.Count > 0)
            {
                // Wybierz następne miasto na podstawie feromonów i heurystyki
                int nextCity = SelectNextCity(currentCity, unvisited);
                tour.Add(nextCity);
                unvisited.Remove(nextCity);
                currentCity = nextCity;
            }
            return tour;
        }

        /// <summary>
        /// Wybiera następne miasto dla mrówki (Ruletka) - BEZ ZMIAN W LOGICE
        /// </summary>
        private int SelectNextCity(int currentCity, HashSet<int> unvisited)
        {
            double totalProbSum = 0;
            var probabilities = new List<(int city, double prob)>();

            // Oblicz sumę (mianownik wzoru)
            foreach (int nextCity in unvisited)
            {
                double tau = _pheromoneMatrix[currentCity, nextCity];
                double eta = _heuristicMatrix[currentCity, nextCity]; // Zawsze będzie 1.0
                
                double prob = Math.Pow(tau, _alpha) * Math.Pow(eta, _beta);
                
                probabilities.Add((nextCity, prob));
                totalProbSum += prob;
            }

            if (totalProbSum == 0)
            {
                return unvisited.First(); 
            }

            // Ruletka (losowanie)
            double spin = _rand.NextDouble() * totalProbSum;
            foreach (var p in probabilities)
            {
                if (spin < p.prob)
                {
                    return p.city;
                }
                spin -= p.prob;
            }
            return unvisited.Last(); // Fallback
        }

        /// <summary>
        /// Aktualizuje feromony: (1) Parowanie, (2) Składanie
        /// </summary>
        private void UpdatePheromones(Individual bestIndividual)
        {
            // 1. Parowanie (Evaporation) na wszystkich ścieżkach
            for (int i = 0; i < _numJobs; i++) // ZMIANA: _numJobs
            {
                for (int j = 0; j < _numJobs; j++) // ZMIANA: _numJobs
                {
                    _pheromoneMatrix[i, j] *= (1.0 - _rho);
                }
            }

            // 2. Składanie (Deposition) tylko na krawędziach najlepszej trasy
            double depositAmount = _q / bestIndividual.Fitness;
            var tour = bestIndividual.Tour;

            for (int i = 0; i < _numJobs - 1; i++) // ZMIANA: _numJobs
            {
                int city1 = tour[i];
                int city2 = tour[i + 1];
                _pheromoneMatrix[city1, city2] += depositAmount;
                _pheromoneMatrix[city2, city1] += depositAmount; 
            }
            _pheromoneMatrix[tour.Last(), tour.First()] += depositAmount;
            _pheromoneMatrix[tour.First(), tour.Last()] += depositAmount;
        }
    }
}