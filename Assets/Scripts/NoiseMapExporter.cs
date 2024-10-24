using System.IO;
using UnityEngine;

public static class NoiseMapExporter
{
    public static NoiseSettings noiseSettings;
    public static Vector2 sampleCenter = Vector2.zero;


    // Function to generate the noise map and save it to a CSV file
    public static void ExportNoiseMap(float[,] noiseMap)
    {
        int mapWidth = noiseMap.GetLength(0);
        int mapHeight = noiseMap.GetLength(1);
        // Define the file path to save the noise map
        string path = Application.dataPath + "/NoiseMapData.csv";
        
        using (StreamWriter writer = new StreamWriter(path))
        {
            // Write the noise map to a CSV file
            for (int i = 0; i < mapWidth; i++)
            {
                for (int j = 0; j < mapHeight; j++)
                {
                    writer.Write(noiseMap[i, j] + ",");
                }
                writer.WriteLine();
            }
        }

        Debug.Log("Noise map exported to " + path);
    }
}
