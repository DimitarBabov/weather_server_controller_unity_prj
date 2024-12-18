using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class UpdateServerGribFile : MonoBehaviour
{
    [Header("GFS Data Request Settings")]
    public string serverUrl = "http://127.0.0.1:5000/fetch_gfs_data";  // URL of the Flask server
    public string deleteFilesUrl = "http://127.0.0.1:5000/delete-files";  // URL for deleting files
    public string generatePngsUrl = "http://127.0.0.1:5000/generate_pngs";  // URL for generating PNGs
    [Header("Parameters Settings")]
    public string[] param_level_pairs;  // List of parameter and level pairs (e.g., "HGT surface", "TMP 500_mb")
    public int numForecasts = 4;  // Number of forecast periods to fetch (e.g., 4)
    [Header("Latitude Longitude Settings")]
    // Geographic bounds for the data request
    public float minLat = 20f;  // Minimum latitude
    public float maxLat = 55f;   // Maximum latitude
    public float minLon = 230f; // Minimum longitude
    public float maxLon = 300f;   // Maximum longitude

   
    public bool fetchGfs = true;

    public TextMeshProUGUI requestResult;
   
    public void UpdateGribFiles()
    {
        fetchGfs = true;
        StartCoroutine(DeleteFilesAndFetchGfsData());
    }
    
    public void UpdatePngs()
    {
        fetchGfs= false;
        StartCoroutine(DeleteFilesAndFetchGfsData());

    }
    /*
    private void OnEnable()
    {
        // Start the process by first deleting old files and then fetching new GFS data
        StartCoroutine(DeleteFilesAndFetchGfsData());
    }*/

    private IEnumerator DeleteFilesAndFetchGfsData()
    {
        if (fetchGfs)
        {
            // Step 1: Delete old files on the server
            using (UnityWebRequest deleteRequest = UnityWebRequest.PostWwwForm(deleteFilesUrl, ""))
            {
                // Wait for the request to complete
                yield return deleteRequest.SendWebRequest();

                // Check for errors during deletion
                if (deleteRequest.result != UnityWebRequest.Result.Success)
                {
                    // Debug.LogError($"Error deleting files: {deleteRequest.error}");
                    requestResult.text = $"Error deleting files: {deleteRequest.error}";
                    yield break;  // Stop execution if file deletion fails
                }
                else
                {
                    Debug.Log("Old files deleted successfully.");
                    requestResult.text = "Old files deleted successfully.";
                }
            }

            // Step 2: After deleting files, fetch new GFS data for each parameter-level pair
            yield return StartCoroutine(FetchGfsDataSequentially());
        }
        else
            StartCoroutine(GeneratePngsSequentially());
            
    }

    private IEnumerator GeneratePngsSequentially()
    {
        // Loop through each parameter-level pair in the list and fetch GFS data
        foreach (string pair in param_level_pairs)
        {
            // Split the pair by space to get the parameter and the level
            string[] parts = pair.Split(' ');
            if (parts.Length != 2)
            {
                requestResult.text = ($"Invalid param-level pair: {pair}. It should be in the format 'param level'.");
                continue;
            }

            string param = parts[0];  // Extract the parameter (e.g., "HGT")
            string level = parts[1];  // Extract the level (e.g., "surface" or "500_mb")

            StartCoroutine(GeneratePngs(param, level, minLat, maxLat, minLon, maxLon));

            // Optionally, add a small delay between requests (to avoid overwhelming the server)
            yield return new WaitForSeconds(1f);
        }

        requestResult.text = ("Finished generating PNGs for all param-level pairs.");
    }
    private IEnumerator FetchGfsDataSequentially()
    {
        // Loop through each parameter-level pair in the list and fetch GFS data
        foreach (string pair in param_level_pairs)
        {
            // Split the pair by space to get the parameter and the level
            string[] parts = pair.Split(' ');
            if (parts.Length != 2)
            {
                requestResult.text = ($"Invalid param-level pair: {pair}. It should be in the format 'param level'.");
                continue;
            }

            string param = parts[0];  // Extract the parameter (e.g., "HGT")
            string level = parts[1];  // Extract the level (e.g., "surface" or "500_mb")

            // Construct the request URL with the specified parameter, level, and forecast count
            string requestUrl = $"{serverUrl}?param={param}&level={level}&forecasts={numForecasts}";

            // Send the request to the Flask server to fetch GFS data for the current parameter-level pair
            using (UnityWebRequest webRequest = UnityWebRequest.Get(requestUrl))
            {
                // Wait for the request to complete
                yield return webRequest.SendWebRequest();

                // Check for errors
                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    requestResult.text = ($"Error fetching GFS data for {param} at {level}: {webRequest.error}");
                    continue;
                }
                else
                {
                    // Handle the response (GFS data fetched successfully)
                    string responseText = webRequest.downloadHandler.text;
                    requestResult.text = ($"GFS data fetched successfully for {param} at {level}: {responseText}");

                    // Step 3: Generate PNGs only if GFS data fetch was successful
                    yield return StartCoroutine(GeneratePngs(param, level, minLat, maxLat, minLon, maxLon));
                }
            }

            // Optionally, add a small delay between requests (to avoid overwhelming the server)
            yield return new WaitForSeconds(1f);
        }

        requestResult.text = ("Finished fetching GFS data and\n generating XR assets for all param-level pairs.");
    }

    private IEnumerator GeneratePngs(string param, string level, float minLat, float maxLat, float minLon, float maxLon)
    {
        // Construct the request URL with parameters and geographic bounds
        string requestUrl = $"{generatePngsUrl}?param={param}&level={level}&min_lat={minLat}&max_lat={maxLat}&min_lon={minLon}&max_lon={maxLon}";
        requestResult.text = ($"Sending XR assets generation request \nto server for {param} at {level}: {requestUrl}");

        // Send the request to the Flask server to generate PNGs and meta files
        using (UnityWebRequest webRequest = UnityWebRequest.Get(requestUrl))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                requestResult.text = ($"Error generating XR assets for {param} at {level}: {webRequest.error}");
                yield break;
            }

            // Parse the JSON response to verify success
            PngMetaResponse response = JsonUtility.FromJson<PngMetaResponse>(webRequest.downloadHandler.text);

            if (response.results.Length == 0)
            {
                requestResult.text = ($"No XR assets on the server for {param} at {level}.");
                yield break;
            }

            requestResult.text = ($"XR assets successfully generated on the server for {param} at {level}.");
        }
    }

    // Class to deserialize the JSON response from the server
    [System.Serializable]
    public class PngMetaResponse
    {
        public Result[] results;
    }

    [System.Serializable]
    public class Result
    {
        public string png_download_url;
        public string meta_download_url;
    }
}
