# 🚀 Dropcore – Backend

Dropcore is a distributed file cloud application that allows users to register accounts and manage their private files – including uploading, viewing, downloading, and deleting them. The backend is built using a microservices architecture with Docker and .NET 9.

---

## 🧱 Microservices Architecture

The backend is composed of the following microservices:

| Service           | Description                                                       |
|-------------------|-------------------------------------------------------------------|
| **AuthService**   | Handles user registration, login, and authentication.             |
| **UserService**   | Manages user profile data.                                        |
| **FileService**   | Handles metadata of uploaded files.                               |
| **StorageNode**   | Stores the actual file content on disk.                           |
| **ViewerService** | Enables file preview (e.g., images, PDFs).                        |
| **Gateway**       | API Gateway that routes requests to the appropriate microservice. |

Each service is containerized and runs independently via Docker Compose.

---

## 🛠️ Technologies

- **.NET 9 / ASP.NET Core**
- **Entity Framework Core**
- **PostgreSQL** (separate DB instance per service)
- **Docker & Docker Compose**
- **Swagger** – API documentation
- **PgAdmin** – Database GUI

---

## ▶️ How to Run

### Prerequisites

- Docker
- Docker Compose

### Start the entire backend:

```bash
docker compose up --build
```

### After launch, the services will be available at:

- AuthService → http://localhost:5001
- UserService → http://localhost:5004
- FileService → http://localhost:5002
- StorageNode → http://localhost:5003
- ViewerService → http://localhost:5005
- Gateway → http://localhost:8000
- PgAdmin → http://localhost:8080 (login: `admin@admin.com`, password: `admin`)

### API Documentation (Swagger)

Each service exposes Swagger UI under:

```
http://localhost:<PORT>/swagger
```

Example: [http://localhost:5001/swagger](http://localhost:5001/swagger)

---

## 📁 Project Structure

```
/services
│
├── AuthService         # Authentication logic
├── UserService         # User data management
├── FileService         # File metadata handling
├── StorageNode         # File storage (local filesystem)
├── ViewerService       # File preview rendering
├── Gateway             # API Gateway (request routing)
```

Each microservice contains its own `Dockerfile` and optionally `Dockerfile.tools` for development tasks.

---

## 🧪 Testing

Each core service includes a corresponding `*.Tests` project for unit testing.

Example:

```
/services
├── AuthService
├── AuthService.Tests
```

Tests can be executed locally or using the `Dockerfile.tools` helper.

---

## 👤 Author

Developed as a semester project for the **Information Systems Management** course.

GitHub: [github.com/Fablek/dropcore-backend](https://github.com/Fablek/dropcore-backend)
