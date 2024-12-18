using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;  // Import TextMeshPro namespace
using SimpleJSON;
using System.Collections.Generic;  // Import SimpleJSON for JSON parsing

public class WeatherRequest : MonoBehaviour
{
    public TMP_InputField zipCodeInputField;  // Reference to the InputField for ZIP code
    public TMP_Text responseText;             // Reference to a TextMeshPro Text element to display the response
    private string initZipCode = "08736";
    // URLs for weather data and forecast
    //private string apiUrl = "https://weathermodelstadium.ue.r.appspot.com//weather/zip?zip=";  // Local server URL for current weather
    //private string forecastApiUrl = "https://weathermodelstadium.ue.r.appspot.com//weather/forecast/zip?zip="; // Local server URL for forecast

    public string apiUrl = "http://127.0.0.1:5000//weather/zip?zip=";  // Local server URL for current weather
    public string forecastApiUrl = "http://127.0.0.1:5000//weather/forecast/zip?zip="; //
    private void Start()
    {
        zipCodeInputField.text = initZipCode;
    }
    // Function called when the user clicks the "Get Weather" button
    public void OnGetWeatherButtonClick()
    {
        // Get the ZIP code from the input field
        string zipCode = zipCodeInputField.text;

        // If the ZIP code is not empty, start the request
        if (!string.IsNullOrEmpty(zipCode))
        {
            string requestUrl = apiUrl + zipCode;
            StartCoroutine(GetWeatherData(requestUrl,false));
        }
        else
        {
            responseText.text = "Please enter a valid ZIP code.";
        }
    }
    public void OnGetWeatherDetailedButtonClick()
    {
        // Get the ZIP code from the input field
        string zipCode = zipCodeInputField.text;

        // If the ZIP code is not empty, start the request
        if (!string.IsNullOrEmpty(zipCode))
        {
            string requestUrl = apiUrl + zipCode;
            StartCoroutine(GetWeatherData(requestUrl,true));
        }
        else
        {
            responseText.text = "Please enter a valid ZIP code.";
        }
    }

    // Function called when the user clicks the "Get Forecast" button
    public void OnForecastButtonClick()
    {
        // Get the ZIP code from the input field
        string zipCode = zipCodeInputField.text;

        // If the ZIP code is not empty, start the request
        if (!string.IsNullOrEmpty(zipCode))
        {
            string requestUrl = forecastApiUrl + zipCode;
            StartCoroutine(GetForecastData(requestUrl));
        }
        else
        {
            responseText.text = "Please enter a valid ZIP code.";
        }
    }

    // Coroutine to make an HTTP request and get the weather data
    IEnumerator GetWeatherData(string url, bool isDetailed)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // Send the request and wait for a response
            yield return request.SendWebRequest();

            // Check for network errors
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + request.error);
                responseText.text = "Error: " + request.error;
            }
            else
            {
                // Get the JSON response
                string jsonResponse = request.downloadHandler.text;
                Debug.Log("Weather Data: " + jsonResponse);

                // Parse the weather data
                var jsonData = SimpleJSON.JSON.Parse(jsonResponse);

                // Get the station name directly from the server response
                string stationName = jsonData["stationName"];

                // Format and display the weather data
                if(isDetailed)
                    responseText.text = FormatJsonResponseDetailed(jsonData, stationName);
                else
                    responseText.text = FormatJsonResponse(jsonData, stationName);
            }
        }
    }

    // Coroutine to make an HTTP request and get the forecast data
    IEnumerator GetForecastData(string url)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // Send the request and wait for a response
            yield return request.SendWebRequest();

            // Check for network errors
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + request.error);
                responseText.text = "Error: " + request.error;
            }
            else
            {
                // Get the JSON response
                string jsonResponse = request.downloadHandler.text;
                Debug.Log("Forecast Data: " + jsonResponse);

                // Parse the forecast data
                var jsonData = SimpleJSON.JSON.Parse(jsonResponse);

                // Format and display the forecast data
                responseText.text = FormatForecastJsonResponse(jsonData);
            }
        }
    }

    // Function to format the JSON response for weather data
    string FormatJsonResponse(JSONNode jsonData, string stationName)
    {
        string formattedResponse = $"Live Weather Data (Station: {stationName}):\n";

        // Extract temperature in Celsius and convert to Fahrenheit
        float temperatureCelsius = jsonData["temperature"]["value"].AsFloat;
        float temperatureFahrenheit = (temperatureCelsius * 9 / 5) + 32; // Convert to Fahrenheit

        // Extract wind speed, humidity, pressure, and precipitation
        float windSpeed = jsonData["windSpeed"]["value"].AsFloat;
        float humidity = jsonData["relativeHumidity"]["value"].AsFloat;
        float pressure = jsonData["barometricPressure"]["value"].AsFloat / 100; // Convert to hPa
        float precipitation = jsonData["precipitationLast3Hours"]["value"].AsFloat;

        // Extract dew point in Celsius and convert to Fahrenheit if available
        string dewPoint = "N/A";
        if (jsonData.HasKey("dewpoint") && jsonData["dewpoint"].HasKey("value"))
        {
            float dewPointCelsius = jsonData["dewpoint"]["value"].AsFloat;
            float dewPointFahrenheit = (dewPointCelsius * 9 / 5) + 32; // Convert to Fahrenheit
            dewPoint = dewPointFahrenheit.ToString("F1") + " °F";
        }

        // Extract heat index in Celsius and convert to Fahrenheit if available
        string heatIndex = "N/A";
        if (jsonData.HasKey("heatIndex") && jsonData["heatIndex"].HasKey("value"))
        {
            float heatIndexCelsius = jsonData["heatIndex"]["value"].AsFloat;
            float heatIndexFahrenheit = (heatIndexCelsius * 9 / 5) + 32; // Convert to Fahrenheit
            heatIndex = heatIndexFahrenheit.ToString("F1") + " °F";
        }

        // Format the response to display the values in Fahrenheit
        formattedResponse += $"Temperature: {temperatureFahrenheit:F1} °F\n";
        formattedResponse += $"Wind Speed: {windSpeed:F1} m/s\n";
        formattedResponse += $"Humidity: {humidity:F1} %\n";
        formattedResponse += $"Barometric Pressure: {pressure:F1} hPa\n";
        formattedResponse += $"Precipitation (last 3 hours): {precipitation:F1} mm\n";
        formattedResponse += $"Dew Point: {dewPoint}\n";
        formattedResponse += $"Heat Index: {heatIndex}\n";

        return formattedResponse;
    }


    // Function to format the JSON response fordetailed weather data
    string FormatJsonResponseDetailed(JSONNode jsonData, string stationName)
    {
        string formattedResponse = $"Live Weather Data (Station: {stationName}):\n";

        // Iterate over each key-value pair in the JSON data to dynamically list all available parameters
        foreach (KeyValuePair<string, JSONNode> kvp in jsonData)
        {
            string parameterName = kvp.Key;
            JSONNode parameterValueNode = kvp.Value;
            string parameterValue;

            // Check if the value node has a "value" property
            if (parameterValueNode.HasKey("value"))
            {
                parameterValue = parameterValueNode["value"].ToString();
            }
            else
            {
                // If not, get the raw value as a string
                parameterValue = parameterValueNode.ToString();
            }

            // Append the parameter and its value to the formatted response
            formattedResponse += $"{parameterName}: {parameterValue}\n";
        }

        return formattedResponse;
    }

    // Function to format the JSON response for forecast data
    string FormatForecastJsonResponse(JSONNode jsonData)
    {
        string formattedResponse = "Forecast Data:\n";

        // Iterate over each forecast period and format the data
        foreach (System.Collections.Generic.KeyValuePair<string, JSONNode> periodPair in jsonData)
        {
            // Access the JSONNode using the Value property
            JSONNode period = periodPair.Value;

            // Extract data from the JSONNode
            string name = period["name"];
            string detailedForecast = period["detailedForecast"];
            string temperature = period["temperature"] + " " + period["temperatureUnit"];
            string windSpeed = period["windSpeed"];
            string windDirection = period["windDirection"];
            string iconUrl = period["icon"];

            // Format the response
            formattedResponse += $"\n{name}:\n" +
                                 $"Temperature: {temperature}\n" +
                                 $"Wind: {windSpeed} {windDirection}\n" +
                                 $"Forecast: {detailedForecast}\n";
        }


        return formattedResponse;
    }

    string FormatDetailedForecastResponse(JSONNode jsonData)
    {
        string formattedResponse = "Forecast Data:\n";

        // Iterate over each forecast period and format the data
        foreach (KeyValuePair<string, JSONNode> periodPair in jsonData)
        {
            // Access the JSONNode using the Value property
            JSONNode period = periodPair.Value;

            // Extract data from the JSONNode
            string name = period["name"];
            string startTime = period["startTime"];
            string endTime = period["endTime"];
            bool isDaytime = period["isDaytime"].AsBool;
            string shortForecast = period["shortForecast"];
            string detailedForecast = period["detailedForecast"];
            string temperature = period["temperature"] + " " + period["temperatureUnit"];
            string windSpeed = period["windSpeed"];
            string windDirection = period["windDirection"];
            string iconUrl = period["icon"];  // URL to the weather icon
            string probabilityOfPrecipitation = period["probabilityOfPrecipitation"]["value"];
            string relativeHumidity = period["relativeHumidity"]["value"];

            // Format the response
            formattedResponse += $"\n{name} ({(isDaytime ? "Day" : "Night")}):\n" +
                                 $"Start: {startTime}\n" +
                                 $"End: {endTime}\n" +
                                 $"Temperature: {temperature}\n" +
                                 $"Wind: {windSpeed} {windDirection}\n" +
                                 $"Short Forecast: {shortForecast}\n" +
                                 $"Forecast: {detailedForecast}\n" +
                                 $"Precipitation Probability: {probabilityOfPrecipitation}%\n" +
                                 $"Humidity: {relativeHumidity}%\n" +
                                 $"Icon: {iconUrl}\n"; // You can choose how to display this, e.g., show the image in UI
        }

        return formattedResponse;
    }

    // Coroutine to make an HTTP request and get the detailed forecast data
    IEnumerator GetDetailedForecastData(string url)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // Send the request and wait for a response
            yield return request.SendWebRequest();

            // Check for network errors
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + request.error);
                responseText.text = "Error: " + request.error;
            }
            else
            {
                // Get the JSON response
                string jsonResponse = request.downloadHandler.text;
                Debug.Log("Forecast Data: " + jsonResponse);

                // Parse the forecast data
                var jsonData = SimpleJSON.JSON.Parse(jsonResponse);

                // Format and display the forecast data
                responseText.text = FormatDetailedForecastResponse(jsonData);
            }
        }
    }

    // Function called when the user clicks the "Get Detailed Forecast" button
    public void OnDetailedForecastButtonClick()
    {
        // Get the ZIP code from the input field
        string zipCode = zipCodeInputField.text;

        // If the ZIP code is not empty, start the request
        if (!string.IsNullOrEmpty(zipCode))
        {
            string requestUrl = forecastApiUrl + zipCode;
            StartCoroutine(GetDetailedForecastData(requestUrl));
        }
        else
        {
            responseText.text = "Please enter a valid ZIP code.";
        }
    }



}
