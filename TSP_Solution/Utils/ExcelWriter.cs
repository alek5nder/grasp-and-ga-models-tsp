using OfficeOpenXml;
using TSP_Solution.Models;
using System.Collections.Generic;
using System.IO;

namespace TSP_Solution.Utils
{
    public class ExcelWriter
    {
        public static void WriteResults(string filePath, List<ResultRecord> results)
        {
            
            var file = new FileInfo(filePath);
            
            // Usuwamy plik jeśli istnieje, dla uproszczenia tego testu
            if (file.Exists) 
            {
                file.Delete();
            }

            using (var package = new ExcelPackage(file))
            {
                var worksheet = package.Workbook.Worksheets.Add("Results");
                
                // Zapisz nagłówki
                worksheet.Cells[1, 1].Value = "Algorithm";
                worksheet.Cells[1, 2].Value = "InstanceName";
                worksheet.Cells[1, 3].Value = "Parameters";
                worksheet.Cells[1, 4].Value = "BestDistance";
                worksheet.Cells[1, 5].Value = "AvgDistance";
                worksheet.Cells[1, 6].Value = "TimeInSeconds";
                worksheet.Cells[1, 7].Value = "BestTour";

                // Zapisz dane
                for (int i = 0; i < results.Count; i++)
                {
                    var record = results[i];
                    int row = i + 2;
                    worksheet.Cells[row, 1].Value = record.Algorithm;
                    worksheet.Cells[row, 2].Value = record.InstanceName;
                    worksheet.Cells[row, 3].Value = record.Parameters;
                    worksheet.Cells[row, 4].Value = record.BestDistance;
                    worksheet.Cells[row, 5].Value = record.AvgDistance;
                    worksheet.Cells[row, 6].Value = record.TimeInSeconds;
                    worksheet.Cells[row, 7].Value = record.BestTour;
                }
                
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                package.Save();
            }
        }
    }
}