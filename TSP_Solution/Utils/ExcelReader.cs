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
    }
}