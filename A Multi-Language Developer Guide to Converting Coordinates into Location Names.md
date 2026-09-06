# Reverse Geocoding with OpenStreetMap
*A Multi-Language Developer Guide to Converting Coordinates into Location Names*

---

## 1. Executive Summary & Core Concept

**Reverse geocoding** is the process of resolving a point coordinate pair (Latitude and Longitude) into a human-readable location address, town, city, or administrative region.

OpenStreetMap (OSM) provides a free reverse geocoding API powered by the **Nominatim** search engine. Because Nominatim exposes a clean, standard HTTP REST API, developers can perform reverse geocoding from *any* programming language that can issue HTTP `GET` requests.

---

## 2. Nominatim API Specification

To perform a reverse geocode request, construct an HTTP `GET` request to the Nominatim endpoint:

`GET https://nominatim.openstreetmap.org/reverse`

### Required & Recommended Query Parameters

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `lat` | Float | Latitude of the coordinate (e.g., `32.57`). |
| `lon` | Float | Longitude of the coordinate (e.g., `34.95`). |
| `format` | String | Format of the response. Set to `json` or `jsonv2`. |
| `zoom` | Integer | Level of detail required (10 = City, 14 = Suburb/Town, 18 = Building). Default is 18. |
| `accept-language` | String | Preferred language tag for results (e.g., `en`, `es`, `he`). |

> **CRITICAL REQUIREMENT: Nominatim Usage Policy**
> 
> Nominatim enforces two essential rules for public API usage:
> 1. **Custom User-Agent Header:** Every request MUST include a valid, custom `User-Agent` identifying your application and contact information. Generic HTTP client agents (e.g., `curl`, `python-requests`) may be blocked.
> 2. **Rate Limiting:** Clients must limit requests to a maximum of **1 request per second**. For large batch jobs, implement proper delays.

---

## 3. Understanding JSON Response Structure

A typical JSON response from Nominatim contains an `address` object with fine-grained regional properties:

```json
{
  "place_id": 28412891,
  "lat": "32.5700",
  "lon": "34.9500",
  "display_name": "Zichron Ya'akov, Haifa District, Israel",
  "address": {
    "town": "Zichron Ya'akov",
    "county": "Haifa District",
    "state": "Haifa District",
    "country": "Israel",
    "country_code": "il"
  }
}

```

*Note:* Depending on the density of the area, the city-level name may appear under different keys in the `address` object: `city`, `town`, `village`, `suburb`, or `municipality`. Applications should check these keys in priority order.

---

## 4. Multi-Language Implementations

### A. JavaScript / Node.js (Fetch API)

```javascript
async function getPlaceName(lat, lon) {
  const url = `[https://nominatim.openstreetmap.org/reverse?lat=$](https://nominatim.openstreetmap.org/reverse?lat=$){lat}&lon=${lon}&format=json`;
  
  const response = await fetch(url, {
    headers: {
      'User-Agent': 'GeoApp/1.0 (contact@example.com)'
    }
  });

  if (!response.ok) throw new Error('Geocoding request failed');
  const data = await response.json();
  
  const addr = data.address || {};
  const city = addr.city || addr.town || addr.village || addr.suburb || 'Unknown Location';
  return `${city}, ${addr.country || ''}`;
}

// Example usage:
getPlaceName(32.57, 34.95).then(console.log); // "Zichron Ya'akov, Israel"

```

### B. Python (using standard `requests`)

```python
import requests


def get_place_name(lat, lon):
    url = "[https://nominatim.openstreetmap.org/reverse](https://nominatim.openstreetmap.org/reverse)"
    params = {"lat": lat, "lon": lon, "format": "json"}
    headers = {"User-Agent": "GeoApp/1.0 (contact@example.com)"}

    response = requests.get(url, params=params, headers=headers)
    if response.status_code == 200:
        data = response.json()
        address = data.get("address", {})
        city = (
            address.get("city")
            or address.get("town")
            or address.get("village")
            or address.get("suburb")
        )
        country = address.get("country", "")
        return f"{city}, {country}" if city else data.get("display_name")
    return None


print(get_place_name(32.57, 34.95))

```

### C. Java (HttpClient - Java 11+)

```java
import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;

public class Geocoder {
    public static void main(String[] args) throws Exception {
        double lat = 32.57;
        double lon = 34.95;
        String url = String.format("[https://nominatim.openstreetmap.org/reverse?lat=%f&lon=%f&format=json](https://nominatim.openstreetmap.org/reverse?lat=%f&lon=%f&format=json)", lat, lon);

        HttpClient client = HttpClient.newHttpClient();
        HttpRequest request = HttpRequest.newBuilder()
                .uri(URI.create(url))
                .header("User-Agent", "GeoApp/1.0 (contact@example.com)")
                .GET()
                .build();

        HttpResponse<String> response = client.send(request, HttpResponse.BodyHandlers.ofString());
        System.out.println(response.body());
    }
}

```

### D. cURL (Command Line)

```bash
curl -A "GeoApp/1.0 (contact@example.com)" \
  "[https://nominatim.openstreetmap.org/reverse?lat=32.57&lon=34.95&format=json](https://nominatim.openstreetmap.org/reverse?lat=32.57&lon=34.95&format=json)"

```

### E. Go (`net/http`)

```go
package main

import (
	"fmt"
	"io"
	"net/http"
)

func main() {
	url := "[https://nominatim.openstreetmap.org/reverse?lat=32.57&lon=34.95&format=json](https://nominatim.openstreetmap.org/reverse?lat=32.57&lon=34.95&format=json)"
	req, _ := http.NewRequest("GET", url, nil)
	req.Header.Set("User-Agent", "GeoApp/1.0 (contact@example.com)")

	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		panic(err)
	}
	defer resp.Body.Close()

	body, _ := io.ReadAll(resp.Body)
	fmt.Println(string(body))
}

```

### F. PHP (cURL)

```php
<?php
$lat = 32.57;
$lon = 34.95;
$url = "[https://nominatim.openstreetmap.org/reverse?lat=](https://nominatim.openstreetmap.org/reverse?lat=){$lat}&lon={$lon}&format=json";

$ch = curl_init();
curl_setopt($ch, CURLOPT_URL,$url);
curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
curl_setopt($ch, CURLOPT_USERAGENT, "GeoApp/1.0 (contact@example.com)");

$response = curl_exec($ch);
curl_close($ch);

$data = json_decode($response, true);
echo $data['display_name'];
?>

```

### G. C# / .NET (`HttpClient`)

```csharp
using System;
using System.Net.Http;
using System.Threading.Tasks;

class Program {
    static async Task Main() {
        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "GeoApp/1.0 (contact@example.com)");
        
        string url = "[https://nominatim.openstreetmap.org/reverse?lat=32.57&lon=34.95&format=json](https://nominatim.openstreetmap.org/reverse?lat=32.57&lon=34.95&format=json)";
        string response = await client.GetStringAsync(url);
        
        Console.WriteLine(response);
    }
}

```

---

## 5. Key Best Practices

* **Fallback Logic:** Always check `city` -> `town` -> `village` -> `suburb` -> `county` sequentially to handle varying global density metadata.
* **Caching:** Store coordinates and resolved address pairs in a database or Redis cache to avoid duplicate API queries.
* **Self-Hosting:** For high-volume production applications (exceeding 1 req/sec), host your own Nominatim server using OpenStreetMap data via Docker.

