using System;
using System.Collections.Generic;
using System.Diagnostics; 
using System.Linq;
using TSP_Solution.Models;
using TSP_Solution.Utils;
using TSP_Solution.Algorithms;
using OfficeOpenXml;
using System.ComponentModel;

namespace TSP_Solution
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Uruchamianie PEŁNEJ ANALIZY Algorytmu Genetycznego...");
            ExcelPackage.License.SetNonCommercialPersonal("TSP Project User");
            
            // --- Konfiguracja Globalna ---
            string instanceFile = "Dane_TSP_127.xlsx";
            string outputFile = "Results_127.xlsx";
            int runs = 5; // Liczba uruchomień dla uśrednienia 

            // --- 1. Definicja Parametrów do Testów ---
            // Spełniamy 4 parametry: Wielkość Populacji, Metoda Selekcji, Metoda Krzyżowania i Metoda Mutacji.
            
            // Parametry testowane (pętle)
            var populationSizes = new List<int> { 50,100,200,500};
            var selectionMethods = new List<SelectionMethod> { SelectionMethod.Tournament, SelectionMethod.Roulette, SelectionMethod.Ranking }; // Parametr 1 (3 wartości) [cite: 10]
            var crossoverMethods = new List<CrossoverMethod> { CrossoverMethod.PMX, CrossoverMethod.OX, CrossoverMethod.CX }; // Parametr 2 (3 wartości) [cite: 10]
            var mutationMethods = new List<MutationMethod> { MutationMethod.Swap, MutationMethod.Inversion, MutationMethod.Scramble }; // Parametr 3 (3 wartości) [cite: 9, 19]

            // Parametry stałe (ustawione na sztywno)
            int generations = 1000;     // Stała wartość
            double mutationRate = 0.05; // Stała wartość (testujemy typ mutacji, nie jej p-stwo)
            
            // Parametry pomocnicze
            int tournamentSize = 5; // Używane tylko gdy SelectionMethod.Tournament


            // --- 2. Wczytaj Dane (Tylko raz) ---
            Console.WriteLine($"Wczytywanie danych z {instanceFile}...");
            TSPData data = ExcelReader.ReadTSPData(instanceFile); 
            Console.WriteLine($"Wczytano: {data.NumberOfCities} miast.");

            // --- 3. Uruchom Główną Pętlę Eksperymentów ---
            var allResults = new List<ResultRecord>();
            var totalStopwatch = Stopwatch.StartNew();

            // ZMODYFIKOWANE: Liczba eksperymentów: 4*3*3*3 = 108
            int experimentCount = populationSizes.Count * selectionMethods.Count * crossoverMethods.Count * mutationMethods.Count;
            int currentExperiment = 0;

            // PĘTLA 1: Wielkość Populacji (NOWA)
            foreach (var popSize in populationSizes)
            {
                // PĘTLA 2: Metoda Selekcji
                foreach (var selMethod in selectionMethods)
                {
                    // PĘTLA 3: Metoda Krzyżowania
                    foreach (var crossMethod in crossoverMethods)
                    {
                        // PĘTLA 4: Metoda Mutacji (Sąsiedztwo)
                        foreach (var mutMethod in mutationMethods)
                        {
                            currentExperiment++;
                            // ZMODYFIKOWANE: zaktualizowany string parametrów
                            string paramString = $"Pop:{popSize},Sel:{selMethod},Cross:{crossMethod},MutM:{mutMethod} (Gen:{generations},MutRate:{mutationRate})";
                            Console.WriteLine($"Uruchamianie testu {currentExperiment}/{experimentCount}: {paramString}");

                            var runDistances = new List<double>();
                            var runStopwatch = Stopwatch.StartNew();
                            Individual bestOverallRun = null;

                            for (int i = 0; i < runs; i++)
                            {
                                // ZMODYFIKOWANE: wywołanie konstruktora używa teraz 'popSize' z pętli
                                var ga = new GeneticAlgorithm(data, popSize, mutationRate, tournamentSize, generations,
                                                              selMethod, crossMethod, mutMethod);
                                
                                var bestOfRun = ga.Run(); 
                                
                                runDistances.Add(bestOfRun.Fitness);
                                if (bestOverallRun == null || bestOfRun.Fitness < bestOverallRun.Fitness)
                                {
                                    bestOverallRun = bestOfRun;
                                }
                            }
                            runStopwatch.Stop();

                            // --- Przetwórz wyniki dla tej kombinacji parametrów ---
                            double bestDistance = runDistances.Min(); // [cite: 16]
                            double avgDistance = runDistances.Average(); // [cite: 16]
                            double avgTime = runStopwatch.Elapsed.TotalSeconds / runs; // [cite: 17]

                            var resultRecord = new ResultRecord
                            {
                                Algorithm = $"GA ({instanceFile})",
                                InstanceName = instanceFile,
                                Parameters = paramString,
                                BestDistance = bestDistance,  
                                AvgDistance = avgDistance,    
                                TimeInSeconds = avgTime,      
                                BestTour = string.Join("-", bestOverallRun.Tour)
                            };
                            allResults.Add(resultRecord);
                        }
                    }
                }
            } // Koniec pętli popSize

            totalStopwatch.Stop();

            // --- 4. Zapisz WSZYSTKIE Wyniki (Tylko raz) ---
            ExcelWriter.WriteResults(outputFile, allResults);

            Console.WriteLine("--- ANALIZA ZAKOŃCZONA ---");
            Console.WriteLine($"Zapisano {allResults.Count} rekordów do {outputFile}");
            Console.WriteLine($"Całkowity czas analizy: {totalStopwatch.Elapsed.TotalMinutes:F2} minut.");
        }
    }
}