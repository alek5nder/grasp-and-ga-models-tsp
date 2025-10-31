using TSP_Solution.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TSP_Solution.Algorithms
{
    public class GeneticAlgorithm
    {
        private readonly PFSPData _data; // Poprawny typ danych PFSP
        private readonly Random _rand = new Random();

        // Parametry GA
        private readonly int _populationSize;
        private readonly double _mutationRate;
        private readonly int _tournamentSize;
        private readonly int _generations;

        // Metody
        private readonly SelectionMethod _selectionMethod;
        private readonly CrossoverMethod _crossoverMethod;
        private readonly MutationMethod _mutationMethod;

        // Konstruktor przyjmujący PFSPData
        public GeneticAlgorithm(PFSPData data, int populationSize, double mutationRate, 
                                int tournamentSize, int generations,
                                SelectionMethod selectionMethod, CrossoverMethod crossoverMethod, 
                                MutationMethod mutationMethod)
        {
            _data = data;
            _populationSize = populationSize;
            _mutationRate = mutationRate;
            _tournamentSize = tournamentSize;
            _generations = generations;
            _selectionMethod = selectionMethod;
            _crossoverMethod = crossoverMethod;
            _mutationMethod = mutationMethod;
        }
        
        // --- NOWA, SZYBKA METODA INICJALIZACJI Z NEH ---
        /// <summary>
        /// Inicjalizuje populację używając "Zasiana Hybrydowego" (NEH + Losowe).
        /// Daje to GA zarówno dobrego kandydata (NEH) jak i różnorodność (Losowi).
        /// </summary>
        private List<Individual> InitializePopulation()
        {
            var population = new List<Individual>(_populationSize);
            int numJobs = _data.NumberOfJobs;

            // 1. Uruchom NEH, aby uzyskać jedno, bardzo dobre rozwiązanie
            var nehAlgorithm = new NehAlgorithm(_data);
            Individual nehSolution = nehAlgorithm.Run();
            nehSolution.CalculateFitness(_data);
            
            // 2. Dodaj to najlepsze rozwiązanie bezpośrednio do populacji
            population.Add(nehSolution); 
            Console.WriteLine($" -> NEH Seed Cmax: {nehSolution.Fitness}");

            // --- TUTAJ ZACZYNA SIĘ ZMIANA ---

            // 3. Oblicz, ile dodać klonów, a ile losowych
            // Połowa populacji będzie oparta na NEH, a druga połowa czysto losowa
            int numClones = (_populationSize / 2) - 1; // Odejmujemy 1 za już dodany nehSolution
            int numRandom = _populationSize - population.Count - numClones;

            // 4. Dodaj "lekkie mutacje" NEH (Klony)
            for (int i = 0; i < numClones; i++)
            {
                var mutatedTour = new List<int>(nehSolution.Tour);
                int numSwaps = 3; 
                for(int k=0; k < numSwaps; k++)
                {
                     int p1 = _rand.Next(numJobs);
                     int p2 = _rand.Next(numJobs);
                     (mutatedTour[p1], mutatedTour[p2]) = (mutatedTour[p2], mutatedTour[p1]);
                }
                
                var individual = new Individual(mutatedTour);
                individual.CalculateFitness(_data);
                population.Add(individual);
            }

            // 5. Dodaj CZYSTO LOSOWYCH osobników (to jest klucz do różnorodności)
            for (int i = 0; i < numRandom; i++)
            {
                var randomIndividual = Individual.CreateRandom(numJobs, _rand);
                randomIndividual.CalculateFitness(_data);
                population.Add(randomIndividual);
            }

            return population;
        }
        //
        // --- RESZTA PLIKU POZOSTAJE BEZ ZMIAN ---
        // Poniższe metody są uniwersalne i działają poprawnie z PFSP
        // (Run, EvolvePopulation, SelectParent, Selections, Crossover, Mutations...)
        //

        public Individual Run()
        {
            var population = InitializePopulation();
            var bestOverall = population.OrderBy(ind => ind.Fitness).First();

            for (int i = 0; i < _generations; i++)
            {
                population = EvolvePopulation(population);
                var bestOfGeneration = population.OrderBy(ind => ind.Fitness).First();

                if (bestOfGeneration.Fitness < bestOverall.Fitness)
                {
                    bestOverall = bestOfGeneration;
                }
            }
            return bestOverall;
        }

        private List<Individual> EvolvePopulation(List<Individual> currentPopulation)
        {
            var newPopulation = new List<Individual>(_populationSize);

            // Elityzm: Zachowaj najlepszego osobnika
            var elite = currentPopulation.OrderBy(ind => ind.Fitness).First();
            newPopulation.Add(new Individual(new List<int>(elite.Tour)) { Fitness = elite.Fitness });

            while (newPopulation.Count < _populationSize)
            {
                var parent1 = SelectParent(currentPopulation);
                var parent2 = SelectParent(currentPopulation);

                var (child1, child2) = Crossover(parent1, parent2);

                Mutate(child1);
                Mutate(child2);

                child1.CalculateFitness(_data);
                child2.CalculateFitness(_data);

                newPopulation.Add(child1);
                if (newPopulation.Count < _populationSize)
                {
                    newPopulation.Add(child2);
                }
            }
            return newPopulation;
        }

        // --- SELEKCJA (BEZ ZMIAN) ---
        private Individual SelectParent(List<Individual> population)
        {
            switch (_selectionMethod)
            {
                case SelectionMethod.Tournament:
                    return TournamentSelection(population);
                case SelectionMethod.Roulette:
                    return RouletteWheelSelection(population);
                case SelectionMethod.Ranking:
                    return RankingSelection(population);
                default:
                    throw new ArgumentException("Nieznana metoda selekcji.");
            }
        }

        private Individual TournamentSelection(List<Individual> population)
        {
            Individual best = null;
            for (int i = 0; i < _tournamentSize; i++)
            {
                var randomInd = population[_rand.Next(population.Count)];
                if (best == null || randomInd.Fitness < best.Fitness)
                {
                    best = randomInd;
                }
            }
            return best;
        }
        
        // Ta implementacja jest poprawna dla minimalizacji (Fitness = Cmax)
        private Individual RouletteWheelSelection(List<Individual> population)
        {
            double worstFitness = population.Max(ind => ind.Fitness) + 1.0;
            var weightedPopulation = population
                .Select(ind => new { Individual = ind, Weight = (worstFitness - ind.Fitness) })
                .ToList();
            
            double totalWeight = weightedPopulation.Sum(x => x.Weight);
            double spin = _rand.NextDouble() * totalWeight;

            foreach (var weightedInd in weightedPopulation)
            {
                if (spin < weightedInd.Weight)
                {
                    return weightedInd.Individual;
                }
                spin -= weightedInd.Weight;
            }
            return population.Last();
        }

        private Individual RankingSelection(List<Individual> population)
        {
            var sortedPopulation = population.OrderBy(ind => ind.Fitness).ToList();
            int n = sortedPopulation.Count;
            double totalRankSum = (double)n * (n + 1) / 2.0;

            double spin = _rand.NextDouble() * totalRankSum;
            double currentSum = 0;

            for (int i = 0; i < n; i++)
            {
                int rank = n - i; // Najlepszy (index 0) ma rangę N, najgorszy ma rangę 1
                currentSum += rank;
                if (spin < currentSum)
                {
                    return sortedPopulation[i];
                }
            }
            return sortedPopulation.Last();
        }


        // --- KRZYŻOWANIE (BEZ ZMIAN) ---
        private (Individual, Individual) Crossover(Individual parent1, Individual parent2)
        {
            switch (_crossoverMethod)
            {
                case CrossoverMethod.PMX:
                    return PMXCrossover(parent1, parent2);
                case CrossoverMethod.OX:
                    return OXCrossover(parent1, parent2);
                case CrossoverMethod.CX:
                    return CXCrossover(parent1, parent2);
                default:
                    throw new ArgumentException("Nieznana metoda krzyżowania.");
            }
        }

        private (Individual, Individual) PMXCrossover(Individual parent1, Individual parent2)
        {
            int size = parent1.Tour.Count;
            var child1Tour = Enumerable.Repeat(-1, size).ToList();
            var child2Tour = Enumerable.Repeat(-1, size).ToList();

            (int p1, int p2) = GetTwoRandomPoints(size);

            var map1 = new Dictionary<int, int>();
            var map2 = new Dictionary<int, int>();

            for (int i = p1; i <= p2; i++)
            {
                child1Tour[i] = parent2.Tour[i];
                child2Tour[i] = parent1.Tour[i];
                map1[parent2.Tour[i]] = parent1.Tour[i];
                map2[parent1.Tour[i]] = parent2.Tour[i];
            }

            for (int i = 0; i < size; i++)
            {
                if (i >= p1 && i <= p2) continue;
                
                int gene1 = parent1.Tour[i];
                while (map1.ContainsKey(gene1))
                {
                    gene1 = map1[gene1];
                }
                child1Tour[i] = gene1;

                int gene2 = parent2.Tour[i];
                while (map2.ContainsKey(gene2))
                {
                    gene2 = map2[gene2];
                }
                child2Tour[i] = gene2;
            }
            return (new Individual(child1Tour), new Individual(child2Tour));
        }

        private (Individual, Individual) OXCrossover(Individual parent1, Individual parent2)
        {
            int size = parent1.Tour.Count;
            var child1Tour = Enumerable.Repeat(-1, size).ToList();
            var child2Tour = Enumerable.Repeat(-1, size).ToList();

            (int p1, int p2) = GetTwoRandomPoints(size);

            for (int i = p1; i <= p2; i++)
            {
                child1Tour[i] = parent1.Tour[i];
                child2Tour[i] = parent2.Tour[i];
            }

            var items1 = child1Tour.Where(x => x != -1).ToHashSet();
            var items2 = child2Tour.Where(x => x != -1).ToHashSet();

            int currentIdx = 0;
            for (int i = 0; i < size; i++)
            {
                if (currentIdx == p1) currentIdx = p2 + 1;
                if (!items1.Contains(parent2.Tour[i]))
                {
                    child1Tour[currentIdx++] = parent2.Tour[i];
                }
            }

            currentIdx = 0;
            for (int i = 0; i < size; i++)
            {
                if (currentIdx == p1) currentIdx = p2 + 1;
                if (!items2.Contains(parent1.Tour[i]))
                {
                    child2Tour[currentIdx++] = parent1.Tour[i];
                }
            }
            return (new Individual(child1Tour), new Individual(child2Tour));
        }
        
        private (Individual, Individual) CXCrossover(Individual parent1, Individual parent2)
        {
            int size = parent1.Tour.Count;
            var child1Tour = new List<int>(parent1.Tour);
            var child2Tour = new List<int>(parent2.Tour);

            int startIndex = 0;
            var cycle = new HashSet<int>();
            
            while(!cycle.Contains(startIndex))
            {
                cycle.Add(startIndex);
                int gene = parent2.Tour[startIndex];
                startIndex = parent1.Tour.IndexOf(gene);
            }

            foreach(int index in cycle)
            {
                (child1Tour[index], child2Tour[index]) = (child2Tour[index], child1Tour[index]);
            }
            
            return (new Individual(child1Tour), new Individual(child2Tour));
        }

        // --- MUTACJA (BEZ ZMIAN) ---
        private void Mutate(Individual individual)
        {
            if (_rand.NextDouble() >= _mutationRate) return;

            switch (_mutationMethod)
            {
                case MutationMethod.Swap:
                    SwapMutation(individual);
                    break;
                case MutationMethod.Inversion:
                    InversionMutation(individual);
                    break;
                case MutationMethod.Scramble:
                    ScrambleMutation(individual);
                    break;
            }
        }
        
        private void SwapMutation(Individual individual)
        {
            (int p1, int p2) = GetTwoRandomPoints(individual.Tour.Count, false);
            (individual.Tour[p1], individual.Tour[p2]) = (individual.Tour[p2], individual.Tour[p1]);
        }

        private void InversionMutation(Individual individual)
        {
            (int p1, int p2) = GetTwoRandomPoints(individual.Tour.Count);
            individual.Tour.Reverse(p1, p2 - p1 + 1);
        }

        private void ScrambleMutation(Individual individual)
        {
            (int p1, int p2) = GetTwoRandomPoints(individual.Tour.Count);
            var segment = individual.Tour.GetRange(p1, p2 - p1 + 1);
            
            for (int k = segment.Count - 1; k > 0; k--)
            {
                int l = _rand.Next(k + 1);
                (segment[k], segment[l]) = (segment[l], segment[k]);
            }
            for (int k = 0; k < segment.Count; k++)
            {
                individual.Tour[p1 + k] = segment[k];
            }
        }

        private (int, int) GetTwoRandomPoints(int size, bool allowEqual = false)
        {
            int p1 = _rand.Next(size);
            int p2 = _rand.Next(size);
            if (!allowEqual)
            {
                while (p1 == p2) p2 = _rand.Next(size);
            }
            return p1 < p2 ? (p1, p2) : (p2, p1);
        }
    }
}