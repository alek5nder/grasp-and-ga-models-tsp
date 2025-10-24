using TSP_Solution.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TSP_Solution.Algorithms
{
    public class AcoAlgorithm
    {
        private readonly TSPData _data;
        private readonly Random _rand = new Random();
        private readonly int _numCities;

        // Macierze
        private double[,] _pheromoneMatrix;
        private readonly double[,] _heuristicMatrix; // (1 / dystans)

        // --- Parametry ACO do testowania ---
        private readonly int _antCount;     // Liczba mrówek
        private readonly int _iterations;   // Liczba generacji
        private readonly double _alpha;     // Wpływ feromonu
        private readonly double _beta;      // Wpływ heurystyki (dystansu)
        private readonly double _rho;       // Współczynnik parowania (evaporation)
        private readonly double _q;         // Ilość feromonu do złożenia (stała)

        public AcoAlgorithm(TSPData data, int antCount, int iterations, double alpha, double beta, double rho, double q)
        {
            _data = data;
            _numCities = data.NumberOfCities;
            _antCount = antCount;
            _iterations = iterations;
            _alpha = alpha;
            _beta = beta;
            _rho = rho;
            _q = q;

            // 1. Inicjalizuj macierz heurystyki (1 / dystans)
            // Robimy to raz, bo jest stała
            _heuristicMatrix = new double[_numCities, _numCities];
            for (int i = 0; i < _numCities; i++)
            {
                for (int j = 0; j < _numCities; j++)
                {
                    if (i == j)
                    {
                        _heuristicMatrix[i, j] = 0;
                    }
                    else
                    {
                        // Dodajemy epsilon, aby uniknąć dzielenia przez zero
                        _heuristicMatrix[i, j] = 1.0 / (_data.GetDistance(i, j) + 1e-9);
                    }
                }
            }

            // 2. Inicjalizuj macierz feromonów (początkowo stała wartość)
            _pheromoneMatrix = new double[_numCities, _numCities];
            double initialPheromone = 1.0; 
            for (int i = 0; i < _numCities; i++)
            {
                for (int j = 0; j < _numCities; j++)
                {
                    _pheromoneMatrix[i, j] = initialPheromone;
                }
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

                // 2. Faza Aktualizacji Feromonów
                UpdatePheromones(overallBestIndividual); // Używamy elityzmu (tylko najlepsza trasa składa feromon)
            }

            return overallBestIndividual;
        }

        /// <summary>
        /// Buduje trasę dla jednej mrówki
        /// </summary>
        private List<int> BuildAntTour()
        {
            var tour = new List<int>(_numCities);
            var unvisited = new HashSet<int>(Enumerable.Range(0, _numCities));

            // Zacznij od losowego miasta
            int currentCity = _rand.Next(_numCities);
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
        /// Wybiera następne miasto dla mrówki (Ruletka)
        /// </summary>
        private int SelectNextCity(int currentCity, HashSet<int> unvisited)
        {
            double totalProbSum = 0;
            var probabilities = new List<(int city, double prob)>();

            // Oblicz sumę (mianownik wzoru)
            foreach (int nextCity in unvisited)
            {
                double tau = _pheromoneMatrix[currentCity, nextCity];
                double eta = _heuristicMatrix[currentCity, nextCity];
                
                double prob = Math.Pow(tau, _alpha) * Math.Pow(eta, _beta);
                
                probabilities.Add((nextCity, prob));
                totalProbSum += prob;
            }

            // Obsługa błędu, jeśli suma = 0
            if (totalProbSum == 0)
            {
                return unvisited.First(); // Wybierz jakiekolwiek
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
            for (int i = 0; i < _numCities; i++)
            {
                for (int j = 0; j < _numCities; j++)
                {
                    _pheromoneMatrix[i, j] *= (1.0 - _rho);
                }
            }

            // 2. Składanie (Deposition) tylko na krawędziach najlepszej trasy
            double depositAmount = _q / bestIndividual.Fitness;
            var tour = bestIndividual.Tour;

            for (int i = 0; i < _numCities - 1; i++)
            {
                int city1 = tour[i];
                int city2 = tour[i + 1];
                _pheromoneMatrix[city1, city2] += depositAmount;
                _pheromoneMatrix[city2, city1] += depositAmount; // Symetryczny TSP
            }
            // Dodaj krawędź zamykającą cykl (ostatni -> pierwszy)
            _pheromoneMatrix[tour.Last(), tour.First()] += depositAmount;
            _pheromoneMatrix[tour.First(), tour.Last()] += depositAmount;
        }
    }
}