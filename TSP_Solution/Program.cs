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
            Console.WriteLine("Uruchamianie PEŁNEJ ANALIZY Algorytmu Mrówkowego (ACO)...");
            ExcelPackage.License.SetNonCommercialPersonal("TSP Project User");
            
            // --- Konfiguracja Globalna ---
            // Użyj dużej instancji - ACO jest szybkie!
            string instanceFile = "Dane_TSP_127.xlsx"; 
            string outputFile = "Results_ACO_127.xlsx"; 
            
            int runs = 5; // Liczba uruchomień dla uśrednienia 

            // --- 1. Definicja Parametrów do Testów (Testujemy 3 parametry) ---
            
            // Parametr 1: Wpływ feromonu (Alpha)
            var alphas = new List<double> { 1.0, 2.0, 3.0,4.0 };
            
            // Parametr 2: Wpływ heurystyki (Beta)
            var betas = new List<double> { 1.0, 3.0, 5.0,6.0 };
            
            // Parametr 3: Współczynnik parowania (Rho)
            var rhos = new List<double> { 0.1, 0.3, 0.5, 0.7 }; // (4 wartości - spełnia wymóg min. 4)
            
            // --- Parametry Stałe ---
            int antCount = 50;      // Liczba mrówek
            int iterations = 200;   // Liczba generacji
            double q = 1.0;         // Stała składania feromonu


            // --- 2. Wczytaj Dane (Tylko raz) ---
            Console.WriteLine($"Wczytywanie danych z {instanceFile}...");
            TSPData data = ExcelReader.ReadTSPData(instanceFile); 
            Console.WriteLine($"Wczytano: {data.NumberOfCities} miast.");

            // --- 3. Uruchom Główną Pętlę Eksperymentów ---
            var allResults = new List<ResultRecord>();
            var totalStopwatch = Stopwatch.StartNew();

            // Liczba eksperymentów: 3 * 3 * 4 = 36
            int experimentCount = alphas.Count * betas.Count * rhos.Count;
            int currentExperiment = 0;

            // PĘTLA 1: Alpha
            foreach (var alpha in alphas)
            {
                // PĘTLA 2: Beta
                foreach (var beta in betas)
                {
                    // PĘTLA 3: Rho
                    foreach (var rho in rhos)
                    {
                        currentExperiment++;
                        string paramString = $"Alpha:{alpha},Beta:{beta},Rho:{rho} (Iter:{iterations},Ants:{antCount})";
                        Console.WriteLine($"Uruchamianie testu {currentExperiment}/{experimentCount}: {paramString}");

                        var runDistances = new List<double>();
                        var runStopwatch = Stopwatch.StartNew();
                        Individual bestOverallRun = null;

                        for (int i = 0; i < runs; i++)
                        {
                            var aco = new AcoAlgorithm(data, antCount, iterations, alpha, beta, rho, q);
                            var bestOfRun = aco.Run(); 
                            
                            runDistances.Add(bestOfRun.Fitness);
                            if (bestOverallRun == null || bestOfRun.Fitness < bestOfRun.Fitness)
                            {
                                bestOverallRun = bestOfRun;
                            }
                        }
                        runStopwatch.Stop();

                        // --- Przetwórz wyniki dla tej kombinacji parametrów ---
                        double bestDistance = runDistances.Min(); 
                        double avgDistance = runDistances.Average(); 
                        double avgTime = runStopwatch.Elapsed.TotalSeconds / runs; 

                        var resultRecord = new ResultRecord
                        {
                            Algorithm = $"ACO ({instanceFile})",
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
            } // Koniec pętli

            totalStopwatch.Stop();

            // --- 4. Zapisz WSZYSTKIE Wyniki (Tylko raz) ---
            ExcelWriter.WriteResults(outputFile, allResults);

            Console.WriteLine("--- ANALIZA ACO ZAKOŃCZONA ---");
            Console.WriteLine($"Zapisano {allResults.Count} rekordów do {outputFile}");
            Console.WriteLine($"Całkowity czas analizy: {totalStopwatch.Elapsed.TotalMinutes:F2} minut.");
        }
    }
}