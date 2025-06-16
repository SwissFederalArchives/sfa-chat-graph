# sfa-chat-graph

`sfa-chat-graph` is a retrieval-augmented generation system for RDF knowledge graphs that translates natural-language queries into SPARQL and executes them against the graph. It features a C#/.NET backend with a document-oriented database, REST and WebSocket endpoints for chat operations and real-time status updates, and an embedded secure Jupyter Notebook service for on-the-fly Python execution alongside an LLM API. The single-page frontend provides a conversational chat panel and an interactive force-directed graph visualization of each answer’s subgraph, complete with fragment downloads and detailed views.

It was built for the Swiss Federal Archives (SFA) by the University of Applied Sciences of the Grisons/Fachhochschule Graubünden (FHGR) in the context of the "SIARD zu Linked Data" project (2024-2025).

# Configuration
Configuration of the System can be adjusted in the `appsettings.json` file, located in the `sfa-chat-graph.Server` folder.
The following Settings can be made.


| Path | Datatype | Default | Description |
|---|---|---|---|
| ConnectionStrings.Mongo | string | `mongodb://mongodb:27017/sfa-chat-graph` | Which Mongodb instance and database should be used by the backend |
| ConnectionStrings.Sparql | string | `https://lindas.admin.ch/query` | A stardog compatible endpoint for query access. Supports every Stardog database, no matter the contained graph |
| ConnectionStrings.Redis | string | `localhost:6379` | A optional redis database if caching should be done in redis and not in memory of the backend. If redis is added via docker this has to be adjusted to the containers hostname |
| Cache.Implementation | Enum `CacheImplementation` | `InMemory` | Which kind of caching should be used |
| Cache.DefaultExpiration | TimeSpan | `00:30:00` | How long chat-history should be retained in cache. Gets reset each time the user continues chatting |
| JupyterOptions.Endpoint | string | `http://jupyter:8888` | Which jupyter notebook server should be used for code execution |
| JupyterOptions.Token | string | - | Optional authentication token for the jupyter notebook server |
| JupyterOptions.SetupScript | string | `pip install numpy pandas statsmodels matplotlib seaborn shapely geopandas[all]` | bash script which should be run the first time the backend tries to execute code since the last restart |
| JupyterOptions.Kernel | string | `python3` | Which kernel should be used for code execution. If not specified the default kernel will be used. Should be any kernel which can execute python code |
| AiConfig.Implementation | string | Enum `AiImplementation` | `OpenAI` |Which AiService should be used for chat inference |
| AiConfig.Model | string | `gpt-4.1` | Which model should be used for inference. **Important**: The model needs to support tool calling. |
| AiConfig.ApiKey | string | - | API-Key for the AI Service |
| Aiconfig.ApiUrl | string | `https://api.openai.com/v1/chat/completions` | Endpoint which should be used for interaction with the chat inference api |


## CacheImplementation
| Value | Description |
|---|---|
| InMemory | Caching should happen in memory of the application itself |
| Redis | Caching should happen in the supplied redis database |
| None | Caching should be disabled. Will lower memory usage in the cost of reponse time of the application |

## AiImplementation
| Value | Description |
|---|---|
| OpenAI | Supports Openai compatible inference endpoints. Uses OpenAI per default |

# Deployment

The repo features a docker-compose.yml for easy setup. In the default configuration four containers are created. The outside ports can be adjusted to make accessible on other ports. If Express is not needed or wanted it can be disabled by commenting out the service in the compose file. The exposed port of the Backend is HTTP. If HTTPS is wanted using a reverse proxy like nginx is highly suggested. If a reverse proxy is not feasible Port 8081 of the backend can be exposed instead. 

| Container | Image | Required | Ports | Description |
|---|---|---|---|---|
| backend | `sfa-chat-graph.Server/Dockerfile` | yes | 1221:8080 | The backend of the system which also hosts the frontend website |
| jupyter | `quay.io/jupyter/scipy-notebook` | yes | - | The jupyter notebook server. It is used by the backend to execute python code in a secure sandbox. **IMPORTANT**: In Production the container should be configured in a way that it does not have outside internet access. This can be achieved by adding the container to the `no-internet` network. And using the image in `jupyter/Dockerfile` instead to install packages |
| mongo | `mongo` | yes | - | The Database the backend uses to store it's chat history. If gets to big, can be deleted, but will result in loss of all chat history |
| express | `mongo-express` | no | 1222:8081 | A webinterface to access the mongo db directly. Should only be enabled in dev environment |

## Environment Variables
The following environment variables can be bassed into the docker compose process.

| Variable | Description |
|---|---|
| OPENAI_KEY | API-Key the backend should use to access OpenAI Services |
| EXPRESS_PW | The password for the mongo-express Webinterface |
