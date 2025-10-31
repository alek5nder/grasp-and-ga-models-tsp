using OfficeOpenXml; // Z pakietu EPPlus
using TSP_Solution.Models;
using System.IO;

namespace TSP_Solution.Utils
{
    public class ExcelReader
    {
        // Usunęliśmy parametr 'numberOfCities'
        public static TSPData ReadTSPData(string filePath)
        {
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                // Zakładamy, że dane są w pierwszym arkuszu
                var worksheet = package.Workbook.Worksheets[0];

                // Dane zaczynają się w B2 (wiersz 2, kolumna 2)
                int startRow = 2;
                int startCol = 2;

                // --- NOWA LOGIKA ---
                // Dynamicznie obliczamy liczbę miast na podstawie Twojej zasady:
                // (Całkowita liczba używanych kolumn - 1)
                // Zakładamy, że pierwsza kolumna (A) to etykiety.
                int numberOfCities = worksheet.Dimension.Columns - 1;

                // Alternatywnie, można też użyć wierszy (powinno dać to samo):
                // int numberOfCities = worksheet.Dimension.Rows - 1;

                var matrix = new double[numberOfCities, numberOfCities];

                for (int i = 0; i < numberOfCities; i++)
                {
                    for (int j = 0; j < numberOfCities; j++)
                    {
                        // Wczytujemy wartość z komórki
                        // Używamy Convert.ToDouble dla bezpieczeństwa (gdyby komórka była pusta)
                        matrix[i, j] = Convert.ToDouble(worksheet.Cells[startRow + i, startCol + j].Value);
                    }
                }
                return new TSPData(matrix);
            }
        }
        
        // --- ZASTĄP TĘ METODĘ ---
        public static PFSPData ReadPFSPData(string filePath)
        {
            var file = new FileInfo(filePath);
            using var package = new ExcelPackage(file);
            var worksheet = package.Workbook.Worksheets[0]; // Zakładamy pierwszy arkusz

            // Dane PFSP zaczynają się od wiersza 2 (wiersz 1 to nagłówki)
            int startRow = 2; 
            
            // --- POPRAWKA 1 ---
            // Zaczynamy wczytywać dane od KOLUMNY 2 (kolumna 1 to "Zadanie")
            int startCol = 2;

            // --- POPRAWKA 2 ---
            // Liczba maszyn = (Liczba wszystkich kolumn - 1)
            int numMachines = worksheet.Dimension.End.Column - 1; 
            
            // Liczba zadań = (Liczba wszystkich wierszy - 1 za nagłówek)
            int numJobs = worksheet.Dimension.End.Row - 1; 

            var processingTimes = new int[numJobs, numMachines];

            for (int i = 0; i < numJobs; i++) // Pętla po zadaniach (wiersze)
            {
                for (int j = 0; j < numMachines; j++) // Pętla po maszynach (kolumny)
                {
                    // Wiersz w Excelu = i + startRow (np. 0+2=2)
                    // Kolumna w Excelu = j + startCol (np. 0+2=2)
                    processingTimes[i, j] = Convert.ToInt32(worksheet.Cells[i + startRow, j + startCol].Value);
                }
            }

            return new PFSPData(processingTimes);
        }
    }
}