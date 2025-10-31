using System;
using System.Collections.Generic;
using System.Diagnostics; 
using System.Linq;
using TSP_Solution.Models;
using TSP_Solution.Utils;
using TSP_Solution.Algorithms; // Upewnij się, że ten using jest (dla NEH)
using OfficeOpenXml;
using System.ComponentModel;

namespace TSP_Solution
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Uruchamianie PEŁNEJ ANALIZY ACO (z NEH Seed) dla PFSP...");
            ExcelPackage.License.SetNonCommercialPersonal("TSP Project User");
            
            // --- Konfiguracja Globalna ---
            string instanceFile = @"Dane\Dane_PFSP\Dane_PFSP_200_10.xlsx";
            string outputFile = "Results_ACO_NEH_init_PFSP_200_10.xlsx"; 
            int runs = 5; 

            // --- 1. Definicja Parametrów do Testów ---
            var alphas = new List<double> { 1.0, 2.0, 3.0,4.0 }; 
            var rhos = new List<double> { 0.1, 0.3, 0.5, 0.7 };
            var antCounts = new List<int> { 25, 50, 100, 200 };
            
            int iterations = 200;   
            double beta = 1.0;      
            double q = 1.0;         

            // --- 2. Wczytaj Dane (PFSP) ---
            Console.WriteLine($"Wczytywanie danych z {instanceFile}...");
            PFSPData data = ExcelReader.ReadPFSPData(instanceFile); 
            Console.WriteLine($"Wczytano: {data.NumberOfJobs} zadań, {data.NumberOfMachines} maszyn.");

            // --- 3. USPRAWNIENIE: Uruchom NEH (TYLKO RAZ) ---
            Console.WriteLine("Uruchamianie NEH w celu 'zasiania' feromonów...");
            var nehAlgorithm = new NehAlgorithm(data);
            Individual nehSolution = nehAlgorithm.Run();
            nehSolution.CalculateFitness(data); // Oblicz Cmax (jeśli NEH sam tego nie zrobił)
            Console.WriteLine($"--- NEH Zakończony. Cmax do 'zasiania': {nehSolution.Fitness} ---");


            // --- 4. Uruchom Główną Pętlę Eksperymentów ---
            var allResults = new List<ResultRecord>();
            var totalStopwatch = Stopwatch.StartNew();

            int experimentCount = alphas.Count * rhos.Count * antCounts.Count;
            int currentExperiment = 0;

            foreach (var alpha in alphas)
            {
                foreach (var rho in rhos)
                {
                    foreach (var antCount in antCounts)
                    {
                        currentExperiment++;
                        string paramString = $"Alpha:{alpha},Rho:{rho},Ants:{antCount} (Iter:{iterations},Beta:{beta})";
                        Console.WriteLine($"Uruchamianie testu {currentExperiment}/{experimentCount}: {paramString}");

                        var runDistances = new List<double>();
                        var runStopwatch = Stopwatch.StartNew();
                        Individual bestOverallRun = null;

                        for (int i = 0; i < runs; i++)
                        {
                            // --- ZMIANA: Przekaż 'nehSolution' do konstruktora ---
                            var aco = new AcoAlgorithm(data, antCount, iterations, alpha, beta, rho, q, nehSolution);
                            var bestOfRun = aco.Run(); 
                                                                                   
                            runDistances.Add(bestOfRun.Fitness);
                            if (bestOverallRun == null || bestOfRun.Fitness < bestOfRun.Fitness)
                            {
                                bestOverallRun = bestOfRun;
                            }
                        }
                        runStopwatch.Stop();

                        // --- Przetwórz wyniki ---
                        double bestDistance = runDistances.Min(); 
                        double avgDistance = runDistances.Average(); 
                        double avgTime = runStopwatch.Elapsed.TotalSeconds / runs; 

                        var resultRecord = new ResultRecord
                        {
                            Algorithm = $"ACO-NEH (PFSP {data.NumberOfJobs}x{data.NumberOfMachines})", // Lepsza nazwa
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

            // --- 5. Zapisz WSZYSTKIE Wyniki ---
            ExcelWriter.WriteResults(outputFile, allResults);

            Console.WriteLine("--- ANALIZA ACO-NEH PFSP ZAKOŃCZONA ---");
            Console.WriteLine($"Zapisano {allResults.Count} rekordów do {outputFile}");
            Console.WriteLine($"Całkowity czas analizy: {totalStopwatch.Elapsed.TotalMinutes:F2} minut.");
        }
    }
}