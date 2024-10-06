
# Leaderboard API Project

## Overview

This project implements a **high-performance leaderboard system** for a real-time mobile game using .NET, Redis, and PostgreSQL. The API processes match scores and maintains a ranked list of players with data consistency. In cases where scores are tied, the ranking is determined based on the player's registration date, level, or trophy count.

## Features

1. **Player Registration and Login**
   - A registration endpoint that accepts a username, password, and device ID.
   - A login endpoint that accepts a username and password, returning an access token.

2. **Match Result Submission**
   - Players can submit their match scores via a dedicated endpoint.
   - The scores are processed and stored in both PostgreSQL (for persistence) and Redis (for frequently accessed data like top 100 rankings).

3. **Real-Time Leaderboard**
   - A real-time leaderboard that ranks players based on their total score.
   - Handles ranking ties using multiple criteria: registration date, player level, and trophy count.
   - Redis is used for fast access to leaderboard data, while PostgreSQL ensures data consistency.

4. **Scalability and Performance**
   - **Indexing** is applied on critical fields like `PlayerId`, `MatchScore`, `RegistrationDate`, `PlayerLevel`, and `TrophyCount` to improve query performance.
   - **Retry mechanisms** and **transaction management** are implemented to ensure data consistency in case of network or database failures.
   - Redis and PostgreSQL are used together to balance performance and data persistence.

5. **Error Handling and Data Consistency**
   - Detailed error handling ensures that data is not lost during score updates.
   - Redis cache is updated with retries in case of network issues, and PostgreSQL transactions prevent data inconsistency.

6. **Security**
   - JWT-based authentication is implemented for all secure endpoints.
   - API calls are protected, ensuring that only authenticated users can submit scores.

7. **Logging and Monitoring**
   - Comprehensive logging is implemented across all critical actions, including score submissions, Redis cache updates, and transaction management.
   - This ensures traceability and helps in debugging and maintaining system integrity.

## Quick Examples

### 1. Register a new player
```
POST /api/auth/register
{
    "username": "player1",
    "password": "mysecurepassword",
    "deviceId": "device12345"
}
```

### 2. Login a player
```
POST /api/auth/login
{
    "username": "player1",
    "password": "mysecurepassword"
}
```

### 3. Submit a match score
```
POST /api/game/submit-score
Authorization: Bearer {login_access_token}
{
    "matchScore": 150
}
```

### 4. Get leaderboard
```
GET /api/game/leaderboard
No authorization required
```

## Setup Instructions

1. **Clone the repository**:
   ```bash
   git clone <repository-url>
   ```

2. **Configure the database and Redis connection strings** in `appsettings.json`:
   - PostgreSQL connection string: `"DefaultConnection"`
   - Redis connection string: `"RedisConnection"`

3. **Run database migrations** to set up the database schema:
   ```bash
   dotnet ef database update
   ```

4. **Run the project**:
   ```bash
   dotnet run
   ```

## Technologies Used

- **.NET** for the backend API.
- **PostgreSQL** for persisting scores and player information.
- **Redis** for caching frequently accessed leaderboard data.
- **JWT** for secure authentication.
- **Entity Framework Core** for database access.
- **StackExchange.Redis** for Redis integration.

## Performance Improvements

- **PostgreSQL Indexing**: Key fields such as `PlayerId`, `MatchScore`, `RegistrationDate`, `PlayerLevel`, and `TrophyCount` are indexed to optimize query performance.
- **Batch Processing**: Scores are processed in transactions to ensure atomic operations and prevent partial failures.
- **Redis Cache**: Redis is used to store the leaderboard and frequently accessed data, improving response times and reducing database load.

## Error Handling and Retry Mechanisms

- **Transaction Management**: Database transactions ensure that score submissions are atomic, and failures roll back any partial changes.
- **Redis Retry Mechanism**: In the case of Redis failure, the system retries cache updates a few times before failing.

## Scripts for Testing the Leaderboard
The following scripts can be used to test the functionality of the leaderboard by creating users and submitting random scores for each user.

### User registration script

```bash
for i in {1..120}
do
  curl -X POST "https://localhost:7105/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
        "Username": "user_'$i'",
        "Password": "password_'$i'",
        "DeviceId": "device_'$i'"
      }'
done
```
### Random score submission script
```bash
for i in {1..120}
do
  response=$(curl -X POST "https://localhost:7105/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
        "Username": "user_'$i'",
        "Password": "password_'$i'"
      }')

  echo "Login response: $response"

  token=$(echo $response | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')

  if [ -z "$token" ]; then
    echo "Error: Token is empty or invalid. Response: $response"
    continue
  else
    echo "Token received: $token"
  fi

  random_score=$((RANDOM % 500 + 50))

  curl -X POST "https://localhost:7105/api/game/submit-score" \
  -H "Authorization: Bearer $token" \
  -H "Content-Type: application/json" \
  -d '{
        "MatchScore": '$random_score'
      }'
done
   ```





## License

This project is licensed under the MIT License.
