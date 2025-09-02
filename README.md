🎬 FlickBinge

FlickBinge is a microservices-based movie discovery platform built with .NET 9.
It integrates with external APIs (currently OMDb API) to provide movie data and is designed to grow into a distributed system with multiple independent services.

🏗️ Architecture

FlickBinge follows a microservices architecture where each domain feature runs as an independent service.

Current Services

🎥 MovieService.Api

Fetches movie details from OMDb API

Exposes REST endpoints for client apps

Planned Services

👤 AuthService – User authentication & JWT-based security

⭐ FavoritesService – Save & manage favorite movies

🧠 RecommendationService – Suggests movies based on user history

📊 AnalyticsService – Collects and reports usage data

📂 Repository Structure
FlickBinge/
│── MovieService.Api/        # Current microservice (movies)
│   ├── Controllers/         
│   ├── Models/              
│   ├── Services/            
│   ├── Program.cs           
│   └── appsettings.json     
│
└── README.md


Each service will live in its own project folder and can be deployed independently.

⚙️ Setup & Run
1️⃣ Clone the repo
git clone https://github.com/your-username/FlickBinge.git
cd FlickBinge/MovieService.Api

2️⃣ Get an OMDb API Key

Register for a free API key 👉 OMDb API

3️⃣ Configure API Key
Option A: appsettings.json
{
  "OMDb": {
    "ApiKey": "YOUR_OMDB_API_KEY"
  }
}

Option B: User Secrets (safe for local dev)
dotnet user-secrets init
dotnet user-secrets set "OMDb:ApiKey" "YOUR_OMDB_API_KEY"

Option C: Environment Variable
setx OMDb__ApiKey "YOUR_OMDB_API_KEY"

4️⃣ Run
dotnet run --project MovieService.Api


API available at:
👉 https://localhost:5001/api/movies/{title}

📡 Example

Request

GET /api/movies/Guardians%20of%20the%20Galaxy%20Vol.%202


Response

{
  "id": "tt3896198",
  "title": "Guardians of the Galaxy Vol. 2",
  "year": "2017",
  "plot": "The Guardians struggle to keep together as a team...",
  "poster": "https://m.media-amazon.com/images/M/...jpg"
}

🛠️ Tech Stack

.NET 9

ASP.NET Core Web API

Microservices architecture

OMDb API integration

Dependency Injection & configuration binding

Secure secrets management

🗺️ Roadmap

 Add AuthService for secure login

 Add FavoritesService with database support

 Add RecommendationService (ML-based or rules)

 Add API Gateway for unified access

 Add Swagger/OpenAPI docs per service

 Add Docker support for deployment

🤝 Contributing

Fork the repo

Create a feature branch

Commit changes

Push to your fork

Open a PR

📄 License

This project is licensed under the MIT License.
