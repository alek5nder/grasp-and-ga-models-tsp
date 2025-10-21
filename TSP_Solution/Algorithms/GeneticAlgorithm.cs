using TSP_Solution.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TSP_Solution.Algorithms
{
    public class GeneticAlgorithm
    {
        private readonly TSPData _data;
        private readonly Random _rand = new Random();
        
        // Parametry GA
        private int _populationSize;
        private double _mutationRate;
        private int _tournamentSize;
        private int _generations; 

        // Parametry Strategii (z wytycznych)
        private SelectionMethod _selectionMethod;
        private CrossoverMethod _crossoverMethod;
        private MutationMethod _mutationMethod;

        private List<Individual> _population;

        // Konstruktor
        public GeneticAlgorithm(TSPData data, int populationSize, double mutationRate, int tournamentSize, int generations,
                                SelectionMethod selection, CrossoverMethod crossover, MutationMethod mutation)
        {
            _data = data;
            _populationSize = populationSize;
            _mutationRate = mutationRate;
            _tournamentSize = tournamentSize;
            _generations = generations;
            _population = new List<Individual>(_populationSize);

            _selectionMethod = selection;
            _crossoverMethod = crossover;
            _mutationMethod = mutation;
        }

        public Individual Run()
        {
            InitializePopulation();
            
            for (int gen = 0; gen < _generations; gen++)
            {
                var newPopulation = new List<Individual>(_populationSize);

                // 1. Elityzm (zachowaj najlepszego osobnika bez zmian)
                // 1. Elityzm (zachowaj najlepszego osobnika bez zmian)
                var bestIndividual = _population.OrderBy(ind => ind.Fitness).First();
                newPopulation.Add(bestIndividual); // <-- POPRAWNA LINIA

                while (newPopulation.Count < _populationSize)
                {
                    // 2a. Selekcja rodziców
                    Individual parent1 = SelectParent();
                    Individual parent2 = SelectParent();
                    
                    // 2b. Krzyżowanie
                    Individual child = Crossover(parent1, parent2);
                    
                    // 2c. Mutacja
                    Mutate(child);
                    
                    newPopulation.Add(child);
                }
                
                _population = newPopulation;
                
                // Oblicz fitness dla nowej populacji (z wyjątkiem elity, która już ma)
                foreach (var ind in _population.Skip(1)) // Pomiń elitę
                {
                    ind.CalculateFitness(_data);
                }
            }
            // Zwróć najlepszego osobnika znalezionego w całej ewolucji
            return _population.OrderBy(ind => ind.Fitness).First();
        }

        private void InitializePopulation()
        {
            for (int i = 0; i < _populationSize; i++)
            {
                var individual = Individual.CreateRandom(_data.NumberOfCities, _rand);
                individual.CalculateFitness(_data);
                _population.Add(individual);
            }
        }

        // --- "DYSPOCZYTORNIE" METOD ---

        private Individual SelectParent()
        {
            switch (_selectionMethod)
            {
                case SelectionMethod.Tournament:
                    return TournamentSelection();
                case SelectionMethod.Roulette:
                    return RouletteWheelSelection(); // Zaimplementowane
                case SelectionMethod.Ranking:
                    return RankingSelection(); // Zaimplementowane
                default:
                    return TournamentSelection();
            }
        }

        private Individual Crossover(Individual parent1, Individual parent2)
        {
            switch (_crossoverMethod)
            {
                case CrossoverMethod.PMX:
                    return PMXCrossover(parent1, parent2);
                case CrossoverMethod.OX:
                    return OXCrossover(parent1, parent2); // Zaimplementowane
                case CrossoverMethod.CX:
                    return CXCrossover(parent1, parent2); // Zaimplementowane
                default:
                    return PMXCrossover(parent1, parent2);
            }
        }

        private void Mutate(Individual individual)
        {
            // Sprawdzenie prawdopodobieństwa mutacji
            if (_rand.NextDouble() > _mutationRate) return; 

            switch (_mutationMethod)
            {
                case MutationMethod.Swap:
                    SwapMutation(individual);
                    break;
                case MutationMethod.Inversion:
                    InversionMutation(individual); // Zaimplementowane
                    break;
                case MutationMethod.Scramble:
                    ScrambleMutation(individual); // Zaimplementowane
                    break;
                default:
                    SwapMutation(individual);
                    break;
            }
        }


        // --- METODY SELEKCJI ---

        // Metoda Turniejowa (już istniała)
        private Individual TournamentSelection()
        {
            var tournament = new List<Individual>();
            for (int i = 0; i < _tournamentSize; i++)
            {
                int randomIndex = _rand.Next(_populationSize);
                tournament.Add(_population[randomIndex]);
            }
            // Zwraca najlepszego (z najniższym fitnessem)
            return tournament.OrderBy(ind => ind.Fitness).First();
        }
        
        // NOWA: Metoda Ruletkowa
        private Individual RouletteWheelSelection()
        {
            // Niższy fitness (dystans) jest lepszy, więc musimy odwrócić wagi
            double worstFitness = _population.Max(ind => ind.Fitness);
            var weights = _population.Select(ind => (worstFitness - ind.Fitness) + 1.0).ToList();
            double totalWeight = weights.Sum();

            double randomValue = _rand.NextDouble() * totalWeight;
            
            for (int i = 0; i < _populationSize; i++)
            {
                if (randomValue < weights[i])
                {
                    return _population[i];
                }
                randomValue -= weights[i];
            }
            return _population.Last(); // Fallback
        }

        // NOWA: Metoda Rankingowa
        private Individual RankingSelection()
        {
            var sortedPopulation = _population.OrderBy(ind => ind.Fitness).ToList();
            int n = _populationSize;
            double totalRankSum = n * (n + 1) / 2.0;
            
            double randomValue = _rand.NextDouble() * totalRankSum;

            for (int i = 0; i < n; i++)
            {
                int rank = n - i; // Najlepszy (i=0) ma rangę n (największą)
                if (randomValue < rank)
                {
                    return sortedPopulation[i];
                }
                randomValue -= rank;
            }
            return sortedPopulation.First(); // Fallback
        }

        // --- METODY KRZYŻOWANIA ---

        // Metoda PMX (już istniała)
        private Individual PMXCrossover(Individual parent1, Individual parent2)
        {
            int size = parent1.Tour.Count;
            var childTour = new int[size].Select(_ => -1).ToList(); 
            
            int cp1 = _rand.Next(size);
            int cp2 = _rand.Next(size);
            if (cp1 > cp2) (cp1, cp2) = (cp2, cp1);

            var mapping = new Dictionary<int, int>();
            for (int i = cp1; i <= cp2; i++)
            {
                childTour[i] = parent1.Tour[i];
                mapping[parent1.Tour[i]] = parent2.Tour[i];
            }

            for (int i = 0; i < size; i++)
            {
                if (i >= cp1 && i <= cp2) continue; 

                int cityToAdd = parent2.Tour[i];
                while (mapping.ContainsKey(cityToAdd))
                {
                    cityToAdd = mapping[cityToAdd];
                }
                childTour[i] = cityToAdd;
            }
            return new Individual(childTour);
        }

        // NOWA: Metoda OX (Order Crossover)
        private Individual OXCrossover(Individual parent1, Individual parent2)
        {
            int size = parent1.Tour.Count;
            var childTour = new int[size].Select(_ => -1).ToList();

            int cp1 = _rand.Next(size);
            int cp2 = _rand.Next(size);
            if (cp1 > cp2) (cp1, cp2) = (cp2, cp1);

            var segment = new HashSet<int>();
            for (int i = cp1; i <= cp2; i++)
            {
                childTour[i] = parent1.Tour[i];
                segment.Add(parent1.Tour[i]);
            }

            int childIndex = (cp2 + 1) % size;
            for (int i = 0; i < size; i++)
            {
                int parentIndex = (cp2 + 1 + i) % size;
                int city = parent2.Tour[parentIndex];

                if (!segment.Contains(city))
                {
                    childTour[childIndex] = city;
                    childIndex = (childIndex + 1) % size;
                }
            }
            return new Individual(childTour);
        }
        
        // NOWA: Metoda CX (Cycle Crossover)
        private Individual CXCrossover(Individual parent1, Individual parent2)
        {
            int size = parent1.Tour.Count;
            var childTour = new int[size].Select(_ => -1).ToList();
            var p1Tour = parent1.Tour;
            var p2Tour = parent2.Tour;

            var visited = new bool[size];
            
            int currentIndex = 0;
            while (visited[currentIndex] == false)
            {
                visited[currentIndex] = true;
                int cityFromP1 = p1Tour[currentIndex];
                childTour[currentIndex] = cityFromP1; 
                
                int cityFromP2 = p2Tour[currentIndex];
                currentIndex = p1Tour.IndexOf(cityFromP2);
            }

            for (int i = 0; i < size; i++)
            {
                if (childTour[i] == -1) // Niewypełnione przez cykl
                {
                    childTour[i] = p2Tour[i];
                }
            }
            return new Individual(childTour);
        }

        // --- METODY MUTACJI ---

        // Metoda Swap (już istniała, lekko zmodyfikowana)
        private void SwapMutation(Individual individual)
        {
            int index1 = _rand.Next(individual.Tour.Count);
            int index2 = _rand.Next(individual.Tour.Count);
            while (index1 == index2)
            {
                index2 = _rand.Next(individual.Tour.Count);
            }
            (individual.Tour[index1], individual.Tour[index2]) = (individual.Tour[index2], individual.Tour[index1]);
        }

        // NOWA: Metoda Inversion (Inwersja)
        private void InversionMutation(Individual individual)
        {
            int cp1 = _rand.Next(individual.Tour.Count);
            int cp2 = _rand.Next(individual.Tour.Count);
            if (cp1 > cp2) (cp1, cp2) = (cp2, cp1);

            // Odwróć segment pomiędzy cp1 a cp2
            individual.Tour.Reverse(cp1, (cp2 - cp1) + 1);
        }

        // NOWA: Metoda Scramble (Mieszanie)
        private void ScrambleMutation(Individual individual)
        {
            int cp1 = _rand.Next(individual.Tour.Count);
            int cp2 = _rand.Next(individual.Tour.Count);
            if (cp1 > cp2) (cp1, cp2) = (cp2, cp1);

            var segment = individual.Tour.GetRange(cp1, (cp2 - cp1) + 1);
            
            // Potasuj segment (Fisher-Yates)
            for (int i = segment.Count - 1; i > 0; i--)
            {
                int j = _rand.Next(i + 1);
                (segment[i], segment[j]) = (segment[j], segment[i]);
            }
            
            // Wstaw potasowany segment z powrotem
            for(int i = 0; i < segment.Count; i++)
            {
                individual.Tour[cp1 + i] = segment[i];
            }
        }
    }
}